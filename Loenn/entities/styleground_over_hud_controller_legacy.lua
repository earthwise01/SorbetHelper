-- Legacy, replaced by the Styleground Depth Controller

local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local stylegroundOverHudController = {}

stylegroundOverHudController.name = "SorbetHelper/StylegroundOverHudController"
stylegroundOverHudController.sprite = sorbetUtils.getControllerSpriteFunction("stylegroundOverHudController", true, true)
stylegroundOverHudController.depth = sorbetUtils.controllerDepth

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
        fieldType = "sorbetHelper.infoButton"
    }
}

return stylegroundOverHudController
