using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class MiniPopupDisplay : Entity {
    private readonly MTexture fallbackBaseTex, fallbackAccentTex;
    private readonly List<Popup> popups = [];
    private readonly HashSet<Popup> toRemove = [];

    private float hiddenCountSlideLerp;

    private static int MiniPopupVisibleCap => Math.Min(SorbetHelperModule.Settings.MiniPopupVisibleCap, (int)(720f / (90f * MiniPopupScale)));
    private static float MiniPopupScale => SorbetHelperModule.Settings.MiniPopupScale + 0.1f;

    private MiniPopupDisplay() : base() {
        Tag |= Tags.Global | Tags.TransitionUpdate | Tags.FrozenUpdate | Tags.HUD;
        Depth = Depths.Top;

        fallbackBaseTex = GFX.Gui["SorbetHelper/popup"];
        fallbackAccentTex = GFX.Gui["SorbetHelper/popupAccent"];
    }

    // Creating the popup returns an action that removes the popup so the respective MiniPopupTrigger can remove it if its mode is set to WhilePlayerInside - grog
    public Action CreatePopup(float activeTime, string mainTextId, string subTextId) {
        Popup popup = new Popup(activeTime, mainTextId, subTextId, Color.Black, Color.LightCoral, Color.White);
        popups.Add(popup);
        return () => popup.Active = false;
    }

    public Action CreatePopup(float activeTime, string mainTextId, string subTextId, Color baseColor, Color accentColor, Color titleColor, string iconPath = null, string texturePath = null, int widthOverride = -1) {
        Popup popup = new Popup(activeTime, mainTextId, subTextId, baseColor, accentColor, titleColor, iconPath,
            texturePath, widthOverride);
        popups.Add(popup);
        return () => popup.Active = false;
    }

    public override void Update() {
        base.Update();

        int activeCount = Math.Min(popups.Count, MiniPopupVisibleCap);
        int hiddenCount = popups.Count - activeCount;
        hiddenCountSlideLerp = Calc.Approach(hiddenCountSlideLerp, hiddenCount > 0 ? 1f : 0f, Engine.DeltaTime / 0.5f);

        for (int i = 0; i < popups.Count; i++) {
            Popup popup = popups[i];

            // fixes silly stuff when adjusting the visible cap with a popup out
            if (i >= activeCount) {
                if (popup.Started)
                    popup.Reset();

                continue;
            }

            if (!popup.Routine.MoveNext())
                toRemove.Add(popup);
        }

        foreach (Popup popup in toRemove)
            popups.Remove(popup);
        toRemove.Clear();
    }

    public override void Render() {
        if (SceneAs<Level>().Paused)
            return;

        // base.Render();

        float scale = MiniPopupScale;
        float outlineStroke = SorbetHelperModule.Settings.MiniPopupScale switch {
            > 1.15f => 3f,
            < 0.85f => 1f,
            _ => 2f,
        };

        const float topYPos = 192f;
        const int distanceY = 90;
        float currentYPos = 0f;

        // todo: move some of this into a render method in the popup class? maybe
        int activeCount = Math.Min(popups.Count, MiniPopupVisibleCap);
        for (int i = 0; i < activeCount; i++) {
            Popup popup = popups[i];

            string mainText = Dialog.Clean(popup.MainTextID);
            string subText = Dialog.Clean(popup.SubTextID);
            MTexture icon = popup.Icon;

            MTexture bgTex = popup.BaseTexture ?? fallbackBaseTex;
            MTexture accentTex = popup.AccentTexture ?? fallbackAccentTex;
            float boxTexHeight = bgTex.Height * 0.5f;
            float boxTexWidth = accentTex.Width * 0.5f;

            const float mainTextScale = 0.9f;
            const float subTextScale = 0.65f;
            const int iconSize = 80;

            // why did i add scalingg  like its cool to have ig but omg its so messy help,
            // | 44px | text | 10px
            // | 44px | text | 10px | 80px img | 10px
            float textWidth = Math.Max(ActiveFont.Measure(mainText).X * mainTextScale, ActiveFont.Measure(subText).X * subTextScale);
            float textOffsetFromRight = icon is null ? 10f : 10f + iconSize + 10f;
            float width = MathF.Ceiling(scale * (popup.WidthOverride < 0f ? 44f + textWidth + textOffsetFromRight : popup.WidthOverride));
            Vector2 drawPos = new Vector2(1920 - width, topYPos) + new Vector2((width + 20) * Ease.CubeIn(1f - popup.SlideLerp), currentYPos);

            Draw.Rect(drawPos.X + scale * (boxTexWidth - 10), MathF.Round(drawPos.Y - scale * boxTexHeight / 2), width, MathF.Round(scale * boxTexHeight), popup.BaseColor);
            bgTex.DrawJustified(drawPos, new Vector2(0f, 0.5f), popup.BaseColor, scale * 0.5f);
            accentTex.DrawJustified(drawPos, new Vector2(0f, 0.5f), popup.AccentColor, scale * 0.5f);

            ActiveFont.DrawOutline(mainText, drawPos + new Vector2(width - scale * textOffsetFromRight, 0f), new Vector2(1f, 1f), new Vector2(scale * mainTextScale), popup.TitleColor * 0.8f, outlineStroke, popup.BaseColor);
            ActiveFont.Draw(subText, drawPos + new Vector2(width - scale * textOffsetFromRight, scale * -12f), new Vector2(1f, 0f), new Vector2(scale * subTextScale), popup.AccentColor * 0.8f);

            icon?.Draw(drawPos + new Vector2(width - scale * (iconSize + 10f), scale * -60f), Vector2.Zero, Color.White, new Vector2(scale * iconSize / icon.Width, scale * iconSize / icon.Height));

            currentYPos += scale * distanceY * Ease.CubeInOut(popup.FinishedMoveUpLerp);
        }

        int hiddenCount = popups.Count - activeCount;
        if (hiddenCountSlideLerp > 0f) {
            float boxTexHeight = fallbackBaseTex.Height * 0.5f;
            float boxTexWidth = fallbackBaseTex.Width * 0.5f;

            float width = scale * 140;
            Vector2 drawPos = new Vector2(1920 - width, topYPos) + new Vector2((width + 20) * Ease.CubeIn(1f - hiddenCountSlideLerp), scale * distanceY * MiniPopupVisibleCap - 5);

            Draw.Rect(drawPos.X + scale * (boxTexWidth * 0.5f - 10), MathF.Round(drawPos.Y - scale * (15f + 0.25f * boxTexHeight)), width, MathF.Round(scale * boxTexHeight * 0.5f), Color.Black);
            fallbackBaseTex.DrawJustified(drawPos - scale * new Vector2(0f, 15f), new Vector2(0f, 0.5f), Color.Black, scale * 0.5f * new Vector2(1f, 0.5f));
            ActiveFont.DrawOutline($"+ {hiddenCount}", drawPos + scale * new Vector2(32f, 1f), new Vector2(0f, 1f), new Vector2(scale * 1f), Color.LightGray * 0.6f, 2f, Color.Black);
        }
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        popups.Clear();
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        popups.Clear();
    }

    public static MiniPopupDisplay GetMiniPopupDisplay(Scene scene) {
        if (scene.Tracker.GetEntities<MiniPopupDisplay>()
                 .Concat(scene.Entities.ToAdd)
                 .FirstOrDefault(e => e is MiniPopupDisplay)
            is not MiniPopupDisplay miniPopupDisplay) {
            scene.Add(miniPopupDisplay = new MiniPopupDisplay());
            Logger.Info("SorbetHelper", $"creating new {nameof(MiniPopupDisplay)}.");
        }

        return miniPopupDisplay;
    }

    private class Popup {
        public IEnumerator Routine;
        public float SlideLerp;
        public float FinishedMoveUpLerp = 1f;
        public bool Started;

        // probably should be privateeee but i was evil apparently and didnt put popup rendering into a render method in the popup class
        public readonly Color BaseColor, AccentColor, TitleColor;
        public readonly string MainTextID, SubTextID;
        public readonly MTexture Icon;
        public readonly MTexture BaseTexture, AccentTexture;
        public readonly int WidthOverride;

        private readonly float activeTime;
        public bool Active = true;

        public Popup(float activeTime, string mainTextId, string subTextId, Color baseColor, Color accentColor, Color titleColor, string iconPath = null, string texturePath = null, int widthOverride = -1) {
            this.activeTime = activeTime;
            Reset();

            MainTextID = mainTextId;
            SubTextID = subTextId;
            BaseColor = baseColor;
            AccentColor = accentColor;
            TitleColor = titleColor;
            Icon = string.IsNullOrEmpty(iconPath) ? null : GFX.Gui[iconPath];
            BaseTexture = string.IsNullOrEmpty(texturePath) ? null : GFX.Gui[texturePath];
            AccentTexture = string.IsNullOrEmpty(texturePath) ?  null : GFX.Gui[texturePath + "Accent"];

            WidthOverride = widthOverride;
        }

        private IEnumerator PopupRoutine() {
            Started = true;

            // slide in
            while (SlideLerp < 1f) {
                yield return null;
                SlideLerp += Engine.DeltaTime / 0.5f;
            }

            // stay around for a bit
            // (if the mode is WhilePlayerInside listen for when active is set to false by the trigger, otherwise do the normal behavior)
            if (activeTime < 0f) {
                while (Active) {
                    yield return null;
                }
            } else {
                float activeTimer = activeTime;
                while (Active && activeTimer > 0f) {
                    yield return null;
                    activeTimer -= Engine.DeltaTime;
                }
            }

            // slide out
            while (SlideLerp > 0f) {
                yield return null;
                SlideLerp -= Engine.DeltaTime / 0.5f;
            }

            // lets any other popups below smoothly move up
            while (FinishedMoveUpLerp > 0f) {
                yield return null;
                FinishedMoveUpLerp -= Engine.DeltaTime / 0.5f;
            }
        }

        public void Reset() {
            Started = false;
            Routine = PopupRoutine();
            SlideLerp = 0f;
            FinishedMoveUpLerp = 1f;
        }
    }
}
