local stylegroundOverHudController = {}

stylegroundOverHudController.name = "SorbetHelper/StylegroundOverHudController"
stylegroundOverHudController.texture = "editorSprites/SorbetHelper/stylegroundOverHudController"
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
        fieldType = "sorbetHelper.info_button"
    }
}

stylegroundOverHudController.fieldOrder = {"x", "y", "pauseBehavior", "_instructionsButton",}

return stylegroundOverHudController