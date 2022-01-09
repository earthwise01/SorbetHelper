using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Entities;

namespace Celeste.Mod.SorbetHelper.Entities {

    [Tracked]
    public class GateBlock : Solid {

        // A large chunk of this code is copy-pasted from MaxHelpingHand's Flag Switch Gates as I didn't want to have to bother with cleaning up the vanilla code
        // Due to this I do not take full credit of the code here
        // Methods which are copied from MaxHelpingHand are marked accordingly

        private ParticleType P_RecoloredFire;
        private ParticleType P_RecoloredFireBack;

        public MTexture texture;

        public Sprite icon;
        public Vector2 iconOffset;

        public Wiggler wiggler;

        public Vector2 node;

        private SoundSource openSfx;

        public int ID { get; private set; }

        public bool Triggered;

        public Color inactiveColor;
        public Color activeColor;
        public Color finishColor;

        public float shakeTime;
        public float moveTime;
        public bool moveEased;

        public string moveSound;
        public string finishedSound;

        public bool smoke;

        public string blockSprite;
        public string iconSprite;

        public GateBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, safe: false) {
            // Mostly copied from MaxHelpingHand's Flag Switch Gates, with a few slight changes to better support how this entity is used
            ID = data.ID;

            if (data.Nodes.Length > 0) {
                node = data.Nodes[0] + offset;
            }

            inactiveColor = Calc.HexToColor(data.Attr("inactiveColor", "5FCDE4"));
            activeColor = Calc.HexToColor(data.Attr("activeColor", "FFFFFF"));
            finishColor = Calc.HexToColor(data.Attr("finishColor", "F141DF"));

            shakeTime = data.Float("shakeTime", 0.5f);
            moveTime = data.Float("moveTime", 1.8f);
            moveEased = data.Bool("moveEased", true);

            moveSound = data.Attr("moveSound", "event:/game/general/touchswitch_gate_open");
            finishedSound = data.Attr("finishedSound", "event:/game/general/touchswitch_gate_finish");

            smoke = data.Bool("smoke", true);

            blockSprite = data.Attr("sprite", "block");
            iconSprite = data.Attr("icon", "switchgate/icon");

            P_RecoloredFire = new ParticleType(TouchSwitch.P_Fire) {
                Color = finishColor
            };
            P_RecoloredFireBack = new ParticleType(TouchSwitch.P_Fire) {
                Color = inactiveColor
            };

            icon = new Sprite(GFX.Game, "objects/" + iconSprite);
            Add(icon);
            icon.Add("spin", "", 0.1f, "spin");
            icon.Play("spin");
            icon.Rate = 0f;
            icon.Color = inactiveColor;
            icon.Position = (iconOffset = new Vector2(data.Width / 2f, data.Height / 2f));
            icon.CenterOrigin();
            Add(wiggler = Wiggler.Create(0.5f, 4f, f => {
                icon.Scale = Vector2.One * (1f + f);
            }));

            texture = GFX.Game["objects/switchgate/" + blockSprite];

            Add(openSfx = new SoundSource());
            Add(new LightOcclude(0.5f));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Add(new Coroutine(Sequence(node)));
        }

        public bool InView() {
            // Copied from MaxHelpingHand's Flag Switch Gates
            Camera camera = (Scene as Level).Camera;
            return Position.X + Width > camera.X - 16f && Position.Y + Height > camera.Y - 16f && Position.X < camera.X + 320f && Position.Y < camera.Y + 180f;
        }

        public override void Render() {
            // Copied from MaxHelpingHand's Flag Switch Gates
            if (!InView()) return;

            int widthInTiles = (int) Collider.Width / 8 - 1;
            int heightInTiles = (int) Collider.Height / 8 - 1;

            Vector2 renderPos = new Vector2(Position.X + Shake.X, Position.Y + Shake.Y);
            Texture2D baseTexture = texture.Texture.Texture;
            int clipBaseX = texture.ClipRect.X;
            int clipBaseY = texture.ClipRect.Y;

            Rectangle clipRect = new Rectangle(clipBaseX, clipBaseY, 8, 8);

            for (int i = 0; i <= widthInTiles; i++) {
                clipRect.X = clipBaseX + ((i < widthInTiles) ? i == 0 ? 0 : 8 : 16);
                for (int j = 0; j <= heightInTiles; j++) {
                    int tilePartY = (j < heightInTiles) ? j == 0 ? 0 : 8 : 16;
                    clipRect.Y = tilePartY + clipBaseY;
                    Draw.SpriteBatch.Draw(baseTexture, renderPos, clipRect, Color.White);
                    renderPos.Y += 8f;
                }
                renderPos.X += 8f;
                renderPos.Y = Position.Y + Shake.Y;
            }

            icon.Position = iconOffset + Shake;
            icon.DrawOutline();

            base.Render();
        }

        public virtual bool TriggerCheck() {
            return Triggered;
        }

        public virtual void PlayMoveSounds() {
            openSfx.Play(moveSound);
        }

        public virtual void PlayFinishedSounds() {
            Audio.Play(finishedSound, Position);
        }

        public IEnumerator Sequence(Vector2 node) {
            // Mostly copied from MaxHelpingHand's Flag Swith Gates with some slight changes here and there to better support how this entity is used
            Vector2 start = Position;

            Color fromColor, toColor;

            fromColor = inactiveColor;
            toColor = finishColor;
            while (!TriggerCheck()) {
                yield return null;
            }

            yield return 0.1f;

            // animate the icon
            PlayMoveSounds();
            if (shakeTime > 0f) {
                StartShaking(shakeTime);
                while (icon.Rate < 1f) {
                    icon.Color = Color.Lerp(fromColor, activeColor, icon.Rate);
                    icon.Rate += Engine.DeltaTime / shakeTime;
                    yield return null;
                }
            } else {
                icon.Rate = 1f;
            }

            yield return 0.1f;

            // move the gate block, emitting particles along the way
            int particleAt = 0;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, moveEased ? Ease.CubeOut : null, moveTime + (moveEased ? 0.2f : 0f), start: true);
            tween.OnUpdate = tweenArg => {
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
            Add(tween);

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
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float) Math.PI);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float) Math.PI);
                    }
                }
            }

            // collide dust particles on the rigth
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
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, -(float) Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, -(float) Math.PI / 2f);
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
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float) Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float) Math.PI / 2f);
                    }
                }
            }
            Collidable = collidableBackup;

            // moving is over
            PlayFinishedSounds();
            StartShaking(0.2f);
            while (icon.Rate > 0f) {
                icon.Color = Color.Lerp(activeColor, toColor, 1f - icon.Rate);
                icon.Rate -= Engine.DeltaTime * 4f;
                yield return null;
            }
            icon.Rate = 0f;
            icon.SetAnimationFrame(0);
            wiggler.Start();

            // emit fire particles if the block is not behind a solid.
            collidableBackup = Collidable;
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(Center) && smoke) {
                for (int i = 0; i < 32; i++) {
                    float angle = Calc.Random.NextFloat((float) Math.PI * 2f);
                    SceneAs<Level>().ParticlesFG.Emit(P_RecoloredFire, Position + iconOffset + Calc.AngleToVector(angle, 4f), angle);
                }
            }
            Collidable = collidableBackup;
        }
    }
}