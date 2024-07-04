using System;
using System.Collections.Generic;
using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using System.Linq;
using System.Collections;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/ExclamationBlock")]
    public class ExclamationBlock : Solid {

        // variables for tracking the current state of the block
        private float activationBuffer;
        private int amountExtended;
        private int targetExtended;
        private float activeTimer;
        private Vector2 scale = Vector2.One;
        private Vector2 hitOffset;

        // properties
        public bool Extend {
            get {
                bool value = activationBuffer > 0f;
                activationBuffer = 0f;
                return value;
            }
            set {
                activationBuffer = value ? 0.125f : 0f;
            }
        }
        // true when fully extended and timer refresh is off
        private bool IsEmptyBlock => targetExtended >= segmentCount && !canRefreshTimer;

        // the thing which doesnt change technically but useful idk words moment im tired
        private readonly EmptyBlock[] segments;
        private readonly Vector2[] targets;
        private readonly int segmentCount;
        private readonly MTexture[,] activeNineSlice;
        private readonly MTexture[,] emptyNineSlice;
        private readonly MTexture exclamationMarkTexture, emptyExclamationMarkTexture;

        // editor facing settings
        private readonly float moveSpeed;
        private readonly bool autoExtend;
        private readonly float activeTime;
        private readonly bool canRefreshTimer;

        public ExclamationBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
            moveSpeed = data.Float("moveSpeed", 128f);
            autoExtend = data.Bool("autoExtend", false);
            activeTime = data.Float("activeTime", 3f);
            canRefreshTimer = data.Bool("canRefreshTimer", false);

            activeNineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/activeBlock"], 8, 8);
            emptyNineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/emptyBlock"], 8, 8);
            exclamationMarkTexture = GFX.Game["objects/SorbetHelper/exclamationBlock/exclamationMark"];
            emptyExclamationMarkTexture = GFX.Game["objects/SorbetHelper/exclamationBlock/emptyExclamationMark"];

            // i need to make this like. actually customizable and not hardcoded lmao
            segments = [null, new(Position, (int)Width, (int)Height), new(Position, (int)Width, (int)Height), new(Position, (int)Width, (int)Height)];
            targets = [Position, Position + new Vector2(Width, 0f), Position + new Vector2(Width * 2f, 0f), Position + new Vector2(Width * 2f, -Height)];
            segmentCount = segments.Length - 1;

            OnDashCollide = OnDashCollision;
            Add(new Coroutine(Sequence()));
            Add(new Coroutine(BlinkRoutine()));
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            // don't activate the block if it's in empty block mode
            if (IsEmptyBlock)
                return DashCollisionResults.NormalCollision;

            // gravity helper support
            bool gravityInverted = GravityHelperImports.IsPlayerInverted?.Invoke() ?? false;
            // make wallbouncing easier
            if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f))
                return DashCollisionResults.NormalCollision;

            // activate the block
            Hit();

            return DashCollisionResults.Rebound;
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            // add blocks in reverse order for correct depth when rendered
            for (int i = segments.Length - 1; i > 0; i--) {
                EmptyBlock block = segments[i];
                if (block is null)
                    continue;

                Scene.Add(block);
                block.Visible = block.Collidable = false;
            }
        }

        public override void Update() {
            base.Update();

            // ease scale and hitOffset towards their default values
            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 2f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 2f);
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 14f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 14f);

            activationBuffer -= Engine.DeltaTime;
        }

        public IEnumerator Sequence() {
            while (true) {
                // check if the block should extend/update timer
                if (Extend) {
                    if (amountExtended == 0 || canRefreshTimer)
                        activeTimer = activeTime;

                    targetExtended = autoExtend ? segmentCount : targetExtended + 1;
                }

                // extending
                while (amountExtended < targetExtended) {
                    int extendingIndex = amountExtended + 1;

                    // cancel extension if it exceeds the amount of segments
                    if (extendingIndex > segmentCount) {
                        targetExtended = segmentCount;
                        break;
                    }

                    EmptyBlock block = segments[extendingIndex];
                    block.Position = targets[amountExtended];
                    block.Visible = block.Collidable = true;

                    Vector2 target = targets[extendingIndex];
                    while (!MoveTowards(block, target, moveSpeed * Engine.DeltaTime)) {
                        yield return null;
                    }

                    // reached target
                    amountExtended += 1;

                    yield return null;
                }

                // idle
                if (amountExtended > 0) {
                    if (activeTimer > 0f) {
                        activeTimer -= Engine.DeltaTime;
                    } else {
                        Break();
                    }
                }

                yield return null;
            }
        }

        public IEnumerator BlinkRoutine() {
            // literally just updates the blink effect before the block disappears
            while (true) {
                while (activeTimer > 0f && activeTimer < 2f) {
                    Blink();
                    yield return 0.5f;
                }
                yield return null;
            }
        }

        public override void Render() {
            base.Render();

            if (targetExtended < segmentCount || canRefreshTimer) {
                Utils.RenderNineSlice(Position + hitOffset, activeNineSlice, (int)Width / 8, (int)Height / 8, scale);
                exclamationMarkTexture.DrawCentered(Position + new Vector2((int)Width / 2, (int)Height / 2) + hitOffset * scale, Color.White, scale);
            } else {
                Utils.RenderNineSlice(Position + hitOffset, emptyNineSlice, (int)Width / 8, (int)Height / 8, scale);
                emptyExclamationMarkTexture.DrawCentered(Position + new Vector2((int)Width / 2, (int)Height / 2) + hitOffset * scale, Color.White, scale);
            }
        }

        public bool Hit() {
            for (int i = 2; i <= base.Width; i += 4) {
                if (!base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f))) {
                    //SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 1, new Vector2(base.X + i, base.Bottom), Vector2.One * 4f);
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 1, new Vector2(base.X + i, base.Bottom), Vector2.One * 4f);
                }
            }

            Extend = true;
            Bounce();

            return true;
        }

        public void Break() {
            foreach (EmptyBlock block in segments) {
                if (block is null)
                    continue;

                // spawn dust particles
                for (int x = 0; x < block.Width / 8; x++) {
                    for (int y = 0; y < block.Height / 8; y++) {
                        // only have a 1/4th chance of actually creating a particle
                        if (Calc.Random.NextSingle() >= 0.25f)
                            continue;

                        float direction = Calc.Random.NextFloat(MathF.PI / 2f) + MathF.PI / 4f;
                        SceneAs<Level>().Particles.Emit(Player.P_SummitLandB, 1, block.Position + new Vector2(x * 8, y * 8) + Vector2.One * 4f, Vector2.One * 2f, Color.White * 0.75f, direction);
                    }
                }

                block.Visible = block.Collidable = false;
                block.Position = targets[0];
            }

            amountExtended = 0;
            targetExtended = 0;
            activationBuffer = 0f;
            activeTimer = 0f;
        }

        private void Blink() {
            foreach (EmptyBlock block in segments) {
                if (block is null)
                    continue;
                block.Blink();
            }
        }

        private void Bounce() {
            scale = new Vector2(0.75f, 0.75f);
            hitOffset = new Vector2(0f, -4f);

            foreach (EmptyBlock block in segments) {
                if (block is null)
                    continue;
                block.Bounce();
            }
        }

        private static bool MoveTowards(Platform self, Vector2 target, float amount) {
            Vector2 pos = Calc.Approach(self.Position, target, amount);
            self.MoveTo(pos);
            return self.Position == target;
        }

        public class EmptyBlock : Solid {
            private readonly MTexture[,] nineSlice;
            private readonly MTexture[,] flashNineSlice;

            private Vector2 scale = Vector2.One;
            private Vector2 hitOffset;
            private float flashOpacity;

            private const float XBounceScale = 0.7f;
            private const float YBounceScale = 0.8f;
            private const float XBounceOffset = 0f;
            private const float YBounceOffset = -6f;

            public EmptyBlock(Vector2 position, int width, int height) : base(position, width, height, false) {
                base.Depth = -8999;

                nineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/emptyBlock"], 8, 8);
                flashNineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/flash"], 8, 8);
            }

            public override void Update() {
                base.Update();

                scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * Math.Abs(XBounceScale) * 4f);
                scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * Math.Abs(YBounceScale) * 4f);
                hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * Math.Abs(XBounceOffset) * 4f);
                hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * Math.Abs(YBounceOffset) * 4f);

                if (flashOpacity > 0f) {
                    flashOpacity -= Engine.DeltaTime * 6f;
                }
            }

            public override void Render() {
                base.Render();

                Utils.RenderNineSlice(Position + hitOffset, nineSlice, flashNineSlice, flashOpacity, (int)Width / 8, (int)Height / 8, scale);
            }

            public void Blink() {
                flashOpacity = 1f;
            }

            public void Bounce() {
                scale = new Vector2(XBounceScale, YBounceScale);
                hitOffset = new Vector2(XBounceOffset, YBounceOffset);
            }
        }
    }
}
