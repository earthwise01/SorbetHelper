local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local lightCover = {}

lightCover.name = "SorbetHelper/LightCoverController"
lightCover.sprite = sorbetUtils.getControllerSpriteFunction("lightCoverController")
lightCover.depth = -1000010
lightCover.placements = {
    name = "controller",
    data = {
        classNames = "",
        minDepth = "",
        maxDepth = "",
        global = false,

        alpha = 1,
    }
}

lightCover.fieldInformation = function()
    return {
        classNames = {
            fieldType = "list",
            elementSeparator = ",",
            elementDefault = "",
            elementOptions = {
                 options = sorbetUtils.getMapSIDs(),
                 searchable = true,
            }
        },
        minDepth = {
            fieldType = "integer",
            allowEmpty = true
        },
        maxDepth = {
            fieldType = "integer",
            allowEmpty = true
        },
        alpha = {
            minimumValue = 0,
            maximumValue = 1
        }
    }
end

lightCover.ignoredFields = {
    "_id", "_name",
    "useFullClassNames"
}

lightCover.fieldOrder = {
    "x", "y",
    "alpha", "maxDepth",
    "classNames", "minDepth"
}

return lightCover
