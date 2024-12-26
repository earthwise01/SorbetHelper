using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Backdrops;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity("SorbetHelper/StylegroundEntityController")]
public class StylegroundEntityController : Entity {
    private readonly BackdropRenderer BackdropRenderer = new();

    public readonly string StylegroundTag;

    public StylegroundEntityController(EntityData data, Vector2 _) {
        Depth = data.Int("depth", Depths.Above);
        StylegroundTag = data.Attr("tag", "");

        Add(new BeforeRenderHook(BeforeRender));
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        if (string.IsNullOrEmpty(StylegroundTag)) {
            RemoveSelf();
            return;
        }

        ConsumeStylegrounds(scene as Level);
    }

    public override void Update() {
        base.Update();
        BackdropRenderer.Update(Scene);
    }

    public void BeforeRender() {
        BackdropRenderer.BeforeRender(Scene);
    }

    public override void Render() {
        GameplayRenderer.End();
        BackdropRenderer.Render(Scene);
        GameplayRenderer.Begin();
        // backdrop renderer doesnt implement afterrender
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        BackdropRenderer.Ended(scene);
    }

    private void ConsumeStylegrounds(Level level) {
        ConsumeStylegrounds(level.Foreground.Backdrops);
        ConsumeStylegrounds(level.Background.Backdrops);
        Logger.Log(LogLevel.Verbose, "SorbetHelper", "[StylegroundEntityDepthController] consumed stylegrounds!");
    }

    private void ConsumeStylegrounds(List<Backdrop> origBackdrops) {
        // i dont know why the fk i need to do this but for some bizarre reason it literally doesnt work if i iterate through the list forwards catplush
        for (int i = origBackdrops.Count - 1; i >= 0; i--) {
            // for (int i = 0; i < origBackdrops.Count; i++) {
            var backdrop = origBackdrops[i];

            foreach (string tag in backdrop.Tags) {
                if (tag == StylegroundTag) {
                    BackdropRenderer.Backdrops.Insert(0, backdrop);
                    backdrop.Renderer = BackdropRenderer;
                    origBackdrops.RemoveAt(i);
                }
            }
        }
    }
}

