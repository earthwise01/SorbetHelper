local fakeTilesHelper = require("helpers.fake_tiles")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local solidTilesDepthSplitter = {}

solidTilesDepthSplitter.name = "SorbetHelper/SolidTilesDepthSplitter"
solidTilesDepthSplitter.sprite = sorbetHelper.getControllerSpriteFunction("solidTilesDepthSplitter", true)
solidTilesDepthSplitter.depth = sorbetHelper.controllerDepth
solidTilesDepthSplitter.placements = {
    name = "tileset_depth_splitter",
    alternativeName = "solid_tiles_depth_splitter",
    data = {
        depth = -10510,
        tiletypes = "h",
        tryFillBehind = true,
        backgroundTiles = false
    }
}

solidTilesDepthSplitter.fieldOrder = {
    "x", "y",
    "tiletypes", "backgroundTiles",
    "depth", "tryFillBehind"
}

function solidTilesDepthSplitter.fieldInformation(entity)
    return {
        depth = {
            fieldType = "integer",
            options = sorbetHelper.getDepths({
                {"Above FG Decals", -10510},
                {"Above FG Terrain", -10010},
                {"Above BG Terrain", 9990}
            }),
            editable = true
        },
        tiletypes = {
            fieldType = "sorbet_helper.value_list",
            elementSeparator = "",
            elementDefault = "3",
            elementOptions = {
                options = fakeTilesHelper.getTilesOptions(entity.backgroundTiles and "tilesBg" or "tilesFg"),
                editable = false
            }
        }
    }
end

return solidTilesDepthSplitter
