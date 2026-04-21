local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local returnBubbleBehaviorController = {}

local collisionModes = {
    "Vanilla",
    "SquishFix",
    "TriggersOnly",
    "NoCollide"
}

returnBubbleBehaviorController.name = "SorbetHelper/ReturnBubbleBehaviorController"
returnBubbleBehaviorController.texture = "editorSprites/SorbetHelper/returnBubbleBehaviorController"
returnBubbleBehaviorController.depth = sorbetHelper.controllerDepth
returnBubbleBehaviorController.placements = {
    name = "return_bubble_controller",
    data = {
        time = 0.625,
        speed = 192,
        useSpeed = false,
        easing = "SineInOut",
        smoothCamera = false,
        refillDash = false,
        collisionMode = "Vanilla"
    }
}

returnBubbleBehaviorController.fieldOrder = {
    "x", "y",
    "time", "easing",
    "speed", "collisionMode",
    "useSpeed", "smoothCamera"
}

returnBubbleBehaviorController.fieldInformation = {
    time = {
        minimumValue = 0.0
    },
    easing = {
        options = sorbetHelper.allEasings,
        editable = false
    },
    collisionMode = {
        options = collisionModes,
        editable = false
    }
}

return returnBubbleBehaviorController
