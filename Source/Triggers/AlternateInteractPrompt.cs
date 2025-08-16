using System;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TalkComponentUI = Celeste.TalkComponent.TalkComponentUI;

namespace Celeste.Mod.SorbetHelper.Triggers;

[Tracked]
[CustomEntity("SorbetHelper/AlternateInteractPrompt")]
public class AlternateInteractPromptWrapper : Entity {
    private readonly string dialogId;
    private readonly TalkComponentAltUI.Styles style;
    private readonly bool origHighlightEffects;

    // BottomCorner style only
    private readonly bool onLeft;

    public AlternateInteractPromptWrapper(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);
        dialogId = data.Attr("dialogId", "sorbethelper_ui_talk");
        style = data.Enum("style", TalkComponentAltUI.Styles.BottomCorner);
        origHighlightEffects = data.Bool("playHighlightSfx", false);
        onLeft = data.Bool("onLeft", false);
    }

    public class TalkComponentAltUI : TalkComponentUI {
        private readonly Wiggler selectWiggle;
        private float selectWiggleDelay;
        private float highlightedEase;

        // hmm this might be good if i add more specific styles later but could also change into a   hasArrow checkbox + position enum (normal, bottom left, bottom right)  and also explode onLeft
        // though if i just wanted to explode onLeft then i could split it hereee too bleh
        public enum Styles { BottomCorner, SmallArrow }

        private readonly string dialogId;
        public readonly Styles Style = Styles.SmallArrow;
        private readonly bool origHighlightEffects;

        // BottomCorner style only
        private readonly bool onLeft;  // i wonder if this should just be setting based instead of mapper selected

        public TalkComponentAltUI(TalkComponent handler, string dialogId, Styles style, bool origHighlightEffects, bool onLeft) : base(handler) {
            Add(selectWiggle = Wiggler.Create(0.4f, 4f));
            this.dialogId = dialogId;
            this.Style = style;
            this.origHighlightEffects = origHighlightEffects;
            this.onLeft = onLeft;

            Depth = -100000;
            Tag |= Tags.TransitionUpdate; // don't get frozen during transition and suddenly disappear

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

            switch (Style) {
                case Styles.BottomCorner: {
                    // hello ??? indentation
                        var slideEase = Math.Min(highlightedEase, slide);

                        if (slideEase <= 0f || level.FrozenOrPaused || Handler.Entity == null)
                            return;

                        const float scale = 0.85f;
                        const int offscreenPadding = 32;

                        string label = Dialog.Clean(dialogId);
                        float width = ButtonUI.Width(label, Input.Talk);

                        var position = new Vector2 {
                            X = onLeft ?
                                40f - (40f + width * scale + offscreenPadding) * (1f - Ease.CubeOut(slideEase)) :
                                1880f + (40f + width * scale + offscreenPadding) * (1f - Ease.CubeOut(slideEase)),
                            Y = 1024f
                        };
                        // position.X += (40f + (num4 + num3) * num + (float)num2) * (1f - Ease.CubeOut(inputEase));

                        ButtonUI.Render(position, label, Input.Talk, scale, onLeft ? 0f : 1f, selectWiggle.Value * 0.05f, alpha: slideEase * 0.5f + 0.5f);

                        break;
                    }
                case Styles.SmallArrow: {
                    // i should probably clean this up but it works so like
                        if (level.FrozenOrPaused || slide <= 0f || Handler.Entity == null)
                            return;

                        var slideEase = Math.Min(highlightedEase, slide);

                        Vector2 camPos = level.Camera.Position.Floor();
                        Vector2 drawPos = Handler.Entity.Position + Handler.DrawAt - camPos;
                        if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
                            drawPos.X = 320f - drawPos.X;

                        drawPos.X *= 6f;
                        drawPos.Y *= 6f;
                        drawPos.Y += (float)Math.Sin(timer * 4f) * MathHelper.Lerp(12f, 6f, highlightedEase) + 64f * (1f - Ease.CubeOut(slide));
                        float wiggle = (!Highlighted) ? (1f + wiggler.Value * 0.5f) : (1f - wiggler.Value * 0.5f);
                        float trueAlpha = Ease.CubeInOut(slide) * alpha;
                        var color = lineColor * trueAlpha;

                        GFX.Gui["SorbetHelper/smallTalkArrow"].DrawJustified(drawPos - new Vector2(0f, 48f * Ease.CubeInOut(highlightedEase)), new Vector2(0.5f, 1f), color * alpha * Calc.ClampedMap(1f - highlightedEase, 0f, 0.75f), wiggle);

                        const float buttonUIScale = 0.75f;

                        string label = Dialog.Clean(dialogId);
                        var position = drawPos - new Vector2(0, 24f) + new Vector2(0, -8f) * highlightedEase;
                        ButtonUI.Render(position, label, Input.Talk, buttonUIScale * ((wiggle - 1f) * 0.5f + 1f), 0.5f, selectWiggle.Value * 0.05f, alpha: Calc.ClampedMap(highlightedEase, 0.25f, 1f) * trueAlpha);

                        break;
                    }
            }
        }

        // just skip any extra unneeded logic when becoming highlighted
        internal delegate void orig_set_Highlighted(TalkComponentUI self, bool value);
        internal static void On_set_Highlighted(orig_set_Highlighted orig, TalkComponentUI self, bool value) {
            if (self is TalkComponentAltUI { origHighlightEffects: false })
                self.highlighted = value;
            else
                orig(self, value);
        }
    }

    private static void IL_TalkComponent_Update(ILContext il) {
        var cursor = new ILCursor(il);

        // swaps out the vanilla talkcomponentui for a custom one if necessary
        cursor.GotoNext(MoveType.Before, instr => instr.MatchNewobj<TalkComponentUI>());

        var skipOrigTalkComponentUI = cursor.DefineLabel();

        // why ami like this,
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

            return new TalkComponentAltUI(self, altUITrigger.dialogId, altUITrigger.style, altUITrigger.origHighlightEffects, altUITrigger.onLeft);
        }

        // adjust the minimum hoverTimer required for custom talkcomponentuis to be "highlighted"
        cursor.Index = -1;
        cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(0.1f));
        cursor.EmitLdarg0();
        cursor.EmitLdfld(typeof(TalkComponent).GetField(nameof(TalkComponent.UI)));
        cursor.EmitDelegate(adjustHoverTimerForCustomUI);

        static float adjustHoverTimerForCustomUI(float orig, TalkComponentUI UI) {
            if (UI is TalkComponentAltUI { Style: TalkComponentAltUI.Styles.BottomCorner } )
                return 0f;

            return orig;
        }
    }

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
}
