local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local controller = {}

controller.name = "SorbetHelper/SparklingWaterColorController"
controller.sprite = sorbetUtils.getControllerSpriteFunction("sparklingWaterColorController")
controller.depth = -1000010
controller.placements = {
    name = "normal",
    data = {
        outlineColor = "9ce4f7de",
        edgeColor = "8dceeb79",
        fillColor = "4289bd97",
        detailTexture = "objects/SorbetHelper/sparklingWater/detail",
        causticScale = 0.8,
        causticAlpha = 0.15,
        bubbleAlpha = 0.3,
        displacementSpeed = 0.25
    }
}

controller.fieldOrder = {
    "x", "y",
    "outlineColor", "edgeColor",
    "detailTexture", "fillColor",
    "causticAlpha", "bubbleAlpha",
    "causticScale", "displacementSpeed"
}

controller.fieldInformation = {
    outlineColor = {
        fieldType = "color",
        showAlpha = true
    },
    edgeColor = {
        fieldType = "color",
        showAlpha = true
    },
    fillColor = {
        fieldType = "color",
        showAlpha = true
    },
    causticAlpha = {
        minimumValue = 0,
        maximumValue = 1
    },
    bubbleAlpha = {
        minimumValue = 0,
        maximumValue = 1
    },
    displacementSpeed = {
        minimumValue = 0,
        maximumValue = 1
    }
}

return controller
