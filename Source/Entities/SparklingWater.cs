using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/SparklingWater")]
[Tracked]
[TrackedAs(typeof(Water))]
public class SparklingWater : Water {
    private class SparklingSurface : Surface {
        // size of the "distance from center" gradient used by the shader
        public const int SurfaceMaxHeight = 16;
        // surfaces have their positions 8px beneath their edge
        public const int PositionYOffset = 8;

        private readonly int surfaceHeight;

        private readonly int edgeStartIndex, borderStartIndex;

        public SparklingSurface(Vector2 position, Vector2 outwards, float width, float bodyHeight, ref VertexPositionColor[] mesh) : base(position, outwards, width, bodyHeight) {
            Rays.Clear();
            this.mesh = null;

            surfaceHeight = Math.Clamp(BodyHeight, 8, SurfaceMaxHeight);
            int segments = (int)(width / Resolution);

            edgeStartIndex = mesh.Length;
            borderStartIndex = mesh.Length + segments * 6;
            Array.Resize(ref mesh, mesh.Length + segments * 6 * 2);

            Color edgeColor = outwards.Y <= 0 ? SparklingWaterRenderer.TopEdgeMaskColor : SparklingWaterRenderer.BottomEdgeMaskColor;
            Color fillColor = Color.Lerp(edgeColor, SparklingWaterRenderer.FillMaskColor, (float)surfaceHeight / (float)SurfaceMaxHeight);

            // edge colors
            for (int i = edgeStartIndex; i < edgeStartIndex + segments * 6; i += 6) {
                mesh[i + 0].Color = edgeColor;
                mesh[i + 1].Color = edgeColor;
                mesh[i + 2].Color = fillColor;
                mesh[i + 3].Color = edgeColor;
                mesh[i + 4].Color = fillColor;
                mesh[i + 5].Color = fillColor;
            }

            // border colors
            for (int i = borderStartIndex; i < borderStartIndex + segments * 6; i++)
                mesh[i].Color = SparklingWaterRenderer.OutlineMaskColor;
        }

        public void Update(Rectangle cameraRect, VertexPositionColor[] mesh) {
            timer += Engine.DeltaTime;

            // update ripples
            for (int i = Ripples.Count - 1; i >= 0; i--) {
                Ripple ripple = Ripples[i];

                if (ripple.Percent > 1f) {
                    Ripples.RemoveAt(i);
                } else {
                    ripple.Position += ripple.Speed * Engine.DeltaTime;
                    if (ripple.Position < 0f || ripple.Position > Width) {
                        ripple.Speed = 0f - ripple.Speed;
                        ripple.Position = Calc.Clamp(ripple.Position, 0f, Width);
                    }

                    ripple.Percent += Engine.DeltaTime / ripple.Duration;
                }
            }

            (bool surfaceOnCamera, int visibleStart, int visibleEnd) = GetVisibility(cameraRect);

            if (!surfaceOnCamera)
                return;

            // update mesh
            int edgeIndex = edgeStartIndex + 6 * visibleStart / Resolution;
            int borderIndex = borderStartIndex + 6 * visibleStart / Resolution;

            Vector2 perpendicular = Outwards.Perpendicular();

            int surfacePos = visibleStart;
            Vector2 worldPos = Position + perpendicular * (-Width / 2 + surfacePos);
            float height = GetSurfaceHeight(surfacePos);
            while (surfacePos < visibleEnd) {
                int surfacePosNext = Math.Min(surfacePos + Resolution, Width);
                Vector2 worldPosNext = Position + perpendicular * (-Width / 2 + surfacePosNext);
                float heightNext = GetSurfaceHeight(surfacePosNext);

                // edge
                mesh[edgeIndex + 0].Position = new Vector3(worldPos + Outwards * height, 0f);
                mesh[edgeIndex + 1].Position = new Vector3(worldPosNext + Outwards * heightNext, 0f);
                mesh[edgeIndex + 2].Position = new Vector3(worldPos - Outwards * (surfaceHeight - PositionYOffset), 0f);
                mesh[edgeIndex + 3].Position = new Vector3(worldPosNext + Outwards * heightNext, 0f);
                mesh[edgeIndex + 4].Position = new Vector3(worldPosNext - Outwards * (surfaceHeight - PositionYOffset), 0f);
                mesh[edgeIndex + 5].Position = new Vector3(worldPos - Outwards * (surfaceHeight - PositionYOffset), 0f);
                // border
                mesh[borderIndex + 0].Position = new Vector3(worldPos + Outwards * (height + 1f), 0f);
                mesh[borderIndex + 1].Position = new Vector3(worldPosNext + Outwards * (heightNext + 1f), 0f);
                mesh[borderIndex + 2].Position = new Vector3(worldPos + Outwards * height, 0f);
                mesh[borderIndex + 3].Position = new Vector3(worldPosNext + Outwards * (heightNext + 1f), 0f);
                mesh[borderIndex + 4].Position = new Vector3(worldPosNext + Outwards * heightNext, 0f);
                mesh[borderIndex + 5].Position = new Vector3(worldPos + Outwards * height, 0f);

                surfacePos += Resolution;
                worldPos = worldPosNext;
                height = heightNext;
                edgeIndex += 6;
                borderIndex += 6;
            }
        }

        private (bool surfaceOnCamera, int visibleStart, int visibleEnd) GetVisibility(Rectangle cameraRect) {
            // this sucks sososo bad and awful and terrible but im so eepyyy weh
            // Need to remember to rewrite this at some point
            bool bottomSurface = Outwards.Y > 0f;

            int left = (int)(Position.X - Width / 2f);
            int right = (int)(Position.X + Width / 2f);

            int top, bottom;
            if (bottomSurface) {
                top = (int)(Position.Y - surfaceHeight + PositionYOffset);
                bottom = (int)(Position.Y + PositionYOffset);
            } else {
                top = (int)(Position.Y - PositionYOffset);
                bottom = (int)(Position.Y + surfaceHeight - PositionYOffset);
            }

            bool surfaceOnCamera = left < cameraRect.Right && right > cameraRect.Left
                                   && top < cameraRect.Bottom && bottom > cameraRect.Top;

            if (!surfaceOnCamera)
                return (false, 0, 0);

            int visibleStart, visibleEnd;
            if (bottomSurface) {
                visibleStart = right - Math.Clamp(cameraRect.Right, left, right);
                visibleEnd = right - Math.Clamp(cameraRect.Left, left, right);
            } else {
                visibleStart = Math.Clamp(cameraRect.Left, left, right) - left;
                visibleEnd = Math.Clamp(cameraRect.Right, left, right) - left;
            }

            visibleStart = (int)MathF.Floor((float)visibleStart / (float)Resolution) * Resolution;
            visibleEnd = (int)MathF.Ceiling((float)visibleEnd / (float)Resolution) * Resolution;

            return (true, visibleStart, visibleEnd);
        }
    }

    private readonly bool collidable;
    private readonly bool canSplash;

    private readonly VertexPositionColor[] mesh;

    private new SparklingSurface TopSurface => base.TopSurface as SparklingSurface;
    private new SparklingSurface BottomSurface => base.BottomSurface as SparklingSurface;

    public bool VisibleOnCamera = true;

    private static readonly ParticleType P_SparklingSplash = new ParticleType() {
        Source = GFX.Game["particles/feather"],
        FadeMode = ParticleType.FadeModes.Linear,
        Acceleration = new Vector2(0f, 150f),
        Size = 1f, SizeRange = 1f / 3f, ScaleOut = true,
        SpeedMin = 30f, SpeedMax = 70f, SpeedMultiplier = 0.98f,
        Direction = -MathF.PI / 2f, DirectionRange = MathF.PI / 4f,
        RotationMode = ParticleType.RotationModes.Random,
        LifeMin = 0.4f, LifeMax = 0.6f
    };

    public SparklingWater(Vector2 position, float width, float height, bool topSurface, bool bottomSurface, int depth = -9999, bool collidable = true, bool canSplash = true)
        : base(position, false, false, width, height) {
        Remove(Get<DisplacementRenderHook>());
        Depth = depth;

        this.collidable = collidable; // the actual `Entity.Collidable` field is set later to allow waterfalls to collide first
        this.canSplash = canSplash;

        mesh = new VertexPositionColor[6];

        if (topSurface) {
            base.TopSurface = new SparklingSurface(Position + new Vector2(width / 2f, SparklingSurface.PositionYOffset), new Vector2(0f, -1f), width, height, ref mesh);
            Surfaces.Add(TopSurface);
            fill.Y += SparklingSurface.SurfaceMaxHeight;
            fill.Height -= SparklingSurface.SurfaceMaxHeight;
        }

        if (bottomSurface) {
            base.BottomSurface = new SparklingSurface(Position + new Vector2(width / 2f, height - SparklingSurface.PositionYOffset), new Vector2(0f, 1f), width, height, ref mesh);
            Surfaces.Add(BottomSurface);
            fill.Height -= SparklingSurface.SurfaceMaxHeight;
        }

        if (fill.Height > 0) {
            const int fillStartIndex = 0;
            for (int i = fillStartIndex; i < fillStartIndex + 6; i++)
                mesh[i].Color = SparklingWaterRenderer.FillMaskColor;

            float fillLeft = X + fill.X, fillRight  = X + fill.X + fill.Width;
            float fillTop  = Y + fill.Y, fillBottom = Y + fill.Y + fill.Height;
            mesh[fillStartIndex + 0].Position = new Vector3(fillLeft, fillTop, 0f);
            mesh[fillStartIndex + 1].Position = new Vector3(fillRight, fillTop, 0f);
            mesh[fillStartIndex + 2].Position = new Vector3(fillLeft, fillBottom, 0f);
            mesh[fillStartIndex + 3].Position = new Vector3(fillRight, fillTop, 0f);
            mesh[fillStartIndex + 4].Position = new Vector3(fillRight, fillBottom, 0f);
            mesh[fillStartIndex + 5].Position = new Vector3(fillLeft, fillBottom, 0f);
        }
    }

    public SparklingWater(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height,
               data.Bool("topSurface", true), data.Bool("bottomSurface", false),
               data.Int("depth", -9999), data.Bool("collidable", true), data.Bool("canSplash", true)) { }

    private void TrackSelf() => SparklingWaterRenderer.GetRenderer(Scene, Depth).Track(this);
    private void UntrackSelf() => SparklingWaterRenderer.GetRenderer(Scene, Depth).Untrack(this);

    [MonoModLinkTo("Monocle.Entity", "System.Void Added(Monocle.Scene)")]
    private extern void base_Added(Scene scene);
    public override void Added(Scene scene) {
        base_Added(scene);
        TrackSelf();
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        Collidable = collidable;
    }

    public override void Removed(Scene scene) {
        UntrackSelf();
        base.Removed(scene);
    }

    [MonoModLinkTo("Monocle.Entity", "System.Void Update()")]
    private extern void base_Update();
    public override void Update() {
        // can't use base.Update() normally since that would call the vanilla surface update methods
        base_Update();

        // update visibility
        Camera camera = SceneAs<Level>().Camera;
        const int visibilityBuffer = 24;
        Rectangle cameraRect = new Rectangle((int)camera.X - visibilityBuffer, (int)camera.Y - visibilityBuffer,
                                             camera.Width + visibilityBuffer, camera.Height + visibilityBuffer);

        VisibleOnCamera = Left < cameraRect.Right && Right > cameraRect.Left
                          && Top < cameraRect.Bottom && Bottom > cameraRect.Top;

        // update surfaces
        foreach (SparklingSurface surface in Surfaces)
            surface.Update(cameraRect, mesh);

        // ripples & splash sfx
        if (canSplash) {
            foreach (WaterInteraction waterInteraction in Scene.Tracker.GetComponents<WaterInteraction>()) {
                Vector2 interactionCenter = waterInteraction.AbsoluteCenter;
                Entity interactionEntity = waterInteraction.Entity;

                bool wasInside = contains.Contains(waterInteraction);
                bool isInside = waterInteraction.Check(this);

                if (wasInside != isInside) {
                    DoSplash(interactionCenter, interactionEntity.Width, 1f);

                    bool isDashing = waterInteraction.IsDashing();
                    int deepParam = (interactionCenter.Y < Center.Y && !Scene.CollideCheck<Solid>(new Vector2(waterInteraction.Bounds.Left, Top + 8f), new Vector2(waterInteraction.Bounds.Right, Top + 8f))) ? 1 : 0;
                    if (wasInside) {
                        if (isDashing)
                            Audio.Play("event:/char/madeline/water_dash_out", interactionCenter, "deep", deepParam);
                        else
                            Audio.Play("event:/char/madeline/water_out", interactionCenter, "deep", deepParam);

                        waterInteraction.DrippingTimer = 2f;
                    } else {
                        if (isDashing && deepParam == 1)
                            Audio.Play("event:/char/madeline/water_dash_in", interactionCenter, "deep", deepParam);
                        else
                            Audio.Play("event:/char/madeline/water_in", interactionCenter, "deep", deepParam);

                        waterInteraction.DrippingTimer = 0f;
                    }

                    if (wasInside)
                        contains.Remove(waterInteraction);
                    else
                        contains.Add(waterInteraction);
                }

                if (BottomSurface is not null && interactionEntity is Player) {
                    if (isInside && interactionEntity.Y > Bottom - 8f) {
                        playerBottomTension ??= BottomSurface.SetTension(interactionEntity.Position, 0f);

                        playerBottomTension.Position = BottomSurface.GetPointAlong(interactionEntity.Position);
                        playerBottomTension.Strength = Calc.ClampedMap(interactionEntity.Y, Bottom - 8f, Bottom + 4f) * 0.25f;
                    } else if (playerBottomTension is not null) {
                        BottomSurface.RemoveTension(playerBottomTension);
                        playerBottomTension = null;
                    }
                }
            }
        }
    }

    public void DoSplash(Vector2 position, float width, float strength) {
        bool onTop = position.Y <= CenterY;
        Surface surface = onTop ? TopSurface : BottomSurface;
        if (surface is null)
            return;

        surface.DoRipple(position, strength);
        const int rippleDistance = 48;
        for (int x = rippleDistance; x < width / 2f; x += rippleDistance) {
            surface.DoRipple(position with { X = position.X + x }, strength);
            surface.DoRipple(position with { X = position.X - x }, strength);
        }

        ParticleSystem particles = SceneAs<Level>().ParticlesFG;
        Color splashColor = SparklingWaterRenderer.GetSettings(Scene, Depth).OutlineColor;
        float splashY = onTop ? Top : Bottom;
        float splashDirection = onTop ? -MathF.PI / 2f : MathF.PI / 2f;
        for (int x = 0; x < width; x += 4)
            particles.Emit(P_SparklingSplash, 2, new Vector2(position.X - width / 2f + x + 2f, splashY), new Vector2(8f, 2f), splashColor, splashDirection);
    }

    public override void Render() { }

    public void DrawMesh() {
        Engine.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, mesh, 0, mesh.Length / 3);
    }
}
