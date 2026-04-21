local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local entityStylegroundController = {}

entityStylegroundController.name = "SorbetHelper/EntityStylegroundController"
entityStylegroundController.sprite = sorbetHelper.getControllerSpriteFunction("entityStylegroundController")
entityStylegroundController.depth = sorbetHelper.controllerDepth
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
    _instructionsButton = {
        fieldType = "sorbet_helper.info_button"
    }
}

return entityStylegroundController
