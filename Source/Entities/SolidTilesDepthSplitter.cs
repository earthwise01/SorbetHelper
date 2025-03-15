using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Backdrops;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using TerrainType = Celeste.Autotiler.TerrainType;


namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity]
[CustomEntity("SorbetHelper/SolidTilesDepthSplitter")]
public class SolidTilesDepthSplitter : Entity {
    private SolidTiles SolidTiles => (Scene as Level)?.SolidTiles;
    private AnimatedTiles AnimatedTiles => (Scene as Level)?.SolidTiles.AnimatedTiles;

    private readonly HashSet<char> tiletypes;
    private readonly bool splitAnimatedTiles;

    private readonly bool tryFillBehind;

    public TileGrid Tiles;

    public SolidTilesDepthSplitter(EntityData data, Vector2 _) : base() {
        Depth = data.Int("depth", Depths.FGDecals - 10);
        tiletypes = data.Attr("tiletypes", "3").ToHashSet();
        tiletypes.Remove('0');

        splitAnimatedTiles = data.Bool("splitAnimatedTiles", false);

        tryFillBehind = data.Bool("tryFillBehind", false);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (splitAnimatedTiles && AnimatedTiles is not null)
            AnimatedTiles.Visible = false;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (tiletypes.Count <= 0)
            return;

        var solidTiles = SolidTiles;

        Position = solidTiles.Position;

        var origTiles = solidTiles.Tiles;
        Tiles = new TileGrid(origTiles.TileWidth, origTiles.TileHeight, origTiles.TilesX, origTiles.TilesY) {
            VisualExtend = origTiles.VisualExtend,
            ClipCamera = origTiles.ClipCamera
        };

        Func<int, int, MTexture> fillBehind = null;
        if (tryFillBehind)
            fillBehind = GenerateFillBehind();

        var tileData = solidTiles.tileTypes;
        for (int x = 0; x < tileData.Columns; x++) {
            for (int y = 0; y < tileData.Rows; y++) {
                if (tiletypes.Contains(tileData[x, y])) {
                    Tiles.Tiles[x, y] = origTiles.Tiles[x, y];
                    // wonder if itd work to be able to have the old tiles filled in with something else?
                    // would require a one time autotiler rerun which is eh but idk
                    origTiles.Tiles[x, y] = tryFillBehind ? fillBehind(x, y) : null;
                }
            }
        }

        // Tiles.Alpha = 0.4f;
        Add(Tiles);
    }

    private Func<int, int, MTexture> GenerateFillBehind() {
        var solidTiles = SolidTiles;
        var tileData = solidTiles.tileTypes;

        var modified = new VirtualMap<char>(tileData.Columns, tileData.Rows, tileData.EmptyValue);
        for (int x = 0; x < tileData.Columns; x++) {
            for (int y = 0; y < tileData.Rows; y++) {
                modified[x, y] = getTile(x, y);
            }
        }

        var generated = GFX.FGAutotiler.GenerateMap(modified, paddingIgnoreOutOfLevel: true);

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

    public override void Render() {
        base.Render();
        if (splitAnimatedTiles)
            AnimatedTiles?.Render();
    }
}
