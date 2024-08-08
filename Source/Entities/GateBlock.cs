using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using System.Runtime.InteropServices;

namespace Celeste.Mod.SorbetHelper.Entities {

    // adaptable switch gate base thing *HEAVILY* based on and inspired by flag switch gates from maddie's helping hand
    // code is probably a bit different now due to me changing a bunch to make it more consistent with the other stuff in this mod/my current coding style and to help myself understand it more, but ultimately it is copied from there
    // https://github.com/maddie480/MaddieHelpingHand/blob/master/Entities/FlagSwitchGate.cs

    // not meant to be used as an entity by itself, but rather as a base for more interesting variations
    // originally made around early 2022 as an excuse to better understand inheritance/polymorphism/virtual methods/whatever its called and to mess around with celeste modding in general more

    [Tracked(true)]
    public class GateBlock : Solid {
        public bool Triggered { get; private set; }

        protected readonly Vector2 node;

        protected Color fillColor;
        protected Vector2 scale = Vector2.One;
        protected Vector2 offset;
        public Vector2 Scale => scale;
        public Vector2 Offset => offset + Shake;

        protected readonly Sprite icon;
        protected readonly Vector2 iconOffset;
        protected readonly Wiggler finishIconScaleWiggler;
        protected readonly SoundSource openSfx;

        protected readonly bool smoke;
        protected readonly Color inactiveColor;
        protected readonly Color activeColor;
        protected readonly Color finishColor;
        protected readonly string moveSound;
        protected readonly string finishedSound;

        protected readonly float shakeTime;
        protected readonly float moveTime;
        protected readonly bool moveEased;
        protected readonly string onActivateFlag;

        public bool VisibleOnCamera { get; private set; } = true;

        protected readonly ParticleType P_RecoloredFire;
        protected readonly ParticleType P_Activate;

        public GateBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, safe: false) {
            if (data.Nodes.Length > 0) {
                node = data.Nodes[0] + offset;
            }

            shakeTime = data.Float("shakeTime", 0.5f);
            moveTime = data.Float("moveTime", 1.8f);
            moveEased = data.Bool("moveEased", true);
            onActivateFlag = data.Attr("linkTag", "");

            smoke = data.Bool("smoke", true);
            inactiveColor = Calc.HexToColor(data.Attr("inactiveColor", "5FCDE4"));
            activeColor = Calc.HexToColor(data.Attr("activeColor", "FFFFFF"));
            finishColor = Calc.HexToColor(data.Attr("finishColor", "F141DF"));
            moveSound = data.Attr("moveSound", "event:/game/general/touchswitch_gate_open");
            finishedSound = data.Attr("finishedSound", "event:/game/general/touchswitch_gate_finish");

            string iconSprite = data.Attr("iconSprite", "switchgate/icon");

            // set up icon
            icon = new Sprite(GFX.Game, "objects/" + iconSprite);
            Add(icon);
            icon.Add("spin", "", 0.1f, "spin");
            icon.Play("spin");
            icon.Rate = 0f;
            icon.Color = fillColor = inactiveColor;
            icon.Position = iconOffset = new Vector2(data.Width / 2f, data.Height / 2f);
            icon.CenterOrigin();
            Add(finishIconScaleWiggler = Wiggler.Create(0.5f, 4f, f => {
                icon.Scale = Vector2.One * (1f + f);
            }));

            Add(openSfx = new SoundSource());
            Add(new LightOcclude(0.5f));

            P_RecoloredFire = new ParticleType(TouchSwitch.P_Fire) {
                Color = finishColor
            };

            P_Activate = new(Seeker.P_HitWall) {
                Color = inactiveColor,
                Color2 = Color.Lerp(inactiveColor, Color.White, 0.75f),
                ColorMode = ParticleType.ColorModes.Blink,
            };
        }

        public void Activate() {
            Triggered = true;

            if (!string.IsNullOrEmpty(onActivateFlag)) {
                SceneAs<Level>().Session.SetFlag(onActivateFlag);
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            GateBlockOutlineRenderer.TryCreateRenderer(scene);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Add(new Coroutine(Sequence(node)));
        }

        public override void Update() {
            VisibleOnCamera = InView((Scene as Level).Camera);

            // ease scale and hitOffset towards their default values
            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 4f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 4f);
            offset.X = Calc.Approach(offset.X, 0f, Engine.DeltaTime * 15f);
            offset.Y = Calc.Approach(offset.Y, 0f, Engine.DeltaTime * 15f);

            base.Update();
        }

        public override void Render() {
            if (!VisibleOnCamera)
                return;

            // only render the icon, any unique gate blocks should handle visuals themselves
            Vector2 iconScale = icon.Scale;
            icon.Scale *= Scale;

            icon.Position = iconOffset + Offset;
            icon.DrawOutline();

            base.Render();

            icon.Scale = iconScale;
        }

        public virtual void RenderOutline() {
            // outline rendering should be implemented per gate block type
        }

        public IEnumerator Sequence(Vector2 node) {
            Vector2 start = Position;

            while (!Triggered) {
                yield return null;
            }

            yield return 0.1f;

            // animate the icon
            openSfx.Play(moveSound);
            if (shakeTime > 0f) {
                StartShaking(shakeTime);
                while (icon.Rate < 1f) {
                    icon.Color = fillColor = Color.Lerp(inactiveColor, activeColor, icon.Rate);
                    icon.Rate += Engine.DeltaTime / shakeTime;
                    yield return null;
                }
            } else {
                icon.Rate = 1f;
            }

            yield return 0.1f;

            // move the gate block, emitting particles along the way
            int particleAt = 0;
            Tween moveTween = Tween.Create(Tween.TweenMode.Oneshot, moveEased ? Ease.CubeOut : null, moveTime + (moveEased ? 0.2f : 0f), start: true);
            moveTween.OnUpdate = tweenArg => {
                MoveTo(Vector2.Lerp(start, node, tweenArg.Eased));
                if (Scene.OnInterval(0.1f)) {
                    particleAt++;
                    particleAt %= 2;
                    for (int tileX = 0; tileX < Width / 8f; tileX++) {
                        for (int tileY = 0; tileY < Height / 8f; tileY++) {
                            if ((tileX + tileY) % 2 == particleAt) {
                                SceneAs<Level>().ParticlesBG.Emit(SwitchGate.P_Behind,
                                    Position + new Vector2(tileX * 8, tileY * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
                            }
                        }
                    }
                }
            };
            Add(moveTween);

            float moveTimeLeft = moveTime;
            while (moveTimeLeft > 0f) {
                yield return null;
                moveTimeLeft -= Engine.DeltaTime;
            }

            bool collidableBackup = Collidable;
            Collidable = false;

            // collide dust particles on the left
            if (node.X <= start.X) {
                Vector2 add = new Vector2(0f, 2f);
                for (int tileY = 0; tileY < Height / 8f; tileY++) {
                    Vector2 collideAt = new Vector2(Left - 1f, Top + 4f + (tileY * 8));
                    Vector2 noCollideAt = collideAt + Vector2.UnitX;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float)Math.PI);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float)Math.PI);
                    }
                }
            }

            // collide dust particles on the right
            if (node.X >= start.X) {
                Vector2 add = new Vector2(0f, 2f);
                for (int tileY = 0; tileY < Height / 8f; tileY++) {
                    Vector2 collideAt = new Vector2(Right + 1f, Top + 4f + (tileY * 8));
                    Vector2 noCollideAt = collideAt - Vector2.UnitX * 2f;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, 0f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, 0f);
                    }
                }
            }

            // collide dust particles on the top
            if (node.Y <= start.Y) {
                Vector2 add = new Vector2(2f, 0f);
                for (int tileX = 0; tileX < Width / 8f; tileX++) {
                    Vector2 collideAt = new Vector2(Left + 4f + (tileX * 8), Top - 1f);
                    Vector2 noCollideAt = collideAt + Vector2.UnitY;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, -(float)Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, -(float)Math.PI / 2f);
                    }
                }
            }

            // collide dust particles on the bottom
            if (node.Y >= start.Y) {
                Vector2 add = new Vector2(2f, 0f);
                for (int tileX = 0; tileX < Width / 8f; tileX++) {
                    Vector2 collideAt = new Vector2(Left + 4f + (tileX * 8), Bottom + 1f);
                    Vector2 noCollideAt = collideAt - Vector2.UnitY * 2f;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float)Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float)Math.PI / 2f);
                    }
                }
            }
            Collidable = collidableBackup;

            // moving is over
            Audio.Play(finishedSound, Position);
            StartShaking(0.2f);
            while (icon.Rate > 0f) {
                icon.Color = Color.Lerp(activeColor, finishColor, 1f - icon.Rate);
                // darken finished fill color a bit
                Color darkFinishColor = new((int)(finishColor.R * 0.725f), (int)(finishColor.G * 0.725f), (int)(finishColor.B * 0.825f), 255);
                fillColor = Color.Lerp(activeColor, darkFinishColor, 1f - icon.Rate);

                icon.Rate -= Engine.DeltaTime * 4f;
                yield return null;
            }
            icon.Rate = 0f;
            icon.SetAnimationFrame(0);
            finishIconScaleWiggler.Start();

            // emit fire particles if the block is not behind a solid
            collidableBackup = Collidable;
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(Center) && smoke) {
                for (int i = 0; i < 32; i++) {
                    float angle = Calc.Random.NextFloat((float)Math.PI * 2f);
                    SceneAs<Level>().ParticlesFG.Emit(P_RecoloredFire, Position + iconOffset + Calc.AngleToVector(angle, 4f), angle);
                }
            }
            Collidable = collidableBackup;
        }


        // i love stealing vanilla code peaceline
        public void ActivateParticles() {
            Vector2 dir = node - Position;

            float direction = Calc.Angle(dir);
            Vector2 position;
            Vector2 positionRange;
            int num;

            dir = dir.FourWayNormal();
            if (dir == Vector2.UnitX) {
                position = CenterRight - Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            } else if (dir == -Vector2.UnitX) {
                position = CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            } else if (dir == Vector2.UnitY) {
                position = BottomCenter - Vector2.UnitY;
                positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            } else {
                position = TopCenter + Vector2.UnitY;
                positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            }
            num += 2;

            (Scene as Level).Particles.Emit(P_Activate, num, position, positionRange, direction);
        }

        protected void DrawNineSlice(MTexture texture, Color color) {
            // completely stolen from maddie's helping hand
            // probably much more performant than anything i could make so its mostly unchanged apart from adding scaling
            int widthInTiles = (int)Collider.Width / 8 - 1;
            int heightInTiles = (int)Collider.Height / 8 - 1;

            Vector2 renderPos = new Vector2(Position.X + Offset.X, Position.Y + Offset.Y);
            Vector2 blockCenter = renderPos + new Vector2(Collider.Width / 2f, Collider.Height / 2f);
            Texture2D baseTexture = texture.Texture.Texture;
            int clipBaseX = texture.ClipRect.X;
            int clipBaseY = texture.ClipRect.Y;

            Rectangle clipRect = new Rectangle(clipBaseX, clipBaseY, 8, 8);

            for (int i = 0; i <= widthInTiles; i++) {
                clipRect.X = clipBaseX + ((i < widthInTiles) ? i == 0 ? 0 : 8 : 16);
                for (int j = 0; j <= heightInTiles; j++) {
                    int tilePartY = (j < heightInTiles) ? j == 0 ? 0 : 8 : 16;
                    clipRect.Y = tilePartY + clipBaseY;
                    Draw.SpriteBatch.Draw(baseTexture, blockCenter + ((renderPos + new Vector2(4, 4) - blockCenter) * Scale), clipRect, color, 0f, new Vector2(4, 4), Scale, SpriteEffects.None, 0f);
                    renderPos.Y += 8f;
                }

                renderPos.X += 8f;
                renderPos.Y = Position.Y + Offset.Y;
            }
        }

        private bool InView(Camera camera) =>
            X < camera.Right + 16f && X + Width > camera.Left - 16f && Y < camera.Bottom + 16f && Y + Height > camera.Top - 16f;

        [Tracked]
        private class GateBlockOutlineRenderer : Entity {
            private static bool rendererJustCreated = false;

            public GateBlockOutlineRenderer() : base() {
                Depth = 1;
                Tag = Tags.Persistent;
            }

            public override void Render() {
                var blocks = Scene.Tracker.GetEntities<GateBlock>();

                foreach (GateBlock block in blocks) {
                    if (block.Visible && block.VisibleOnCamera) {
                        block.RenderOutline();
                    }
                }
            }

            public override void Awake(Scene scene) {
                base.Awake(scene);

                rendererJustCreated = false;
            }

            public static void TryCreateRenderer(Scene scene) {
                if (!rendererJustCreated && scene.Tracker.GetEntities<GateBlockOutlineRenderer>().Count == 0) {
                    scene.Add(new GateBlockOutlineRenderer());
                    rendererJustCreated = true;
                }
            }
        }
    }
}
