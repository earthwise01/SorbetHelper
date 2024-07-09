using System;
using System.Collections.Generic;
using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using System.Linq;
using System.Collections;
using FMOD;
using FMOD.Studio;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/ExclamationBlock")]
    public class ExclamationBlock : Solid {
        private float activationBuffer;
        private int amountExtended;
        private int targetExtended;
        private float activeTimer;
        private Vector2 scale = Vector2.One;
        private Vector2 hitOffset;

        private const float activationBufferTime = 0.125f;
        public void Extend() => activationBuffer = activationBufferTime;
        private bool CanActivate => targetExtended < segmentCount || canRefreshTimer;
        private bool NeedsToExtend => amountExtended < targetExtended;

        private readonly EmptyBlock[] segments;
        private readonly Vector2[] targets;
        private readonly int segmentCount;
        private readonly MTexture[,] activeNineSlice;
        private readonly MTexture[,] emptyNineSlice;
        private readonly MTexture exclamationMarkTexture, emptyExclamationMarkTexture;
        private readonly SoundSource extendingSound;

        private readonly float moveSpeed;
        private readonly bool autoExtend;
        private readonly float activeTime;
        private readonly bool canRefreshTimer;

        public static ParticleType P_SmashDust { get; private set; }

        public ExclamationBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
            moveSpeed = data.Float("moveSpeed", 128f);
            autoExtend = data.Bool("autoExtend", false);
            activeTime = data.Float("activeTime", 3f);
            canRefreshTimer = data.Bool("canRefreshTimer", false);

            activeNineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/activeBlock"], 8, 8);
            emptyNineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/emptyBlock"], 8, 8);
            exclamationMarkTexture = GFX.Game["objects/SorbetHelper/exclamationBlock/exclamationMark"];
            emptyExclamationMarkTexture = GFX.Game["objects/SorbetHelper/exclamationBlock/emptyExclamationMark"];
            SurfaceSoundIndex = SurfaceIndex.Girder;

            // i need to make this like. actually customizable and not hardcoded lmao
            segments = [null, new(Position, (int)Width, (int)Height), new(Position, (int)Width, (int)Height), new(Position, (int)Width, (int)Height)];
            targets = [Position, Position + new Vector2(Width, 0f), Position + new Vector2(Width * 2f, 0f), Position + new Vector2(Width * 2f, -Height)];
            segmentCount = segments.Length - 1;

            OnDashCollide = OnDashCollision;
            Add(new Coroutine(Sequence()));
            Add(new Coroutine(BlinkRoutine()));
            Add(extendingSound = new SoundSource());
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            // don't activate the block if it's in empty block mode
            if (!CanActivate)
                return DashCollisionResults.NormalCollision;

            // gravity helper support
            bool gravityInverted = GravityHelperImports.IsPlayerInverted?.Invoke() ?? false;
            // make wallbouncing easier
            if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f))
                return DashCollisionResults.NormalCollision;

            // activate the block
            Hit(dir);

            return DashCollisionResults.Rebound;
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            foreach (EmptyBlock block in segments) {
                if (block is null)
                    continue;

                Scene.Add(block);
                block.Active = block.Visible = block.Collidable = false;
            }
        }

        public override void Update() {
            base.Update();

            // ease scale and hitOffset towards their default values
            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 2f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 2f);
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 14f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 14f);

            if (activationBuffer > 0f)
                activationBuffer -= Engine.DeltaTime;
        }

        public IEnumerator Sequence() {
            while (true) {
                // check if the block should extend/update timer
                if (activationBuffer > 0f) {
                    activationBuffer = 0f;

                    if (amountExtended == 0 || canRefreshTimer)
                        activeTimer = activeTime;

                    targetExtended = autoExtend ? segmentCount : targetExtended + 1;

                    if (NeedsToExtend)
                        extendingSound.Play("event:/sorbethelper/sfx/exclamationblock_extending");
                }

                // extending
                while (NeedsToExtend) {
                    int extendingIndex = amountExtended + 1;

                    // cancel extension if it exceeds the amount of segments
                    if (extendingIndex > segmentCount) {
                        targetExtended = segmentCount;
                        break;
                    }

                    EmptyBlock block = segments[extendingIndex];
                    block.Position = targets[amountExtended];
                    block.Active = block.Visible = block.Collidable = true;

                    Vector2 target = targets[extendingIndex];
                    while (!block.MoveTowards(target, moveSpeed * Engine.DeltaTime)) {
                        yield return null;
                    }

                    // finished extending
                    amountExtended = extendingIndex;
                    Audio.Play("event:/sorbethelper/sfx/exclamationblock_extended", block.Center, "index", Math.Clamp(12 * (amountExtended - 1) / Math.Max(segmentCount - 1, 1), 0, 12));

                    // reached target
                    if (!NeedsToExtend)
                        extendingSound.Param("end", 1f);

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

            if (CanActivate) {
                Utils.RenderNineSlice(Position + hitOffset, activeNineSlice, (int)Width / 8, (int)Height / 8, scale);
                exclamationMarkTexture.DrawCentered(Position + new Vector2((int)Width / 2, (int)Height / 2) + hitOffset * scale, Color.White, scale);
            } else {
                Utils.RenderNineSlice(Position + hitOffset, emptyNineSlice, (int)Width / 8, (int)Height / 8, scale);
                emptyExclamationMarkTexture.DrawCentered(Position + new Vector2((int)Width / 2, (int)Height / 2) + hitOffset * scale, Color.White, scale);
            }
        }

        public bool Hit(Vector2 dir) {
            SmashParticles(dir.Perpendicular());
            SmashParticles(-dir.Perpendicular());
            Bounce();
            Audio.Play("event:/sorbethelper/sfx/exclamationblock_hit", Center);

            Extend();

            return true;
        }

        public void Break() {
            foreach (EmptyBlock block in segments) {
                if (block is null || !block.Visible)
                    continue;

                block.Break();
                block.Position = targets[0];
            }

            Audio.Play("event:/sorbethelper/sfx/exclamationblock_break", Position);

            amountExtended = 0;
            targetExtended = 0;
            activationBuffer = 0f;
            activeTimer = 0f;
        }

        private void Blink() {
            foreach (EmptyBlock block in segments) {
                if (block is null || !block.Visible)
                    continue;

                block.Blink();
            }

            Audio.Play("event:/sorbethelper/sfx/exclamationblock_blink", base.Center);
        }

        private void Bounce() {
            scale = new Vector2(0.75f, 0.75f);
            hitOffset = new Vector2(0f, -4f);

            foreach (EmptyBlock block in segments) {
                if (block is null || !block.Visible)
                    continue;

                block.Bounce();
            }
        }

        // i love stealing vanilla code !!
        private void SmashParticles(Vector2 dir) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX) {
                direction = 0f;
                position = CenterRight - Vector2.UnitX * 2f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            } else if (dir == -Vector2.UnitX) {
                direction = MathF.PI;
                position = CenterLeft + Vector2.UnitX * 2f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            } else if (dir == Vector2.UnitY) {
                direction = MathF.PI / 2f;
                position = BottomCenter - Vector2.UnitY * 2f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            } else {
                direction = -MathF.PI / 2f;
                position = TopCenter + Vector2.UnitY * 2f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            }
            num = (num + 2) / 2;
            SceneAs<Level>().ParticlesFG.Emit(P_SmashDust, num, position, positionRange, direction, MathF.PI / 8f);
        }

        internal static void Intitialize() {
            P_SmashDust = new ParticleType(Player.P_SummitLandB) {
                SpeedMin = 50f,
                SpeedMax = 90f,
                SpeedMultiplier = 0.1f,
                Color = Calc.HexToColor("ffd12e") * 0.75f,
                Color2 = Calc.HexToColor("fffe9c") * 0.5f,
                ColorMode = ParticleType.ColorModes.Fade
            };
        }

        public class EmptyBlock : Solid {
            private readonly MTexture[,] nineSlice;
            private readonly MTexture[,] flashNineSlice;

            private Vector2 scale = Vector2.One;
            private Vector2 hitOffset;
            private float flashOpacity;
            private Vector2 floatPos;

            public EmptyBlock(Vector2 position, int width, int height) : base(position, width, height, false) {
                base.Depth = -8999;

                nineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/emptyBlock"], 8, 8);
                flashNineSlice = Utils.CreateNineSlice(GFX.Game["objects/SorbetHelper/exclamationBlock/flash"], 8, 8);
                SurfaceSoundIndex = SurfaceIndex.Girder;
            }

            public override void Update() {
                base.Update();

                // deltatime is multiplied with the values set in Bounce() so that they all take the same time to reset to normal
                scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 0.7f * 4f);
                scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 0.8f * 4f);
                hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 0f * 4f);
                hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 6f * 4f);

                if (flashOpacity > 0f) {
                    flashOpacity -= Engine.DeltaTime * 6f;
                }
            }

            public override void Render() {
                base.Render();

                Utils.RenderNineSlice(Position + hitOffset, nineSlice, flashNineSlice, flashOpacity, (int)Width / 8, (int)Height / 8, scale);
            }

            public bool MoveTowards(Vector2 target, float amount) {
                Vector2 pos = Position + floatPos;

                pos = Calc.Approach(pos, target, amount);

                // need to store the decimal value bc otherwise it breaks at slow speeds since MoveTo rounds when moving
                floatPos.X = pos.X - MathF.Round(pos.X);
                floatPos.Y = pos.Y - MathF.Round(pos.Y);

                MoveTo(pos);
                return Position == target;
            }

            public void Break() {
                SceneAs<Level>().Particles.Emit(Player.P_SummitLandB, (int)(Width / 8) * (int)(Height / 8) / 5 * 2, Center, new Vector2(Width / 2, Height / 2), Color.White * 0.75f, MathF.PI / 2f, MathF.PI / 6f);

                Active = Visible = Collidable = false;
                flashOpacity = 0f;
                scale = Vector2.One;
                hitOffset = Vector2.Zero;
            }

            public void Blink() {
                flashOpacity = 1f;
            }

            public void Bounce() {
                scale = new Vector2(0.7f, 0.8f);
                hitOffset = new Vector2(0f, -6f);
            }
        }
    }
}
