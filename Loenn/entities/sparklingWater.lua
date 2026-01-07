local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local depths = require("mods").requireFromPlugin("libraries.depths")

local water = {}

water.name = "SorbetHelper/SparklingWater"
water.warnBelowSize = {8, 8}
water.placements = {
    name = "normal",
    data = {
        width = 8,
        height = 8,
        depth = -9999,
        collidable = true,
        canSplash = true,
        topSurface = true,
        bottomSurface = false,
    }
}

water.fieldOrder = {
    "x", "y",
    "width", "height",
    "depth", "collidable", "canSplash",
    "topSurface", "bottomSurface",
}

water.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = depths.addDepths(depths.getDepths(), {
            {"Water", -9999},
        }),
        editable = true
    },
}

function water.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width or 8, entity.height or 16)
end

local function getColors(room)
    local defaultOutline, defaultFill = utils.getColor("a0ddeeff"), utils.getColor("549cd17d")

    for _, entity in pairs(room.entities) do
        if entity._name == "SorbetHelper/SparklingWaterColorController" then
            return utils.getColor(entity.outlineColor) or defaultOutline, utils.getColor(entity.fillColor) or defaultFill
        end
    end

    return defaultOutline, defaultFill
end

function water.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 16

    local outlineColor, fillColor = getColors(room)
    -- reduce alpha slightly
    outlineColor[4] = outlineColor[4] * 0.8;
    fillColor[4] = fillColor[4] * 0.8;

    return drawableRectangle.fromRectangle("bordered", x, y, width, height, fillColor, outlineColor)
end

function water.depth(room, entity)
    return entity.depth or -9999
end

return water
