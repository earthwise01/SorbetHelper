using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/TouchGateBlock")]
[Tracked]
public class TouchGateBlock : GateBlock {

    private readonly MTexture mainTexture;

    private readonly bool moveOnGrab;
    private readonly bool moveOnStaticMover;

    public TouchGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
        moveOnGrab = data.Bool("moveOnGrab", true);
        moveOnStaticMover = data.Bool("moveOnStaticMoverInteract", false);

        string blockSprite = data.Attr("blockSprite", "SorbetHelper/gateblock/touch/block");
        mainTexture = GFX.Game[$"objects/{blockSprite}"];
    }

    public override void OnStaticMoverTrigger(StaticMover sm) {
        if (!Triggered && moveOnStaticMover) {
            Activate();
            if (smoke)
                ActivateParticles();
            Audio.Play("event:/game/general/fallblock_shake", Position);
            Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
        }
    }

    public override void Update() {
        // maybe kinda messy but i dont rlly care enough to try and fix it rn
        if (!Triggered && ((moveOnGrab && HasPlayerRider()) || (!moveOnGrab && HasPlayerOnTop()))) {
            Activate();
            if (smoke)
                ActivateParticles();
            Audio.Play("event:/game/general/fallblock_shake", Position);
            Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
        }

        base.Update();
    }

    public override void Render() {
        if (!VisibleOnCamera)
            return;

        // main block
        Draw.Rect(Position + Offset + new Vector2(2f, 2f), Collider.Width - 4f, Collider.Height - 4f, fillColor);
        DrawNineSlice(mainTexture, Color.White);

        // render icon
        base.Render();
    }

    public override void RenderOutline() {
        Draw.Rect(Position + Offset - new Vector2(1f, 1f), Collider.Width + 2f, Collider.Height + 2f, Color.Black);
    }
}
