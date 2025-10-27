local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local controller = {}

controller.name = "SorbetHelper/DarknessTransparencyFixController"
controller.sprite = sorbetUtils.getControllerSpriteFunction("darknessTransparencyFixController")
controller.depth = -1000010
controller.placements = {
    name = "controller",
    data = {
        global = false,
    }
}

return controller