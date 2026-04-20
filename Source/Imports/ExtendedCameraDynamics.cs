using ModInteropImportGenerator;

namespace Celeste.Mod.SorbetHelper.Imports;

[GenerateImports("ExtendedCameraDynamics", RequiredDependency = false)]
public static partial class ExtendedCameraDynamics
{
    public static partial bool ExtendedCameraHooksEnabled();
}

