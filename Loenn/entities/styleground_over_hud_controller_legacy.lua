-- Legacy, replaced by the Styleground Depth Controller

local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local stylegroundOverHudController = {}

stylegroundOverHudController.name = "SorbetHelper/StylegroundOverHudController"
stylegroundOverHudController.sprite = sorbetHelper.getControllerSpriteFunction("stylegroundOverHudController", true, true)
stylegroundOverHudController.depth = sorbetHelper.controllerDepth

stylegroundOverHudController.fieldOrder = {
    "x", "y",
    "pauseBehavior",
    "_instructionsButton"
}

stylegroundOverHudController.fieldInformation = {
    pauseBehavior = {
        options = {
            {"Disable When Paused", 2},
            {"Update When Paused", 1},
            -- {"Pause Above", 0},
        },
        editable = false
    },
    _instructionsButton = {
        fieldType = "sorbet_helper.info_button"
    }
}

return stylegroundOverHudController
