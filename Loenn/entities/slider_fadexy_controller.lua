local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local sliderFadeXYController = {}

sliderFadeXYController.name = "SorbetHelper/SliderFadeXY"
sliderFadeXYController.sprite = sorbetHelper.getControllerSpriteFunction("sliderFadeXYController")
sliderFadeXYController.depth = sorbetHelper.controllerDepth
sliderFadeXYController.placements = {
    name = "camera_fade_xy_to_slider_controller",
    data = {
        slider = "",
        fadeX = "",
        fadeY = "",
        global = false
    }
}

sliderFadeXYController.fieldOrder = {
    "x", "y",
    "fadeX", "fadeY",
    "slider", "global"
}

return sliderFadeXYController
