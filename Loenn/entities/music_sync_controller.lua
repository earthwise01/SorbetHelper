local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local musicSyncController = {}

musicSyncController.name = "SorbetHelper/MusicSyncControllerFMOD"
musicSyncController.sprite = sorbetHelper.getControllerSpriteFunction("musicSyncController", true, true)
musicSyncController.depth = sorbetHelper.controllerDepth
musicSyncController.placements = {
    name = "fmod_marker_to_flag_controller",
    alternativeName = "music_sync_controller",
    data = {
        eventNames = "",
        showDebugUI = false,
        _infoButton = true
    }
}

musicSyncController.fieldOrder = {
    "x", "y",
    "eventNames", "showDebugUI",
    "_infoButton"
}

musicSyncController.fieldInformation = {
    eventNames = {
        fieldType = "list",
        elementDefault = "event:/new_content/music/lvl10/final_run"
    },
    _infoButton = {
        fieldType = "sorbet_helper.info_button"
    }
}

return musicSyncController
