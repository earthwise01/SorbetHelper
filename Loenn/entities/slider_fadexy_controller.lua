local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local sliderFadeXY = {}

sliderFadeXY.name = "SorbetHelper/SliderFadeXY"
sliderFadeXY.sprite = sorbetUtils.getControllerSpriteFunction("sliderFadeXY")
sliderFadeXY.depth = -1000010
sliderFadeXY.placements = {
    name = "sliderFadeXY",
    data = {
        slider = "",
        fadeX = "", fadeY = "",
        global = true
    }
}

return sliderFadeXY