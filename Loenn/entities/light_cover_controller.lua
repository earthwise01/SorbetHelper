local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local lightCoverController = {}

lightCoverController.name = "SorbetHelper/LightCoverController"
lightCoverController.sprite = sorbetUtils.getControllerSpriteFunction("lightCoverController")
lightCoverController.depth = sorbetUtils.controllerDepth
lightCoverController.placements = {
    name = "light_cover_controller",
    data = {
        alpha = 1,
        classNames = "",
        minDepth = "",
        maxDepth = "",
        global = false
    }
}

lightCoverController.ignoredFields = {
    "_id", "_name",
    "useFullClassNames"
}

lightCoverController.fieldOrder = {
    "x", "y",
    "alpha", "maxDepth",
    "classNames", "minDepth"
}

lightCoverController.fieldInformation = {
    classNames = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return sorbetUtils.getMapSIDs() end,
             searchable = true
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

return lightCoverController
