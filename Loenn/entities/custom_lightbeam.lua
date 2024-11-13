local lightBeamHelper = require("helpers.light_beam")
local drawing = require("utils.drawing")
local utils = require("utils")
local drawableLine = require("structs.drawable_line")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")

local lightBeam = {}

lightBeam.name = "SorbetHelper/CustomLightbeam"
lightBeam.depth = -9998
lightBeam.placements = {
    {
        name = "customlightbeam",
        data = {
            width = 32,
            height = 24,
            flag = "",
            inverted = false,
            rotation = 0.0,
            depth = -9998,
            noParticles = false,
            texture = "util/lightbeam",
            color = "CCFFFF",
            alpha = 1.0,
            rainbow = false,
            useCustomRainbowColors = false,
            colors = "89E5AE,88E0E0,87A9DD,9887DB,D088E2",
            gradientSize = 280.0,
            loopColors = false,
            centerX = 0.0,
            centerY = 0.0,
            gradientSpeed = 50.0,
            singleColor = false,
            fadeWhenNear = true,
            fadeOnTransition = true,
            flagFadeTime = 0.25
        }
    },
    {
        name = "customlightbeamrainbow",
        alternativeName = "rainbowlightbeam",
        data = {
            width = 32,
            height = 24,
            flag = "",
            inverted = false,
            rotation = 0.0,
            depth = -9998,
            noParticles = false,
            texture = "util/lightbeam",
            color = "CCFFFF",
            alpha = 1.0,
            rainbow = true,
            useCustomRainbowColors = false,
            colors = "89E5AE,88E0E0,87A9DD,9887DB,D088E2",
            gradientSize = 280.0,
            loopColors = false,
            centerX = 0.0,
            centerY = 0.0,
            gradientSpeed = 50.0,
            singleColor = false,
            fadeWhenNear = true,
            fadeOnTransition = true,
            flagFadeTime = 0.25
        }
    },
}

lightBeam.fieldInformation = {
    colors = {
        fieldType = "list",
        elementOptions = {
            fieldType = "color",
            allowXNAColors = false,
            showAlpha = true,
        }
    },
    color = {
        fieldType = "color",
        allowXNAColors = false
    },
    alpha = {
        maximumValue = 1.0,
        minimumValue = 0.0
    },
    flagFadeTime = {
        minimumValue = 0.0
    }
}

-- hide rainbow specific fields unless rainbow is enabled and vice versa
function lightBeam.fieldOrder(entity)
    local fields = {}
    if entity.rainbow == true and entity.useCustomRainbowColors == true then
        fields = {
            "x", "y", "width", "height", "colors", "centerX", "gradientSize", "centerY", "gradientSpeed", "alpha", "depth", "rotation", "flag", "flagFadeTime", "texture",
            "inverted", "fadeOnTransition", "rainbow", "useCustomRainbowColors", "noParticles", "fadeWhenNear", "singleColor", "loopColors"
        }
    else
        fields = {
            "x", "y", "width", "height", "color", "alpha", "depth", "rotation", "flag", "flagFadeTime", "texture",
            "inverted", "fadeOnTransition", "rainbow", "useCustomRainbowColors", "noParticles", "fadeWhenNear", "singleColor"
        }
    end
    return fields
end

function lightBeam.ignoredFields(entity)
    local ignored = {}
    if entity.rainbow == true and entity.useCustomRainbowColors == true then
        ignored = {
            "_id", "_name",
            "color"
        }
    else
        ignored = {
            "_id", "_name",
            "colors", "gradientSize", "gradientSpeed", "centerX", "centerY", "loopColors"
        }
    end
    return ignored
end

lightBeam.selection = lightBeamHelper.getSelection
lightBeam.rotate = lightBeamHelper.rotate
lightBeam.updateResizeSelection = lightBeamHelper.updateResizeSelection

-- lua-ified versions of a couple methods from fna/monocle and the gethue method needed for the gradient effect
local function clamp(value, min, max)
    if value < min then
        return min
    end
    if value > max then
        return max
    end

    return value
end

local function lerp(value1, value2, amount)
    return value1 * (1 - amount) + value2 * amount
end

local function lerpColor(value1, value2, amount)
    amount = clamp(amount, 0, 1)

    return {
        lerp(value1[1], value2[1], amount),
        lerp(value1[2], value2[2], amount),
        lerp(value1[3], value2[3], amount),
        lerp(value1[4] or 1, value2[4] or 1, amount)
    }
end

local function vectorLength(x, y)
    return math.sqrt(x * x + y * y)
end

local function yoyo(value)
    if value <= 0.5 then
        return value * 2
    end

    return 1 - (value - 0.5) * 2
end

local function getHue(x, y, colors, gradientSize, loopColors, centerX, centerY)
    if (#colors == 1) then
        return colors[1]
    end

    local progress = vectorLength(x - centerX, y - centerY)

    while progress < 0 do
        progress = progress + gradientSize
    end

    progress = progress % gradientSize / gradientSize
    if not loopColors then
        progress = yoyo(progress)
    end

    if progress == 1 then
        return colors[#colors]
    end

    local globalProgress = progress * (#colors - 1)
    local colorIndex = math.floor(globalProgress)
    local progressInIndex = globalProgress - colorIndex
    return lerpColor(colors[colorIndex + 1], colors[colorIndex + 2], progressInIndex)
end

local function split(inputstr, seperator)
    local result = {}

    seperator = seperator or " "
    for string in string.gmatch(inputstr, "([^" .. seperator .. "]+)") do
        table.insert(result, string)
    end

    return result
end

-- rendering stuff
function lightBeam.sprite(room, entity)
    local result = {}

    if entity.rainbow then
        local colors = {}
        for _, v in pairs(split(entity.colors, ",")) do
            table.insert(colors, utils.getColor(v))
        end
        if entity.loopColors then
            table.insert(colors, colors[1])
        end

        if entity.singleColor then
            result = lightBeamHelper.getSprites(room, entity, colors[1], false)
        else
            result = lightBeam.getSpritesRainbow(room, entity, colors, false)
        end
    else
        result = lightBeamHelper.getSprites(room, entity, utils.getColor(entity.color), false)
    end

    return result
end

-- modified from lightBeamHelper.getSprites to have a rainbow gradient effect
function lightBeam.getSpritesRainbow(room, entity, colors, onlyBase)
    local lightBeamTexture = "util/lightbeam"
    -- Shallowcopy so we can change the alpha later
    --local color = table.shallowcopy(colors[1] or { 0.8, 1.0, 1.0, 0.4 })
    local sprites = {}
    local x, y = entity.x + room.x, entity.y + room.y

    local theta = math.rad(entity.rotation or 0)
    local width = entity.width or 32
    local height = entity.height or 24
    local halfWidth = math.floor(width / 2)
    local widthOffsetX, widthOffsetY = halfWidth * math.cos(theta), halfWidth * math.sin(theta)

    for i = 0, width - 1, 4 do
        local sprite = drawableSprite.fromTexture(lightBeamTexture, entity)
        local widthScale = (height - 4) / sprite.meta.width
        local extraOffset = i - width + 4
        local offsetX = utils.round(extraOffset * math.cos(theta))
        local offsetY = utils.round(extraOffset * math.sin(theta))

        local color = getHue(x + widthOffsetX + offsetX, y + widthOffsetY + offsetY, colors, entity.gradientSize, entity.loopColors, entity.centerX, entity.centerY)
        color[4] = 0.4

        sprite:addPosition(widthOffsetX, widthOffsetY)
        sprite:addPosition(offsetX, offsetY)
        sprite:setColor(color)
        sprite:setJustification(0.0, 0.0)
        sprite:setScale(widthScale, 4)
        sprite.rotation = theta + math.pi / 2

        table.insert(sprites, sprite)
    end

    utils.setSimpleCoordinateSeed(x, y)

    -- Selection doesn't need the extra visual beams
    if not onlyBase then
        for i = 0, width - 1, 4 do
            local num = i * 0.6
            local lineWidth = 4 + math.sin(num * 0.5 + 1.2) * 4.0
            local alpha = 0.6 + math.sin(num + 0.8) * 0.3
            local offset = math.sin((num + i * 32) * 0.1 + math.sin(num * 0.05 + i * 0.1) * 0.25) * (width / 2.0 - lineWidth / 2.0)

            -- Makes rendering a bit less boring, not used by game
            local offsetMultiplier = (math.random() - 0.5) * 2

            for _ = 1, 2 do
                local beamSprite = drawableSprite.fromTexture(lightBeamTexture, entity)
                local beamWidth = math.random(-4, 4)
                local extraOffset = offset * offsetMultiplier - width / 2 + beamWidth
                local offsetX = utils.round(extraOffset * math.cos(theta))
                local offsetY = utils.round(extraOffset * math.sin(theta))
                local beamLengthScale = (height - math.random(4, math.floor(height / 2))) / beamSprite.meta.width

                local color = getHue(x + widthOffsetX + offsetX, y + widthOffsetY + offsetY, colors, entity.gradientSize, entity.loopColors, entity.centerX, entity.centerY)
                color[4] = alpha

                beamSprite:addPosition(widthOffsetX, widthOffsetY)
                beamSprite:addPosition(offsetX, offsetY)
                beamSprite:setColor(color)
                beamSprite:setJustification(0.0, 0.0)
                beamSprite:setScale(beamLengthScale, beamWidth)
                beamSprite.rotation = theta + math.pi / 2

                table.insert(sprites, beamSprite)
            end
        end
    end

    return sprites
end

return lightBeam
