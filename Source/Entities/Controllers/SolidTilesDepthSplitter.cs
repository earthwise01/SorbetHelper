namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/SolidTilesDepthSplitter")]
[GlobalEntity]
public class SolidTilesDepthSplitter : Entity
{
    public static Entity Load(Level level, LevelData levelData, Vector2 position, EntityData entityData)
    {
        SolidTilesDepthSplitter depthSplitter = new SolidTilesDepthSplitter(entityData.Int("depth", Depths.FGDecals - 10),
            entityData.Attr("tiletypes", "3").ToHashSet(), entityData.Bool("tryFillBehind", false));

        if (!entityData.Bool("backgroundTiles", false))
            depthSplitter.SplitTiles(level.SolidTiles.Position, level.SolidsData, level.SolidTiles.Tiles, level.SolidTiles.AnimatedTiles, GFX.FGAutotiler);
        else
            depthSplitter.SplitTiles(level.BgTiles.Position, level.BgData, level.BgTiles.Tiles, level.BgTiles.AnimatedTiles, GFX.BGAutotiler);

        return depthSplitter;
    }

    private static readonly Autotiler.Behaviour AutotilerBehaviour = new Autotiler.Behaviour()
    {
        EdgesExtend = true,
        EdgesIgnoreOutOfLevel = false,
        PaddingIgnoreOutOfLevel = true
    };

    private readonly HashSet<char> tiletypes;
    private readonly bool tryFillBehind;

    private TileGrid tiles;
    private AnimatedTiles animatedTiles;

    private SolidTilesDepthSplitter(int depth, HashSet<char> tiletypes, bool tryFillBehind) : base()
    {
        Depth = depth;
        this.tiletypes = tiletypes;
        this.tiletypes.Remove('0');
        this.tryFillBehind = tryFillBehind;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        tiles.ClipCamera = SceneAs<Level>().Camera;
        animatedTiles?.ClipCamera = tiles.ClipCamera;
    }

    private void SplitTiles(Vector2 position, VirtualMap<char> tileData, TileGrid origTiles, AnimatedTiles origAnimTiles, Autotiler autotiler)
    {
        if (tiletypes.Count <= 0)
            return;

        Position = position;

        tiles = new TileGrid(origTiles.TileWidth, origTiles.TileHeight, origTiles.TilesX, origTiles.TilesY)
        {
            VisualExtend = origTiles.VisualExtend
        };

        Func<int, int, MTexture> tryGetFillBehind = null;
        if (tryFillBehind)
            tryGetFillBehind = GenerateFillBehind(tileData, autotiler);

        const int segmentSize = VirtualMap<char>.SegmentSize;
        for (int segmentX = 0; segmentX < tileData.Columns; segmentX += segmentSize)
        for (int segmentY = 0; segmentY < tileData.Rows; segmentY += segmentSize)
        {
            if (!tileData.AnyInSegmentAtTile(segmentX, segmentY))
                continue;

            int segmentEndX = Math.Min(segmentX + segmentSize, tileData.Columns);
            int segmentEndY = Math.Min(segmentY + segmentSize, tileData.Rows);
            for (int x = segmentX; x < segmentEndX; x++)
            for (int y = segmentY; y < segmentEndY; y++)
            {
                if (!tiletypes.Contains(tileData[x, y]))
                    continue;

                tiles.Tiles[x, y] = origTiles.Tiles[x, y];
                origTiles.Tiles[x, y] = tryGetFillBehind?.Invoke(x, y);

                if (origAnimTiles.tiles[x, y] is null)
                    continue;

                animatedTiles ??= new AnimatedTiles(origAnimTiles.tiles.Columns, origAnimTiles.tiles.Rows, origAnimTiles.Bank);

                animatedTiles.tiles[x, y] = origAnimTiles.tiles[x, y];
                origAnimTiles.tiles[x, y] = null;
            }
        }

        // tiles.Alpha = 0.4f;
        Add(tiles);
        if (animatedTiles is not null)
            Add(animatedTiles);
    }

    private Func<int, int, MTexture> GenerateFillBehind(VirtualMap<char> tileData, Autotiler autotiler)
    {
        VirtualMap<char> filledTileData = new VirtualMap<char>(tileData.Columns, tileData.Rows, tileData.EmptyValue);

        const int segmentSize = VirtualMap<char>.SegmentSize;
        for (int segmentX = 0; segmentX < tileData.Columns; segmentX += segmentSize)
        for (int segmentY = 0; segmentY < tileData.Rows; segmentY += segmentSize)
        {
            if (!tileData.AnyInSegmentAtTile(segmentX, segmentY))
                continue;

            int segmentEndX = Math.Min(segmentX + segmentSize, tileData.Columns);
            int segmentEndY = Math.Min(segmentY + segmentSize, tileData.Rows);
            for (int x = segmentX; x < segmentEndX; x++)
            for (int y = segmentY; y < segmentEndY; y++)
                filledTileData[x, y] = GetFillTile(x, y, tileData, autotiler.lookup);
        }

        return (int x, int y) => !tiletypes.Contains(filledTileData[x, y])
            ? autotiler.Generate(filledTileData, x, y, 1, 1, false, '0', AutotilerBehaviour).TileGrid.Tiles[0, 0]
            : null;
    }

    private char GetFillTile(int x, int y, VirtualMap<char> tileData, Dictionary<char, Autotiler.TerrainType> terrainLookup)
    {
        char origTile = tileData[x, y];
        if (!tiletypes.Contains(origTile))
            return origTile;

        // try to get the fill tile from top/bottom/left/right neighbours
        // otherwise, try the topleft/bottomleft/topright/bottomright neighbours
        // otherwise, return the original tile
        return TryGetFillFrom([tileData[x, y - 1], tileData[x, y + 1], tileData[x - 1, y], tileData[x + 1, y]])
               ?? TryGetFillFrom([tileData[x - 1, y - 1], tileData[x - 1, y + 1], tileData[x + 1, y - 1], tileData[x + 1, y + 1]])
               ?? origTile;

        // returns whichever neighbour that connects to the original tile ignores the most of the others
        char? TryGetFillFrom(char[] neighbours)
        {
            char? fillTile = null;
            foreach (char neighbour in neighbours)
            {
                // ignore any neighbours that are also being split
                if (tiletypes.Contains(neighbour))
                    continue;

                // air ('0') isn't in the lookup but is treated as always connecting to origTile and always ignored by the other neighbours
                if ((neighbour == '0' && fillTile is null)
                    || terrainLookup.TryGetValue(neighbour, out Autotiler.TerrainType neighbourData)
                    && !neighbourData.Ignore(origTile)
                    && (fillTile is null || fillTile == '0' || neighbourData.Ignore(fillTile.Value)))
                    fillTile = neighbour;
            }

            return fillTile;
        }
    }
}
