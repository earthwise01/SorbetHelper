local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local darknessTransparencyFixController = {}

darknessTransparencyFixController.name = "SorbetHelper/DarknessTransparencyFixController"
darknessTransparencyFixController.sprite = sorbetHelper.getControllerSpriteFunction("darknessTransparencyFixController")
darknessTransparencyFixController.depth = sorbetHelper.controllerDepth
darknessTransparencyFixController.placements = {
    name = "darkness_transparency_fix_controller",
    data = {
        global = false
    }
}

return darknessTransparencyFixController
