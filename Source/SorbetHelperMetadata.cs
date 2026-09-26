// i loove stealing code from fc and aonhelper :revolving_hearts:

namespace Celeste.Mod.SorbetHelper;

public class SorbetHelperMetadata
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(SorbetHelperMetadata)}";

    private static readonly Dictionary<string, SorbetHelperMetadata> CachedMetadata = [];

    #region Metadata Properties

    private class SorbetHelperYaml
    {
        public SorbetHelperMetadata SorbetHelperMetadata { get; set; } = null;
    }

    public class CustomStarfieldWipeSettingsData
    {
        public int StarPoints { get; set; } = CustomStarfieldWipe.DefaultStarPoints;
        public float StarPointLength { get; set; } = CustomStarfieldWipe.DefaultStarPointLength;
        public float? StarPointinessAngle { get; set; } = null;
        public float StarScale { get; set; } = CustomStarfieldWipe.DefaultStarScale;
        public float StarRotation { get; set; } = CustomStarfieldWipe.DefaultStarRotation;
        public float StarRotationRange { get; set; } = CustomStarfieldWipe.DefaultStarRotationRange;
    }

    public CustomStarfieldWipeSettingsData CustomStarfieldWipeSettings { get; set; } = CustomStarfieldWipe.DefaultSettings;

    #endregion

    public static bool TryGetMetadata(AreaKey areaKey, out SorbetHelperMetadata metadata)
    {
        metadata = null;

        if (CachedMetadata.TryGetValue(areaKey.SID, out metadata))
            return metadata is not null;

        // todo: hmm would this have any weird behaviour with b/c sides
        string filename = AreaData.Get(areaKey).Mode[(int)areaKey.Mode].Path;

        if (Everest.Content.TryGet<AssetTypeYaml>($"Maps/{filename}.meta", out ModAsset asset)
            && asset is not null
            && asset.PathVirtual.StartsWith("Maps")
            && asset.TryDeserialize(out SorbetHelperYaml meta)
            && meta?.SorbetHelperMetadata is { } deserialized)
        {
            Logger.Info(LogID, $"Caching Sorbet Helper Metadata for '{areaKey.SID}' from 'Maps/{filename}.meta.yaml'");

            metadata = CachedMetadata[areaKey.SID] = deserialized;
            return true;
        }

        Logger.Info(LogID, $"No Sorbet Helper Metadata found for '{areaKey.SID}' from 'Maps/{filename}.meta.yaml'.");

        CachedMetadata[areaKey.SID] = null;
        return false;
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        Everest.Content.OnUpdate += OnContentUpdate;
    }

    [OnUnload]
    internal static void Unload()
    {
        Everest.Content.OnUpdate -= OnContentUpdate;
    }

    private static void OnContentUpdate(ModAsset old, ModAsset _)
    {
        // maybe a bit overkill
        // (could try to clear the cache only for the reloaded meta.yaml ? but that may also be a bit overkill in the other direction hmm)
        if (old is not null
            && old.Type == typeof(AssetTypeYaml)
            && old.PathVirtual.StartsWith("Maps")
            && old.PathVirtual.EndsWith(".meta"))
            CachedMetadata.Clear();
    }

    #endregion
}
