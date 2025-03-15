using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;


namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity]
[CustomEntity("SorbetHelper/SolidTilesDepthSplitter")]
public class SolidTilesDepthSplitter : Entity {
    // i feel like this  Works as a way to add bgtile support ?? hopefully not a messy 3am brain moment
    public static Entity Load(Level level, LevelData levelData, Vector2 position, EntityData entityData) {
        var entity = new SolidTilesDepthSplitter(entityData);
        if (entityData.Bool("backgroundTiles", false))
            entity.SplitTiles(level.BgTiles.Position, level.BgData, level.BgTiles.Tiles, level.BgTiles.AnimatedTiles, GFX.BGAutotiler);
        else
            entity.SplitTiles(level.SolidTiles.Position, level.SolidsData, level.SolidTiles.Tiles, level.SolidTiles.AnimatedTiles, GFX.FGAutotiler);
        return entity;
    }

    private readonly HashSet<char> tiletypes;

    private readonly bool tryFillBehind;

    public TileGrid Tiles;
    public AnimatedTiles AnimatedTiles;

    public SolidTilesDepthSplitter(EntityData data) : base() {
        Depth = data.Int("depth", Depths.FGDecals - 10);
        tiletypes = data.Attr("tiletypes", "3").ToHashSet();
        tiletypes.Remove('0');

        tryFillBehind = data.Bool("tryFillBehind", false);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        Tiles.ClipCamera = (scene as Level).Camera;
        if (AnimatedTiles is not null)
            AnimatedTiles.ClipCamera = Tiles.ClipCamera;
    }

    private void SplitTiles(Vector2 position, VirtualMap<char> tileData, TileGrid origTiles, AnimatedTiles origAnimTiles, Autotiler autotiler) {
        if (tiletypes.Count <= 0)
            return;

        Position = position;

        Tiles = new TileGrid(origTiles.TileWidth, origTiles.TileHeight, origTiles.TilesX, origTiles.TilesY) {
            VisualExtend = origTiles.VisualExtend
        };

        Func<int, int, MTexture> fillBehind = null;
        if (tryFillBehind)
            fillBehind = GenerateFillBehind(tileData, autotiler);

        for (int x = 0; x < tileData.Columns; x++) {
            for (int y = 0; y < tileData.Rows; y++) {
                if (tiletypes.Contains(tileData[x, y])) {
                    Tiles.Tiles[x, y] = origTiles.Tiles[x, y];
                    origTiles.Tiles[x, y] = tryFillBehind ? fillBehind(x, y) : null;

                    // only create anim tiles if necessary
                    if (origAnimTiles.tiles.AnyInSegmentAtTile(x, y)) {
                        if (origAnimTiles.tiles[x, y] is null)
                            continue;

                        AnimatedTiles ??= new AnimatedTiles(origAnimTiles.tiles.Columns, origAnimTiles.tiles.Rows, origAnimTiles.Bank);

                        AnimatedTiles.tiles[x, y] = origAnimTiles.tiles[x, y];
                        origAnimTiles.tiles[x, y] = null;
                    }
                }
            }
        }

        // Tiles.Alpha = 0.4f;
        Add(Tiles);
        if (AnimatedTiles is not null)
            Add(AnimatedTiles);
    }

    private Func<int, int, MTexture> GenerateFillBehind(VirtualMap<char> tileData, Autotiler autotiler) {
        var modified = new VirtualMap<char>(tileData.Columns, tileData.Rows, tileData.EmptyValue);
        for (int x = 0; x < tileData.Columns; x++) {
            for (int y = 0; y < tileData.Rows; y++) {
                modified[x, y] = getTile(x, y);
            }
        }

        var generated = autotiler.GenerateMap(modified, paddingIgnoreOutOfLevel: true);

        return (x, y) => {
            if (tiletypes.Contains(modified[x, y]))
                return null;

            return generated.TileGrid.Tiles[x, y];
        };

        char getTile(int x, int y) {
            var c = tileData[x, y];
            if (!tiletypes.Contains(c))
                return c;

            var lookup = GFX.FGAutotiler.lookup;
            // up, down, left, right (no diagonals)
            var neighbours = new char[4] {tileData[x, y - 1], tileData[x, y + 1], tileData[x - 1, y], tileData[x + 1, y]};

            // if all neighbours are c, return c
            // if all neighbours are either c or air, teturn '0'
            // otherwise, return whichever neighbour ignores the most of the others
            var result = '0';
            var neighboursAir = false;
            for (int l = 0; l < neighbours.Length; l++) {
                var n = neighbours[l];

                if (n == c)
                    continue;

                var isValidTile = lookup.TryGetValue(n, out var data);

                // neighbours that ignore c count as '0'
                if (n == '0' || !isValidTile || data.Ignore(c)) {
                    neighboursAir = true;
                    continue;
                }

                // result should be equal to whichever neighbour ignores the most of the others
                if (result == '0' || data.Ignore(result))
                    result = n;
            }

            // if all neighbours are c, result will still be 0 but neighboursAir will be false
            // if all neighbours are either c or air, result will be 0 and neighboursAir will be true
            // otherwise, result will not be 0 and neighboursAir is not relevant
            if (result == '0')
                if (neighboursAir)
                    return '0';
                else
                    return c;
            else
                return result;
        }
    }
}
