using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Entities;

// a *lot* taken from cassette blocks and color switches (https://github.com/CommunalHelper/VortexHelper/blob/dev/Code/Entities/ColorSwitch.cs) + switch blocks (https://github.com/CommunalHelper/VortexHelper/blob/dev/Code/Entities/SwitchBlock.cs) from vortex helper

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

    public DashSwitchBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
        SurfaceSoundIndex = 35;
        Index = data.Int("index", 0);
        color = Index switch {
            0 => Calc.HexToColor("00c2c2"),
            1 => Calc.HexToColor("fca700"),
            _ => throw new System.NotImplementedException($"Index {Index} is not supported!"),
        };

        Add(lightOcclude = new LightOcclude(0.8f));

        OnDashCollide = OnDashed;
    }

    private DashCollisionResults OnDashed(Player player, Vector2 dir) {
        if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>())
            return DashCollisionResults.NormalCollision;

        // gravity helper support
        bool gravityInverted = GravityHelperImports.IsPlayerInverted?.Invoke() ?? false;

        // make wallbouncing easier if dash corner correction is enabled
        if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f)) {
            return DashCollisionResults.NormalCollision;
        }

        Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
        SceneAs<Level>().DirectionalShake(dir, 0.25f);
        Audio.Play("event:/game/03_resort/forcefield_bump", Center);
        Audio.Play("event:/game/05_mirror_temple/button_activate", Center);
        Switch();

        return DashCollisionResults.Rebound;
    }

    public void Switch() {
        var session = (Scene as Level).Session;
        var currentIndex = GetDashSwitchBlockIndex(session);
        var nextIndex = (currentIndex + 1) % 2;
        SetDashSwitchBlockIndex(session, nextIndex);

        foreach (var dashSwitchBlock in Scene.Tracker.GetEntities<DashSwitchBlock>().Cast<DashSwitchBlock>())
            dashSwitchBlock.UpdateState(nextIndex);
    }

    private bool BlockedCheck() {
        var player = Scene.Tracker.GetEntity<Player>();
        if (player is null)
            return true;

        foreach (var dashSwitchBlock in group) {
            if (dashSwitchBlock.CollideCheck(player))
                return false;
        }

        return true;
    }

    public void UpdateState(int index, bool silent = false) {
        Activated = Index == index;

        if (groupLeader && Activated && !Collidable) {
            bool canActivate = BlockedCheck();
            if (canActivate) {
                foreach (var dashSwitchBlock in group) {
                    dashSwitchBlock.Collidable = true;
                    dashSwitchBlock.EnableStaticMovers();

                    if (!silent)
                        dashSwitchBlock.ActivateEffects();
                }

                if (!silent)
                    wiggler.Start();
            }
        } else if (!Activated && Collidable) {
            Collidable = false;
            DisableStaticMovers();

            if (!silent)
                DeactivateEffects();
        }

        UpdateVisualState();
    }

    private void UpdateVisualState() {
        Depth = Collidable ? Depths.Player - 10 : 9880;

        foreach (StaticMover staticMover in staticMovers)
            staticMover.Entity.Depth = Depth + 1;

        lightOcclude.Visible = Collidable;

        foreach (var image in solidImages)
            image.Visible = Collidable;
        foreach (var image in pressedImages)
            image.Visible = !Collidable;

        if (groupLeader) {
            var scale = new Vector2(1f + wiggler.Value * 0.05f * wigglerScaler.X, 1f + wiggler.Value * 0.15f * wigglerScaler.Y);
            foreach (var dashSwitchBlock in group) {
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

    private void ActivateEffects() {

    }

    private void DeactivateEffects() {
        var particles = (Scene as Level).Particles;

        var particle = new ParticleType(ParticleTypes.VentDust) {
            Color = color,
            Color2 = Color.White,
            ColorMode = ParticleType.ColorModes.Fade
        };

        for (int x = 0; x < Width / 8f; x++) {
            for (int y = 0; y < Height / 8f; y++) {
                particles.Emit(particle, 1, Position + new Vector2(x * 8 + 4, y * 8 + 4), Vector2.One * 4f, color, Calc.Random.NextAngle());
            }
        }
    }

    public override void Update() {
        base.Update();

        UpdateState(GetDashSwitchBlockIndex((Scene as Level).Session));
    }

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

        UpdateState(GetDashSwitchBlockIndex((Scene as Level).Session), silent: true);
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
        var atlasSubtextures = GFX.Game.GetAtlasSubtextures("objects/cassetteblock/pressed");
        pressedImages.Add(CreateImage(x, y, tx, ty, atlasSubtextures[Index % atlasSubtextures.Count]));
        solidImages.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/cassetteblock/solid"]));
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