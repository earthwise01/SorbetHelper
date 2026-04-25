namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked(inherited: true)]
public abstract class EntityProcessingController : Entity
{
    protected enum ProcessModes
    {
        OnProcessorAwake,
        OnEntityAwake
    }

    protected HashSet<string> AffectedTypes { get; init; }
    protected int MinDepth { get; init; } = int.MinValue;
    protected int MaxDepth { get; init; } = int.MaxValue;

    protected virtual int ProcessPriority => 0;

    private readonly ProcessModes processMode;

    private readonly bool roomWide;
    private string fromRoom;

    protected EntityProcessingController(EntityData data, Vector2 offset, ProcessModes processMode = ProcessModes.OnEntityAwake)
        : base (data.Position + offset)
    {
        this.processMode = processMode;

        if (data.Width > 0 && data.Height > 0)
        {
            Collider = new Hitbox(data.Width, data.Height);
            roomWide = false;
        }
        else if (data.Nodes.Length == 2)
        {
            Vector2[] nodes = data.NodesOffset(offset);

            float topLeftX = MathF.Min(nodes[0].X, nodes[1].X);
            float topLeftY = MathF.Min(nodes[0].Y, nodes[1].Y);
            float width = MathF.Max(nodes[0].X, nodes[1].X) - topLeftX;
            float height = MathF.Max(nodes[0].Y, nodes[1].Y) - topLeftY;

            Collider = new Hitbox(width, height, topLeftX - X, topLeftY - Y);
            roomWide = false;
        }
        else
        {
            Collider = null;
            roomWide = true;
        }
    }

    protected virtual bool ControllerContains(Entity entity)
        => Collider is null || (entity.Collider is not null ? entity.CollideCheck(this) : CollidePoint(entity.Position));

    protected abstract void ProcessEntity(Entity entity);

    private void Process(Entity entity)
    {
        if (!ControllerContains(entity))
            return;

        if (AffectedTypes is not null && !entity.CheckTypeName(AffectedTypes))
            return;

        if (!entity.Depth.IsInRange(MinDepth, MaxDepth))
            return;

        ProcessEntity(entity);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        fromRoom = (scene as Level)?.Session.Level;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        // exploding backwards compat with my mind
        if (processMode == ProcessModes.OnProcessorAwake)
        {
            foreach (Entity entity in Scene.Entities)
                Process(entity);
        }
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        IL.Monocle.EntityList.UpdateLists += IL_EntityList_UpdateLists;
    }

    [OnUnload]
    internal static void Unload()
    {
        IL.Monocle.EntityList.UpdateLists -= IL_EntityList_UpdateLists;
    }

    private static void IL_EntityList_UpdateLists(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<EntityList>(nameof(EntityList.toAwake)),
            instr => instr.MatchCallvirt<List<Entity>>(nameof(List<>.GetEnumerator)),
            instr => instr.MatchStloc(4)))
            throw new HookHelper.HookException(il, "Unable to find foreach loop over `EntityList.toAwake` to prepare the EntityProcessingController list before.");

        VariableDefinition entityProcessorsVariable = cursor.AddVariable<EntityProcessingController[]>();

        cursor.EmitLdarg0();
        cursor.EmitDelegate(GetEntityProcessors);
        cursor.EmitStloc(entityProcessorsVariable);

        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallOrCallvirt<Entity>(nameof(Entity.Awake))))
            throw new HookHelper.HookException(il, "Unable to find `Entity.Awake` call to add processing after.");

        cursor.EmitLdloc(5); // entity
        cursor.EmitLdloc(entityProcessorsVariable);
        cursor.EmitDelegate(ProcessEntity);

        return;

        static EntityProcessingController[] GetEntityProcessors(EntityList entities)
        {
            if (entities.Scene.Tracker.GetEntities<EntityProcessingController>() is not { Count: > 0 } trackedEntityProcessors)
                return [];

            string currentRoom = (entities.Scene as Level)?.Session.Level;

            // linq :revolving_hearts:
            // (blehh is there a better way to do this,)
            return trackedEntityProcessors.Cast<EntityProcessingController>()
                                          .Where(c => c is { processMode: ProcessModes.OnEntityAwake }
                                                      && (c.fromRoom == currentRoom || c.TagCheck(Tags.Global | Tags.Persistent)))
                                          .OrderBy(c => c.roomWide)
                                          .ThenBy(c => c.ProcessPriority)
                                          .ToArray();
        }

        static void ProcessEntity(Entity entity, EntityProcessingController[] entityProcessors)
        {
            foreach (EntityProcessingController entityProcessor in entityProcessors)
                entityProcessor.Process(entity);
        }
    }

    #endregion
}
