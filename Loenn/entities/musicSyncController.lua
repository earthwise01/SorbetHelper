local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local controller = {}

controller.name = "SorbetHelper/MusicSyncControllerFMOD"
controller.sprite = sorbetUtils.getControllerSpriteFunction("musicSyncController", true, true)
controller.depth = -1000010
controller.placements = {
    name = "controller",
    data = {
        eventNames = "",
        showDebugUI = false,

        _infoButton = true
    }
}

controller.fieldInformation = {
    eventNames = {
        fieldType = "list",
        elementDefault = "event:/new_content/music/lvl10/final_run"
    },
    _infoButton = {
        fieldType = "sorbetHelper.infoButton"
    }
}

controller.fieldOrder = {
    "x", "y",
    "eventNames", "showDebugUI",
    "_infoButton"
}

return controller