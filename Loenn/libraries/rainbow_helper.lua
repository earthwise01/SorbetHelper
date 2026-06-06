local utils = require("utils")

local rainbowHelper = {}

-- lua-ified versions of a few methods from fna/monocle needed for the two gethue methods
local function clamp(x, min, max)
    return x < min and min or x > max and max or x
end

local function lerp(a, b, amount)
    return a + (b - a) * amount
end

local function lerpColor(a, b, amount)
    amount = clamp(amount, 0, 1)

    return {
        lerp(a[1], b[1], amount),
        lerp(a[2], b[2], amount),
        lerp(a[3], b[3], amount),
        lerp(a[4] or 1, b[4] or 1, amount)
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

local colorsCache = {}
function rainbowHelper.getColorList(colorsString)
    local cached = colorsCache[colorsString]
    if cached then
        return cached
    end

    local colors = {}
    for _, color in pairs(string.split(colorsString, ",")()) do
        table.insert(colors, utils.getColor(color))
    end

    colorsCache[colorsString] = colors
    return colors
end

-- lua-ified version of the GetHue method from rainbow spinners
function rainbowHelper.getVanillaHue(x, y)
    local r, g, b = utils.hsvToRgb(0.4 + yoyo(vectorLength(x, y) % 280 / 280) * 0.4, 0.4, 0.9)
    return { r, g, b, 1 }
end

-- lua-ified version of the GetRainbowHue method from custom lightbeams
function rainbowHelper.getModdedHue(x, y, colors, gradientSize, loopColors, centerX, centerY)
    x, y = x or 0, y or 0
    colors = colors or rainbowHelper.getColorList("89e5ae,88e0e0,87a9dd,9887db,d088e2")
    gradientSize = gradientSize or 280
    loopColors = loopColors or false
    centerX, centerY = centerX or 0, centerY or 0

    if #colors == 1 then
        return colors[1]
    end

    local progress = vectorLength(x - centerX, y - centerY)
    while progress < 0 do
        progress = progress + gradientSize
    end
    progress = progress % gradientSize / gradientSize

    -- hmm
    local progressInColors = not loopColors and ((#colors - 1) * yoyo(progress)) or (#colors * progress)
    local colorIndex = math.floor(progressInColors)
    local nextColorIndex = colorIndex + 1
    local progressInIndex = progressInColors - colorIndex
    return lerpColor(colors[(colorIndex % #colors) + 1], colors[(nextColorIndex % #colors) + 1], progressInIndex)
end

local function getHueFromController(x, y, controller)
    return rainbowHelper.getModdedHue(x, y, rainbowHelper.getColorList(controller.colors), controller.gradientSize, controller.loopColors, controller.centerX, controller.centerY)
end

function rainbowHelper.getHue(x, y, room, controller)
    if controller then
        return getHueFromController(x, y, controller)
    end

    if room then
        local roomWideController = nil
        for _, entity in pairs(room.entities) do
            local name = entity._name
            if name == "MaxHelpingHand/RainbowSpinnerColorAreaController" or name == "MaxHelpingHand/FlagRainbowSpinnerColorAreaController" then
                if utils.aabbCheckInline(room.x + entity.x, room.y + entity.y, entity.width, entity.height, x - 1, y - 1, 2, 2) then
                    return getHueFromController(x, y, entity)
                end

            elseif roomWideController == nil and (name == "MaxHelpingHand/RainbowSpinnerColorController" or name == "MaxHelpingHand/FlagRainbowSpinnerColorController") then
                roomWideController = entity
            end
        end

        if roomWideController then
            return getHueFromController(x, y, roomWideController)
        end
    end

    return rainbowHelper.getVanillaHue(x, y)
end

return rainbowHelper
