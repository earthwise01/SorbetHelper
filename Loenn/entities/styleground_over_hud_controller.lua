local stylegroundOverHudController = {}

stylegroundOverHudController.name = "SorbetHelper/StylegroundOverHudController"
stylegroundOverHudController.texture = "objects/SorbetHelper/stylegroundOverHudController/icon"
stylegroundOverHudController.placements = {
    name = "stylegroundOverHud",
    alternativeName = "altname",
    data = {
        _instructionsButton = true
    }
}

stylegroundOverHudController.fieldInformation = {
    _instructionsButton = {
        fieldType = "sorbetHelper.info_button"
    }
}

return stylegroundOverHudController