using ModInteropImportGenerator;

namespace Celeste.Mod.SorbetHelper.Imports;

// wehh the modinteropimportgenerator sourcegen doesnt work on nested classes
[GenerateImports("CommunalHelper.DashStates", RequiredDependency = false)]
public static partial class CommunalHelperDashStates
{
    public static partial Component DreamTunnelInteraction(Action<Player> onPlayerEnter, Action<Player> onPlayerExit);
}
