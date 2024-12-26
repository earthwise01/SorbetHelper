using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste;
using Celeste.Mod.SorbetHelper.Entities;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Components;

/// <summary>
/// replaces the call to its entity's render method in entitylist.renderexcept with its own entityrenderoverride method<br/>
/// intended to have other components extend from it<br/>
/// originally part of depthadheringdisplacementrenderhook
/// </summary>
[Tracked(true)]
public class RenderOverride : Component {
    public RenderOverride(bool active, bool visible) : base(active, visible) {

    }

    /// <summary>
    /// replaces the entity's render method, by default just calls it anyway<br/>
    /// not called if either visible or entity.visible is false
    /// </summary>
    public virtual void EntityRenderOverride() {
        Entity.Render();
    }

    internal static void Load() {
        IL.Monocle.EntityList.RenderExcept += modEntityListRenderExcept;
    }

    internal static void Unload() {
        IL.Monocle.EntityList.RenderExcept -= modEntityListRenderExcept;
    }

    // this is probably all extremely inefficient but alskdjf it works and im lazy so
    private static void modEntityListRenderExcept(ILContext il) {
        var cursor = new ILCursor(il);

        var needToCheckComponents = new VariableDefinition(il.Import(typeof(bool)));
        il.Body.Variables.Add(needToCheckComponents);

        // get render hook list
        cursor.EmitLdarg0();
        cursor.EmitCallvirt(typeof(EntityList).GetMethod("get_Scene"));
        cursor.EmitDelegate(getComponentsExist);
        cursor.EmitStloc(needToCheckComponents);

        ILLabel label = cursor.DefineLabel();

        // need to match against the tag check instead of render to attempt to avoid confilcts
        // hopefully this doesnt break anything else!!! (pain)
        if (cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchCallvirt<Entity>("TagCheck"))) {
            // check if the entity has a RenderOverrideComponent, if it does skip rendering the entity and render it instead
            Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting check to render RenderOverrideComponents instead of their entity at {cursor.Index} in CIL code for {cursor.Method.FullName}");
            cursor.EmitLdloc1();
            cursor.EmitLdloc(needToCheckComponents);
            cursor.EmitDelegate(checkSkipEntity);
        } else {
            Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject check to render RenderOverrideComponents instead of their entity in CIL code for {cursor.Method.FullName}");
        }
    }

    private static bool getComponentsExist(Scene scene) {
        if (scene is null)
            return false;

        return scene.Tracker.GetComponents<RenderOverride>().Count != 0;
    }

    // returns true if entity.render should get skipped
    private static bool checkSkipEntity(bool origShouldSkip, Entity entity, bool needToCheckComponents) {
        if (origShouldSkip || !needToCheckComponents)
            return origShouldSkip;

        var renderHook = entity.Get<RenderOverride>();
        if (renderHook is not null) {
            if (renderHook.Visible)
                renderHook.EntityRenderOverride();

            return true;
        }

        return origShouldSkip;
    }
}
