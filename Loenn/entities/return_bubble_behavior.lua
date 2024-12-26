local easings = require("mods").requireFromPlugin("libraries.easings")
local collisionModes = { "Vanilla", "DisableSquish", "NoCollide" }

local returnBubbleBehavior = {}

returnBubbleBehavior.name = "SorbetHelper/ReturnBubbleBehaviorController"
returnBubbleBehavior.texture = "editorSprites/SorbetHelper/returnBubbleBehavior"
returnBubbleBehavior.placements = {
    name = "controller",
    data = {
        time = 0.625,
        speed = 192,
        useSpeed = false,
        easing = "SineInOut", -- this is a mistake probably but whatever
        smoothCamera = true, -- vanilla value is off but its nicer on i think
        -- canSkip = false,
        -- refillDash = false,
        collisionMode = "DisableSquish"
    }
}
returnBubbleBehavior.fieldInformation = {
    time = {
        minimumValue = 0.0
    },
    easing = {
        options = easings,
        editable = false
    },
    collisionMode = {
        options = collisionModes,
        editable = false
    }
}

returnBubbleBehavior.fieldOrder = {
    "x", "y",
    "time", "easing",
    "speed", "collisionMode",
    "useSpeed", "smoothCamera"
}

return returnBubbleBehavior