local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local controller = {}

controller.name = "SorbetHelper/MusicSyncController"
controller.sprite = sorbetUtils.getControllerSpriteFunction("musicSyncController", true)
controller.depth = -1000010
controller.placements = {
    name = "controller",
    data = {
        eventName = "",
        tempoMarkers = "120-4-4-0",
        markers = "Marker-1000",
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
    "tempoMarkers", "eventName",
    "markers", "sessionPrefix",
    "showDebugUI",
    "_infoButton"
}

return controller