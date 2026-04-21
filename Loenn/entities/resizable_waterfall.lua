local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local waterfallHelper = require("helpers.waterfalls")
local connectedEntities = require("helpers.connected_entities")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local resizableWaterfall = {}

local splashParticleDepths = {
    {"BG Particles (8000)", "ParticlesBG"},
    {"Particles (-8000)", "Particles"},
    {"FG Particles (-50000)", "ParticlesFG"},
    {"None", "None"}
}

resizableWaterfall.name = "SorbetHelper/BigWaterfall"
resizableWaterfall.canResize = {true, false}
resizableWaterfall.minimumSize = {8, 0}
resizableWaterfall.placements = {
    {
        name = "resizable_waterfall_lines",
        alternativeName = "search_engine_optimization",
        data = {
            width = 8,
            lines = true,
            color = "87cefa",
            alpha = 1.0,
            wavePercent = 1.0,
            depth = -9999,
            splashParticleDepth = "ParticlesFG",
            ignoreSolids = false,
            ignoreWater = false,
            rippleWater = true
        }
    },
    {
        name = "resizable_waterfall",
        alternativeName = "search_engine_optimization",
        data = {
            width = 8,
            lines = false,
            color = "87cefa",
            alpha = 1.0,
            wavePercent = 0.8,
            depth = -9999,
            splashParticleDepth = "ParticlesFG",
            ignoreSolids = false,
            ignoreWater = false,
            rippleWater = true
        }
    },
    {
        name = "resizable_waterfall_lines_above",
        alternativeName = "search_engine_optimization",
        data = {
            width = 8,
            lines = true,
            color = "87cefa",
            alpha = 1.0,
            wavePercent = 1.0,
            depth = -49900,
            splashParticleDepth = "ParticlesFG",
            ignoreSolids = true,
            ignoreWater = false,
            rippleWater = true
        }
    }
}

resizableWaterfall.fieldOrder = {
    "x", "y",
    "width", "color",
    "depth", "alpha",
    "splashParticleDepth", "wavePercent",
    "ignoreSolids", "ignoreWater", "rippleWater", "lines"
}

resizableWaterfall.fieldInformation = {
    color = {
        fieldType = "color"
    },
    alpha = {
        minimumValue = 0,
        maximumValue = 1
    },
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepths({
            {"Water & Waterfalls", -9999},
            {"FG Waterfalls", -49900}
        }),
        editable = true
    },
    splashParticleDepth = {
        options = splashParticleDepths,
        editable = false
    },
    wavePercent = {
        options = {1.0, 0.8},
        minimumValue = 0,
        maximumValue = 1
    }
}

local function waterSearchPredicate(entity)
    return entity._name == "water" or entity._name == "pandorasBox/coloredWater" or utils.endsWith(entity._name, "SparklingWater")
end

local function collideFirst(rectangle, rectangles)
    for _, rect in ipairs(rectangles) do
        if utils.aabbCheck(rect, rectangle) then
            return rect
        end
    end

    return nil
end

-- stolen from loenn waterfallHelper but with the ability to ignore tiles & water
local function getWaterfallHeight(room, entity, ignoreTiles, ignoreWater)
    local waterBlocks = utils.filter(waterSearchPredicate, room.entities)
    local waterRectangles = connectedEntities.getEntityRectangles(waterBlocks)

    local x, y = entity.x or 0, entity.y or 0
    local tileX, tileY = math.floor(x / 8) + 1, math.floor(y / 8) + 1

    local roomHeight = room.height
    local wantedHeight = 8 - y % 8

    local tileMatrix = room.tilesFg.matrix

    while wantedHeight < roomHeight - y do
        if not ignoreWater then
            local waterfallRect = utils.rectangle(x, y + wantedHeight, 8, 8)
            local waterRect = collideFirst(waterfallRect, waterRectangles)
            if waterRect then
                wantedHeight = waterRect.y - y
                break
            end
        end

        if not ignoreTiles and tileMatrix:get(tileX, tileY + 1, "0") ~= "0" then
            break
        end

        wantedHeight += 8
        tileY += 1
    end

    return wantedHeight
end

-- adapted from loenn's waterfall helper with some edits
local function getWaterfallSprite(room, entity, fillColor, borderColor)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, getWaterfallHeight(room, entity, entity.ignoreSolids or false, entity.ignoreWater or false)

    local hasLines = entity.lines or false
    -- local linesOffset = (width <= 8 ? 1 : 2)
    local borderOffset = (width <= 8 ? 1 : 2)

    local sprites = {}

    local middleRectangle = drawableRectangle.fromRectangle("fill", x, y, width, height, fillColor)
    local leftRectangle = drawableRectangle.fromRectangle("fill", x - 1, y, borderOffset, height, borderColor)
    local rightRectangle = drawableRectangle.fromRectangle("fill", x + width + 1 - borderOffset, y, borderOffset, height, borderColor)

    table.insert(sprites, middleRectangle:getDrawableSprite())
    table.insert(sprites, leftRectangle:getDrawableSprite())
    table.insert(sprites, rightRectangle:getDrawableSprite())

    local addWaveLineSprite = waterfallHelper.addWaveLineSprite

    -- Add wave pattern
    for i = 0, height, 21 do
        -- From left to right in the waterfall
        -- Parts connected to side borders
        addWaveLineSprite(sprites, y, height, x - 1 + borderOffset, y + i + 9, 1, 12, borderColor)
        addWaveLineSprite(sprites, y, height, x + borderOffset, y + i + 11, 1, 8, borderColor)
        addWaveLineSprite(sprites, y, height, x + width - 1 - borderOffset, y + i, 1, 9, borderColor)
        addWaveLineSprite(sprites, y, height, x + width - 0 - borderOffset, y + i - 2, 1, 13, borderColor)

        if hasLines then
            -- Wave on left border
            addWaveLineSprite(sprites, y, height, x + 0 + borderOffset, y + i, 1, 9, borderColor)
            addWaveLineSprite(sprites, y, height, x + 1 + borderOffset, y + i + 9, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + 1 + borderOffset, y + i + 19, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + 2 + borderOffset, y + i + 11, 1, 8, borderColor)

            -- Wave on right border
            addWaveLineSprite(sprites, y, height, x + width - 1 - borderOffset, y + i - 10, 1, 8, borderColor)
            addWaveLineSprite(sprites, y, height, x + width - 2 - borderOffset, y + i - 2, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + width - 2 - borderOffset, y + i + 9, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + width - 3 - borderOffset, y + i, 1, 9, borderColor)
        end
    end

    return sprites
end

local function multiplyAlpha(color, alpha)
    -- loenn premultiplies waterfall colors normally even though it draws with alphamultiply blending
    -- return { color[1] * alpha, color[2] * alpha, color[3] * alpha, alpha }
    -- don't do that for better visual consistency with the game
    return {color[1], color[2], color[3], (color[4] or 1) * alpha}
end

function resizableWaterfall.sprite(room, entity)
    local color = multiplyAlpha(utils.getColor(entity.color), entity.alpha or 1)
    local fillColor = multiplyAlpha(color, 0.3)
    local borderColor = multiplyAlpha(color, 0.8)

    local result = getWaterfallSprite(room, entity, fillColor, borderColor)

    return result
end

function resizableWaterfall.depth(room, entity)
    return entity.depth or (entity.ignoreSolids and -49900 or -9999)
end

function resizableWaterfall.rectangle(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local height = getWaterfallHeight(room, entity, entity.ignoreSolids)

    return utils.rectangle(x, y, entity.width, height)
end

return resizableWaterfall
