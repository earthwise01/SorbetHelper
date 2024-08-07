using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/DashGateBlock")]
    [Tracked]
    public class DashGateBlock : GateBlock {
        public enum Axes {
            Both,
            Horizontal,
            Vertical
        }

        private readonly MTexture mainTexture, lightsTexture;

        private readonly bool canMoveVertically;
        private readonly bool canMoveHorizontally;
        private readonly bool allowWavedash;
        private readonly bool dashCornerCorrection;

        private float activationFlash;

        private readonly ParticleType P_Activate;

        public DashGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
            allowWavedash = data.Bool("allowWavedash", false);
            dashCornerCorrection = data.Bool("dashCornerCorrection", false);

            Axes axes = data.Enum("axes", Axes.Both);
            string blockSprite = data.Attr("blockSprite", "SorbetHelper/gateblock/dash/block");
            switch (axes) {
                default:
                    // no need to update the block sprite here
                    // blockSprite = blockSprite;
                    canMoveHorizontally = canMoveVertically = true;
                    break;
                case Axes.Horizontal:
                    blockSprite += "_h";
                    canMoveHorizontally = true;
                    canMoveVertically = false;
                    break;
                case Axes.Vertical:
                    blockSprite += "_v";
                    canMoveHorizontally = false;
                    canMoveVertically = true;
                    break;
            }
            mainTexture = GFX.Game[$"objects/{blockSprite}"];
            lightsTexture = GFX.Game[$"objects/{blockSprite}_lights"];

            OnDashCollide = OnDashed;

            P_Activate = new(Seeker.P_HitWall) {
                Color = inactiveColor,
                Color2 = Color.Lerp(inactiveColor, Color.White, 0.75f),
                ColorMode = ParticleType.ColorModes.Blink,
            };
        }

        public DashCollisionResults OnDashed(Player player, Vector2 dir) {
            if (!Triggered && CanActivate(dir)) {
                bool gravityInverted = GravityHelperImports.IsPlayerInverted?.Invoke() ?? false;

                // make wallbouncing easier
                if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f) && dashCornerCorrection) {
                    return DashCollisionResults.NormalCollision;
                }

                // trigger the gate
                Activate();

                // fancy effects and stuff
                if (smoke)
                    ActivateParticles(node - Position);
                activationFlash = 1f;
                (Scene as Level).DirectionalShake(dir);
                scale = new Vector2(
                    1f + Math.Abs(dir.Y) * 0.28f - Math.Abs(dir.X) * 0.28f,
                    1f + Math.Abs(dir.X) * 0.28f - Math.Abs(dir.Y) * 0.28f
                );
                offset = dir * 4.15f;
                Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
                Audio.Play("event:/game/06_reflection/crushblock_activate", Center);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

                if (allowWavedash && dir.Y == (gravityInverted ? -1f : 1f)) {
                    return DashCollisionResults.NormalCollision;
                }

                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;
        }

        private bool CanActivate(Vector2 direction) =>
            (direction.X != 0f && canMoveHorizontally) || (direction.Y != 0f && canMoveVertically);

        public override void Update() {
            base.Update();

            if (activationFlash > 0f)
                activationFlash -= Engine.DeltaTime * 5f;
        }

        public override void Render() {
            if (!VisibleOnCamera)
                return;

            // outline
            Vector2 scaledTopLeft = Center + Offset - (new Vector2(Collider.Width / 2f, Collider.Height / 2f) * Scale);
            float scaledWidth = Collider.Width * Scale.X;
            float scaledHeight = Collider.Height * Scale.Y;
            Draw.Rect(scaledTopLeft - Vector2.UnitY, scaledWidth, scaledHeight + 2, Color.Black);
            Draw.Rect(scaledTopLeft - Vector2.UnitX, scaledWidth + 2, scaledHeight, Color.Black);

            // main block
            // might maybe try and do shader stuff or smth to try and make the lights look less plain? but idk this still looks okay i think
            DrawNineSlice(mainTexture, Color.White);
            DrawNineSlice(lightsTexture, Color.Lerp(currentColor, Color.White, activationFlash * 0.4f));

            // render icon
            base.Render();
        }

        // i love stealing vanilla code peaceline
        public void ActivateParticles(Vector2 dir) {
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
    }
}
