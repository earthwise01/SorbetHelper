local drawableRectangle = require("structs.drawable_rectangle")
local roomStruct = require("structs.room")
local xnaColors = require("consts.xna_colors")
local utils = require("utils")
local waterfallHelper = require("helpers.waterfalls")
local connectedEntities = require("helpers.connected_entities")

local resizableWaterfall = {}

resizableWaterfall.name = "SorbetHelper/BigWaterfall"
resizableWaterfall.canResize = {true, false}
resizableWaterfall.minimumSize = {16, 0}
resizableWaterfall.fieldInformation = {
    depth = {
        options = {-9999, -49900},
        editable = true
    },
    color = {
        fieldType = "color",
        allowXNAColors = false
    }
}

resizableWaterfall.fieldOrder = {
    "x", "y", "width", "color", "depth", "lines", "ignoreSolids"
}

resizableWaterfall.ignoredFields = {
    "height", "_id", "_name"
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

-- normally loenn doesnt check for pandoras box water but i do here anyway bc itd look weird sometimes otherwise
local function waterSearchPredicate(entity)
    return entity._name == "water" or entity._name == "pandorasBox/coloredWater"
end

local function anyCollisions(rectangle, rectangles)
    for _, rect in ipairs(rectangles) do
        if utils.aabbCheck(rect, rectangle) then
            return true
        end
    end

    return false
end

-- stolen from loenn waterfallHelper but with the ability to ignore tiles
local function getWaterfallHeight(room, entity, ignoreTiles)
    local waterBlocks = utils.filter(waterSearchPredicate, room.entities)
    local waterRectangles = connectedEntities.getEntityRectangles(waterBlocks)

    local x, y = entity.x or 0, entity.y or 0
    local tileX, tileY = math.floor(x / 8) + 1, math.floor(y / 8) + 1

    local roomHeight = room.height
    local wantedHeight = 8 - y % 8

    local tileMatrix = room.tilesFg.matrix

    while wantedHeight < roomHeight - y do
        local rectangle = utils.rectangle(x, y + wantedHeight, 8, 8)

        if anyCollisions(rectangle, waterRectangles) then
            break
        end

        if not ignoreTiles and tileMatrix:get(tileX, tileY + 1, "0") ~= "0" then
            break
        end

        wantedHeight += 8
        tileY += 1
    end

    return wantedHeight
end

function resizableWaterfall.sprite(room, entity)
    -- height isn't ever supposed to be anything other than nil but it's probably bad practice to actively remove data anyway? meaning we back up the existing height value before doing a hackfix
    local entityHeightHackfix = entity.height
    -- temporarily change the height and layer to make the waterfall Actually Work visually
    entity.height = getWaterfallHeight(room, entity, entity.ignoreSolids)
    entity.layer = "FG"

    local color = utils.getColor(entity.color)

    local fillColor = {color[1] * 0.3, color[2] * 0.3, color[3] * 0.3, 0.3}
    local borderColor = {color[1] * 0.8, color[2] * 0.8, color[3] * 0.8, 0.8}
    -- using the hackfixed height and layer values from earlier, grab a correct big waterfall sprite
    local result = waterfallHelper.getBigWaterfallSprite(room, entity, fillColor, borderColor)
    -- reset the height back to what it was before (it *should* always be nil but we're doing this instead of just setting it to nil due to the reason stated earlier)
    entity.height = entityHeightHackfix
    entity.layer = nil
    return result
end

function resizableWaterfall.rectangle(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local height = getWaterfallHeight(room, entity, entity.ignoreSolids)

    return utils.rectangle(x, y, entity.width, height)
end

function resizableWaterfall.depth(room, entity)
    if entity.ignoreSolids then
        return -49900
    end

    return -9999
end

return resizableWaterfall
