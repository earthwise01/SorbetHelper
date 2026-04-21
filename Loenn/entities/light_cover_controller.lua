local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local lightCoverController = {}

lightCoverController.name = "SorbetHelper/LightCoverController"
lightCoverController.sprite = sorbetHelper.getControllerSpriteFunction("lightCoverController")
lightCoverController.depth = sorbetHelper.controllerDepth
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
             options = function() return sorbetHelper.getMapSIDs() end,
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
