using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity]
[CustomEntity("SorbetHelper/SolidTilesDepthSplitter")]
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

        for (int x = 0; x < tileData.Columns; x++)
        for (int y = 0; y < tileData.Rows; y++)
        {
            if (tiletypes.Contains(tileData[x, y]))
            {
                tiles.Tiles[x, y] = origTiles.Tiles[x, y];
                origTiles.Tiles[x, y] = tryGetFillBehind?.Invoke(x, y);

                // only create anim tiles if necessary
                if (origAnimTiles.tiles.AnyInSegmentAtTile(x, y))
                {
                    if (origAnimTiles.tiles[x, y] is null)
                        continue;

                    animatedTiles ??= new AnimatedTiles(origAnimTiles.tiles.Columns, origAnimTiles.tiles.Rows, origAnimTiles.Bank);

                    animatedTiles.tiles[x, y] = origAnimTiles.tiles[x, y];
                    origAnimTiles.tiles[x, y] = null;
                }
            }
        }

        // tiles.Alpha = 0.4f;
        Add(tiles);
        if (animatedTiles is not null)
            Add(animatedTiles);
    }

    private Func<int, int, MTexture> GenerateFillBehind(VirtualMap<char> origTiles, Autotiler autotiler)
    {
        VirtualMap<char> newTiles = new VirtualMap<char>(origTiles.Columns, origTiles.Rows, origTiles.EmptyValue);
        for (int x = 0; x < origTiles.Columns; x++)
        for (int y = 0; y < origTiles.Rows; y++)
            newTiles[x, y] = GetTile(x, y);

        Autotiler.Generated generated = autotiler.GenerateMap(newTiles, paddingIgnoreOutOfLevel: true);

        return (int x, int y) => tiletypes.Contains(newTiles[x, y]) ? null : generated.TileGrid.Tiles[x, y];

        char GetTile(int x, int y)
        {
            char origTile = origTiles[x, y];
            if (!tiletypes.Contains(origTile))
                return origTile;

            // try to get the fill tile from top/bottom/left/right neighbours
            // otherwise, try the topleft/bottomleft/topright/bottomright neighbours
            // otherwise, return the original tile
            return TryGetFillFrom([origTiles[x, y - 1], origTiles[x, y + 1], origTiles[x - 1, y], origTiles[x + 1, y]])
                   ?? TryGetFillFrom([origTiles[x - 1, y - 1], origTiles[x - 1, y + 1], origTiles[x + 1, y - 1], origTiles[x + 1, y + 1]])
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
                        || autotiler.lookup.TryGetValue(neighbour, out Autotiler.TerrainType neighbourData)
                        && !neighbourData.Ignore(origTile)
                        && (fillTile is null || fillTile == '0' || neighbourData.Ignore(fillTile.Value)))
                        fillTile = neighbour;
                }

                return fillTile;
            }
        }
    }
}
