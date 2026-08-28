local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local waterInteractionController = {}

waterInteractionController.name = "SorbetHelper/WaterInteractionController"
waterInteractionController.sprite = sorbetHelper.getControllerSpriteFunction("waterInteractionController")
waterInteractionController.depth = sorbetHelper.controllerDepth
waterInteractionController.placements = {
    name = "water_interaction_controller",
    data = {
        affectedTypes = "",
        global = false
    }
}

waterInteractionController.fieldInformation = {
    affectedTypes = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return sorbetHelper.getMapSIDs() end,
             searchable = true
        }
    }
}

return waterInteractionController
