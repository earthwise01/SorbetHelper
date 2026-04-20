using TalkComponentUI = Celeste.TalkComponent.TalkComponentUI;

namespace Celeste.Mod.SorbetHelper.Triggers;

[Tracked]
[CustomEntity("SorbetHelper/AlternateInteractPrompt")]
public class AlternateInteractPromptWrapper(EntityData data, Vector2 offset) : Trigger(data, offset)
{
    public class TalkComponentAltUI : TalkComponentUI
    {
        public enum Styles
        {
            BottomCorner,
            SmallArrow,
            Vanilla
        }

        public record Options(
            Styles Style,
            string LabelDialogId,
            bool HighlightEffects,
            bool FlipPrompt,
            bool UseUpInput);

        private readonly Options options;

        private readonly Wiggler selectWiggle;
        private float selectWiggleDelay;
        private float highlightedEase;

        private int lastMoveYValue;

        public TalkComponentAltUI(TalkComponent handler, Options options) : base(handler)
        {
            this.options = options;
            Add(selectWiggle = Wiggler.Create(0.4f, 4f));

            Tag |= Tags.TransitionUpdate;
        }

        public override void Update()
        {
            highlightedEase = Calc.Approach(highlightedEase, Highlighted ? 1f : 0f, Engine.DeltaTime * 4f);

            if (selectWiggleDelay > 0f)
                selectWiggleDelay -= Engine.DeltaTime;

            base.Update();
        }

        public override void Render()
        {
            Level level = SceneAs<Level>();

            if (level is null || level.FrozenOrPaused || slide <= 0f || Handler.Entity == null)
                return;

            switch (options.Style)
            {
                case Styles.BottomCorner:
                {
                    float slideEase = Math.Min(highlightedEase, slide);

                    if (slideEase <= 0f)
                        return;

                    string label = Dialog.Clean(options.LabelDialogId);

                    const int offscreenPaddingX = 16;
                    const int edgePaddingX = 100;
                    const int edgePaddingY = 80;
                    float width = GetPromptWidth(label);
                    Vector2 drawPos = new Vector2(
                        x: 1920f - edgePaddingX + (edgePaddingX + width + offscreenPaddingX) * (1f - Ease.CubeOut(slideEase)),
                        y: 1080f - edgePaddingY
                    );

                    if (options.FlipPrompt)
                        drawPos.X = 1920 - drawPos.X;

                    RenderPrompt(
                        drawPos, label, 1f,
                        justifyX: options.FlipPrompt ? 0f : 1f,
                        flipX: options.FlipPrompt,
                        wiggle: selectWiggle.Value * 0.05f,
                        promptAlpha: slideEase * 0.5f + 0.5f
                    );

                    break;
                }
                case Styles.SmallArrow:
                {
                    Vector2 upsideDownScale = options.FlipPrompt ? new Vector2(1f, -1f) : Vector2.One;

                    Vector2 drawPos = WorldToScreen(level, Handler.Entity.Position + Handler.DrawAt);

                    float zoomScale = level.Zoom * (320f - level.ScreenPadding * 2f) / 320f;
                    if (ExtendedVariantsCompat.IsLoaded)
                        zoomScale *= ExtendedVariantsCompat.GetZoomLevel();
                    zoomScale = MathF.Min(zoomScale, 1f);

                    drawPos.Y += (float)Math.Sin(timer * 4f) * MathHelper.Lerp(12f, 6f, highlightedEase) * zoomScale * upsideDownScale.Y;

                    float arrowScale = (!Highlighted ? 1f + wiggler.Value * 0.4f : 1f - wiggler.Value * 0.4f) * zoomScale;
                    float promptScale = (!Highlighted ? 1f + wiggler.Value * 0.275f : 1f - wiggler.Value * 0.275f) * zoomScale;
                    Vector2 arrowPos = drawPos + new Vector2(0f, 64f * (1f - Ease.CubeOut(slide))) * zoomScale * upsideDownScale;
                    Vector2 promptPos = drawPos - new Vector2(0, 80f) * zoomScale * upsideDownScale;
                    float promptAlpha = Ease.CubeInOut(slide) * alpha;

                    GFX.Gui["SorbetHelper/smallTalkArrow"].DrawJustified(arrowPos, new Vector2(0.5f, 1f), lineColor * promptAlpha, arrowScale * upsideDownScale);

                    RenderPrompt(
                        promptPos, Dialog.Clean(options.LabelDialogId), promptScale,
                        justifyX: 0.5f,
                        flipX: true,
                        wiggle: selectWiggle.Value * 0.05f,
                        promptAlpha: promptAlpha * highlightedEase
                    );

                    break;
                }
                case Styles.Vanilla:
                {
                    Vector2 upsideDownScale = options.FlipPrompt ? new Vector2(1f, -1f) : Vector2.One;

                    Vector2 drawPos = WorldToScreen(level, Handler.Entity.Position + Handler.DrawAt);

                    float zoomScale = level.Zoom * (320f - level.ScreenPadding * 2f) / 320f;
                    if (ExtendedVariantsCompat.IsLoaded)
                        zoomScale *= ExtendedVariantsCompat.GetZoomLevel();
                    zoomScale = MathF.Min(zoomScale, 1f);

                    drawPos.Y += ((float)Math.Sin(timer * 4f) * 12f + 64f * (1f - Ease.CubeOut(slide))) * zoomScale * upsideDownScale.Y;

                    float promptScale = (!Highlighted ? 1f + wiggler.Value * 0.5f : 1f - wiggler.Value * 0.5f) * zoomScale;
                    float promptAlpha = Ease.CubeInOut(slide) * alpha;
                    Color hoverColor = lineColor * promptAlpha;

                    if (!Highlighted) {
                        GFX.Gui["hover/idle"].DrawJustified(drawPos, new Vector2(0.5f, 1f), hoverColor * alpha, promptScale * upsideDownScale);
                        break;
                    }

                    Handler.HoverUI.Texture.DrawJustified(drawPos, new Vector2(0.5f, 1f), hoverColor * alpha, promptScale * upsideDownScale);

                    Vector2 promptPos = drawPos + Handler.HoverUI.InputPosition * promptScale * upsideDownScale;
                    if (options.UseUpInput || Input.GuiInputController(Input.PrefixMode.Latest))
                        GetButtonTexture().DrawJustified(promptPos, new Vector2(0.5f), Color.White * promptAlpha, promptScale);
                    else
                        ActiveFont.DrawOutline(Input.FirstKey(Input.Talk).ToString().ToUpper(), promptPos, new Vector2(0.5f), new Vector2(promptScale), Color.White * promptAlpha, 2f, Color.Black);

                    break;
                }
            }
        }

        // can't use Level.WorldToScreen since that doesn't account for extvar zoom (should that just b added there?)
        public static Vector2 WorldToScreen(Level level, Vector2 position)
            => Vector2.Transform(position, level.Camera.Matrix * level.GetCameraToScreenMatrix());

        // feel like there could b better input button textures than these
        public MTexture GetButtonTexture()
            => options.UseUpInput
                ? GFX.Gui[GravityHelper.IsImported && GravityHelper.IsPlayerInverted()
                    ? "SorbetHelper/inputDown"
                    : "SorbetHelper/inputUp"]
                : Input.GuiButton(Input.Talk, Input.PrefixMode.Latest);

        private float GetPromptWidth(string label)
        {
            MTexture buttonTexture = GetButtonTexture();

            if (string.IsNullOrEmpty(label))
                return buttonTexture.Width;

            return ActiveFont.Measure(label).X + 8f + buttonTexture.Width;
        }

        private void RenderPrompt(Vector2 position, string label, float scale, float justifyX = 0.5f, float justifyY = 1f, bool flipX = false, float wiggle = 0f, float promptAlpha = 1f)
        {
            MTexture buttonTexture = GetButtonTexture();
            float promptWidth = GetPromptWidth(label);

            position.X -= scale * promptWidth * (justifyX - 0.5f);

            Vector2 buttonOrigin = new Vector2(buttonTexture.Width - promptWidth / 2f, buttonTexture.Height / 2f);
            if (flipX)
                buttonOrigin.X = buttonTexture.Width - buttonOrigin.X;
            buttonTexture.Draw(position, buttonOrigin, Color.White * promptAlpha, scale + wiggle);

            if (string.IsNullOrEmpty(label))
                return;

            float textWidth = ActiveFont.Measure(label).X;
            Vector2 textJustify = new Vector2(promptWidth / 2f / textWidth, 0.5f);
            if (flipX)
                textJustify.X = 1f - textJustify.X;
            ActiveFont.DrawOutline(label, position, textJustify, Vector2.One * (scale + wiggle), Color.White * promptAlpha, scale > 0.5f ? 2f : 1f, Color.Black * promptAlpha);
        }

        #region Hooks

        private static Hook hook_set_Highlighted = null;

        [OnLoad]
        internal static void Load()
        {
            hook_set_Highlighted = new Hook(
                typeof(TalkComponentUI).GetProperty(nameof(Highlighted), HookHelper.Bind.PublicInstance)!.GetSetMethod()!,
                On_set_Highlighted
            );
            IL.Celeste.TalkComponent.Update += IL_TalkComponent_Update;
        }

        [OnUnload]
        internal static void Unload()
        {
            HookHelper.DisposeAndSetNull(ref hook_set_Highlighted);
            IL.Celeste.TalkComponent.Update -= IL_TalkComponent_Update;
        }

        private delegate void orig_set_Highlighted(TalkComponentUI self, bool value);
        private static void On_set_Highlighted(orig_set_Highlighted orig, TalkComponentUI self, bool value)
        {
            if (self is TalkComponentAltUI { options.HighlightEffects: false })
                self.highlighted = value;
            else
                orig(self, value);
        }

        private static void IL_TalkComponent_Update(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNextBestFit(MoveType.After,
                instr => instr.MatchLdsfld(typeof(Input), nameof(Input.Talk)),
                instr => instr.MatchCallOrCallvirt<VirtualButton>($"get_{nameof(VirtualButton.Pressed)}")))
                throw new HookHelper.HookException(il, "Unable to find `Input.Talk.Pressed` check to modify.");

            cursor.EmitLdarg0();
            cursor.EmitDelegate(CheckUpInput);

            if (!cursor.TryGotoNextBestFit(MoveType.After,
                instr => instr.MatchLdarg0(),
                instr => instr.MatchLdfld<TalkComponent>(nameof(TalkComponent.OnTalk)),
                instr => instr.MatchBrfalse(out _)))
                throw new HookHelper.HookException(il, "Unable to find `OnTalk` null check to play the select animation before.");

            cursor.EmitLdarg0();
            cursor.EmitDelegate(SelectAnimate);

            if (!cursor.TryGotoNextBestFit(MoveType.After,
                instr => instr.MatchLdfld<TalkComponent>(nameof(TalkComponent.hoverTimer)),
                instr => instr.MatchLdcR4(0.1f)))
                throw new HookHelper.HookException(il, "Unable to find `hoverTimer > 0.1f` check to modify.");

            cursor.EmitLdarg0();
            cursor.EmitDelegate(AdjustHoverTimerCheck);

            if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchRet()))
                throw new HookHelper.HookException(il, "Unable to find method end to save the current Input.MoveY value before.");

            cursor.EmitLdarg0();
            cursor.EmitDelegate(SetLastUpInput);

            return;

            static bool CheckUpInput(bool orig, TalkComponent talkComponent)
            {
                if (talkComponent.UI is not TalkComponentAltUI { options.UseUpInput: true } altUi)
                    return orig;

                int playerUp = GravityHelper.IsImported && GravityHelper.IsPlayerInverted() ? 1 : -1; // is this a good idea?
                return Input.MoveY.Value == playerUp && altUi.lastMoveYValue != playerUp;
            }

            static void SelectAnimate(TalkComponent talkComponent)
            {
                if (talkComponent.UI is not TalkComponentAltUI altUi)
                    return;

                if (altUi.selectWiggleDelay > 0f)
                    return;

                altUi.selectWiggle.Start();
                altUi.selectWiggleDelay = 0.5f;
            }

            static void SetLastUpInput(TalkComponent talkComponent)
            {
                if (talkComponent.UI is not TalkComponentAltUI { options.UseUpInput: true } altUi)
                    return;

                altUi.lastMoveYValue = Input.MoveY.Value;
            }

            static float AdjustHoverTimerCheck(float orig, TalkComponent talkComponent)
            {
                if (talkComponent.UI is not TalkComponentAltUI altUi)
                    return orig;

                return altUi.options.Style switch
                {
                    Styles.BottomCorner => 0f,
                    _                   => orig
                };
            }
        }

        #endregion
    }

    private readonly TalkComponentAltUI.Options options = new TalkComponentAltUI.Options(
        data.Enum("style", TalkComponentAltUI.Styles.BottomCorner),
        data.Attr("dialogId", "sorbethelper_ui_talk"),
        data.Bool("playHighlightSfx", false),
        data.Bool("onLeft", false),
        data.Bool("useUpInput", false));

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (!scene.Tracker.IsComponentTracked<TalkComponent>())
        {
            Tracker.AddTypeToTracker(typeof(TalkComponent));
            Tracker.Refresh(scene);
        }

        // todo: this might suckkk idk if i shd use an entityawakeprocessor or like just always iterate through every entity
        // need to do this here so gravityhelper watchtowers can be affected
        foreach (TalkComponent talkComponent in scene.Tracker.GetComponents<TalkComponent>())
        {
            if (talkComponent.UI is not null)
                continue;

            Entity entity = talkComponent.Entity;
            AlternateInteractPromptWrapper wrapper = entity.Collider is not null
                ? entity.CollideFirst<AlternateInteractPromptWrapper>()
                : entity.Scene.CollideFirst<AlternateInteractPromptWrapper>(entity.Position);

            if (wrapper is not null)
                scene.Add(talkComponent.UI = new TalkComponentAltUI(talkComponent, wrapper.options));
        }
    }
}
