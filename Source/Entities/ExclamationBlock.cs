using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Components;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/ExclamationBlock")]
    [Tracked]
    public class ExclamationBlock : Solid {
        private float activationBuffer;
        private int amountExtended;
        private int targetExtended;
        private float activeTimer;
        private float liftboostLeniencyTimer;
        private Vector2 previousDirection;
        private Vector2 scale = Vector2.One;
        private Vector2 offset;
        public Vector2 Scale => scale;
        public Vector2 Offset => offset + Shake;

        private const float activationBufferTime = 0.15f;
        public bool CanActivate => targetExtended < segmentCount || (canRefreshTimer && !pauseTimerWhileExtending);
        public bool Extending => amountExtended < targetExtended;

        private List<EmptyBlock> segments;
        private List<Vector2> nodes;
        private List<int> targetNodes;
        private int segmentCount;
        private readonly SoundSource extendingSound;
        private readonly Coroutine blinkRoutine;
        private readonly string spriteDirectory;
        private readonly MTexture[,] activeNineSlice, emptyNineSlice;
        private readonly MTexture exclamationMarkTexture, emptyExclamationMarkTexture;
        private readonly bool drawOutline;
        private readonly Color outlineColor;
        private readonly Color smashParticleColor;
        private ExclamationBlockOutlineRenderer outlineRenderer;

        private readonly float moveSpeed;
        private readonly float activeTime;
        private readonly bool canRefreshTimer;
        private readonly bool pauseTimerWhileExtending;
        private readonly bool canWavedash;
        private readonly bool attachStaticMovers;
        private readonly bool disableFriction;
        private readonly bool dashActivated = true;
        private readonly bool explodeActivated = true;

        public bool VisibleOnCamera { get; private set; } = true;

        public static ParticleType P_SmashDust { get; private set; }

        public ExclamationBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
            moveSpeed = data.Float("moveSpeed", 160f);
            activeTime = data.Float("activeTime", 3f);
            canRefreshTimer = data.Bool("canRefreshTimer", false);
            pauseTimerWhileExtending = data.Bool("pauseTimerWhileExtending", true);
            canWavedash = data.Bool("canWavedash", false);
            attachStaticMovers = data.Bool("attachStaticMovers", true);
            disableFriction = data.Bool("disableFriction", false);

            spriteDirectory = data.Attr("spriteDirectory", "objects/SorbetHelper/exclamationBlock");
            drawOutline = data.Bool("drawOutline", true);
            outlineColor = data.HexColor("outlineColor", Calc.HexToColor("3d0200"));
            smashParticleColor = data.HexColor("smashParticleColor", Calc.HexToColor("ffd12e")) * 0.75f;

            activeNineSlice = Util.CreateNineSlice(GFX.Game[$"{spriteDirectory}/activeBlock"], 8, 8);
            emptyNineSlice = Util.CreateNineSlice(GFX.Game[$"{spriteDirectory}/emptyBlock"], 8, 8);
            exclamationMarkTexture = GFX.Game[$"{spriteDirectory}/exclamationMark"];
            emptyExclamationMarkTexture = GFX.Game[$"{spriteDirectory}/emptyExclamationMark"];
            SurfaceSoundIndex = SurfaceIndex.Girder;

            GenerateNodes(data.NodesWithPosition(offset), data.Attr("extendTo", "NextBlock").ToLower());

            OnDashCollide = OnDashed;
            Add(new MovingBlockHittable(Hit));
            Add(new Coroutine(Sequence()));
            Add(blinkRoutine = new Coroutine(BlinkRoutine()) { Active = false } ); // manually updated so that it pauses when the block is extending
            Add(extendingSound = new SoundSource());
            Add(new LightOcclude());
        }

        private void GenerateNodes(Vector2[] origNodes, string extendTo) {
            int nodeIndex = 0;
            var baseTargetNodes = (from node in extendTo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   where int.TryParse(node, out nodeIndex)
                                   select nodeIndex).ToList();

            List<Vector2> nodes = [];
            List<int> targetNodes = [];
            nodes.Add(origNodes[0]);

            Vector2 pos = nodes[0];
            for (int i = 1; i < origNodes.Length; i++) {
                Vector2 node = origNodes[i];

                int direction;
                while (!Collide.RectToPoint(pos.X, pos.Y, Width, Height, node)) {
                    direction = pos.X < node.X ? 1 : -1;
                    if (pos.X > node.X || pos.X + Width <= node.X) {
                        pos.X += Width * direction;
                        nodes.Add(pos);
                    }

                    direction = pos.Y < node.Y ? 1 : -1;
                    if (pos.Y > node.Y || pos.Y + Height <= node.Y) {
                        pos.Y += Height * direction;
                        nodes.Add(pos);
                    }
                }

                if (baseTargetNodes.Contains(i)) {
                    targetNodes.Add(nodes.Count - 1);
                }
            }

            if (extendTo != "nextblock")
                targetNodes.Add(nodes.Count);

            targetNodes.Sort();

            List<EmptyBlock> segments = [];
            for (int i = 0; i < nodes.Count; i++) {
                EmptyBlock block = new EmptyBlock(Position, Width, Height, spriteDirectory, fromExclamationBlock: true);
                if (disableFriction)
                    block.Add(new DisableFrictionComponent());

                segments.Add(block);

                if (attachStaticMovers) {
                    block.StaticMoverAttachPosition = nodes[i];
                    block.AllowStaticMovers = true;
                }
            }
            segmentCount = segments.Count - 1;

            this.segments = segments;
            this.nodes = nodes;
            this.targetNodes = targetNodes;
        }

        public DashCollisionResults OnDashed(Player player, Vector2 dir) {
            if (player.StateMachine.State == Player.StRedDash)
                player.StateMachine.State = Player.StNormal;

            if (!CanActivate || !dashActivated)
                return DashCollisionResults.NormalCollision;

            // gravity helper support
            bool gravityInverted = GravityHelperImports.IsPlayerInverted?.Invoke() ?? false;
            // make wallbouncing easier
            if (player.DashDir.X == 0 && (player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f)) {
                return DashCollisionResults.NormalCollision;
            }

            // activate the block
            Hit(dir);

            if (canWavedash && dir.Y == (gravityInverted ? -1f : 1f)) {
                return DashCollisionResults.NormalCollision;
            }

            return DashCollisionResults.Rebound;
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            // adds segments in reverse order if disable friction is true for Visual Clarity(tm) (i think) (hopefully)
            foreach (EmptyBlock block in disableFriction ? segments.GetReverseEnumerator() : segments) {
                Scene.Add(block);
                block.Disappear();
            }

            if (drawOutline) {
                // force the use of the default empty color if the outline is set to the default
                // kinda hacky but it works and is easier to deal with editor side hopefully
                Color emptyColor = outlineColor != Calc.HexToColor("3d0200") ? outlineColor : Calc.HexToColor("161021");

                Scene.Add(outlineRenderer = new ExclamationBlockOutlineRenderer(this, outlineColor, emptyColor));
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            // call awake on each block in the correct order to make sure static movers attach correctly
            foreach (EmptyBlock block in segments) {
                block.orig_Awake(scene);
            }
        }

        public override void Removed(Scene scene) {
            scene.Remove(outlineRenderer);
            outlineRenderer = null;

            foreach (EmptyBlock block in segments) {
                block.RemoveSelf();
            }
            segments.Clear();

            base.Removed(scene);
        }

        public override void Update() {
            base.Update();

            VisibleOnCamera = InView(SceneAs<Level>().Camera);

            // ease scale and offset towards their default values
            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 2f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 2f);
            offset.X = Calc.Approach(offset.X, 0f, Engine.DeltaTime * 14f);
            offset.Y = Calc.Approach(offset.Y, 0f, Engine.DeltaTime * 14f);

            // update timer and break the block if it finishes
            if (!Extending || !pauseTimerWhileExtending) {
                if (amountExtended > 0) {
                    activeTimer -= Engine.DeltaTime;

                    if (activeTimer <= 0)
                        Break();
                }

                blinkRoutine.Update();
            }

            if (activationBuffer > 0f)
                activationBuffer -= Engine.DeltaTime;
        }

        public IEnumerator Sequence() {
            while (true) {
                // check if the block should extend
                if (activationBuffer > 0f) {
                    activationBuffer = 0f;

                    targetExtended = Math.Clamp(targetNodes.Count != 0 ? targetNodes.Find(node => { return node > amountExtended; }) : targetExtended + 1, 0, segmentCount);
                }

                // prepare to extend if needed
                if (Extending) {
                    // set liftboost leniency timer when the block starts extending so that the first block to extend always uses it's own direction and doesn't include a 3 frame leniency window with a likely incorrect direction
                    // couldve maybe just. set the direction to be correct here but im lazy and this works so
                    liftboostLeniencyTimer = 0f;
                    extendingSound.Position = Vector2.Zero;
                    extendingSound.Play("event:/sorbethelper/sfx/exclamationblock_extend_loop");
                    // might mess with these values a bit more later idk
                    SceneAs<Level>().Shake(0.15f);
                    yield return 0.15f;
                }

                // extending loop
                while (Extending) {
                    int extendingIndex = amountExtended + 1;

                    // cancel extension if it exceeds the amount of segments
                    if (extendingIndex > segmentCount) {
                        targetExtended = segmentCount;
                        extendingSound.Param("end", 1f);
                        break;
                    }

                    EmptyBlock block = segments[extendingIndex];
                    Vector2 start = nodes[extendingIndex - 1];
                    Vector2 target = nodes[extendingIndex];
                    block.MoveToNaive(start);
                    block.Appear();
                    Audio.Play("event:/sorbethelper/sfx/exclamationblock_extend", block.Center, "index", Math.Clamp(12 * (amountExtended - 1) / Math.Max(segmentCount - 1, 1), 0, 12));

                    Vector2 direction = (target - start).SafeNormalize();
                    float timeToExtend = (target - start).Length() / moveSpeed;
                    float progress = 0f;
                    while (block.Position != target) {
                        yield return null;

                        progress += Engine.DeltaTime;
                        float lerp = Calc.ClampedMap(progress, 0f, timeToExtend);
                        block.MoveTo(Vector2.Lerp(start, target, lerp), (liftboostLeniencyTimer > 0f ? previousDirection : direction) * moveSpeed);
                        extendingSound.Position = block.Position - Position;
                        if (liftboostLeniencyTimer > 0f)
                            liftboostLeniencyTimer -= Engine.DeltaTime;
                    }

                    // finished extending
                    // keep previous liftboost direction stored to allow for some leniency when jumping off the block when its changing directions
                    previousDirection = direction;
                    liftboostLeniencyTimer = 0.05f;
                    // don't set amount extended if the timer already ran out to prevent breaking the block twice
                    if (activeTimer > 0f)
                        amountExtended = extendingIndex;

                    // reached target
                    if (!Extending) {
                        extendingSound.Param("end", 1f);
                        Audio.Play("event:/sorbethelper/sfx/exclamationblock_extend_finish", block.Position);
                        block.StartShaking(0.2f);
                        break;
                    }

                    yield return null;
                }

                yield return null;
            }
        }

        public IEnumerator BlinkRoutine() {
            // literally just updates the blink effect before the block disappears
            while (true) {
                while (activeTimer > 0f && activeTimer < 2f) {
                    Blink();
                    yield return 0.5f;
                }
                yield return null;
            }
        }

        public override void Render() {
            base.Render();

            if (!VisibleOnCamera)
                return;

            if (CanActivate) {
                Util.RenderNineSlice(Position + Offset, activeNineSlice, (int)Width / 8, (int)Height / 8, scale);
                exclamationMarkTexture.DrawCentered(Center + Offset, Color.White, scale);
            } else {
                Util.RenderNineSlice(Position + Offset, emptyNineSlice, (int)Width / 8, (int)Height / 8, scale);
                emptyExclamationMarkTexture.DrawCentered(Center + Offset, Color.White, scale);
            }
        }

        public void Extend() {
            activationBuffer = activationBufferTime;

            // update timer if necessary
            if (amountExtended == 0 || (canRefreshTimer && (!Extending || !pauseTimerWhileExtending))) {
                activeTimer = activeTime;
                blinkRoutine.Jump();
            }
        }

        public void Hit(Vector2 dir) {
            if (!CanActivate)
                return;

            SmashParticles(dir.Perpendicular());
            SmashParticles(-dir.Perpendicular());
            Bounce();
            Audio.Play("event:/sorbethelper/sfx/exclamationblock_hit", Center);

            Extend();
        }

        public void Break() {
            int particleCount = Math.Max((int)(Width / 8) * (int)(Height / 8) / 5 * 2, 1);
            int visibleBlocks = segments.Where(block => { return block.Visible && block.VisibleOnCamera; }).Count();
            particleCount = Math.Min(particleCount * visibleBlocks, 150) / Math.Max(visibleBlocks, 1);

            // play the break sfx at the block closest to the player
            // honestly might be faster if i just checked for this during the foreach but oooh cool linq fancy very clean whoa idk
            Entity player = Scene.Tracker.GetEntity<Player>();
            Vector2 sfxPosition = Position;
            if (player is not null)
                sfxPosition = segments.MinBy(block => Vector2.DistanceSquared(block.Position, player.Position)).Position;

            Audio.Play("event:/sorbethelper/sfx/exclamationblock_break", sfxPosition);
            StartShaking(0.15f);
            SceneAs<Level>().Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);

            foreach (EmptyBlock block in segments) {
                block.Break(particleCount);
                block.MoveToNaive(Position);
            }

            amountExtended = 0;
            targetExtended = 0;
            activationBuffer = 0f;
            activeTimer = 0f;
            liftboostLeniencyTimer = 0f;
        }

        private void Blink() {
            Entity player = Scene.Tracker.GetEntity<Player>();
            Vector2 sfxPosition = Position;
            if (player is not null)
                sfxPosition = segments.MinBy(block => Vector2.DistanceSquared(block.Position, player.Position)).Position;

            Audio.Play("event:/sorbethelper/sfx/exclamationblock_blink", sfxPosition);

            foreach (EmptyBlock block in segments) {
                block.Blink();
            }
        }

        private void Bounce() {
            scale = new Vector2(0.75f, 0.75f);
            offset = new Vector2(0f, -4f);

            foreach (EmptyBlock block in segments) {
                block.Bounce();
            }
        }

        // i love stealing vanilla code !!
        private void SmashParticles(Vector2 dir) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX) {
                direction = 0f;
                position = CenterRight - Vector2.UnitX * 2f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            } else if (dir == -Vector2.UnitX) {
                direction = MathF.PI;
                position = CenterLeft + Vector2.UnitX * 2f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            } else if (dir == Vector2.UnitY) {
                direction = MathF.PI / 2f;
                position = BottomCenter - Vector2.UnitY * 2f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            } else {
                direction = -MathF.PI / 2f;
                position = TopCenter + Vector2.UnitY * 2f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            }
            num = (num + 2) / 2;
            SceneAs<Level>().ParticlesFG.Emit(P_SmashDust, num, position, positionRange, smashParticleColor, direction, MathF.PI / 8f);
        }

        private bool InView(Camera camera) =>
            X < camera.Right + 8f && X + Width > camera.Left - 8f && Y < camera.Bottom + 8f && Y + Height > camera.Top - 8f;

        internal static void Intitialize() {
            P_SmashDust = new ParticleType(Player.P_SummitLandB) {
                SpeedMin = 50f,
                SpeedMax = 90f,
                SpeedMultiplier = 0.1f,
                Color = Calc.HexToColor("ffd12e") * 0.75f,
                Color2 = Calc.HexToColor("ffffff") * 0.5f,
                ColorMode = ParticleType.ColorModes.Fade
            };
        }

        private static ILHook seekerRegenerateCoroutineHook;

        internal static void Load() {
            On.Celeste.Seeker.SlammedIntoWall += onSeekerSlammedIntoWall;
            seekerRegenerateCoroutineHook = new ILHook(
                typeof(Seeker).GetMethod("RegenerateCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                modSeekerRegenerateCoroutine
            );
            IL.Celeste.Puffer.Explode += modPufferExplode;
        }

        internal static void Unload() {
            On.Celeste.Seeker.SlammedIntoWall -= onSeekerSlammedIntoWall;
            seekerRegenerateCoroutineHook.Dispose();
            IL.Celeste.Puffer.Explode -= modPufferExplode;
        }

        private static void onSeekerSlammedIntoWall(On.Celeste.Seeker.orig_SlammedIntoWall orig, Seeker self, CollisionData data) {
            if (data.Hit is ExclamationBlock exclamationBlock) {
                exclamationBlock.Hit(data.Direction);
            }
            orig(self, data);
        }

        private static void modSeekerRegenerateCoroutine(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            int seekerVariable = 1;

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdloc(out seekerVariable),
            instr => instr.MatchCallOrCallvirt<Entity>("CollideFirst"),
            instr => instr.MatchStloc(out _))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make seeker explosions activate exclamation mark blocks at {cursor.Index} in CIL code for {cursor.Method.Name}");

                cursor.EmitLdloc(seekerVariable);
                cursor.EmitDelegate(makePuffersAndSeekersActivateExclamationBlocks);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make seeker explosions activate exclamation mark blocks in CIL code for {cursor.Method.Name}!");
            }
        }

        private static void modPufferExplode(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            int pufferVariable = 0;

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(out pufferVariable),
            instr => instr.MatchCallOrCallvirt<Entity>("CollideFirst"),
            instr => instr.MatchStloc(out _))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make puffer explosions activate exclamation mark blocks at {cursor.Index} in CIL code for {cursor.Method.Name}");

                cursor.EmitLdarg0();
                cursor.EmitDelegate(makePuffersAndSeekersActivateExclamationBlocks);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make puffer explosions activate exclamation mark blocks in CIL code for {cursor.Method.Name}!");
            }
        }

        private static void makePuffersAndSeekersActivateExclamationBlocks(Entity self) {
            foreach (ExclamationBlock exclamationBlock in self.Scene.Tracker.GetEntities<ExclamationBlock>()) {
                if (exclamationBlock.explodeActivated && self.CollideCheck(exclamationBlock)) {
                    exclamationBlock.Hit((exclamationBlock.Center - self.Position).FourWayNormal());
                }
            }
        }

        private class ExclamationBlockOutlineRenderer : Entity {
            private readonly ExclamationBlock parent;
            private readonly Color activeColor, emptyColor;

            public ExclamationBlockOutlineRenderer(ExclamationBlock parent, Color activeColor, Color emptyColor) {
                base.Depth = 5000;
                this.parent = parent;
                this.activeColor = activeColor;
                this.emptyColor = emptyColor;
            }

            public override void Render() {
                float width = parent.Width;
                float height = parent.Height;

                foreach (EmptyBlock block in parent.segments) {
                    if (block.Visible && block.VisibleOnCamera)
                        DrawRoundedOutline(block.Center + block.Offset - (new Vector2(width / 2f, height / 2f) * block.Scale), width * block.Scale.X, height * block.Scale.Y, emptyColor);
                }

                if (parent.Visible && parent.VisibleOnCamera)
                    DrawRoundedOutline(parent.Center + parent.Offset - (new Vector2(width / 2f, height / 2f) * parent.Scale), width * parent.Scale.X, height * parent.Scale.Y, parent.CanActivate ? activeColor : emptyColor);
            }

            private static void DrawRoundedOutline(Vector2 position, float width, float height, Color color) {
                Draw.Rect(position - Vector2.UnitY, width, height + 2, color);
                Draw.Rect(position - Vector2.UnitX, width + 2, height, color);
            }
        }
    }

    public class EmptyBlock : Solid {
        private readonly MTexture[,] nineSlice, flashNineSlice;
        private readonly bool fromExclamationBlock;

        private Vector2 scale = Vector2.One;
        private Vector2 offset;
        public Vector2 Scale => scale;
        public Vector2 Offset => offset + Shake;
        private float flashOpacity;

        public Vector2 StaticMoverAttachPosition;
        public bool VisibleOnCamera { get; private set; } = true;

        public EmptyBlock(Vector2 position, float width, float height, string directory, bool fromExclamationBlock = false) : base(position, width, height, false) {
            base.Depth = -8999;
            StaticMoverAttachPosition = Position;
            AllowStaticMovers = false;
            this.fromExclamationBlock = fromExclamationBlock;

            nineSlice = Util.CreateNineSlice(GFX.Game[$"{directory}/emptyBlock"], 8, 8);
            flashNineSlice = Util.CreateNineSlice(GFX.Game[$"{directory}/flash"], 8, 8);
            SurfaceSoundIndex = SurfaceIndex.Girder;
            Add(new LightOcclude());
        }

        public override void Awake(Scene scene) {
            // don't automatically call awake so that static movers get attached in the correct order
            if (!fromExclamationBlock)
                orig_Awake(scene);
        }

        public void orig_Awake(Scene scene) {
            if (StaticMoverAttachPosition != Position && AllowStaticMovers) {
                Vector2 actualPosition = Position;
                Position = StaticMoverAttachPosition;

                base.Awake(scene);

                MoveToNaive(actualPosition);
            } else {
                base.Awake(scene);
            }

            if (!Collidable)
                DisableStaticMovers();
        }

        public override void Update() {
            base.Update();

            VisibleOnCamera = InView(SceneAs<Level>().Camera);

            // ease scale and offset towards their default values
            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 2f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 2f);
            offset.X = Calc.Approach(offset.X, 0f, Engine.DeltaTime * 14f);
            offset.Y = Calc.Approach(offset.Y, 0f, Engine.DeltaTime * 14f);

            if (flashOpacity > 0f)
                flashOpacity -= Engine.DeltaTime * 4f;
        }

        public override void Render() {
            base.Render();

            if (VisibleOnCamera)
                Util.RenderNineSlice(Position + offset + Shake, nineSlice, flashNineSlice, flashOpacity, (int)Width / 8, (int)Height / 8, scale);
        }

        public void Appear() {
            Visible = Collidable = true;
            EnableStaticMovers();

            // spawn a few particles around static movers so they just dont pop into existence (as abruptly at least,,)
            foreach (StaticMover staticMover in staticMovers) {
                if (staticMover.Entity.Visible)
                    SceneAs<Level>().ParticlesFG.Emit(ParticleTypes.VentDust, (int)Math.Max(Math.Max(staticMover.Entity.Width, staticMover.Entity.Height) / 6f, 2f), staticMover.Entity.Center, new Vector2(staticMover.Entity.Width / 2, staticMover.Entity.Height / 2), Color.WhiteSmoke, 0f, MathF.PI * 2f);
            }
        }

        public void Disappear() {
            Visible = Collidable = false;
            DisableStaticMovers();

            flashOpacity = 0f;
            scale = Vector2.One;
            offset = Vector2.Zero;
        }

        public void Break(int particleCount) {
            if (Visible && VisibleOnCamera)
                SceneAs<Level>().Particles.Emit(Player.P_SummitLandB, particleCount, Center, new Vector2(Width / 2, Height / 2), Color.White * 0.75f, MathF.PI / 2f, MathF.PI / 6f);

            Disappear();
        }

        public void Blink() {
            flashOpacity = 1f;
        }

        public void Bounce() {
            scale = new Vector2(0.75f, 0.75f);
            offset = new Vector2(0f, -4f);
        }

        private bool InView(Camera camera) =>
            X < camera.Right + 8f && X + Width > camera.Left - 8f && Y < camera.Bottom + 8f && Y + Height > camera.Top - 8f;
    }
}
