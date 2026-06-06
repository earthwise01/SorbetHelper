local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local loadedState = require("loaded_state")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local sparklingWater = {}

sparklingWater.name = "SorbetHelper/SparklingWater"
sparklingWater.warnBelowSize = {8, 8}
sparklingWater.placements = {
    name = "sparkling_water",
    data = {
        width = 8,
        height = 8,
        depth = -9999,
        collidable = true,
        canSplash = true,
        topSurface = true,
        bottomSurface = false
    }
}

sparklingWater.fieldOrder = {
    "x", "y",
    "width", "height",
    "depth", "collidable", "canSplash",
    "topSurface", "bottomSurface"
}

sparklingWater.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepthOptions({
            {"Water", -9999}
        }),
        editable = true
    }
}

function sparklingWater.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width or 8, entity.height or 16)
end

local defaultFillColor, defaultOutlineColor = utils.getColor("4480b890"), utils.getColor("87cefaf0")
local sessionFillColor, sessionOutlineColor = {1, 1, 1, 0.3 / 0.8}, {1, 1, 1, 0.8 / 0.8}

local function getColorsFromController(controller)
    local fillColor = sorbetHelper.isCounterOrSessionExpression(controller.fillColor) and sessionFillColor or utils.getColor(controller.fillColor) or defaultFillColor
    local outlineColor = sorbetHelper.isCounterOrSessionExpression(controller.outlineColor) and sessionOutlineColor or utils.getColor(controller.outlineColor) or defaultOutlineColor
    return fillColor, outlineColor
end

local function getColors(currentRoom, self)
    local map = loadedState.map
    if not map then
        return defaultFillColor, defaultOutlineColor
    end

    local allDepthsController = nil
    for _, room in pairs(map.rooms) do
        for _, entity in pairs(room.entities) do
            if entity._name == "SorbetHelper/SparklingWaterColorController" and (room == currentRoom or entity.global or utils.startsWith(room.name, "_bb_global")) then
                if entity.affectedDepth == self.depth then
                    return getColorsFromController(entity)
                end

                if not entity.affectedDepth and not allDepthsController then
                    allDepthsController = entity
                end
            end
        end
    end

    if allDepthsController then
        return getColorsFromController(allDepthsController)
    end

    return defaultFillColor, defaultOutlineColor
end

local function multiplyAlpha(color, alpha)
    return {color[1], color[2], color[3], (color[4] or 1) * alpha}
end

function sparklingWater.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 16

    local fillColor, outlineColor = getColors(room, entity)
    -- reduce alpha slightly
    fillColor = multiplyAlpha(fillColor, 0.8)
    outlineColor = multiplyAlpha(outlineColor, 0.8)

    return drawableRectangle.fromRectangle("bordered", x, y, width, height, fillColor, outlineColor)
end

function sparklingWater.depth(room, entity)
    return entity.depth or -9999
end

return sparklingWater
