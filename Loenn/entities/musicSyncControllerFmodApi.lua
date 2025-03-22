local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local controller = {}

controller.name = "SorbetHelper/MusicSyncControllerFMOD"
controller.sprite = sorbetUtils.getControllerSpriteFunction("musicSyncController", true, true)
controller.depth = -1000010
controller.placements = {
    name = "fmodcontroller",
    data = {
        eventNames = "",
        sessionPrefix = "musicSync",
        showDebugUI = false
    }
}

controller.fieldInformation = {
    _infoButton = {
        fieldType = "sorbetHelper.infoButton"
    }
}

controller.fieldOrder = {
    "x", "y",
    "eventNames", "sessionPrefix",
    "showDebugUI",
    "_infoButton"
}

return controller