using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

// heavily inspired by color switches (https://github.com/CommunalHelper/VortexHelper/blob/dev/Code/Entities/ColorSwitch.cs)
// and switch blocks (https://github.com/CommunalHelper/VortexHelper/blob/dev/Code/Entities/SwitchBlock.cs) from vortex helper.
// code also taken from vanilla cassette blocks, and playback billboards (for rendering)

[CustomEntity("SorbetHelper/ColorDashBlock")]
[Tracked]
public class ColorDashBlock : Solid
{
    private readonly int index;

    private List<ColorDashBlock> group;
    private Vector2 groupOrigin;
    private bool groupLeader;

    private bool activated;

    private readonly Color color;
    private readonly ParticleType P_Shatter;

    private Wiggler wiggler;
    private Vector2 wigglerScaler;
    private readonly LightOcclude lightOcclude;

    private readonly List<Image> pressedImages = [];
    private readonly List<Image> solidImages = [];
    private readonly List<Image> allImages = [];
    private uint noiseSeed;

    public ColorDashBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false)
    {
        SurfaceSoundIndex = 35;
        index = data.Int("index", 0);
        color = index switch
        {
            0 => Calc.HexToColor("00c2c2"),
            1 => Calc.HexToColor("fca700"),
            _ => throw new NotImplementedException($"Index {index} is not supported!"),
        };

        P_Shatter = new ParticleType(Lightning.P_Shatter)
        {
            Color = Color.Lerp(color, Color.White, 0.75f),
            Color2 = Color.White,
            ColorMode = ParticleType.ColorModes.Fade
        };

        Add(lightOcclude = new LightOcclude(0.8f));

        OnDashCollide = OnDashed;

        // switch if communal helper dream tunnel dashed
        bool switchOnDreamTunnel = data.Bool("switchOnDreamTunnel", false);
        if (switchOnDreamTunnel && CommunalHelperDashStatesInterop.IsImported)
        {
            void OnEnter(Player player) { }
            void OnExit(Player player)
            {
                Switch();
                Add(new Coroutine(GlitchSequence(), true));
            }

            static IEnumerator GlitchSequence()
            {
                Glitch.Value = 0.22f;
                while (Glitch.Value > 0.0f)
                {
                    Glitch.Value -= 0.5f * Engine.DeltaTime;
                    yield return null;
                }
                Glitch.Value = 0.0f;
            }

            Add(CommunalHelperDashStatesInterop.DreamTunnelInteraction(OnEnter, OnExit));
        }
    }

    #region Setup

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        // setup colors for static movers
        Color fadeColor = Calc.HexToColor("667da5");
        Color disabledColor = new Color(fadeColor.R / 255f * (color.R / 255f), fadeColor.G / 255f * (color.G / 255f), fadeColor.B / 255f * (color.B / 255f), 1f);
        foreach (StaticMover staticMover in staticMovers)
        {
            if (staticMover.Entity is Spikes spikes)
            {
                spikes.EnabledColor = color;
                spikes.DisabledColor = disabledColor;
                spikes.VisibleWhenDisabled = true;
                spikes.SetSpikeColor(color);
            }

            if (staticMover.Entity is Spring spring)
            {
                spring.DisabledColor = disabledColor;
                spring.VisibleWhenDisabled = true;
            }
        }

        // setup group
        if (group is null)
        {
            groupLeader = true;
            group = [this];
            FindInGroup(this);

            float groupLeft = float.MaxValue;
            float groupRight = float.MinValue;
            float groupTop = float.MaxValue;
            float groupBottom = float.MinValue;
            foreach (ColorDashBlock colorDashBlock in group)
            {
                if (colorDashBlock.Left < groupLeft)
                    groupLeft = colorDashBlock.Left;
                if (colorDashBlock.Right > groupRight)
                    groupRight = colorDashBlock.Right;
                if (colorDashBlock.Bottom > groupBottom)
                    groupBottom = colorDashBlock.Bottom;
                if (colorDashBlock.Top < groupTop)
                    groupTop = colorDashBlock.Top;
            }

            groupOrigin = new Vector2((int)(groupLeft + (groupRight - groupLeft) / 2f), (int)groupBottom);
            wigglerScaler = new Vector2(Calc.ClampedMap(groupRight - groupLeft, 32f, 96f, 1f, 0.2f), Calc.ClampedMap(groupBottom - groupTop, 32f, 96f, 1f, 0.2f));
            Add(wiggler = Wiggler.Create(0.3f, 3f));

            foreach (ColorDashBlock colorDashBlock in group)
            {
                colorDashBlock.wiggler = wiggler;
                colorDashBlock.wigglerScaler = wigglerScaler;
                colorDashBlock.groupOrigin = groupOrigin;
            }
        }

        // setup spike origins
        foreach (StaticMover staticMover in staticMovers)
        {
            if (staticMover.Entity is Spikes spikes)
                spikes.SetOrigins(groupOrigin);
        }

        // cassette block autotiling
        for (float x = Left; x < Right; x += 8f)
        {
            for (float y = Top; y < Bottom; y += 8f)
            {
                bool leftCheck = CheckForSame(x - 8f, y);
                bool rightCheck = CheckForSame(x + 8f, y);
                bool topCheck = CheckForSame(x, y - 8f);
                bool bottomCheck = CheckForSame(x, y + 8f);

                if (leftCheck && rightCheck && topCheck && bottomCheck)
                {
                    if (!CheckForSame(x + 8f, y - 8f))
                        SetImage(x, y, 3, 0);
                    else if (!CheckForSame(x - 8f, y - 8f))
                        SetImage(x, y, 3, 1);
                    else if (!CheckForSame(x + 8f, y + 8f))
                        SetImage(x, y, 3, 2);
                    else if (!CheckForSame(x - 8f, y + 8f))
                        SetImage(x, y, 3, 3);
                    else
                        SetImage(x, y, 1, 1);
                }
                else if (leftCheck && rightCheck && !topCheck && bottomCheck)
                    SetImage(x, y, 1, 0);
                else if (leftCheck && rightCheck && topCheck && !bottomCheck)
                    SetImage(x, y, 1, 2);
                else if (leftCheck && !rightCheck && topCheck && bottomCheck)
                    SetImage(x, y, 2, 1);
                else if (!leftCheck && rightCheck && topCheck && bottomCheck)
                    SetImage(x, y, 0, 1);
                else if (leftCheck && !rightCheck && !topCheck && bottomCheck)
                    SetImage(x, y, 2, 0);
                else if (!leftCheck && rightCheck && !topCheck && bottomCheck)
                    SetImage(x, y, 0, 0);
                else if (leftCheck && !rightCheck && topCheck && !bottomCheck)
                    SetImage(x, y, 2, 2);
                else if (!leftCheck && rightCheck && topCheck && !bottomCheck)
                    SetImage(x, y, 0, 2);
            }
        }

        if (!Collidable)
            DisableStaticMovers();

        UpdateState(playEffects: false);
    }

    private void FindInGroup(ColorDashBlock block)
    {
        foreach (ColorDashBlock otherBlock in Scene.Tracker.GetEntities<ColorDashBlock>())
        {
            if (otherBlock != this && otherBlock != block && otherBlock.index == index && (otherBlock.CollideRect(new Rectangle((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) || otherBlock.CollideRect(new Rectangle((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))) && !group.Contains(otherBlock))
            {
                group.Add(otherBlock);
                FindInGroup(otherBlock);
                otherBlock.group = group;
            }
        }
    }

    private bool CheckForSame(float x, float y)
    {
        foreach (ColorDashBlock otherBlock in Scene.Tracker.GetEntities<ColorDashBlock>())
        {
            if (otherBlock.index == index && otherBlock.Collider.Collide(new Rectangle((int)x, (int)y, 8, 8)))
                return true;
        }

        return false;
    }

    private void SetImage(float x, float y, int tx, int ty)
    {
        pressedImages.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/SorbetHelper/colorDashBlock/pressed"]));
        solidImages.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/SorbetHelper/colorDashBlock/solid"]));
    }

    private Image CreateImage(float x, float y, int tx, int ty, MTexture tex)
    {
        Vector2 imageOffset = new Vector2(x - X, y - Y);
        Image image = new Image(tex.GetSubtexture(tx * 8, ty * 8, 8, 8));
        Vector2 imageGroupOrigin = groupOrigin - Position;
        image.Origin = imageGroupOrigin - imageOffset;
        image.Position = imageGroupOrigin;
        image.Color = color;
        Add(image);
        allImages.Add(image);
        return image;
    }

    #endregion

    #region Behaviour

    private const string CounterName = "SorbetHelper_ColorDashBlockIndex";

    public static int GetColorDashBlockIndex(Session session) => session.GetCounter(CounterName);
    public static void SetColorDashBlockIndex(Session session, int index) => session.SetCounter(CounterName, index);

    private DashCollisionResults OnDashed(Player player, Vector2 dir)
    {
        if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>())
            return DashCollisionResults.NormalCollision;

        SceneAs<Level>().DirectionalShake(dir, 0.25f);
        Switch();

        return DashCollisionResults.Rebound;
    }

    public void Switch(bool playSfx = true)
    {
        if (playSfx)
        {
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
            Audio.Play("event:/game/03_resort/forcefield_bump", Center);
            Audio.Play("event:/game/05_mirror_temple/button_activate", Center);
        }

        Session session = SceneAs<Level>().Session;
        int currentIndex = GetColorDashBlockIndex(session);
        int nextIndex = (currentIndex + 1) % 2;
        SetColorDashBlockIndex(session, nextIndex);

        foreach (ColorDashBlock colorDashBlock in Scene.Tracker.GetEntities<ColorDashBlock>())
            colorDashBlock.UpdateState(playEffects: true);
    }

    public void UpdateState(bool playEffects = false)
    {
        int currentIndex = GetColorDashBlockIndex(SceneAs<Level>().Session);
        activated = index == currentIndex;

        if (groupLeader && activated && !Collidable)
        {
            bool canActivate = BlockedCheck();
            if (canActivate)
            {
                foreach (ColorDashBlock colorDashBlock in group)
                {
                    colorDashBlock.Collidable = true;
                    colorDashBlock.EnableStaticMovers();
                }

                if (playEffects)
                    wiggler.Start();
            }
        }
        else if (!activated && Collidable)
        {
            Collidable = false;
            DisableStaticMovers();

            if (playEffects)
            {
                for (int x = 0; x < Width; x += 8)
                for (int y = 0; y < Height; y += 8)
                {
                    Vector2 particlePos = Position + new Vector2(x + 4, y + 4);

                    SceneAs<Level>().ParticlesFG.Emit(P_Shatter, 1, particlePos, Vector2.One * 4f, color, (particlePos - Center).Angle());
                }
            }
        }

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        Depth = Collidable ? Depths.Player - 10 : (activated ? 9870 : 9880);

        foreach (StaticMover staticMover in staticMovers)
            staticMover.Entity.Depth = Depth + 1;

        lightOcclude.Visible = Collidable;

        foreach (Image image in solidImages)
            image.Visible = Collidable;
        foreach (Image image in pressedImages)
            image.Visible = !Collidable;

        if (groupLeader)
        {
            // shake when blocked from activating
            if (activated && !Collidable)
                StartShaking();
            else
                StopShaking();

            Vector2 scale = new Vector2(1f + wiggler.Value * 0.05f * wigglerScaler.X, 1f + wiggler.Value * 0.15f * wigglerScaler.Y);
            foreach (ColorDashBlock colorDashBlock in group)
            {
                colorDashBlock.shakeAmount = shakeAmount;

                foreach (Image image in colorDashBlock.allImages)
                    image.Scale = scale;

                foreach (StaticMover staticMover in colorDashBlock.staticMovers)
                {
                    if (staticMover.Entity is not Spikes spikes)
                        continue;

                    foreach (Component component in spikes.Components)
                    {
                        if (component is Image image)
                            image.Scale = scale;
                    }
                }
            }
        }
    }

    private bool BlockedCheck()
    {
        Player player = Scene.Tracker.GetEntity<Player>();
        if (player is null)
            return true;

        foreach (ColorDashBlock colorDashBlock in group)
        {
            if (colorDashBlock.CollideCheck(player))
                return false;

            // inconsistent with cassette blocks & color switch blocks but feels nice
            foreach (StaticMover staticMover in colorDashBlock.staticMovers)
            {
                if (staticMover.Entity is Spikes spikes && spikes.CollideCheck(player))
                    return false;
            }
        }

        return true;
    }

    public override void OnShake(Vector2 amount)
    {
        base.OnShake(amount);

        if (groupLeader)
        {
            foreach (ColorDashBlock colorDashBlock in group)
            {
                if (!colorDashBlock.groupLeader)
                    colorDashBlock.OnShake(amount);
            }
        }
    }

    public override void Update()
    {
        base.Update();

        UpdateState(playEffects: true);

        if (activated && Scene.OnInterval(0.1f))
            noiseSeed++;
    }

    #endregion

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;

        uint seed = noiseSeed;
        DrawNoise(ref seed);

        base.Render();

        Position = position;
    }

    // taken and slighly modified from PlaybackBillboard
    private void DrawNoise(ref uint seed)
    {
        Vector2 scale = new Vector2(1f + wiggler.Value * 0.05f * wigglerScaler.X, 1f + wiggler.Value * 0.15f * wigglerScaler.Y);

        Vector2 rectPosition = groupOrigin;
        Vector2 rectOrigin = groupOrigin - Position;
        int rectX = (int)Math.Round(rectPosition.X - rectOrigin.X * scale.X);
        int rectY = (int)Math.Round(rectPosition.Y - rectOrigin.Y * scale.Y);
        int rectW = (int)Math.Round(rectPosition.X + (Width - rectOrigin.X) * scale.X) - rectX;
        int rectH = (int)Math.Round(rectPosition.Y + (Height - rectOrigin.Y) * scale.Y) - rectY;
        Rectangle noiseBounds = new Rectangle(rectX, rectY, rectW, rectH);

        string texturePath = $"objects/SorbetHelper/colorDashBlock/{(Collidable ? "solidNoise" : "pressedNoise")}";
        MTexture noiseTexture = GFX.Game[texturePath];
        Vector2 noiseRandPos = new Vector2(PseudoRandRange(ref seed, 0f, noiseTexture.Width / 2f), PseudoRandRange(ref seed, 0f, noiseTexture.Height / 2f));
        Vector2 noiseOriginOffset = Position - new Vector2(noiseBounds.X, noiseBounds.Y);
        Vector2 noiseHalfSize = new Vector2(noiseTexture.Width, noiseTexture.Height) / 2f;

        for (float x = 0f; x < noiseBounds.Width; x += noiseHalfSize.X)
        {
            float sourceWidth = Math.Min(noiseBounds.Width - x, noiseHalfSize.X);
            for (float y = 0f; y < noiseBounds.Height; y += noiseHalfSize.Y)
            {
                float sourceHeight = Math.Min(noiseBounds.Height - y, noiseHalfSize.Y);
                int sourceX = (int)(noiseTexture.ClipRect.X + noiseRandPos.X + noiseOriginOffset.X);
                int sourceY = (int)(noiseTexture.ClipRect.Y + noiseRandPos.Y + noiseOriginOffset.Y);
                Rectangle sourceRect = new Rectangle(sourceX, sourceY, (int)sourceWidth, (int)sourceHeight);
                Draw.SpriteBatch.Draw(noiseTexture.Texture.Texture_Safe, new Vector2(noiseBounds.X + x, noiseBounds.Y + y), sourceRect, color);
            }
        }

        switch (index)
        {
            case 0:
                for (int y = noiseBounds.Y + (int)(noiseOriginOffset.Y % 2); y < noiseBounds.Bottom; y += 2)
                {
                    float alpha = 0.05f + (1f + (float)Math.Sin(y / 16f + Scene.TimeActive * 2f)) / 2f * 0.2f;
                    Draw.Rect(noiseBounds.X, y, noiseBounds.Width, 1f, Color.Black * alpha);
                }

                break;
            case 1:
                for (int x = noiseBounds.X + (int)(noiseOriginOffset.X % 2); x < noiseBounds.Right; x += 2)
                {
                    float alpha = 0.05f + (1f + (float)Math.Sin(x / 16f + Scene.TimeActive * 2f)) / 2f * 0.2f;
                    Draw.Rect(x, noiseBounds.Y, 1f, noiseBounds.Height, Color.Black * alpha);
                }

                break;
        }
    }

    private static uint PseudoRand(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        return seed;
    }

    private static float PseudoRandRange(ref uint seed, float min, float max)
        => min + PseudoRand(ref seed) % 1000 / 1000f * (max - min);
}
