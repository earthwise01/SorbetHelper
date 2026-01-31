local lightBeamHelper = require("helpers.light_beam")
local drawing = require("utils.drawing")
local utils = require("utils")
local drawableLine = require("structs.drawable_line")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local drawableText = require("structs.drawable_text")
local constColors = require("consts.colors")
local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = mods.requireFromPlugin("libraries.utils")
local rainbowHelper = mods.requireFromPlugin("libraries.rainbow_helper")

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
            flagFadeTime = 0.25,
            scroll = 1.0,
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
            flagFadeTime = 0.25,
            scroll = 1.0,
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
    },
    depth = {
        fieldType = "integer",
        options = depths.addDepth(depths.getDepths(), "Lightbeams", -9998),
        editable = true
    },
    -- wip (update nvm   some other time i guess)
    -- scrollAnchor = {
    --     fieldType = "sorbetHelper.vector",
    -- }
}

-- hide rainbow specific fields unless rainbow is enabled and vice versa
function lightBeam.fieldOrder(entity)
    local fields = {}
    --  this sucksss  why do i always put effort into trying to make these look pretty
    if entity.rainbow == true and entity.useCustomRainbowColors == true then
        fields = {
            "x", "y",
            "width", "height",
            "colors", "centerX",
            "gradientSize", "centerY",
            "gradientSpeed", "alpha",
            "depth", "rotation",
            "flag",
            "scroll",
            "flagFadeTime",
            "texture", "inverted", "fadeOnTransition",
            "rainbow", "useCustomRainbowColors", "noParticles", "fadeWhenNear",
            "singleColor", "loopColors"
        }
    else
        fields = {
            "x", "y",
            "width", "height",
            "color", "alpha",
            "depth", "rotation",
            "flag",
            "scroll",
            "flagFadeTime",
            "texture", "inverted", "fadeOnTransition",
            "rainbow", "useCustomRainbowColors", "noParticles", "fadeWhenNear",
            "singleColor"
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


function lightBeam.selection(room, entity)
    local base = lightBeamHelper.getSelection(room, entity)
    local nodes = entity.nodes or {}

    if #nodes < 1 then
        return base, nil
    end

    -- for scroll anchor
    local nx, ny = nodes[1].x or 0, nodes[1].y or 0
    return base, {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

lightBeam.rotate = lightBeamHelper.rotate
lightBeam.updateResizeSelection = lightBeamHelper.updateResizeSelection

-- rendering stuff
function lightBeam.sprite(room, entity)
    local result = {}

    if entity.rainbow then
        local colors = rainbowHelper.getColors(entity.colors or "89E5AE,88E0E0,87A9DD,9887DB,D088E2")

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

-- node stuff, only for scroll anchor sadly, no node based rotation (yet? if i ever get around to it,)
function lightBeam.nodeLimits(room, entity)
    if entity.scroll then
        return 0, 1
    end

    return 0, 0
end

function lightBeam.nodeSprite(room, entity, node)
    local x, y = entity.x or 0, entity.y or 0
    local nx, ny = node.x or 0, node.y or 0
    local anchor = sorbetUtils.getGenericNodeSprite(nx, ny, constColors.selectionCompleteNodeLineColor)
    local line = drawableLine.fromPoints({x, y, nx, ny}, constColors.selectionCompleteNodeLineColor)
    local desc = drawableText.fromText("Parallax Anchor", nx - 16, ny - 14, 32, 8, nil, 0.75)

    return { anchor, line, desc }
end

function lightBeam.nodeAdded(room, entity, nodeIndex)
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

return lightBeam
