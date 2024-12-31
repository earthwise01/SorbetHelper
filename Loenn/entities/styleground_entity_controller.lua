local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")

local stylegroundEntityController = {}

stylegroundEntityController.name = "SorbetHelper/StylegroundEntityController"
stylegroundEntityController.texture = "editorSprites/SorbetHelper/stylegroundEntityController"
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