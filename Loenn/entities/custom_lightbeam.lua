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
customLightbeam.associatedMods = sorbetHelper.getSessionExpressionAssociatedModsFunction({"flag", "rotation", "color"})
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
    rotation = {
        fieldType = "sorbet_helper.float_source"
    },
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepthOptions({
            {"Lightbeams", -9998}
        }),
        editable = true
    },
    flagFadeTime = {
        minimumValue = 0.0
    },
    color = {
        fieldType = "sorbet_helper.color_source"
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

local function getSprites(room, entity)
    local x, y = room.x + entity.x, room.y + entity.y
    local width, height = entity.width or 32, entity.height or 24
    local halfWidth = math.floor(width / 2)
    local theta = math.rad(tonumber(entity.rotation) or 0)

    local beamTexture = entity.texture or "util/lightbeam"
    local color = sorbetHelper.isCounterOrSessionExpression(entity.color) and {1, 1, 1} or utils.getColor(entity.color or "ccffff")
    local rainbow = entity.rainbow or false
    local rainbowSingleColor = entity.singleColor or false
    local useCustomRainbowColors = entity.useCustomRainbowColors or false

    local function getRainbowHue(x, y, offsetX, offsetY)
        if not rainbowSingleColor then
            x, y = x + offsetX or 0, y + offsetY or 0
        end

        return rainbowHelper.getHue(x, y, room, useCustomRainbowColors and entity or nil)
    end

    local sprites = {}

    if rainbow and not singleColor then
        for i = 0, width - 1, 4 do
            local beamSprite = drawableSprite.fromTexture(beamTexture, entity)
            local beamLengthScale = (height - 4) / beamSprite.meta.width
            local beamOffset = -halfWidth + i
            local offsetX = beamOffset * math.cos(theta)
            local offsetY = beamOffset * math.sin(theta)

            local beamColor = table.shallowcopy(rainbow and getRainbowHue(x, y, offsetX, offsetY) or color)
            beamColor[4] = 0.4

            beamSprite:addPosition(offsetX, offsetY)
            beamSprite:setColor(beamColor)
            beamSprite:setJustification(0.0, 1.0)
            beamSprite:setScale(beamLengthScale, 4)
            beamSprite.rotation = theta + math.pi / 2

            table.insert(sprites, beamSprite)
        end

    else
        local baseSprite = drawableSprite.fromTexture(beamTexture, entity)
        local baseLengthScale = (height - 4) / baseSprite.meta.width
        local offsetX = -halfWidth * math.cos(theta)
        local offsetY = -halfWidth * math.sin(theta)

        local baseColor = table.shallowcopy(rainbow and getRainbowHue(x, y) or color)
        baseColor[4] = 0.4

        baseSprite:addPosition(offsetX, offsetY)
        baseSprite:setColor(baseColor)
        baseSprite:setJustification(0.0, 1.0)
        baseSprite:setScale(baseLengthScale, width)
        baseSprite.rotation = theta + math.pi / 2

        table.insert(sprites, baseSprite)
    end

    utils.setSimpleCoordinateSeed(x, y)

    for i = 0, width - 1, 4 do
        local num = i * 0.6
        local lineWidth = 4 + math.sin(num * 0.5 + 1.2) * 4.0
        local alpha = 0.6 + math.sin(num + 0.8) * 0.3
        local offset = math.sin((num + i * 32) * 0.1 + math.sin(num * 0.05 + i * 0.1) * 0.25) * (width / 2.0 - lineWidth / 2.0)

        -- not accurate to ingame but "makes rendering a bit less boring"
        local offsetMultiplier = (math.random() - 0.5) * 2

        for _ = 1, 2 do
            local beamSprite = drawableSprite.fromTexture(beamTexture, entity)
            local beamWidth = math.random(-4, 4)
            local beamOffset = offset * offsetMultiplier + beamWidth
            local offsetX = beamOffset * math.cos(theta)
            local offsetY = beamOffset * math.sin(theta)
            local beamLengthScale = (height - math.random(4, math.floor(height / 2))) / beamSprite.meta.width

            local beamColor = table.shallowcopy(rainbow and getRainbowHue(x, y, offsetX, offsetY) or color)
            beamColor[4] = alpha

            beamSprite:addPosition(offsetX, offsetY)
            beamSprite:setColor(beamColor)
            beamSprite:setJustification(0.0, 0.0)
            beamSprite:setScale(beamLengthScale, beamWidth)
            beamSprite.rotation = theta + math.pi / 2

            table.insert(sprites, beamSprite)
        end
    end


    return sprites
end

function customLightbeam.sprite(room, entity)
    return getSprites(room, entity)
end

function customLightbeam.depth(room, entity)
    return entity.depth or -9998
end

local function getSelection(entity)
    local width, height = entity.width or 32, entity.height or 24
    local theta = math.rad(tonumber(entity.rotation) or 0)
    local beamTexture = entity.texture or "util/lightbeam"

    local baseSprite = drawableSprite.fromTexture(beamTexture, entity)
    local baseLengthScale = (height - 4) / baseSprite.meta.width

    baseSprite:setJustification(0.0, 0.5) -- 0.5 doesnt work when rendering fsr but it Does for getRectangle ?
    baseSprite:setScale(baseLengthScale, width)
    baseSprite.rotation = theta + math.pi / 2

    return baseSprite:getRectangle()
end

function customLightbeam.selection(room, entity)
    local base = getSelection(entity)
    local nodes = entity.nodes or {}

    if #nodes < 1 then
        return base, nil
    end

    -- for scroll anchor
    local nx, ny = nodes[1].x or 0, nodes[1].y or 0
    return base, {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

customLightbeam.rotate = lightBeamHelper.rotate

function customLightbeam.updateResizeSelection(room, entity, node, selection, offsetX, offsetY, directionX, directionY)
    local newSelection = getSelection(entity)

    selection.x = newSelection.x
    selection.y = newSelection.y

    selection.width = newSelection.width
    selection.height = newSelection.height
end

function customLightbeam.nodeLimits(room, entity)
    return 0, entity.scroll and 1 or 0
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
