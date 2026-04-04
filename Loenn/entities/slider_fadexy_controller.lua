local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local sliderFadeXYController = {}

sliderFadeXYController.name = "SorbetHelper/SliderFadeXY"
sliderFadeXYController.sprite = sorbetUtils.getControllerSpriteFunction("sliderFadeXYController")
sliderFadeXYController.depth = sorbetUtils.controllerDepth
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
    "x", "y"
    "fadeX", "fadeY",
    "slider", "global"
}

return sliderFadeXYController
