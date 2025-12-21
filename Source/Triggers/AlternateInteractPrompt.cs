using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TalkComponentUI = Celeste.TalkComponent.TalkComponentUI;

namespace Celeste.Mod.SorbetHelper.Triggers;

[Tracked]
[CustomEntity("SorbetHelper/AlternateInteractPrompt")]
public class AlternateInteractPromptWrapper(EntityData data, Vector2 offset) : Trigger(data, offset) {

    private readonly TalkComponentAltUI.Options options = new TalkComponentAltUI.Options(
        Style: data.Enum("style", TalkComponentAltUI.Styles.BottomCorner),
        LabelDialogId: data.Attr("dialogId", "sorbethelper_ui_talk"),
        HighlightEffects: data.Bool("playHighlightSfx", false),
        OnLeftCorner: data.Bool("onLeft", false)
    );

    #region Custom TalkComponentUI

    public class TalkComponentAltUI : TalkComponentUI {
        public enum Styles {
            BottomCorner,
            SmallArrow
        }

        public readonly record struct Options(
            Styles Style,
            string LabelDialogId,
            bool HighlightEffects,
            bool OnLeftCorner = false // for BottomCorner style only
        );

        private readonly Wiggler selectWiggle;
        private float selectWiggleDelay;
        private float highlightedEase;

        private readonly Options options;

        private TalkComponentAltUI(TalkComponent handler, Options options) : base(handler) {
            Add(selectWiggle = Wiggler.Create(0.4f, 4f));
            this.options = options;

            Depth = -100000;
            Tag |= Tags.TransitionUpdate; // (for BottomCorner style) don't get frozen during transition and suddenly disappear

            // swap to subhud
            Tag &= ~Tags.HUD;
            Tag |= TagsExt.SubHUD;
        }

        public override void Update() {
            highlightedEase = Calc.Approach(highlightedEase, Highlighted ? 1f : 0f, Engine.DeltaTime * 4f);

            if (Input.Talk.Pressed && selectWiggleDelay <= 0f) {
                selectWiggle.Start();
                selectWiggleDelay = 0.5f;
            }
            selectWiggleDelay -= Engine.DeltaTime;

            base.Update();
        }

        public override void Render() {
            // base.Render();
            Level level = SceneAs<Level>();

            if (level is null || level.FrozenOrPaused || slide <= 0f || Handler.Entity == null)
                return;

            switch (options.Style) {
                case Styles.BottomCorner: {
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

                        if (options.OnLeftCorner)
                            drawPos.X = 1920 - drawPos.X;

                        RenderPrompt(
                            drawPos, label, 1f,
                            justifyX: options.OnLeftCorner ? 0f : 1f,
                            flipX: options.OnLeftCorner,
                            wiggle: selectWiggle.Value * 0.05f,
                            alpha: slideEase * 0.5f + 0.5f,
                            backgroundAlpha: 0f
                        );

                        break;
                    }
                case Styles.SmallArrow: {
                        Vector2 camPos = level.Camera.Position.Floor();
                        Vector2 drawPos = Handler.Entity.Position + Handler.DrawAt - camPos;
                        if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
                            drawPos.X = level.Camera.Viewport.Width - drawPos.X;

                        drawPos *= 6f;
                        drawPos.Y += (float)Math.Sin(timer * 4f) * MathHelper.Lerp(12f, 6f, highlightedEase);

                        float zoomScale = level.Camera.Viewport.Width != 320 ? level.Zoom : 1f;

                        float arrowWiggle = (!Highlighted) ? (1f + wiggler.Value * 0.4f) : (1f - wiggler.Value * 0.4f);
                        float promptWiggle = (!Highlighted) ? (1f + wiggler.Value * 0.275f) : (1f - wiggler.Value * 0.275f);

                        float arrowAlpha = Ease.CubeInOut(slide) * alpha;

                        Vector2 arrowPos = drawPos + new Vector2(0f, 64f * (1f - Ease.CubeOut(slide))); // - new Vector2(0f, 48f * Ease.CubeInOut(highlightedEase));
                        GFX.Gui["SorbetHelper/smallTalkArrow"].DrawJustified(arrowPos * zoomScale, new Vector2(0.5f, 1f), lineColor * arrowAlpha, arrowWiggle * zoomScale);

                        string label = Dialog.Clean(options.LabelDialogId);
                        Vector2 promptPos = (drawPos - new Vector2(0, 80f)) * zoomScale;
                        float promptScale = promptWiggle * zoomScale;

                        RenderPrompt(
                            promptPos, label, promptScale,
                            justifyX: 0.5f,
                            flipX: true,
                            wiggle: selectWiggle.Value * 0.05f,
                            alpha: arrowAlpha * highlightedEase,
                            backgroundAlpha: 0f
                        );

                        break;
                    }
            }
        }

        private static float GetPromptWidth(string label) {
            MTexture buttonTexture = Input.GuiButton(Input.Talk, Input.PrefixMode.Latest);

            if (string.IsNullOrEmpty(label))
                return buttonTexture.Width;
            else
                return ActiveFont.Measure(label).X + 8f + buttonTexture.Width;
        }

        private static void RenderPrompt(Vector2 position, string label, float scale, float justifyX = 0.5f, bool flipX = false, float wiggle = 0f, float alpha = 1f, float backgroundAlpha = 1f) {
            RenderBackground(position, label, scale, justifyX, backgroundAlpha * alpha);

            MTexture buttonTexture = Input.GuiButton(Input.Talk, Input.PrefixMode.Latest);
            float promptWidth = GetPromptWidth(label);

            position.X -= scale * promptWidth * (justifyX - 0.5f);

            // draw button
            Vector2 buttonOrigin = new Vector2(buttonTexture.Width - promptWidth / 2f, buttonTexture.Height / 2f);
            if (flipX)
                buttonOrigin.X = buttonTexture.Width - buttonOrigin.X;
            buttonTexture.Draw(position, buttonOrigin, Color.White * alpha, scale + wiggle);

            // draw text
            if (string.IsNullOrEmpty(label))
                return;

            float textWidth = ActiveFont.Measure(label).X;
            Vector2 textJustify = new Vector2(promptWidth / 2f / textWidth, 0.5f);
            if (flipX)
                textJustify.X = 1f - textJustify.X;
            ActiveFont.DrawOutline(label, position, textJustify, Vector2.One * (scale + wiggle), Color.White * alpha, 2f, Color.Black * alpha);
        }

        private static void RenderBackground(Vector2 position, string label, float scale = 1f, float justifyX = 0.5f, float alpha = 1f) {
            if (alpha <= 0f)
                return;

            MTexture edgeTexture = GFX.Gui["SorbetHelper/semicircle"];
            Color color = Color.Black * alpha;

            float width = GetPromptWidth(label) * scale;
            float height = edgeTexture.Height * scale;

            float edgeInnerDistance = 0f; // edgeTexture.Width / 6f * scale;
            float rectX = MathF.Floor(position.X - width * justifyX + edgeInnerDistance);
            float rectWidth = MathF.Ceiling(width - edgeInnerDistance * 2f);

            if (rectWidth > 0f)
                Draw.Rect(rectX, position.Y - height / 2f, rectWidth, height, color);
            edgeTexture.DrawJustified(new Vector2(rectX, (int)position.Y), new Vector2(1f, 0.5f), color, scale, 0f, SpriteEffects.FlipHorizontally);
            edgeTexture.DrawJustified(new Vector2(rectX + rectWidth, (int)position.Y), new Vector2(0f, 0.5f), color, scale);

        }

        #region Hooks

        private static Hook hook_set_Highlighted = null;
        internal static void Load() {
            hook_set_Highlighted = new Hook(typeof(TalkComponentUI).GetMethod("set_Highlighted"), TalkComponentAltUI.On_set_Highlighted);
            IL.Celeste.TalkComponent.Update += IL_TalkComponent_Update;
        }
        internal static void Unload() {
            hook_set_Highlighted?.Dispose();
            hook_set_Highlighted = null;
            IL.Celeste.TalkComponent.Update -= IL_TalkComponent_Update;
        }

        private delegate void orig_set_Highlighted(TalkComponentUI self, bool value);
        private static void On_set_Highlighted(orig_set_Highlighted orig, TalkComponentUI self, bool value) {
            if (self is TalkComponentAltUI { options.HighlightEffects: false })
                self.highlighted = value;
            else
                orig(self, value);
        }

        private static void IL_TalkComponent_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // swap out the vanilla TalkComponentUI for a custom one if colliding with an AlternateInteractPromptWrapper
            cursor.GotoNext(MoveType.Before, instr => instr.MatchNewobj<TalkComponentUI>());

            ILLabel skipOrigTalkComponentUI = cursor.DefineLabel();

            cursor.EmitDelegate(TryGetCustomUi);
            cursor.EmitDup();
            cursor.EmitBrtrue(skipOrigTalkComponentUI);
            cursor.EmitPop();
            cursor.EmitLdarg0();

            cursor.GotoNext(MoveType.After, instr => instr.MatchNewobj<TalkComponentUI>());
            cursor.MarkLabel(skipOrigTalkComponentUI);

            // adjust the minimum hoverTimer required for TalkComponentAltUIs to be considered highlighted
            cursor.Index = -1;
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(0.1f));
            cursor.EmitLdarg0();
            cursor.EmitLdfld(typeof(TalkComponent).GetField(nameof(TalkComponent.UI)));
            cursor.EmitDelegate(AdjustCustomUiHoverTimer);

            return;

            static TalkComponentAltUI TryGetCustomUi(TalkComponent self) {
                if (self.Entity is not { } entity)
                    return null;

                return entity.Scene.CollideFirst<AlternateInteractPromptWrapper>(entity.Position) is { } wrapper
                    ? new TalkComponentAltUI(self, wrapper.options)
                    : null;
            }

            static float AdjustCustomUiHoverTimer(float orig, TalkComponentUI ui) {
                if (ui is TalkComponentAltUI altUi) {
                    return altUi.options.Style switch {
                        Styles.BottomCorner => 0f,
                        _ => 0.1f
                    };
                }

                return orig;
            }
        }

        #endregion

    }

    #endregion

}
