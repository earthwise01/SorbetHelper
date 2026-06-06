-- Legacy, replaced by the Styleground Depth Controller

local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local stylegroundEntityController = {}

stylegroundEntityController.name = "SorbetHelper/StylegroundEntityController"
stylegroundEntityController.sprite = sorbetHelper.getControllerSpriteFunction("stylegroundEntityController", true)
stylegroundEntityController.depth = sorbetHelper.controllerDepth

stylegroundEntityController.fieldOrder = {
    "x", "y",
    "tag", "depth"
}

stylegroundEntityController.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepthOptions(),
        editable = true
    }
}

return stylegroundEntityController
