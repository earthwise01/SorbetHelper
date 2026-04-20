local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local returnBubbleBehaviorController = {}

local collisionModes = {
    "Vanilla",
    "SquishFix",
    "TriggersOnly",
    "NoCollide"
}

returnBubbleBehaviorController.name = "SorbetHelper/ReturnBubbleBehaviorController"
returnBubbleBehaviorController.texture = "editorSprites/SorbetHelper/returnBubbleBehaviorController"
returnBubbleBehaviorController.depth = sorbetUtils.controllerDepth
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
        options = sorbetUtils.allEasings,
        editable = false
    },
    collisionMode = {
        options = collisionModes,
        editable = false
    }
}

return returnBubbleBehaviorController
