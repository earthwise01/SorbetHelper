local lightBeamHelper = require("helpers.light_beam")
local utils = require("utils")
local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")
local drawableText = require("structs.drawable_text")
local constColors = require("consts.colors")
local mods = require("mods")
local sorbetHelper = mods.requireFromPlugin("libraries.sorbet_helper")
local rainbowHelper = mods.requireFromPlugin("libraries.rainbow_helper")

local customLightbeam = {}

customLightbeam.name = "SorbetHelper/CustomLightbeam"
customLightbeam.placements = {
    -- todo: should i be using this or maybe just the `default` fieldInformation options ? or even some custom way of declaring them
    default = {
        data = {
            rotation = 0.0,
            depth = -9998,
            flag = "",
            inverted = false,
            flagFadeTime = 0.25,
            fadeWhenNear = true,
            fadeOnTransition = true,
            noParticles = false,
            texture = "util/lightbeam",
            color = "ccffff",
            alpha = 1.0,
            additive = false,
            rainbow = false,
            singleColor = false,
            useCustomRainbowColors = false,
            colors = "89e5ae,88e0e0,87a9dd,9887db,d088e2",
            gradientSize = 280.0,
            gradientSpeed = 50.0,
            loopColors = false,
            centerX = 0.0,
            centerY = 0.0,
            scroll = 1.0
        }
    },
    {
        name = "custom_lightbeam",
        data = {
            width = 32,
            height = 24
        }
    },
    {
        name = "custom_lightbeam_rainbow",
        alternativeName = "rainbow_lightbeam",
        data = {
            width = 32,
            height = 24,
            rainbow = true
        }
    },
}

-- hide rainbow specific fields unless rainbow is enabled and vice versa
function customLightbeam.ignoredFields(entity)
    if entity.rainbow and entity.useCustomRainbowColors then
        return {
            "_id", "_name",
            "color"
        }
    else
        return {
            "_id", "_name",
            "colors", "gradientSize", "gradientSpeed", "centerX", "centerY", "loopColors"
        }
    end
end

function customLightbeam.fieldOrder(entity)
    if not entity.rainbow or not entity.useCustomRainbowColors then
        return {
            "x", "y",
            "width", "height",
            "color", "alpha",
            "depth", "rotation",
            "texture", "scroll",
            "flag", "flagFadeTime",
            "inverted", "fadeOnTransition", "fadeWhenNear", "noParticles",
            "rainbow", "useCustomRainbowColors", "singleColor", "additive"
        }
    else
        return {
            "x", "y",
            "width", "height",
            "colors", "alpha",
            "gradientSize", "centerX",
            "gradientSpeed", "centerY",
            "depth", "rotation",
            "texture", "scroll",
            "flag", "flagFadeTime",
            "inverted", "fadeOnTransition", "fadeWhenNear", "noParticles",
            "rainbow", "useCustomRainbowColors", "singleColor", "additive",
            "loopColors"
        }
    end
end

customLightbeam.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepths({
            {"Lightbeams", -9998}
        }),
        editable = true
    },
    flagFadeTime = {
        minimumValue = 0.0
    },
    color = {
        fieldType = "color"
    },
    alpha = {
        default = 1.0,
        maximumValue = 1.0,
        minimumValue = 0.0
    },
    additive = {
        default = false
    },
    colors = {
        fieldType = "list",
        elementOptions = {
            fieldType = "color",
            showAlpha = true
        }
    },
    scroll = {
        default = 1.0
    }
}

function customLightbeam.sprite(room, entity)
    local sprites = {}

    if entity.rainbow then
        local colors = rainbowHelper.getColors(entity.colors or "89E5AE,88E0E0,87A9DD,9887DB,D088E2")

        if entity.singleColor then
            sprites = lightBeamHelper.getSprites(room, entity, colors[1], false)
        else
            sprites = customLightbeam.getSpritesRainbow(room, entity, colors, false)
        end
    else
        sprites = lightBeamHelper.getSprites(room, entity, utils.getColor(entity.color), false)
    end

    return sprites
end

-- modified from lightBeamHelper.getSprites to have a rainbow gradient effect
function customLightbeam.getSpritesRainbow(room, entity, colors, onlyBase)
    local lightBeamTexture = "util/lightbeam"
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

        local color = rainbowHelper.getHue(x + widthOffsetX + offsetX, y + widthOffsetY + offsetY, colors, entity.gradientSize, entity.loopColors, entity.centerX, entity.centerY)
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

                local color = rainbowHelper.getHue(x + widthOffsetX + offsetX, y + widthOffsetY + offsetY, colors, entity.gradientSize, entity.loopColors, entity.centerX, entity.centerY)
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

function customLightbeam.depth(room, entity)
    return entity.depth or -9998
end

function customLightbeam.selection(room, entity)
    local base = lightBeamHelper.getSelection(room, entity)
    local nodes = entity.nodes or {}

    if #nodes < 1 then
        return base, nil
    end

    -- for scroll anchor
    local nx, ny = nodes[1].x or 0, nodes[1].y or 0
    return base, {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

customLightbeam.rotate = lightBeamHelper.rotate
customLightbeam.updateResizeSelection = lightBeamHelper.updateResizeSelection

function customLightbeam.nodeLimits(room, entity)
    if entity.scroll then
        return 0, 1
    end

    return 0, 0
end

function customLightbeam.nodeSprite(room, entity, node)
    local x, y = entity.x or 0, entity.y or 0
    local nx, ny = node.x or 0, node.y or 0
    local anchor = sorbetHelper.getGenericNodeSprite(nx, ny, constColors.selectionCompleteNodeLineColor)
    local line = drawableLine.fromPoints({x, y, nx, ny}, constColors.selectionCompleteNodeLineColor)
    local desc = drawableText.fromText("Parallax Anchor", nx - 16, ny - 14, 32, 8, nil, 0.75)

    return {anchor, line, desc}
end

function customLightbeam.nodeAdded(room, entity, nodeIndex)
    local nodes = entity.nodes or {}

    if nodeIndex == 0 then
        local nodeX = entity.x
        local nodeY = entity.y

        table.insert(nodes, 1, {x = nodeX, y = nodeY})

    else
        local nodeX = nodes[nodeIndex].x
        local nodeY = nodes[nodeIndex].y - 16

        table.insert(nodes, nodeIndex + 1, {x = nodeX, y = nodeY})
    end

    return true
end

return customLightbeam
