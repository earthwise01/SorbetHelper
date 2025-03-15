local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local stylegroundEntityController = {}

stylegroundEntityController.name = "SorbetHelper/StylegroundEntityController"
stylegroundEntityController.sprite = sorbetUtils.getControllerSpriteFunction("stylegroundEntityController", true)
stylegroundEntityController.depth = -1000010
stylegroundEntityController.placements = {
    name = "controller",
    alternativeName = "altname",
    data = {
        depth = 0,
        tag = ""
    }
}

stylegroundEntityController.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = depths.getDepths(),
        editable = true
    }
}

stylegroundEntityController.fieldOrder = {
    "x", "y",
    "tag", "depth",
}

return stylegroundEntityController