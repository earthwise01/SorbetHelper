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
        fieldType = "integer"
    }
}

stylegroundEntityController.fieldOrder = {
    "x", "y",
    "tag", "depth",
}

return stylegroundEntityController