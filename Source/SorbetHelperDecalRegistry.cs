using System.Xml;
using Celeste.Mod.Registry.DecalRegistryHandlers;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper;

public static class SorbetHelperDecalRegistry {
    private class LightCoverHandler : DecalRegistryHandler {
        public override string Name => "sorbetHelper_lightCover";

        private int minDepth, maxDepth;
        private float alpha;

        public override void ApplyTo(Decal decal) {
            if (decal.Depth.IsInRange(minDepth, maxDepth))
                decal.Add(new LightCover(alpha));
        }

        public override void Parse(XmlAttributeCollection xml) {
            // defaults to make only decals above the player cover light
            minDepth = Get(xml, "minimumDepth", int.MinValue);
            maxDepth = Get(xml, "maximumDepth", -1);
            alpha = Get(xml, "alpha", 1f);
        }
    }

    private class DecalStylegroundHandler : DecalRegistryHandler {
        public override string Name => "sorbetHelper_styleground";

        private int minDepth, maxDepth;
        private string tag;

        public override void ApplyTo(Decal decal) {
            if (decal.Depth.IsInRange(minDepth, maxDepth))
                decal.Add(new EntityStylegroundMarker(tag));
        }

        public override void Parse(XmlAttributeCollection xml) {
            minDepth = Get(xml, "minimumDepth", int.MinValue);
            maxDepth = Get(xml, "maximumDepth", int.MaxValue);
            tag = GetString(xml, "rendererTag", "");
        }
    }

    internal static void LoadHandlers() {
        DecalRegistry.AddPropertyHandler<LightCoverHandler>();
        DecalRegistry.AddPropertyHandler<DecalStylegroundHandler>();
    }
}
