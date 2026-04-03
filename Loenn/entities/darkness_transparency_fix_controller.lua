local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local darknessTransparencyFixController = {}

darknessTransparencyFixController.name = "SorbetHelper/DarknessTransparencyFixController"
darknessTransparencyFixController.sprite = sorbetUtils.getControllerSpriteFunction("darknessTransparencyFixController")
darknessTransparencyFixController.depth = sorbetUtils.controllerDepth
darknessTransparencyFixController.placements = {
    name = "darkness_transparency_fix_controller",
    data = {
        global = false,
    }
}

return darknessTransparencyFixController
