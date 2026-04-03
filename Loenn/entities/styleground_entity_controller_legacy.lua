-- Legacy, replaced by the Styleground Depth Controller

local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local stylegroundEntityController = {}

stylegroundEntityController.name = "SorbetHelper/StylegroundEntityController"
stylegroundEntityController.sprite = sorbetUtils.getControllerSpriteFunction("stylegroundEntityController", true)
stylegroundEntityController.depth = sorbetUtils.controllerDepth

stylegroundEntityController.fieldOrder = {
    "x", "y",
    "tag", "depth"
}

stylegroundEntityController.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = sorbetUtils.getDepths(),
        editable = true
    }
}

return stylegroundEntityController
