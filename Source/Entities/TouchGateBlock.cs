namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/TouchGateBlock")]
[Tracked]
public class TouchGateBlock : GateBlock
{
    private readonly MTexture mainTexture;

    private readonly bool moveOnGrab;
    private readonly bool moveOnStaticMover;

    public TouchGateBlock(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id)
    {
        moveOnGrab = data.Bool("moveOnGrab", true);
        moveOnStaticMover = data.Bool("moveOnStaticMoverInteract", false);

        string blockSprite = data.Attr("blockSprite", "SorbetHelper/gateblock/touch/block");
        mainTexture = GFX.Game[$"objects/{blockSprite}"];
    }

    public override void OnStaticMoverTrigger(StaticMover _)
    {
        if (!Triggered && moveOnStaticMover)
        {
            Activate();
            Audio.Play("event:/game/general/fallblock_shake", Position);
            Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
        }
    }

    public override void Update()
    {
        if (!Triggered && (moveOnGrab ? HasPlayerRider() : HasPlayerOnTop()))
        {
            Activate();
            Audio.Play("event:/game/general/fallblock_shake", Position);
            Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
        }

        base.Update();
    }

    protected override void RenderBlock()
    {
        Rectangle blockRect = GetBlockRectangle();
        Draw.Rect(blockRect.X + 2, blockRect.Y + 2, blockRect.Width - 4, blockRect.Height - 4, FillColor);
        DrawBlockNiceSlice(mainTexture, Color.White);
    }

    protected override void RenderOutline()
    {
        Rectangle blockRect = GetBlockRectangle();
        Draw.Rect(blockRect.X - 1, blockRect.Y - 1, blockRect.Width + 2, blockRect.Height + 2, Color.Black);
    }
}
