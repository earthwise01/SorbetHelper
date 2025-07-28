
namespace Celeste.Mod.SorbetHelper;

public class SorbetHelperMetadataYaml {
    public SorbetHelperMetadata SorbetHelperMetadata { get; set; } = new SorbetHelperMetadata();
}

public class SorbetHelperMetadata {
    // todo: cache this instead of deserializing it every single time
    public static SorbetHelperMetadata TryGetMetadata(Session session) {
        if (!Everest.Content.TryGet($"Maps/{session.MapData.Filename}.meta", out ModAsset asset))
            return null;
        if (asset is null)
            return null;
        if (!asset.PathVirtual.StartsWith("Maps"))
            return null;
        if (!asset.TryDeserialize(out SorbetHelperMetadataYaml meta))
            return null;

        return meta.SorbetHelperMetadata;
    }

    public class HiResRenderingData {
        public bool Enabled { get; set; } = false;
        public bool HiResLighting { get; set; } = false;
        public bool HiResBloom { get; set; } = false;
        public bool HiResDistort { get; set; } = false;
    }

    public HiResRenderingData HiResRendering { get; set; } = new HiResRenderingData();
}
