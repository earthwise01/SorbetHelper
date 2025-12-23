using System;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
// hold myself

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class MiniPopupDisplay : Entity {
    private readonly MTexture fallbackBaseTex, fallbackAccentTex;
    private readonly List<Popup> Popups = [];
    private readonly HashSet<Popup> ToRemove = [];

    private float hiddenCountSlideLerp;

    private static int MiniPopupVisibleCap => Math.Min(SorbetHelperModule.Settings.MiniPopupVisibleCap, (int)(720f / (90 * MiniPopupScale)));
    private static float MiniPopupScale => SorbetHelperModule.Settings.MiniPopupScale + 0.1f;

    public MiniPopupDisplay() : base() {
        Tag |= Tags.Global | Tags.TransitionUpdate | Tags.FrozenUpdate | Tags.HUD;//TagsExt.SubHUD; // can't be subhud because it looks weird going behind talkcomponentuis (although i wonder if being able to make those subhud would also be nice)
        Depth = Depths.Top;

        // could make customizable maybe?    done
        fallbackBaseTex = GFX.Gui["SorbetHelper/popup"];
        fallbackAccentTex = GFX.Gui["SorbetHelper/popupAccent"];
    }

    // i wonder if i shd add a cooldown for adding popups so if multiple are added simultaneously they slide in more gradually vs all at once,
    // Creating the popup returns an action that removes the popup so the respective MiniPopupTrigger can remove it if its mode is set to WhilePlayerInside - grog
    public Action CreatePopup(float activeTime, string mainTextId, string subTextId) {
        Popup popup = new Popup(activeTime, mainTextId, subTextId, Color.Black, Color.LightCoral, Color.White);
        Popups.Add(popup);
        return () => {popup.active = false;};
    }

    public Action CreatePopup(float activeTime, string mainTextId, string subTextId, Color baseColor, Color accentColor, Color titleColor, string iconPath = null, string texturePath = null, int widthOverride = -1) {
        Popup popup = new Popup(activeTime, mainTextId, subTextId, baseColor, accentColor, titleColor, iconPath,
            texturePath, widthOverride);
        Popups.Add(popup);
        return () => {popup.active = false;};
    }


    public override void Update() {
        base.Update();

        int activeCount = Math.Min(Popups.Count, MiniPopupVisibleCap);
        int hiddenCount = Popups.Count - activeCount;
        hiddenCountSlideLerp = Calc.Approach(hiddenCountSlideLerp, hiddenCount > 0 ? 1f : 0f, Engine.DeltaTime / 0.5f);

        for (int i = 0; i < Popups.Count; i++) {
            var popup = Popups[i];

            // fixes silly stuff when adjusting the visible cap with a popup out
            if (i >= activeCount) {
                if (popup.Started)
                    popup.Reset();

                continue;
            }

            if (!popup.Routine.MoveNext())
                ToRemove.Add(popup);
        }

        foreach (var popup in ToRemove)
            Popups.Remove(popup);
        ToRemove.Clear();
    }

    public override void Render() {
        if ((Scene as Level).Paused)
            return;

        // base.Render();

        var scale = MiniPopupScale;
        var outlineStroke = SorbetHelperModule.Settings.MiniPopupScale switch {
            > 1.15f => 3f,
            < 0.85f => 1f,
            _ => 2f,
        };

        const float topYPos = 192f;
        const int distanceY = 90;
        var currentYPos = 0f;

        int activeCount = Math.Min(Popups.Count, MiniPopupVisibleCap);
        for (int i = 0; i < activeCount; i++) {
            var popup = Popups[i];

            var mainText = Dialog.Clean(popup.MainTextID);
            var subText = Dialog.Clean(popup.SubTextID);
            var icon = popup.Icon;

            var bgTex = popup.BaseTexture ?? fallbackBaseTex;
            var accentTex = popup.AccentTexture ?? fallbackAccentTex;
            var boxTexHeight = bgTex.Height * 0.5f;
            var boxTexWidth = accentTex.Width * 0.5f;

            const float mainTextScale = 0.9f;
            const float subTextScale = 0.65f;
            const int iconSize = 80;

            // why did i add scalingg  like its cool to have ig but omg its so messy help,
            // | 44px | text | 10px
            // | 44px | text | 10px | 80px img | 10px
            var textWidth = Math.Max(ActiveFont.Measure(mainText).X * mainTextScale, ActiveFont.Measure(subText).X * subTextScale);
            var textOffsetFromRight = icon is null ? 10f : 10f + iconSize + 10f;
            var width = MathF.Ceiling(scale * (popup.WidthOverride < 0f ? 44f + textWidth + textOffsetFromRight : popup.WidthOverride));
            var drawPos = new Vector2(1920 - width, topYPos) + new Vector2((width + 20) * Ease.CubeIn(1f - popup.SlideLerp), currentYPos);

            Draw.Rect(drawPos.X + scale * (boxTexWidth - 10), MathF.Round(drawPos.Y - scale * boxTexHeight / 2), width, MathF.Round(scale * boxTexHeight), popup.BaseColor);
            bgTex.DrawJustified(drawPos, new Vector2(0f, 0.5f), popup.BaseColor, scale * 0.5f);
            accentTex.DrawJustified(drawPos, new Vector2(0f, 0.5f), popup.AccentColor, scale * 0.5f);

            ActiveFont.DrawOutline(mainText, drawPos + new Vector2(width - scale * textOffsetFromRight, 0f), new Vector2(1f, 1f), new Vector2(scale * mainTextScale), popup.TitleColor * 0.8f, outlineStroke, popup.BaseColor);
            ActiveFont.Draw(subText, drawPos + new Vector2(width - scale * textOffsetFromRight, scale * -12f), new Vector2(1f, 0f), new Vector2(scale * subTextScale), popup.AccentColor * 0.8f);

            icon?.Draw(drawPos + new Vector2(width - scale * (iconSize + 10f), scale * -60f), Vector2.Zero, Color.White, new Vector2(scale * iconSize / icon.Width, scale * iconSize / icon.Height));

            currentYPos += scale * distanceY * Ease.CubeInOut(popup.FinishedMoveUpLerp);
        }

        int hiddenCount = Popups.Count - activeCount;
        if (hiddenCountSlideLerp > 0f) {
            var boxTexHeight = fallbackBaseTex.Height * 0.5f;
            var boxTexWidth = fallbackBaseTex.Width * 0.5f;

            var width = scale * 140;
            var drawPos = new Vector2(1920 - width, topYPos) + new Vector2((width + 20) * Ease.CubeIn(1f - hiddenCountSlideLerp), scale * distanceY * MiniPopupVisibleCap - 5);

            Draw.Rect(drawPos.X + scale * (boxTexWidth * 0.5f - 10), MathF.Round(drawPos.Y - scale * (15f + 0.25f * boxTexHeight)), width, MathF.Round(scale * boxTexHeight * 0.5f), Color.Black);
            fallbackBaseTex.DrawJustified(drawPos - scale * new Vector2(0f, 15f), new Vector2(0f, 0.5f), Color.Black, scale * 0.5f * new Vector2(1f, 0.5f));
            ActiveFont.DrawOutline($"+ {hiddenCount}", drawPos + scale * new Vector2(32f, 1f), new Vector2(0f, 1f), new Vector2(scale * 1f), Color.LightGray * 0.6f, 2f, Color.Black);
        }

        /* backup before trying some tweaks + scaling
        for (int i = 0; i < activeCount; i++) {
            var popup = Popups[i];

            var mainText = Dialog.Clean(popup.MainText);
            var subText = Dialog.Clean(popup.SubText);
            var icon = popup.Icon;
            // | 40px | text | 10px
            // | 32px | 80px img | 18px | text
            // | 32px | 80px img | 18px | text
            var width = MathF.Ceiling(popup.WidthOverride < 0f ? Math.Max(ActiveFont.Measure(mainText).X * 0.75f, ActiveFont.Measure(subText).X * 0.5f) + 50f + (icon is null ? 0f : 80f) : popup.WidthOverride);
            var drawPos = new Vector2(1920 - width, 192f) + new Vector2((width + 20) * Ease.CubeIn(1f - popup.SlideLerp), currentYPos);

            Draw.Rect(drawPos.X + 100, drawPos.Y - 26, width, 52, popup.BaseColor);
            bgTex.DrawJustified(drawPos, new Vector2(0f, 0.5f), popup.BaseColor);
            accentTex.DrawJustified(drawPos, new Vector2(0f, 0.5f), popup.AccentColor);

            ActiveFont.DrawOutline(mainText, drawPos + new Vector2(width - 10, 1f), new Vector2(1f, 1f), new Vector2(0.75f), popup.TitleColor * 0.8f, 2f, popup.BaseColor);
            ActiveFont.Draw(subText, drawPos + new Vector2(width - 10, -8f), new Vector2(1f, 0f), new Vector2(0.5f), popup.AccentColor * 0.8f);

            icon?.Draw(drawPos + new Vector2(32f, -60f), Vector2.Zero, Color.White, new Vector2(80f / icon.Width, 80f / icon.Height));

            currentYPos += 90 * Ease.CubeInOut(popup.FinishedMoveUpLerp);
        }
        */
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        Popups.Clear();
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        Popups.Clear();
    }

    private class Popup {
        public IEnumerator Routine;
        public float SlideLerp;
        public float FinishedMoveUpLerp = 1f;
        public bool Started;

        public Color BaseColor, AccentColor, TitleColor;
        public string MainTextID, SubTextID;
        public MTexture Icon = null;
        public MTexture BaseTexture, AccentTexture = null;
        public int WidthOverride;

        private readonly float activeTime;
        public bool active = true;

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
            // BaseColor = Color.Lerp(Color.Black, Color.DarkSlateGray, 0.25f);
            // AccentColor = Color.Lerp(Color.DarkGoldenrod, Color.DimGray, 0.2f);
            // new Color(12, 20, 20), new Color(125, 145, 185)

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
            // (if the mode is WhilePlayerInside listen for when active is set to false by the trigger, otherwise do the normal behavior
            float activeTimer = activeTime;
            if (activeTimer == -1)
                while (active) {
                    yield return null;
                }
            else
                while (activeTimer > 0f) {
                    yield return null;
                    activeTimer -= Engine.DeltaTime;
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

    private static void OnLoadingThread(Level level) {
        level.Add(new MiniPopupDisplay()); // only add if triggers are found maybe???
    }

    internal static void Load() {
        Everest.Events.LevelLoader.OnLoadingThread += OnLoadingThread;
    }

    internal static void Unload() {
        Everest.Events.LevelLoader.OnLoadingThread -= OnLoadingThread;
    }
}
