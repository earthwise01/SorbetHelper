using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.SorbetHelper.Components;

// todo: allow entity sids

/// <summary>
/// component for per-room controllers that affect entities based on their runtime class type<br/>
/// <br/>
/// checks for the following entitydata attributes:<br/>
/// classNames - string, comma seperated list of type names to affect<br/>
/// useFullClassNames - bool, whether to use fully qualified type names, e.g. SummitCloud vs Celeste.SummitCloud<br/>
/// minDepth - int, the minimum (closest to the camera) depth an entity can have while being affected by the component<br/>
/// maxDepth - int, the maximum (farthest from the camera) depth an entity can have while being affected by the component<br/>
/// <br/>
/// processes entities in its EntityAwake method
/// </summary>
public sealed class TypeNameProcessor : Component {
    private readonly HashSet<string> ClassNames;
    private readonly bool UseFullClassName;
    private readonly int MinDepth, MaxDepth;

    public readonly Action<Entity> ProcessEntity;

    public TypeNameProcessor(EntityData data, Action<Entity> processEntity) : base(false, false) {
        ClassNames = [.. data.List("classNames", str => str)];
        UseFullClassName = data.Bool("useFullClassNames", false);
        MinDepth = data.Int("minDepth", int.MinValue);
        MaxDepth = data.Int("maxDepth", int.MaxValue);

        ProcessEntity = processEntity;
    }

    public override void EntityAwake() {
        base.EntityAwake();

        foreach (var entity in Scene.Entities) {
            var className = UseFullClassName ? entity.GetType().FullName : entity.GetType().Name;
            if (ClassNames.Contains(className) && entity.Depth.IsInRange(MinDepth, MaxDepth)) {
                ProcessEntity(entity);
            }
        }
    }
}

/// <summary>
/// component for global controllers that affect entities based on their runtime class type<br/>
/// <br/>
/// checks for the following entitydata attributes:<br/>
/// classNames - string, comma seperated list of type names to affect<br/>
/// useFullClassNames - bool, whether to use fully qualified type names, e.g. SummitCloud vs Celeste.SummitCloud<br/>
/// minDepth - int, the minimum (closest to the camera) depth an entity can have while being affected by the component<br/>
/// maxDepth - int, the maximum (farthest from the camera) depth an entity can have while being affected by the component<br/>
/// <br/>
/// processes entities after *their own* Awake methods
/// </summary>
[Tracked]
public sealed class GlobalTypeNameProcessor : Component {
    private readonly HashSet<string> ClassNames;
    private readonly bool UseFullClassName;
    private readonly int MinDepth, MaxDepth;

    public readonly Action<Entity> ProcessEntity;

    public GlobalTypeNameProcessor(EntityData data, Action<Entity> processEntity) : base(false, false) {
        ClassNames = [.. data.List("classNames", str => str)];
        UseFullClassName = data.Bool("useFullClassNames", false);
        MinDepth = data.Int("minDepth", int.MinValue);
        MaxDepth = data.Int("maxDepth", int.MaxValue);

        ProcessEntity = processEntity;
    }

    private static void IL_EntityList_UpdateLists(ILContext il) {
        var cursor = new ILCursor(il) {
            Index = -1
        };

        var typeNameProcessors = new VariableDefinition(il.Import(typeof(List<Component>)));
        il.Body.Variables.Add(typeNameProcessors);

        var typeNameProcessorsIsNull = cursor.DefineLabel();

        cursor.GotoPrevBestFit(MoveType.After, i => i.MatchLdfld<EntityList>("adding"), i => i.MatchCallOrCallvirt<HashSet<Entity>>("Clear"));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(getTypeNameProcessors);
        cursor.EmitStloc(typeNameProcessors);

        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Entity>("Awake"));
        cursor.EmitLdloc(typeNameProcessors);
        cursor.EmitBrfalse(typeNameProcessorsIsNull);

        cursor.EmitLdloc(5); // entity
        cursor.EmitLdloc(typeNameProcessors);
        cursor.EmitDelegate(processEntity);

        cursor.MarkLabel(typeNameProcessorsIsNull);

        static List<Component> getTypeNameProcessors(EntityList entities) {
            var tracker = entities.Scene.Tracker;
            if (!tracker.IsComponentTracked<GlobalTypeNameProcessor>()) // fix dependency load crash????
                return null;

            var typeNameProcessors = tracker.GetComponents<GlobalTypeNameProcessor>();
            return typeNameProcessors.Count == 0 ? null : typeNameProcessors; // return null if count is 0 to (hopefully) speed up the check for whether to process entities
        }

        static void processEntity(Entity entity, List<Component> typeNameProcessors) {
            var type = entity.GetType();
            var fullName = type.FullName;
            var name = type.Name;

            foreach (GlobalTypeNameProcessor typeNameProcessor in typeNameProcessors) {
                if (typeNameProcessor.ClassNames.Contains(typeNameProcessor.UseFullClassName ? fullName : name) && entity.Depth.IsInRange(typeNameProcessor.MinDepth, typeNameProcessor.MaxDepth)) {
                    typeNameProcessor.ProcessEntity(entity);
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
