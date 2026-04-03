local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local entityStylegroundController = {}

entityStylegroundController.name = "SorbetHelper/EntityStylegroundController"
entityStylegroundController.sprite = sorbetUtils.getControllerSpriteFunction("entityStylegroundController")
entityStylegroundController.depth = sorbetUtils.controllerDepth
entityStylegroundController.placements = {
    name = "entity_as_styleground_controller",
    data = {
        classNames = "",
        minDepth = "",
        maxDepth = "",
        global = false,
        tag = "",
        _instructionsButton = true
    }
}

entityStylegroundController.ignoredFields = {
    "_id", "_name",
    "useFullClassNames"
}

entityStylegroundController.fieldOrder = {
    "x", "y",
    "tag", "maxDepth",
    "classNames", "minDepth",
    "global",
    "_instructionsButton"
}

entityStylegroundController.fieldInformation = {
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
    _instructionsButton = {
        fieldType = "sorbetHelper.infoButton"
    }
}

return entityStylegroundController
