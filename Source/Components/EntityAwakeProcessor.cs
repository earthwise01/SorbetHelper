using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Helpers;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public sealed class EntityAwakeProcessor(Action<Entity> onProcessEntity, EntityAwakeProcessor.ProcessModes processMode = EntityAwakeProcessor.ProcessModes.OnEntityAwake)
    : Component(false, false)
{
    public enum ProcessModes
    {
        OnProcessorAwake,
        OnEntityAwake
    }

    private Action<Entity> onProcessEntity = onProcessEntity;
    private readonly ProcessModes processMode = processMode;

    private string fromLevel;

    private void Process(Entity entity) => onProcessEntity.Invoke(entity);

    public EntityAwakeProcessor WithTypeNameCheck(HashSet<string> typeNames)
    {
        Action<Entity> orig = onProcessEntity;
        onProcessEntity = entity =>
        {
            if (entity.CheckTypeName(typeNames))
                orig.Invoke(entity);
        };
        return this;
    }

    public EntityAwakeProcessor WithDepthCheck(int minDepth, int maxDepth)
    {
        if (minDepth == int.MinValue && maxDepth == int.MaxValue)
            return this;

        Action<Entity> orig = onProcessEntity;
        onProcessEntity = entity =>
        {
            if (entity.Depth.IsInRange(minDepth, maxDepth))
                orig.Invoke(entity);
        };
        return this;
    }

    public EntityAwakeProcessor WithCollideCheck()
    {
        Action<Entity> orig = onProcessEntity;
        onProcessEntity = entity =>
        {
            if (entity.Collider?.Collide(Entity) ?? Entity.CollidePoint(entity.Position))
                orig.Invoke(entity);
        };
        return this;
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);

        fromLevel = (scene as Level)?.Session.Level;
    }

    public override void EntityAwake()
    {
        base.EntityAwake();

        if (processMode == ProcessModes.OnProcessorAwake)
        {
            foreach (Entity entity in Scene.Entities)
                Process(entity);
        }
    }

    #region Hooks

    internal static void Load()
    {
        IL.Monocle.EntityList.UpdateLists += IL_EntityList_UpdateLists;
    }

    internal static void Unload()
    {
        IL.Monocle.EntityList.UpdateLists -= IL_EntityList_UpdateLists;
    }

    private static void IL_EntityList_UpdateLists(ILContext il)
    {
        ILCursor cursor = new ILCursor(il)
        {
            Index = -1
        };

        if (!cursor.TryGotoPrevBestFit(MoveType.After,
            instr => instr.MatchLdfld<EntityList>(nameof(EntityList.adding)),
            instr => instr.MatchCallOrCallvirt<HashSet<Entity>>(nameof(HashSet<Entity>.Clear))))
            throw new HookHelper.HookException(il, "Unable to find where to prepare the EntityAwakeProcessor list.");

        VariableDefinition entityAwakeProcessors = new VariableDefinition(il.Import(typeof(EntityAwakeProcessor[])));
        il.Body.Variables.Add(entityAwakeProcessors);

        cursor.EmitLdarg0();
        cursor.EmitDelegate(GetEntityAwakeProcessors);
        cursor.EmitStloc(entityAwakeProcessors);

        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallOrCallvirt<Entity>("Awake")))
            throw new HookHelper.HookException(il, "Unable to find Entity.Awake call to emit processing after.");

        ILLabel noEntityAwakeProcessorsLabel = cursor.DefineLabel();

        cursor.EmitLdloc(entityAwakeProcessors);
        cursor.EmitBrfalse(noEntityAwakeProcessorsLabel);
        cursor.EmitLdloc(5); // entity
        cursor.EmitLdloc(entityAwakeProcessors);
        cursor.EmitDelegate(ProcessEntity);
        cursor.MarkLabel(noEntityAwakeProcessorsLabel);

        return;

        static EntityAwakeProcessor[] GetEntityAwakeProcessors(EntityList entities)
        {
            string levelName = (entities.Scene as Level)?.Session.Level;
            EntityAwakeProcessor[] typeNameProcessors = entities.Scene.Tracker.GetComponents<EntityAwakeProcessor>()
                                                                              .Where(c => c is EntityAwakeProcessor { processMode: ProcessModes.OnEntityAwake } p
                                                                                          && (p.fromLevel == levelName || p.Entity.TagCheck(Tags.Global | Tags.Persistent)))
                                                                              .Cast<EntityAwakeProcessor>()
                                                                              .ToArray();
            return typeNameProcessors.Length == 0 ? null : typeNameProcessors;
        }

        static void ProcessEntity(Entity entity, EntityAwakeProcessor[] entityAwakeProcessors)
        {
            foreach (EntityAwakeProcessor processor in entityAwakeProcessors)
                processor.Process(entity);
        }
    }

    #endregion
}
