using System;
using System.Collections.Generic;
using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Entities {

    /*
        todo:
        cleanup/rewrite. like this works mostly kinda not rlly but theres probably stuff i can make cleaner and easier to add onto later
        remove/improve/replace the stupid flashing effect to make it less Bad
        add custom pathing (preferably in a way that isnt extremely jank)
        More(tm)

        idk this is mostly just a proof of concept rn theres still like a lot either just done weirdly i think or not even implemented asdkfjaksdf
    */

    [CustomEntity("SorbetHelper/ExclamationBlock")]
    public class ExclamationBlock : Solid {
        public enum States {
            Idle,
            ReadyToExtend,
            Extending
        }

        public States activationState;
        private int amountExtended;
        private float activeTimer;

        private MTexture[,] nineSlice;
        private MTexture exclamationMarkTexture;
        private Vector2 scale = Vector2.One;
        private Vector2 hitOffset;

        private float activationBuffer;
        public bool ShouldActivate {
            get {
                bool value = activationBuffer > 0f;
                activationBuffer = 0f;
                return value;
            }
            set {
                activationBuffer = value ? 0.125f : 0f;
            }
        }
        public bool Extended => amountExtended > 0;

        private readonly EmptyBlock[] segments;
        private readonly Vector2[] targets;
        private readonly int segmentCount;

        private readonly float moveSpeed;
        private readonly bool autoExtend;
        private readonly float activeTime;

        public ExclamationBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
            activationState = States.Idle;

            moveSpeed = data.Float("moveSpeed", 128f);
            autoExtend = data.Bool("autoExtend", false);
            activeTime = data.Float("activeTime", 3f);

            nineSlice = new MTexture[3, 3];
            MTexture texture = GFX.Game["objects/SorbetHelper/exclamationBlock/activeBlock"];
            exclamationMarkTexture = GFX.Game["objects/SorbetHelper/exclamationBlock/exclamationMark"];

            for (int x = 0; x < 3; x++) {
                for (int y = 0; y < 3; y++) {
                    nineSlice[x, y] = texture.GetSubtexture(x * 8, y * 8, 8, 8);
                }
            }

            segments = [null, new(Position, (int)Width, (int)Height), new(Position, (int)Width, (int)Height)];
            targets = [Position, Position + new Vector2(Width, 0f), Position + new Vector2(Width * 2f, 0f)];
            segmentCount = segments.Length - 1;

            OnDashCollide = OnDashCollision;
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 direction) {
            Hit();

            return DashCollisionResults.Rebound;
        }

        public override void Added(Scene scene) {
            base.Added(scene);

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
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 15f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 15f);

            activationBuffer -= Engine.DeltaTime;

            switch (activationState) {
                case States.Idle:
                    if (Extended) {
                        if (activeTimer > 0f) {
                            activeTimer -= Engine.DeltaTime;

                            if (activeTimer <= 1f && Scene.OnInterval(1f / 3f)) {
                                Blink();
                            }
                        } else {
                            Reset();
                            break;
                        }
                    }

                    if (ShouldActivate) {
                        activationState = States.ReadyToExtend;
                    }

                    break;
                case States.ReadyToExtend:
                    activeTimer = activeTime;

                    if (amountExtended >= segmentCount) {
                        activationState = States.Idle;
                        break;
                    }

                    EmptyBlock block = segments[amountExtended + 1];
                    block.Position = targets[amountExtended];
                    block.Visible = block.Collidable = true;

                    activationState = States.Extending;

                    // immediately start extending instead of waiting until the next frame
                    goto case States.Extending;
                case States.Extending:
                    EmptyBlock block1 = segments[amountExtended + 1];
                    Vector2 target = targets[amountExtended + 1];

                    block1.MoveTowardsX(target.X, moveSpeed * Engine.DeltaTime);
                    block1.MoveTowardsY(target.Y, moveSpeed * Engine.DeltaTime);

                    // reached target, increase amount extended and either return to idle or extend again
                    if (block1.Position == target) {
                        amountExtended++;
                        activationState = autoExtend ? States.ReadyToExtend : States.Idle;
                        break;
                    }

                    break;
            }
        }

        public override void Render() {
            base.Render();

            renderNineSlice(Position + hitOffset, nineSlice, (int)Width / 8, (int)Height / 8, scale);
            exclamationMarkTexture.Draw(Position + new Vector2((int)Width / 2 - 4, (int)Height / 2 - 4));
        }

        public bool Hit() {
            for (int i = 2; i <= base.Width; i += 4) {
                if (!base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f))) {
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 1, new Vector2(base.X + i, base.Bottom), Vector2.One * 4f);
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 1, new Vector2(base.X + i, base.Bottom), Vector2.One * 4f);
                }
            }

            ShouldActivate = true;
            Bounce();

            return true;
        }

        private void Reset() {
            foreach (EmptyBlock block in segments) {
                if (block is null)
                    continue;
                block.Visible = block.Collidable = false;
                block.Position = targets[0];
            }

            amountExtended = 0;
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
            scale = new Vector2(0.8f, 0.8f);
            hitOffset = Vector2.UnitY * -3f;
            foreach (EmptyBlock block in segments) {
                if (block is null)
                    continue;
                block.Bounce();
            }
        }

        protected internal static void renderNineSlice(Vector2 position, MTexture[,] nineSlice, int width, int height, Vector2 scale, float alpha = 1f) {
            Vector2 center = position + new Vector2(width * 8f / 2f, height * 8f / 2f);

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    int textureX = x < width - 1 ? Math.Min(x, 1) : 2;
                    int textureY = y < height - 1 ? Math.Min(y, 1) : 2;
                    Vector2 tilePosition = position + new Vector2(x * 8, y * 8) + new Vector2(4, 4);
                    tilePosition = center + ((tilePosition - center) * scale);

                    nineSlice[textureX, textureY].DrawCentered(tilePosition, Color.White * alpha);
                }
            }
        }
    }

    public class EmptyBlock : Solid {

        private MTexture[,] nineSlice;
        private MTexture[,] flashNineSlice;
        private Vector2 scale = Vector2.One;
        private Vector2 hitOffset;

        private float flashOpacity;

        public EmptyBlock(Vector2 position, int width, int height) : base(position, width, height, false) {
            base.Depth = -8999;

            nineSlice = new MTexture[3, 3];
            flashNineSlice = new MTexture[3, 3];
            MTexture texture = GFX.Game["objects/SorbetHelper/exclamationBlock/emptyBlock"];
            MTexture flashTexture = GFX.Game["objects/SorbetHelper/exclamationBlock/flash"];

            for (int x = 0; x < 3; x++) {
                for (int y = 0; y < 3; y++) {
                    nineSlice[x, y] = texture.GetSubtexture(x * 8, y * 8, 8, 8);
                }
            }

            for (int x = 0; x < 3; x++) {
                for (int y = 0; y < 3; y++) {
                    flashNineSlice[x, y] = flashTexture.GetSubtexture(x * 8, y * 8, 8, 8);
                }
            }
        }

        public override void Update() {
            base.Update();

            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 0.7f * 4f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 0.8f * 4f);
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 6f * 4f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 6f * 4f);

            if (flashOpacity > 0f) {
                flashOpacity -= Engine.DeltaTime * 6f;
            }
        }

        public override void Render() {
            base.Render();

            ExclamationBlock.renderNineSlice(Position + hitOffset, nineSlice, (int)Width / 8, (int)Height / 8, scale);

            if (flashOpacity > 0f) {
                ExclamationBlock.renderNineSlice(Position + hitOffset, flashNineSlice, (int)Width / 8, (int)Height / 8, scale, flashOpacity);
            }
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
