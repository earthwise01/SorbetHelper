local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

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
wingedStrawberryDirectionController.depth = sorbetHelper.controllerDepth
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
