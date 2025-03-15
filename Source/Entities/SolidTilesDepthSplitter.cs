using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Backdrops;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity("SorbetHelper/SolidTilesDepthSplitter")]
public class SolidTilesDepthSplitter : Entity {
    private SolidTiles SolidTiles => (Scene as Level)?.SolidTiles;
    private AnimatedTiles AnimatedTiles => (Scene as Level)?.SolidTiles.AnimatedTiles;

    private readonly HashSet<char> tiletypes;
    private readonly bool splitAnimatedTiles;

    public TileGrid Tiles;

    public SolidTilesDepthSplitter(EntityData data, Vector2 _) : base() {
        Depth = data.Int("depth", Depths.FGDecals - 10);
        tiletypes = data.Attr("tiletypes", "3").Split(',').Select(tiletype => tiletype.FirstOrDefault('0')).ToHashSet();
        tiletypes.Remove('0');

        splitAnimatedTiles = data.Bool("splitAnimatedTiles", false);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (splitAnimatedTiles && AnimatedTiles is not null)
            AnimatedTiles.Visible = false;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (this.tiletypes.Count <= 0)
            return;

        var solidTiles = SolidTiles;

        Position = solidTiles.Position;

        var origTiles = solidTiles.Tiles;
        Tiles = new TileGrid(origTiles.TileWidth, origTiles.TileHeight, origTiles.TilesX, origTiles.TilesY) {
            VisualExtend = origTiles.VisualExtend,
            ClipCamera = origTiles.ClipCamera
        };

        var tiletypes = solidTiles.tileTypes;
        for (int x = 0; x < tiletypes.Columns; x++) {
            for (int y = 0; y < tiletypes.Rows; y++) {
                if (this.tiletypes.Contains(tiletypes[x, y])) {
                    Tiles.Tiles[x, y] = origTiles.Tiles[x, y];
                    // wonder if itd work to be able to have the old tiles filled in with something else?
                    // would require a one time autotiler rerun which is eh but idk
                    origTiles.Tiles[x, y] = null;
                }
            }
        }

        Add(Tiles);
    }

    public override void Render() {
        base.Render();
        if (splitAnimatedTiles)
            AnimatedTiles?.Render();
    }
}
