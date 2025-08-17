using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TalkComponentUI = Celeste.TalkComponent.TalkComponentUI;

namespace Celeste.Mod.SorbetHelper.Triggers;

[Tracked]
[CustomEntity("SorbetHelper/AlternateInteractPrompt")]
public class AlternateInteractPromptWrapper : Entity {
    private readonly TalkComponentAltUI.Options Options;

    public AlternateInteractPromptWrapper(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);

        Options = new TalkComponentAltUI.Options(
            Style: data.Enum("style", TalkComponentAltUI.Styles.BottomCorner),
            LabelDialogID: data.Attr("dialogId", "sorbethelper_ui_talk"),
            HighlightEffects: data.Bool("playHighlightSfx", false),
            OnLeftCorner: data.Bool("onLeft", false)
        );
    }

    #region Custom TalkComponentUI

    public class TalkComponentAltUI : TalkComponentUI {
        public enum Styles {
            BottomCorner,
            SmallArrow
        }

        public readonly record struct Options(
            Styles Style,
            string LabelDialogID,
            bool HighlightEffects,
            bool OnLeftCorner = false // for BottomCorner style only
        );

        private const float PromptScale = 0.85f;

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
            Level level = Scene as Level;

            if (level.FrozenOrPaused || slide <= 0f || Handler.Entity == null)
                return;

            switch (options.Style) {
                case Styles.BottomCorner: {
                        float slideEase = Math.Min(highlightedEase, slide);

                        if (slideEase <= 0f)
                            return;

                        string label = Dialog.Clean(options.LabelDialogID);

                        float width = ButtonUI.Width(label, Input.Talk);
                        const int offscreenPadding = 32;
                        Vector2 drawPos = new Vector2(
                            x: 1880f + (40f + width * PromptScale + offscreenPadding) * (1f - Ease.CubeOut(slideEase)),
                            y: 1024f
                        );

                        if (options.OnLeftCorner)
                            drawPos.X = 1920 - drawPos.X;

                        RenderPrompt(drawPos, label, justifyX: options.OnLeftCorner ? 0f : 1f, alpha: slideEase * 0.5f + 0.5f);

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

                        float arrowWiggle = (!Highlighted) ? (1f + wiggler.Value * 0.5f) : (1f - wiggler.Value * 0.5f);
                        float promptWiggle = (!Highlighted) ? (1f + wiggler.Value * 0.375f) : (1f - wiggler.Value * 0.375f);
                        float trueAlpha = Ease.CubeInOut(slide) * alpha;
                        Color color = lineColor * trueAlpha;

                        Vector2 arrowPos = drawPos + new Vector2(0f, 64f * (1f - Ease.CubeOut(slide)));// - new Vector2(0f, 48f * Ease.CubeInOut(highlightedEase));
                        GFX.Gui["SorbetHelper/smallTalkArrow"].DrawJustified(arrowPos * zoomScale, new Vector2(0.5f, 1f), color, arrowWiggle * zoomScale);

                        string label = Dialog.Clean(options.LabelDialogID);
                        Vector2 promptPos = drawPos - new Vector2(0, 68f);
                        RenderPrompt(promptPos * zoomScale, label, scale: promptWiggle * zoomScale, alpha: trueAlpha * highlightedEase);

                        break;
                    }
            }
        }

        private void RenderPrompt(Vector2 position, string label, float scale = 1f, float justifyX = 0.5f, float alpha = 1f) {
            if (!string.IsNullOrEmpty(label))
                ButtonUI.Render(position, label, Input.Talk, PromptScale * scale, justifyX, selectWiggle.Value * 0.05f, alpha: alpha);
            else
                Input.GuiButton(Input.Talk, Input.PrefixMode.Latest).DrawJustified(position, new Vector2(justifyX, 0.5f), Color.White * alpha, PromptScale * scale + selectWiggle.Value * 0.05f);
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
            var cursor = new ILCursor(il);

            // swap out the vanilla TalkComponentUI for a custom one if colliding with an AlternateInteractPromptWrapper
            cursor.GotoNext(MoveType.Before, instr => instr.MatchNewobj<TalkComponentUI>());

            var skipOrigTalkComponentUI = cursor.DefineLabel();

            cursor.EmitDelegate(tryGetAltUI);
            cursor.EmitDup();
            cursor.EmitBrtrue(skipOrigTalkComponentUI);
            cursor.EmitPop();
            cursor.EmitLdarg0();

            cursor.GotoNext(MoveType.After, instr => instr.MatchNewobj<TalkComponentUI>());
            cursor.MarkLabel(skipOrigTalkComponentUI);

            static TalkComponentAltUI tryGetAltUI(TalkComponent self) {
                if (self.Entity is not { } entity)
                    return null;

                var altUITrigger = entity.Scene.CollideFirst<AlternateInteractPromptWrapper>(entity.Position);
                if (altUITrigger is null)
                    return null;

                return new TalkComponentAltUI(self, altUITrigger.Options);
            }

            // adjust the minimum hoverTimer required for TalkComponentAltUIs to be considered highlighted
            cursor.Index = -1;
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(0.1f));
            cursor.EmitLdarg0();
            cursor.EmitLdfld(typeof(TalkComponent).GetField(nameof(TalkComponent.UI)));
            cursor.EmitDelegate(adjustHoverTimerForCustomUI);

            static float adjustHoverTimerForCustomUI(float orig, TalkComponentUI UI) {
                if (UI is TalkComponentAltUI altUI) {
                    return altUI.options.Style switch {
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