local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local entityStylegroundController = {}

entityStylegroundController.name = "SorbetHelper/EntityStylegroundController"
entityStylegroundController.sprite = sorbetUtils.getControllerSpriteFunction("entityStylegroundController")
entityStylegroundController.depth = -1000010
entityStylegroundController.placements = {
    name = "controller",
    data = {
        classNames = "",
        useFullClassNames = false,
        minDepth = "",
        maxDepth = "",
        global = false,

        tag = "",

        _instructionsButton = true
    }
}

entityStylegroundController.fieldInformation = {
    classNames = {
        fieldType = "list",
        minimumElements = 0,
        elementDefault = "",
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
        fieldType = "sorbetHelper.info_button"
    }
}

entityStylegroundController.fieldOrder = {
    "x", "y",
    "tag", "maxDepth",
    "classNames", "minDepth",
    "useFullClassNames", "global",
    "_instructionsButton", 
}

return entityStylegroundController