using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Celeste.Mod.Helpers;

namespace Celeste.Mod.SorbetHelper.Entities;

/// <summary>
/// base class for per-room controllers that affect entities based on their runtime class type<br/>
/// <br/>
/// checks for the following entitydata attributes:<br/>
/// classNames - string, comma seperated list of type names to affect<br/>
/// useFullClassNames - bool, whether to use fully qualified type names, e.g. SummitCloud vs Celeste.SummitCloud<br/>
/// minDepth - int, the minimum (closest to the camera) depth an entity can have while being affected by the controller<br/>
/// maxDepth - int, the maximum (farthest from the camera) depth an entity can have while being affected by the controller<br/>
/// <br/>
/// processes entities during its Awake method
/// </summary>
public abstract class ClassControllerBase : Entity {
    protected readonly HashSet<string> ClassNames;
    protected readonly bool UseFullClassName;
    protected readonly int MinDepth, MaxDepth;

    public ClassControllerBase(EntityData data) {
        ClassNames = [.. data.List("classNames", str => str)];
        UseFullClassName = data.Bool("useFullClassNames", false);
        MinDepth = data.Int("minDepth", int.MinValue);
        MaxDepth = data.Int("maxDepth", int.MaxValue);
    }

    public abstract void ProcessEntity(Entity entity);

    public override void Awake(Scene scene) {
        base.Awake(scene);

        foreach (var entity in scene.Entities) {
            // might try looking into how other mods deal with class names sometime in case i could be doing this in a more performant way
            var className = UseFullClassName ? entity.GetType().FullName : entity.GetType().Name;
            if (ClassNames.Contains(className) && entity.Depth.IsInRange(MinDepth, MaxDepth)) {
                ProcessEntity(entity);
            }
        }
    }
}

/// <summary>
/// base class for global controllers that affect entities based on their runtime class type<br/>
/// <br/>
/// checks for the following entitydata attributes:<br/>
/// classNames - string, comma seperated list of type names to affect<br/>
/// useFullClassNames - bool, whether to use fully qualified type names, e.g. SummitCloud vs Celeste.SummitCloud<br/>
/// minDepth - int, the minimum (closest to the camera) depth an entity can have while being affected by the controller<br/>
/// maxDepth - int, the maximum (farthest from the camera) depth an entity can have while being affected by the controller<br/>
/// <br/>
/// processes entities after *their own* Awake methods
/// </summary>

// slightly more performant than the non global one during screen transitions i think? due to only having to go through newly added entities instead of *all* entities
// ..but does come with the cost that it uses a hook and could check more often because of working based on entity awake instead of only on room load
[Tracked(true)]
public abstract class GlobalClassControllerBase : Entity {
    protected readonly HashSet<string> ClassNames;
    protected readonly bool UseFullClassName;
    protected readonly int MinDepth, MaxDepth;

    public GlobalClassControllerBase(EntityData data) {
        ClassNames = [.. data.List("classNames", str => str)];
        UseFullClassName = data.Bool("useFullClassNames", false);
        MinDepth = data.Int("minDepth", int.MinValue);
        MaxDepth = data.Int("maxDepth", int.MaxValue);
    }

    public abstract void ProcessEntity(Entity entity);

    // note that this runs when an entity calls *base.Awake* and not when its own Awake is called
    // might be better as an ilhook to entitylist.updatelists?
    // private static void On_Entity_Awake(On.Monocle.Entity.orig_Awake orig, Entity self, Scene scene) {
    //     orig(self, scene);

    //     var globalClassModifiers = scene.Tracker.GetEntities<GlobalClassNameControllerBase>();
    //     if (globalClassModifiers.Count == 0)
    //         return;

    //     var type = self.GetType();
    //     var fullName = type.FullName;
    //     var name = type.Name;
    //     foreach (var entity in globalClassModifiers) {
    //         var classModifier = entity as GlobalClassNameControllerBase;
    //         if (classModifier.ClassNames.Contains(classModifier.UseFullClassName ? fullName : name) && entity.Depth.IsInRange(classModifier.MinDepth, classModifier.MaxDepth)) {
    //             classModifier.ProcessEntity(self);
    //         }
    //     }
    // }

    // the ilhook in question
    private static void IL_EntityList_UpdateLists(ILContext il) {
        var cursor = new ILCursor(il) {
            Index = -1
        };

        var globalControllers = new VariableDefinition(il.Import(typeof(List<Entity>)));
        il.Body.Variables.Add(globalControllers);

        var globalControllersIsNull = cursor.DefineLabel();

        cursor.GotoPrevBestFit(MoveType.After, i => i.MatchLdfld<EntityList>("adding"), i => i.MatchCallOrCallvirt<HashSet<Entity>>("Clear"));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(getGlobalControllers);
        cursor.EmitStloc(globalControllers);

        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Entity>("Awake"));
        cursor.EmitLdloc(globalControllers);
        cursor.EmitBrfalse(globalControllersIsNull);

        cursor.EmitLdloc(5); // entity
        cursor.EmitLdloc(globalControllers);
        cursor.EmitDelegate(processEntity);

        cursor.MarkLabel(globalControllersIsNull);

        static List<Entity> getGlobalControllers(EntityList entities) {
            var globalClassControllers = entities.Scene.Tracker.GetEntities<GlobalClassControllerBase>();
            return globalClassControllers.Count == 0 ? null : globalClassControllers; // return null if count is 0 to (hopefully) speed up the check for whether to process entities
        }

        static void processEntity(Entity entity, List<Entity> globalClassControllers) {
            // dont need this check bc process entity gets skipped if there arent any controllers
            // if (globalClassControllers.Count == 0)
            //     return;

            var type = entity.GetType();
            var fullName = type.FullName;
            var name = type.Name;

            foreach (var gcc in globalClassControllers) {
                var classController = (GlobalClassControllerBase)gcc;
                if (classController.ClassNames.Contains(classController.UseFullClassName ? fullName : name) && entity.Depth.IsInRange(classController.MinDepth, classController.MaxDepth)) {
                    classController.ProcessEntity(entity);
                }
            }
        }
    }

    internal static void Load() {
        // On.Monocle.Entity.Awake += On_Entity_Awake;
        IL.Monocle.EntityList.UpdateLists += IL_EntityList_UpdateLists;
    }

    internal static void Unload() {
        // On.Monocle.Entity.Awake -= On_Entity_Awake;
        IL.Monocle.EntityList.UpdateLists -= IL_EntityList_UpdateLists;
    }
}
