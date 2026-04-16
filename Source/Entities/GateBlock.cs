using System.Diagnostics;

namespace Celeste.Mod.SorbetHelper.Entities;

// heavily based on flag switch gates from maddie's helping hand
// https://github.com/maddie480/MaddieHelpingHand/blob/master/Entities/FlagSwitchGate.cs

[Tracked(true)]
public abstract class GateBlock : Solid
{
    private readonly Vector2 start;
    private readonly Vector2 node;

    protected bool Triggered { get; private set; }
    protected bool AtNode { get; private set; }

    private Vector2 hitScale = Vector2.One;
    private Vector2 hitOffset;
    private float hitFlash;

    private Vector2 Scale => hitScale;
    private Vector2 Offset => hitOffset + Shake;

    private Color fillColor;
    protected Color FillColor => Color.Lerp(fillColor, Color.White, hitFlash * 0.5f);

    private readonly Sprite icon;
    private readonly Vector2 iconOffset;
    private readonly Wiggler finishIconScaleWiggler;
    private readonly bool emitSmoke;

    private readonly string moveSound;
    private readonly string finishedSound;
    private readonly SoundSource moveSoundSource;

    private readonly bool drawOutline;
    protected readonly Color startColor;
    protected readonly Color nodeColor;
    protected readonly Color activeColor;

    private readonly float shakeTime;
    private readonly float moveTime;
    private readonly bool moveEased;
    private readonly bool allowReturn;
    private readonly bool persistent;
    private readonly string persistentFlag;
    private readonly string activatedFlag;

    private bool VisibleOnCamera { get; set; } = true;

    private readonly ParticleType P_RecoloredFire;
    private readonly ParticleType P_Activate;
    private readonly ParticleType P_ActivateReturn;

    protected GateBlock(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, data.Width, data.Height, safe: false)
    {
        start = Position;
        if (data.Nodes.Length > 0)
            node = data.Nodes[0] + offset;

        shakeTime = data.Float("shakeTime", 0.5f);
        moveTime = data.Float("moveTime", 1.8f);
        moveEased = data.Bool("moveEased", true);
        allowReturn = data.Bool("allowReturn", false);
        persistent = data.Bool("persistent", false);
        persistentFlag = "flag_sorbetHelper_gateBlock_persistent_" + id.ID;
        activatedFlag = data.Attr("linkTag", "");

        emitSmoke = data.Bool("smoke", true);
        drawOutline = data.Bool("drawOutline", true);
        startColor = Calc.HexToColor(data.Attr("inactiveColor", "5FCDE4"));
        activeColor = Calc.HexToColor(data.Attr("activeColor", "FFFFFF"));
        nodeColor = Calc.HexToColor(data.Attr("finishColor", "F141DF"));
        moveSound = data.Attr("moveSound", "event:/sorbethelper/sfx/gateblock_open");
        finishedSound = data.Attr("finishedSound", "event:/sorbethelper/sfx/gateblock_finish");

        string iconSpritePath = data.Attr("iconSprite", "switchgate/icon");
        icon = new Sprite(GFX.Game, "objects/" + iconSpritePath);
        icon.Add("spin", "", 0.1f, "spin");
        icon.Play("spin");
        icon.Rate = 0f;
        icon.Color = fillColor = startColor;
        icon.Position = iconOffset = new Vector2(data.Width / 2f, data.Height / 2f);
        icon.CenterOrigin();
        Add(icon);
        Add(finishIconScaleWiggler = Wiggler.Create(0.5f, 4f, f => { icon.Scale = Vector2.One * (1f + f); }));

        Add(moveSoundSource = new SoundSource());
        Add(new LightOcclude(0.5f));

        P_RecoloredFire = new ParticleType(TouchSwitch.P_Fire)
        {
            Color = nodeColor
        };
        P_Activate = new ParticleType(Seeker.P_HitWall)
        {
            Color = startColor,
            Color2 = Color.Lerp(startColor, Color.White, 0.75f),
            ColorMode = ParticleType.ColorModes.Blink,
        };
        P_ActivateReturn = new ParticleType(P_Activate)
        {
            Color = nodeColor,
            Color2 = Color.Lerp(nodeColor, Color.White, 0.75f),
            ColorMode = ParticleType.ColorModes.Blink,
        };
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (drawOutline)
            GateBlockOutlineRenderer.TryCreateRenderer(scene);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (persistent && SceneAs<Level>().Session.GetFlag(persistentFlag))
        {
            AtNode = true;

            if (allowReturn)
                Add(new Coroutine(BackAndForthSequence()));
            else
                Triggered = true;

            MoveTo(node);
            icon.Rate = 0f;
            icon.SetAnimationFrame(0);
            icon.Color = nodeColor;
            fillColor = allowReturn
                ? nodeColor 
                : new Color((int)(nodeColor.R * 0.7f), (int)(nodeColor.G * 0.67f), (int)(nodeColor.B * 0.8f), 255);
        }
        else
        {
            AtNode = false;

            if (allowReturn)
                Add(new Coroutine(BackAndForthSequence()));
            else
                Add(new Coroutine(Sequence()));
        }
    }

    private void HitEffects(Vector2? hitDirection)
    {
        if (hitDirection is { } dir)
        {
            hitScale = new Vector2(
                1f + Math.Abs(dir.Y) * 0.28f - Math.Abs(dir.X) * 0.28f,
                1f + Math.Abs(dir.X) * 0.28f - Math.Abs(dir.Y) * 0.28f
            );
            hitOffset = dir * 4.15f;
        }

        hitFlash = 1f;
    }

    private void ActivateParticles()
    {
        Vector2 dir = node - start;
        if (AtNode)
            dir = -dir;

        float direction = dir.Angle();
        Vector2 position;
        Vector2 positionRange;
        int particleCount;

        switch (dir.FourWayNormal())
        {
            case { X: 1f, Y: 0f }:
                position = CenterRight - Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
                particleCount = (int)(Height / 8f) * 4;
                break;
            case { X: -1f, Y: 0f }:
                position = CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
                particleCount = (int)(Height / 8f) * 4;
                break;
            case { X: 0f, Y: 1f }:
                position = BottomCenter - Vector2.UnitY;
                positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
                particleCount = (int)(Width / 8f) * 4;
                break;
            case { X: 0f, Y: -1f }:
                position = TopCenter + Vector2.UnitY;
                positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
                particleCount = (int)(Width / 8f) * 4;
                break;
            case { X: 0f, Y: 0f }:
                return;
            default:
                throw new UnreachableException();
        }

        particleCount += 2;

        SceneAs<Level>().Particles.Emit(AtNode ? P_ActivateReturn : P_Activate, particleCount, position, positionRange, direction);
    }

    protected void Activate(Vector2? hitDirection = null)
    {
        if (Triggered)
            return;

        Triggered = true;

        HitEffects(hitDirection);
        ActivateParticles();

        if (!string.IsNullOrEmpty(activatedFlag))
            SceneAs<Level>().Session.SetFlag(activatedFlag);
    }

    private IEnumerator BackAndForthSequence()
    {
        while (true)
        {
            IEnumerator seq = Sequence();

            while (seq.MoveNext())
                yield return seq.Current;
        }
    }

    private IEnumerator Sequence()
    {
        Vector2 moveFrom = Position;
        Vector2 moveTo;
        Color fromColor, toColor;

        if (!AtNode)
        {
            moveTo = node;

            fromColor = startColor;
            toColor = nodeColor;
        }
        else
        {
            moveTo = start;

            fromColor = nodeColor;
            toColor = startColor;
        }

        Color noReturnFillColor = new Color((int)(toColor.R * 0.7f), (int)(toColor.G * 0.67f), (int)(toColor.B * 0.8f), 255);

        // wait until triggered
        while (!Triggered)
            yield return null;

        if (persistent)
            SceneAs<Level>().Session.SetFlag(persistentFlag, !AtNode);

        yield return 0.1f;

        // animate the icon
        moveSoundSource.Play(moveSound);
        if (shakeTime > 0f)
        {
            StartShaking(shakeTime);
            while (icon.Rate < 1f)
            {
                icon.Color = fillColor = Color.Lerp(fromColor, activeColor, icon.Rate);
                icon.Rate += Engine.DeltaTime / shakeTime;
                yield return null;
            }
        }
        else
        {
            icon.Color = fillColor = activeColor;
            icon.Rate = 1f;
        }

        yield return 0.1f;

        // move the gate block, emitting particles along the way
        int particleAt = 0;
        Tween moveTween = Tween.Create(Tween.TweenMode.Oneshot, moveEased ? Ease.CubeOut : null, moveTime + (moveEased ? 0.2f : 0f), start: true);
        moveTween.OnUpdate = tween =>
        {
            MoveTo(Vector2.Lerp(moveFrom, moveTo, tween.Eased));

            if (Scene.OnInterval(0.1f))
            {
                particleAt++;
                particleAt %= 2;

                for (int tx = 0; tx < Width / 8f; tx++)
                for (int ty = 0; ty < Height / 8f; ty++)
                {
                    if ((tx + ty) % 2 == particleAt)
                        SceneAs<Level>().ParticlesBG.Emit(SwitchGate.P_Behind, Position + new Vector2(tx * 8, ty * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
                }
            }
        };
        Add(moveTween);

        float moveTimer = moveTime;
        while (moveTimer > 0f)
        {
            yield return null;
            moveTimer -= Engine.DeltaTime;
        }

        // moving is over
        // create dust particles on any edges touching solids
        using (new SetTemporaryValue<bool>(ref Collidable, false))
        {
            Vector2 dustSpacing = new Vector2(0f, 2f);
            bool makeLeftDust = moveTo.X <= moveFrom.X;
            bool makeRightDust = moveTo.X >= moveFrom.X;
            for (int ty = 0; ty < Height / 8f; ty++)
            {
                if (makeLeftDust)
                {
                    Vector2 collideAt = new Vector2(Left - 1f, Top + 4f + ty * 8);
                    Vector2 noCollideAt = collideAt + Vector2.UnitX;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + dustSpacing, (float)Math.PI);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - dustSpacing, (float)Math.PI);
                    }
                }

                if (makeRightDust)
                {
                    Vector2 collideAt = new Vector2(Right + 1f, Top + 4f + ty * 8);
                    Vector2 noCollideAt = collideAt - Vector2.UnitX * 2f;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + dustSpacing, 0f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - dustSpacing, 0f);
                    }
                }
            }

            dustSpacing = new Vector2(2f, 0f);
            bool makeTopDust = moveTo.Y <= moveFrom.Y;
            bool makeBottomDust = moveTo.Y >= moveFrom.Y;
            for (int tx = 0; tx < Width / 8f; tx++)
            {
                if (makeTopDust)
                {
                    Vector2 collideAt = new Vector2(Left + 4f + tx * 8, Top - 1f);
                    Vector2 noCollideAt = collideAt + Vector2.UnitY;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + dustSpacing, -(float)Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - dustSpacing, -(float)Math.PI / 2f);
                    }
                }

                if (makeBottomDust)
                {
                    Vector2 collideAt = new Vector2(Left + 4f + tx * 8, Bottom + 1f);
                    Vector2 noCollideAt = collideAt - Vector2.UnitY * 2f;
                    if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + dustSpacing, (float)Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - dustSpacing, (float)Math.PI / 2f);
                    }
                }
            }
        }

        Audio.Play(finishedSound, Position);
        StartShaking(0.2f);
        while (icon.Rate > 0f)
        {
            icon.Color = Color.Lerp(activeColor, toColor, 1f - icon.Rate);
            fillColor = Color.Lerp(activeColor, allowReturn ? toColor : noReturnFillColor, 1f - icon.Rate);

            icon.Rate -= Engine.DeltaTime * 4f;
            yield return null;
        }
        icon.Rate = 0f;
        icon.SetAnimationFrame(0);
        finishIconScaleWiggler.Start();

        // emit fire particles if the icon is not behind a solid
        using (new SetTemporaryValue<bool>(ref Collidable, false))
        {
            if (emitSmoke && !Scene.CollideCheck<Solid>(Center))
            {
                for (int i = 0; i < 32; i++)
                {
                    float angle = Calc.Random.NextFloat((float)Math.PI * 2f);
                    SceneAs<Level>().ParticlesFG.Emit(P_RecoloredFire, Position + iconOffset + Calc.AngleToVector(angle, 4f), toColor, angle);
                }
            }
        }

        AtNode = !AtNode;
        if (allowReturn)
            Triggered = false;
    }

    public override void Update()
    {
        Camera camera = SceneAs<Level>().Camera;
        VisibleOnCamera = X < camera.Right + 16f && X + Width > camera.Left - 16f
                          && Y < camera.Bottom + 16f && Y + Height > camera.Top - 16f;

        hitScale.X = Calc.Approach(hitScale.X, 1f, Engine.DeltaTime * 4f);
        hitScale.Y = Calc.Approach(hitScale.Y, 1f, Engine.DeltaTime * 4f);
        hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 15f);
        hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 15f);
        hitFlash = Calc.Approach(hitFlash, 0f, Engine.DeltaTime * 5f);

        base.Update();
    }

    protected abstract void RenderOutline();

    protected abstract void RenderBlock();

    private void RenderIcon()
    {
        using (new SetTemporaryValue<Vector2>(ref icon.Scale, icon.Scale * Scale))
        {
            icon.Position = iconOffset + Offset;
            icon.DrawOutline();
            icon.Render();
        }
    }

    public override void Render()
    {
        if (!VisibleOnCamera)
            return;

        RenderBlock();
        RenderIcon();
    }
    
    protected Rectangle GetBlockRectangle()
    {
        Vector2 renderPosition = Position + Offset;

        if (hitScale == Vector2.One && hitOffset == Vector2.Zero)
            return new Rectangle((int)renderPosition.X, (int)renderPosition.Y, (int)Width, (int)Height);

        Vector2 origin = new Vector2(Width / 2f, Height / 2f);
        renderPosition += origin;
        int rectX = (int)Math.Round(renderPosition.X - origin.X * Scale.X);
        int rectY = (int)Math.Round(renderPosition.Y - origin.Y * Scale.Y);
        int rectW = (int)Math.Round(renderPosition.X + (Width - origin.X) * Scale.X) - rectX;
        int rectH = (int)Math.Round(renderPosition.Y + (Height - origin.Y) * Scale.Y) - rectY;
        return new Rectangle(rectX, rectY, rectW, rectH);
    }

    protected void DrawBlockNiceSlice(MTexture nineSlice, Color color)
    {
        Vector2 renderPosition = Position + Offset;
        Vector2 center = new Vector2(renderPosition.X + Width / 2f, renderPosition.Y + Height / 2f);
        Vector2 tilePosition = renderPosition;

        int widthInTiles = (int)Width / 8;
        int heightInTiles = (int)Height / 8;

        Texture2D texture = nineSlice.Texture.Texture_Safe;
        int clipStartX = nineSlice.ClipRect.X;
        int clipStartY = nineSlice.ClipRect.Y;
        Rectangle clipRect = new Rectangle(clipStartX, clipStartY, 8, 8);

        for (int tx = 0; tx < widthInTiles; tx++)
        {
            clipRect.X = clipStartX + ((tx < widthInTiles - 1) ? (tx == 0 ? 0 : 8) : 16);
            for (int ty = 0; ty < heightInTiles; ty++)
            {
                clipRect.Y = clipStartY + ((ty < heightInTiles - 1) ? (ty == 0 ? 0 : 8) : 16);
                Draw.SpriteBatch.Draw(texture, center, clipRect, color, 0f, center - tilePosition, Scale, SpriteEffects.None, 0f);

                tilePosition.Y += 8f;
            }

            tilePosition.X += 8f;
            tilePosition.Y = renderPosition.Y;
        }
    }

    [Tracked]
    private class GateBlockOutlineRenderer : Entity
    {
        private GateBlockOutlineRenderer()
        {
            Depth = 1;
            Tag = Tags.Persistent;
        }

        public override void Render()
        {
            foreach (GateBlock block in Scene.Tracker.GetEntities<GateBlock>())
            {
                if (!block.Visible || !block.VisibleOnCamera || !block.drawOutline)
                    continue;

                block.RenderOutline();
            }
        }

        public static void TryCreateRenderer(Scene scene)
        {
            if (scene.Tracker.GetEntities<GateBlockOutlineRenderer>()
                             .Concat(scene.Entities.ToAdd)
                             .FirstOrDefault(r => r is GateBlockOutlineRenderer)
                is not GateBlockOutlineRenderer)
                scene.Add(new GateBlockOutlineRenderer());
        }
    }
}
