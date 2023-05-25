local wingedStrawberryDirectionController = {}

directions = {
    "Up",
    "Down",
    "Left",
    "Right"
}

wingedStrawberryDirectionController.name = "SorbetHelper/WingedStrawberryDirectionController"
wingedStrawberryDirectionController.texture = "@Internal@/northern_lights"
wingedStrawberryDirectionController.placements = {
    name = "wingedStrawberryController",
    data = {
        direction = "Up"
    }
}
wingedStrawberryDirectionController.fieldInformation = {
    direction = {
        options = directions,
        editable = false
    }
}

return wingedStrawberryDirectionController