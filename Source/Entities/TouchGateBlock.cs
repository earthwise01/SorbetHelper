using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/TouchGateBlock")]
    [Tracked]
    public class TouchGateBlock : GateBlock {

        private readonly MTexture mainTexture;

        private readonly bool moveOnGrab;
        private readonly bool moveOnStaticMover;

        public TouchGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
            moveOnGrab = data.Bool("moveOnGrab", true);
            moveOnStaticMover = data.Bool("moveOnStaticMoverInteract", false);

            string blockSprite = data.Attr("blockSprite", "moveBlock/base");
            mainTexture = GFX.Game[$"objects/{blockSprite}"];
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            if (!Triggered && moveOnStaticMover) {
                Activate();
                Audio.Play("event:/game/general/fallblock_shake", Position);
                Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
            }
        }

        public override void Update() {
            if (!Triggered && ((moveOnGrab && HasPlayerRider()) || (!moveOnGrab && HasPlayerOnTop()))) {
                Activate();
                Audio.Play("event:/game/general/fallblock_shake", Position);
                Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
            }

            base.Update();
        }

        public override void Render() {
            if (!VisibleOnCamera)
                return;

            // outline
            Vector2 scaledTopLeft = Center + Offset - (new Vector2(Collider.Width / 2f, Collider.Height / 2f) * Scale);
            float scaledWidth = Collider.Width * Scale.X;
            float scaledHeight = Collider.Height * Scale.Y;
            Draw.Rect(scaledTopLeft - Vector2.One, scaledWidth + 2, scaledHeight + 2, Color.Black);

            // main block
            Draw.Rect(scaledTopLeft + Vector2.One, scaledWidth - 2, scaledHeight - 2, currentColor);
            DrawNineSlice(mainTexture, Color.White);

            // render icon
            base.Render();
        }
    }
}
