using ModInteropImportGenerator;

namespace Celeste.Mod.SorbetHelper.Imports;

[GenerateImports("GravityHelper", RequiredDependency = false)]
public static partial class GravityHelper
{
    public static partial bool IsPlayerInverted();
}
