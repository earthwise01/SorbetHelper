local lightBeamHelper = require("helpers.light_beam")
local utils = require("utils")

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
            color = "CCFFFF",
            rainbow = false,
            colors = "89E5AE,88E0E0,87A9DD,9887DB,D088E2",
            gradientSize = 280.0,
            loopColors = false,
            centerX = 0.0,
            centerY = 0.0,
            gradientSpeed = 50.0,
            singleColor = false,
            fadeWhenNear = true,
            fadeOnTransition = true,
        }
    },
    {
        name = "rainbowlightbeam",
        data = {
            width = 32,
            height = 24,
            flag = "",
            inverted = false,
            rotation = 0.0,
            color = "CCFFFF",
            rainbow = true,
            colors = "89E5AE,88E0E0,87A9DD,9887DB,D088E2",
            gradientSize = 280.0,
            loopColors = false,
            centerX = 0.0,
            centerY = 0.0,
            gradientSpeed = 50.0,
            singleColor = false,
            fadeWhenNear = true,
            fadeOnTransition = true,
        }
    },
}

lightBeam.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = false
    }
}

-- hide rainbow specific fields unless rainbow is enabled and vice versa
function lightBeam.fieldOrder(entity)
    local fields = {}
    if entity.rainbow == true then
        fields = {
            "x", "y", "width", "height", "centerX", "centerY", "colors", "gradientSize", "gradientSpeed", "rotation", "flag",
            "fadeWhenNear", "fadeOnTransition", "inverted", "rainbow", "loopColors", "singleColor"
        }
    else
        fields = {
            "x", "y", "width", "height", "color", "rotation", "flag",
            "fadeWhenNear", "fadeOnTransition", "inverted", "rainbow"
        }
    end
    return fields
end

function lightBeam.ignoredFields(entity)
    local ignored = {}
    if entity.rainbow == true then
        ignored = {
            "_id", "_name",
            "color"
        }
    else
        ignored = {
            "_id", "_name",
            "colors", "gradientSize", "gradientSpeed", "centerX", "centerY", "singleColor", "loopColors"
        }
    end
    return ignored
end

function lightBeam.sprite(room, entity)
    -- force the lightbeam to be colored white instead of the regular color if rainbow is enabled
    local color

    if entity.rainbow == true then
        color = utils.getColor("FFFFFF")
    else
        color = utils.getColor(entity.color)
    end

    result = lightBeamHelper.getSprites(room, entity, color, false)

    return result
end

lightBeam.selection = lightBeamHelper.getSelection
lightBeam.rotate = lightBeamHelper.rotate

return lightBeam
