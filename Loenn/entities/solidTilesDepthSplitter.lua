local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")
local fakeTilesHelper = require("helpers.fake_tiles")

local depthSplitter = {}

depthSplitter.name = "SorbetHelper/SolidTilesDepthSplitter"
depthSplitter.sprite = sorbetUtils.getControllerSpriteFunction("solidTilesDepthSplitter", true)
depthSplitter.depth = -1000010
depthSplitter.placements = {
    name = "depthSplitter",
    data = {
        depth = -10510,
        tiletypes = "3",
        splitAnimatedTiles = false,
        tryFillBehind = false,
        backgroundTiles = false
    }
}

function depthSplitter.fieldInformation(entity)
    local depthOptions = depths.addDepths(depths.getDepths(), {
        {"Above FG Decals", -10510},
        {"Above BG Terrain", 9990}
    })

    return {
        depth = {
            fieldType = "integer",
            options = depthOptions,
            editable = true
        },
        tiletypes = {
            fieldType = "list",
            minimumElements = 0,
            elementSeparator = "",
            elementDefault = "3",
            elementOptions = {
                options = function()
                    return fakeTilesHelper.getTilesOptions(entity.backgroundTiles and "tilesBg" or "tilesFg")
                end,
                editable = false,
            }
        }
    }
end

depthSplitter.fieldOrder = {
    "x", "y",
    "tiletypes", "depth",
    "tryFillBehind", "splitAnimatedTiles", "backgroundTiles"
}

return depthSplitter