local mods = require("mods")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local wingedStrawberryDirectionController = {}

local directions = {
    "Up",
    "Down",
    "Left",
    "Right",
    "UpLeft",
    "UpRight",
    "DownLeft",
    "DownRight"
}

wingedStrawberryDirectionController.name = "SorbetHelper/WingedStrawberryDirectionController"
wingedStrawberryDirectionController.texture = "editorSprites/SorbetHelper/wingedStrawberryDirectionController"
wingedStrawberryDirectionController.depth = sorbetUtils.controllerDepth
wingedStrawberryDirectionController.placements = {
    name = "winged_strawberry_direction_controller",
    data = {
        direction = "Up"
    }
}

wingedStrawberryDirectionController.fieldOrder = {
    "x", "y",
    "direction"
}

wingedStrawberryDirectionController.fieldInformation = {
    direction = {
        options = directions,
        editable = false
    }
}

return wingedStrawberryDirectionController
