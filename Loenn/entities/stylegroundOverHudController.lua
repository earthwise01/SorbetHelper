local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local stylegroundOverHudController = {}

stylegroundOverHudController.name = "SorbetHelper/StylegroundOverHudController"
stylegroundOverHudController.sprite = sorbetUtils.getControllerSpriteFunction("stylegroundOverHudController", true, true)
stylegroundOverHudController.depth = -1000010
stylegroundOverHudController.placements = {
    name = "stylegroundOverHud",
    alternativeName = "altname",
    data = {
        pauseBehavior = 2,
        _instructionsButton = true
    }
}

stylegroundOverHudController.fieldInformation = {
    pauseBehavior = {
        options = {
            {"Disable When Paused", 2},
            {"Update When Paused", 1},
            {"Pause Above", 0},
        },
        editable = false
    },
    _instructionsButton = {
        fieldType = "sorbetHelper.infoButton"
    }
}

stylegroundOverHudController.fieldOrder = {"x", "y", "pauseBehavior", "_instructionsButton",}

return stylegroundOverHudController