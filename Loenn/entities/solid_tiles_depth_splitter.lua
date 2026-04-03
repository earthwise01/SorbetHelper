local fakeTilesHelper = require("helpers.fake_tiles")
local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local solidTilesDepthSplitter = {}

local depthOptions = sorbetUtils.getDepths({
    {"Above FG Decals", -10510},
    {"Above FG Terrain", -10010},
    {"Above BG Terrain", 9990}
})

solidTilesDepthSplitter.name = "SorbetHelper/SolidTilesDepthSplitter"
solidTilesDepthSplitter.sprite = sorbetUtils.getControllerSpriteFunction("solidTilesDepthSplitter", true)
solidTilesDepthSplitter.depth = sorbetUtils.controllerDepth
solidTilesDepthSplitter.placements = {
    name = "tiletype_depth_splitter",
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
            options = depthOptions,
            editable = true
        },
        tiletypes = {
            fieldType = "sorbetHelper.unicodeCharList",
            minimumElements = 0,
            -- elementSeparator = "",
            elementDefault = "3",
            elementOptions = {
                options = fakeTilesHelper.getTilesOptions(entity.backgroundTiles and "tilesBg" or "tilesFg"),
                editable = false,
            }
        }
    }
end

return solidTilesDepthSplitter
