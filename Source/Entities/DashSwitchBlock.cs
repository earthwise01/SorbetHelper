using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Entities;

// heavily inspired by color switches (https://github.com/CommunalHelper/VortexHelper/blob/dev/Code/Entities/ColorSwitch.cs) + switch blocks (https://github.com/CommunalHelper/VortexHelper/blob/dev/Code/Entities/SwitchBlock.cs) from vortex helper
// code also taken from vanilla cassette blocks, and playback billboards (for rendering)

[CustomEntity("SorbetHelper/DashSwitchBlock")]
[Tracked]
public class DashSwitchBlock : Solid {
    public readonly int Index;
    private Color color;

    public bool Activated;

    private List<DashSwitchBlock> group;
    private bool groupLeader;
    private Vector2 groupOrigin;

    private Wiggler wiggler;
    private Vector2 wigglerScaler;
    private readonly LightOcclude lightOcclude;

    private readonly List<Image> pressedImages = [];
    private readonly List<Image> solidImages = [];
    private readonly List<Image> allImages = [];
    private uint noiseSeed;

    public DashSwitchBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
        SurfaceSoundIndex = 35;
        Index = data.Int("index", 0);
        color = Index switch {
            0 => Calc.HexToColor("00c2c2"),
            1 => Calc.HexToColor("fca700"),
            _ => throw new NotImplementedException($"Index {Index} is not supported!"),
        };

        Add(lightOcclude = new LightOcclude(0.8f));

        OnDashCollide = OnDashed;

        // switch if communal helper dream tunnel dashed
        var switchOnDreamTunnel = data.Bool("switchOnDreamTunnel", false);
        if (switchOnDreamTunnel && CommunalHelperDashStateImports.DreamTunnelInteraction is not null) {
            void onEnter(Player player) { }
            void onExit(Player player) {
                Switch();
                Add(new Coroutine(GlitchSequence(), true));
            }

            static IEnumerator GlitchSequence() {
                Glitch.Value = 0.22f;
                while (Glitch.Value > 0.0f) {
                    Glitch.Value -= 0.5f * Engine.DeltaTime;
                    yield return null;
                }
                Glitch.Value = 0.0f;
            }

            Add(CommunalHelperDashStateImports.DreamTunnelInteraction(onEnter, onExit));
        }
    }

    private DashCollisionResults OnDashed(Player player, Vector2 dir) {
        if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>())
            return DashCollisionResults.NormalCollision;

        SceneAs<Level>().DirectionalShake(dir, 0.25f);
        Switch();

        return DashCollisionResults.Rebound;
    }

    public void Switch(bool playSfx = true) {
        if (playSfx) {
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
            Audio.Play("event:/game/03_resort/forcefield_bump", Center);
            Audio.Play("event:/game/05_mirror_temple/button_activate", Center);
        }

        var session = (Scene as Level).Session;
        var currentIndex = GetDashSwitchBlockIndex(session);
        var nextIndex = (currentIndex + 1) % 2;
        SetDashSwitchBlockIndex(session, nextIndex);

        foreach (var dashSwitchBlock in Scene.Tracker.GetEntities<DashSwitchBlock>().Cast<DashSwitchBlock>())
            dashSwitchBlock.UpdateState(playEffects: true);
    }

    private bool BlockedCheck() {
        var player = Scene.Tracker.GetEntity<Player>();
        if (player is null)
            return true;

        foreach (var dashSwitchBlock in group) {
            if (dashSwitchBlock.CollideCheck(player))
                return false;

            // sometime i wonder if this rly should've just been a map specific entity
            // idk hopefully this doesn't end up causing issues in the future even though most other similar blocks dont do this
            foreach (var staticMover in dashSwitchBlock.staticMovers) {
                if (staticMover.Entity is Spikes spikes && spikes.CollideCheck(player))
                    return false;
            }
        }

        return true;
    }

    public void UpdateState(bool playEffects = false) {
        var currentIndex = GetDashSwitchBlockIndex((Scene as Level).Session);
        Activated = Index == currentIndex;

        if (groupLeader && Activated && !Collidable) {
            bool canActivate = BlockedCheck();
            if (canActivate) {
                foreach (var dashSwitchBlock in group) {
                    dashSwitchBlock.Collidable = true;
                    dashSwitchBlock.EnableStaticMovers();

                    if (playEffects)
                        dashSwitchBlock.ActivateEffects();
                }

                if (playEffects)
                    wiggler.Start();
            }
        } else if (!Activated && Collidable) {
            Collidable = false;
            DisableStaticMovers();

            if (playEffects)
                DeactivateEffects();
        }

        UpdateVisualState();
    }

    private void UpdateVisualState() {
        Depth = Collidable ? Depths.Player - 10 : (Activated ? 9870 : 9880);

        foreach (StaticMover staticMover in staticMovers)
            staticMover.Entity.Depth = Depth + 1;

        lightOcclude.Visible = Collidable;

        foreach (var image in solidImages)
            image.Visible = Collidable;
        foreach (var image in pressedImages)
            image.Visible = !Collidable;

        if (groupLeader) {
            // shake when blocked from activating
            if (Activated && !Collidable)
                StartShaking();
            else
                StopShaking();

            var scale = new Vector2(1f + wiggler.Value * 0.05f * wigglerScaler.X, 1f + wiggler.Value * 0.15f * wigglerScaler.Y);
            foreach (var dashSwitchBlock in group) {
                dashSwitchBlock.shakeAmount = shakeAmount;

                foreach (var image in dashSwitchBlock.allImages)
                    image.Scale = scale;

                foreach (var staticMover in dashSwitchBlock.staticMovers)
                    if (staticMover.Entity is Spikes spikes)
                        foreach (Component component in spikes.Components)
                            if (component is Image image)
                                image.Scale = scale;
            }
        }
    }

    public override void OnShake(Vector2 amount) {
        base.OnShake(amount);

        if (groupLeader) {
            foreach (var dashSwitchBlock in group) {
                if (!dashSwitchBlock.groupLeader) {
                    dashSwitchBlock.OnShake(amount);
                }
            }
        }
    }

    private void ActivateEffects() {

    }

    private void DeactivateEffects() {
        Level level = Scene as Level;

        var particle = new ParticleType(Lightning.P_Shatter) {
            Color = Color.Lerp(color, Color.White, 0.75f),
            Color2 = Color.White,
            ColorMode = ParticleType.ColorModes.Fade
        };

        for (int x = 0; x < Width / 8f; x++) {
            for (int y = 0; y < Height / 8f; y++) {
                var position = Position + new Vector2(x * 8 + 5, y * 8 + 3);

                level.ParticlesFG.Emit(particle, 1, position, Vector2.One * 4f, color, (position - Center).Angle());
            }
        }
    }

    public override void Update() {
        base.Update();

        UpdateState(playEffects: true);

        if (Activated && Scene.OnInterval(0.1f))
            noiseSeed++;
    }

    public override void Render() {
        var position = Position;
        Position += Shake;

        uint seed = noiseSeed;
        DrawNoise(ref seed);

        base.Render();

        Position = position;
    }

    // taken and slighly modified from PlaybackBillboard
    private void DrawNoise(ref uint seed) {
        // todo: make this scale properly with the bounce effect
        Rectangle bounds = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
        string texture = Collidable ? "objects/SorbetHelper/dashSwitchBlock/solidNoise" : "objects/SorbetHelper/dashSwitchBlock/pressedNoise";

        MTexture noiseTexture = GFX.Game[texture];
        Vector2 noiseRandPos = new Vector2(PseudoRandRange(ref seed, 0f, noiseTexture.Width / 2), PseudoRandRange(ref seed, 0f, noiseTexture.Height / 2));
        Vector2 noiseHalfSize = new Vector2(noiseTexture.Width, noiseTexture.Height) / 2f;
        for (float x = 0f; x < bounds.Width; x += noiseHalfSize.X) {
            float sourceWidth = Math.Min(bounds.Width - x, noiseHalfSize.X);
            for (float y = 0f; y < bounds.Height; y += noiseHalfSize.Y) {
                float sourceHeight = Math.Min(bounds.Height - y, noiseHalfSize.Y);
                int sourceX = (int)(noiseTexture.ClipRect.X + noiseRandPos.X);
                int sourceY = (int)(noiseTexture.ClipRect.Y + noiseRandPos.Y);
                Rectangle sourceRect = new Rectangle(sourceX, sourceY, (int)sourceWidth, (int)sourceHeight);
                Draw.SpriteBatch.Draw(noiseTexture.Texture.Texture_Safe, new Vector2(bounds.X + x, bounds.Y + y), sourceRect, color);
            }
        }

        switch (Index) {
            case 0:
                for (int y = bounds.Y; (float)y < bounds.Bottom; y += 2) {
                    float alpha = 0.05f + (1f + (float)Math.Sin(y / 16f + Scene.TimeActive * 2f)) / 2f * 0.2f;
                    Draw.Line(bounds.X, y, bounds.X + bounds.Width, y, Color.Black * alpha);
                }
                break;
            case 1:
                for (int x = bounds.X; (float)x < bounds.Right; x += 2) {
                    float alpha = 0.05f + (1f + (float)Math.Sin(x / 16f + Scene.TimeActive * 2f)) / 2f * 0.2f;
                    Draw.Line(x + 1, bounds.Y, x + 1, bounds.Y + bounds.Height, Color.Black * alpha);
                }
                break;
        }
    }

    private static uint PseudoRand(ref uint seed) {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        return seed;
    }

    private static float PseudoRandRange(ref uint seed, float min, float max) =>
        min + PseudoRand(ref seed) % 1000 / 1000f * (max - min);

    // -- SETUP --

    public override void Awake(Scene scene) {
        base.Awake(scene);

        // setup colours for static movers (taken from cassette blocks)
        var fadeColor = Calc.HexToColor("667da5");
        var disabledColor = new Color(fadeColor.R / 255f * (color.R / 255f), fadeColor.G / 255f * (color.G / 255f), fadeColor.B / 255f * (color.B / 255f), 1f);
        foreach (StaticMover staticMover in staticMovers) {
            if (staticMover.Entity is Spikes spikes) {
                spikes.EnabledColor = color;
                spikes.DisabledColor = disabledColor;
                spikes.VisibleWhenDisabled = true;
                spikes.SetSpikeColor(color);
            }

            if (staticMover.Entity is Spring spring) {
                spring.DisabledColor = disabledColor;
                spring.VisibleWhenDisabled = true;
            }
        }

        // setup group (also taken from cassette blocks)
        if (group is null) {
            groupLeader = true;
            group = [this];
            FindInGroup(this);

            float groupLeft = float.MaxValue;
            float groupRight = float.MinValue;
            float groupTop = float.MaxValue;
            float groupBottom = float.MinValue;
            foreach (var dashSwitchBlock in group) {
                if (dashSwitchBlock.Left < groupLeft)
                    groupLeft = dashSwitchBlock.Left;
                if (dashSwitchBlock.Right > groupRight)
                    groupRight = dashSwitchBlock.Right;
                if (dashSwitchBlock.Bottom > groupBottom)
                    groupBottom = dashSwitchBlock.Bottom;
                if (dashSwitchBlock.Top < groupTop)
                    groupTop = dashSwitchBlock.Top;
            }

            groupOrigin = new Vector2((int)(groupLeft + (groupRight - groupLeft) / 2f), (int)groupBottom);
            wigglerScaler = new Vector2(Calc.ClampedMap(groupRight - groupLeft, 32f, 96f, 1f, 0.2f), Calc.ClampedMap(groupBottom - groupTop, 32f, 96f, 1f, 0.2f));
            Add(wiggler = Wiggler.Create(0.3f, 3f));

            foreach (var dashSwitchBlock in group) {
                dashSwitchBlock.wiggler = wiggler;
                dashSwitchBlock.wigglerScaler = wigglerScaler;
                dashSwitchBlock.groupOrigin = groupOrigin;
            }
        }

        // setup spike origins (do you get the idea yet)
        foreach (var staticMover in staticMovers) {
            if (staticMover.Entity is Spikes spikes) {
                spikes.SetOrigins(groupOrigin);
            }
        }

        // cassette block autotiling
        for (float x = Left; x < Right; x += 8f) {
            for (float y = Top; y < Bottom; y += 8f) {
                bool leftCheck = CheckForSame(x - 8f, y);
                bool rightCheck = CheckForSame(x + 8f, y);
                bool topCheck = CheckForSame(x, y - 8f);
                bool bottomCheck = CheckForSame(x, y + 8f);

                if (leftCheck && rightCheck && topCheck && bottomCheck) {
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
                } else if (leftCheck && rightCheck && !topCheck && bottomCheck)
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

    private void FindInGroup(DashSwitchBlock block) {
        foreach (var entity in Scene.Tracker.GetEntities<DashSwitchBlock>().Cast<DashSwitchBlock>()) {
            if (entity != this && entity != block && entity.Index == Index && (entity.CollideRect(new Rectangle((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) || entity.CollideRect(new Rectangle((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))) && !group.Contains(entity)) {
                group.Add(entity);
                FindInGroup(entity);
                entity.group = group;
            }
        }
    }

    private bool CheckForSame(float x, float y) {
        foreach (var dashSwitchBlock in Scene.Tracker.GetEntities<DashSwitchBlock>().Cast<DashSwitchBlock>()) {
            if (dashSwitchBlock.Index == Index && dashSwitchBlock.Collider.Collide(new Rectangle((int)x, (int)y, 8, 8))) {
                return true;
            }
        }

        return false;
    }

    private void SetImage(float x, float y, int tx, int ty) {
        var atlasSubtextures = GFX.Game.GetAtlasSubtextures("objects/SorbetHelper/dashSwitchBlock/pressed");
        pressedImages.Add(CreateImage(x, y, tx, ty, atlasSubtextures[Index % atlasSubtextures.Count]));
        solidImages.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/SorbetHelper/dashSwitchBlock/solid"]));
    }

    private Image CreateImage(float x, float y, int tx, int ty, MTexture tex) {
        var imageOffset = new Vector2(x - X, y - Y);
        var image = new Image(tex.GetSubtexture(tx * 8, ty * 8, 8, 8));
        var imageGroupOrigin = groupOrigin - Position;
        image.Origin = imageGroupOrigin - imageOffset;
        image.Position = imageGroupOrigin;
        image.Color = color;
        Add(image);
        allImages.Add(image);
        return image;
    }

    // -- SESSION HELPERS --

    public static int GetDashSwitchBlockIndex(Session session) => session.GetCounter("SorbetHelper_DashSwitchBlockIndex");
    public static void SetDashSwitchBlockIndex(Session session, int index) => session.SetCounter("SorbetHelper_DashSwitchBlockIndex", index);
}
