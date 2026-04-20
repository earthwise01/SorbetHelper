namespace Celeste.Mod.SorbetHelper;

public class SorbetHelperModule : EverestModule
{
    public static SorbetHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(SorbetHelperSettings);
    public static SorbetHelperSettings Settings => (SorbetHelperSettings)Instance._Settings;
    public override Type SessionType => typeof(SorbetHelperSession);
    public static SorbetHelperSession Session => (SorbetHelperSession)Instance._Session;

    public SorbetHelperModule()
    {
        Instance = this;
    }

    public override void Initialize()
    {
        SorbetHelperImports.Initialize();
    }

    public override void Load()
    {
        SorbetHelperExports.Load();
        SorbetHelperDecalRegistry.LoadHandlers();

        LifecycleMethods.OnLoad();
    }

    public override void LoadContent(bool firstLoad)
    {
        SorbetHelperGFX.LoadContent(firstLoad);

        LifecycleMethods.OnLoadContent(firstLoad);
    }

    public override void Unload()
    {
        SorbetHelperGFX.UnloadContent();

        LifecycleMethods.OnUnload();
    }

    public override void PrepareMapDataProcessors(MapDataFixup context)
    {
        base.PrepareMapDataProcessors(context);

        context.Add<SorbetHelperMapDataProcessor>();
    }
}
