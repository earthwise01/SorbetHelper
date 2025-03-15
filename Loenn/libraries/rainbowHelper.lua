local utils = require("utils")

local rainbowHelper = {}

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

local function split(inputstr, seperator)
    local result = {}

    seperator = seperator or " "
    for string in string.gmatch(inputstr, "([^" .. seperator .. "]+)") do
        table.insert(result, string)
    end

    return result
end

function rainbowHelper.getColors(colorsString, loopColors)
    loopColors = loopColors or false
    local colors = {}

    for _, v in pairs(split(colorsString, ",")) do
        table.insert(colors, utils.getColor(v))
    end

    if loopColors then
        table.insert(colors, colors[1])
    end

    return colors
end

local defaultColors = rainbowHelper.getColors("89E5AE,88E0E0,87A9DD,9887DB,D088E2")

function rainbowHelper.getHue(x, y, colors, gradientSize, loopColors, centerX, centerY)
    x, y = x or 0, y or 0
    colors = colors or defaultColors
    gradientSize = gradientSize or 280
    loopColors = loopColors or false
    centerX, centerY = centerX or 0, centerY or 0

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

return rainbowHelper