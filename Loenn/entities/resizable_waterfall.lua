local drawableRectangle = require("structs.drawable_rectangle")
local roomStruct = require("structs.room")
local xnaColors = require("consts.xna_colors")
local utils = require("utils")
local waterfallHelper = require("helpers.waterfalls")

local resizableWaterfall = {}

resizableWaterfall.name = "SorbetHelper/BigWaterfall"
resizableWaterfall.depth = -9999
resizableWaterfall.canResize = {true, false}
resizableWaterfall.minimumSize = {16, 8}
resizableWaterfall.fieldInformation = {
    depth = {
        options = {-9999, -49900},
        editable = true
    }
}
resizableWaterfall.placements = {
    {
        name = "normal",
        data = {
            width = 16,
            color = "87CEFA",
            ignoreSolids = false,
            lines = true,
            depth = -9999
        }
    },
    {
        name = "abovefg",
        data = {
            width = 16,
            color = "87CEFA",
            ignoreSolids = true,
            lines = true,
            depth = -49900
        }
    }
}

function resizableWaterfall.sprite(room, entity)
    -- cursed
    local entityHackfix = entity
    entityHackfix.height = waterfallHelper.getWaterfallHeight(room, entity)

    local color = utils.getColor(entity.color)

    local fillColor = {color[1] * 0.3, color[2] * 0.3, color[3] * 0.3, 0.3}
    local borderColor = {color[1] * 0.8, color[2] * 0.8, color[3] * 0.8, 0.8}
    return waterfallHelper.getBigWaterfallSprite(room, entityHackfix, fillColor, borderColor)
end

function resizableWaterfall.rectangle(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local height = waterfallHelper.getWaterfallHeight(room, entity)

    return utils.rectangle(x, y, entity.width, height)
end

return resizableWaterfall
