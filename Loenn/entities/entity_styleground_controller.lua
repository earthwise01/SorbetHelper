local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local entityStylegroundController = {}

entityStylegroundController.name = "SorbetHelper/EntityStylegroundController"
entityStylegroundController.sprite = sorbetUtils.getControllerSpriteFunction("entityStylegroundController")
entityStylegroundController.depth = -1000010
entityStylegroundController.placements = {
    name = "controller",
    data = {
        classNames = "",
        minDepth = "",
        maxDepth = "",
        global = false,

        tag = "",

        _instructionsButton = true
    }
}

entityStylegroundController.fieldInformation = function()
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
        _instructionsButton = {
            fieldType = "sorbetHelper.infoButton"
        }
    }
end

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

return entityStylegroundController
