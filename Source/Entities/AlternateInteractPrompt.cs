using System;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TalkComponentUI = Celeste.TalkComponent.TalkComponentUI;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
[CustomEntity("SorbetHelper/AlternateInteractPrompt")]
public class AlternateInteractPromptWrapper : Entity {
    private readonly string dialogId;
    private readonly bool onLeft;
    private readonly bool origHighlightEffects;

    public AlternateInteractPromptWrapper(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);
        dialogId = data.Attr("dialogId", "sorbethelper_ui_talk");
        onLeft = data.Bool("onLeft", false);
        origHighlightEffects = data.Bool("playHighlightSfx", false);
    }

    public class TalkComponentAltUI : TalkComponentUI {
        private readonly Wiggler selectWiggle;
        private float selectWiggleDelay;
        private float highlightedEase;

        private readonly string dialogId;
        private readonly bool onLeft;  // i wonder if this should just be setting based instead of mapper selected
        private readonly bool origHighlightEffects;

        public TalkComponentAltUI(TalkComponent handler, string dialogId, bool onLeft, bool origHighlightEffects) : base(handler) {
            Add(selectWiggle = Wiggler.Create(0.4f, 4f));
            this.dialogId = dialogId;
            this.onLeft = onLeft;
            this.origHighlightEffects = origHighlightEffects;

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

            var slideEase = Math.Min(highlightedEase, slide);

            if (slideEase <= 0f || level.Frozen || Handler.Entity == null)
                return;

            const float scale = 0.8f;
            const int offscreenPadding = 32;

            string label = Dialog.Clean(dialogId);
            // string label2 = Dialog.Clean("ui_confirm");
            float width = ButtonUI.Width(label, Input.Talk);
            // float num4 = ButtonUI.Width(label2, Input.MenuConfirm);

            var position = new Vector2 {
                X = onLeft ?
                    40f - (40f + width * scale + offscreenPadding) * (1f - Ease.CubeOut(slideEase)) :
                    1880f + (40f + width * scale + offscreenPadding) * (1f - Ease.CubeOut(slideEase)),
                Y = 1024f
            };
            // position.X += (40f + (num4 + num3) * num + (float)num2) * (1f - Ease.CubeOut(inputEase));

            ButtonUI.Render(position, label, Input.Talk, scale, onLeft ? 0f : 1f, selectWiggle.Value * 0.05f, alpha: slideEase * 0.5f + 0.5f);
            // if (Overworld.ShowConfirmUI) {
            //     position.X -= num * num3 + (float)num2;
            //     ButtonUI.Render(position, label2, Input.MenuConfirm, num, 1f, confirmWiggle.Value * 0.05f);
            // }
                //             X = onLeft switch {
                //     false => 1880f + (40f + width * scale + offscreenPadding) * (1f - Ease.CubeOut(slideEase)),
                //     true => 40f - (40f + width * scale + offscreenPadding) * (1f - Ease.CubeOut(slideEase))
                // },
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

            return new TalkComponentAltUI(self, altUITrigger.dialogId, altUITrigger.onLeft, altUITrigger.origHighlightEffects);
        }

        // adjust the minimum hoverTimer required for custom talkcomponentuis to be "highlighted"
        cursor.Index = -1;
        cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(0.1f));
        cursor.EmitLdarg0();
        cursor.EmitLdfld(typeof(TalkComponent).GetField(nameof(TalkComponent.UI)));
        cursor.EmitDelegate(adjustHoverTimerForCustomUI);

        static float adjustHoverTimerForCustomUI(float orig, TalkComponentUI UI) {
            if (UI is TalkComponentAltUI)
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
