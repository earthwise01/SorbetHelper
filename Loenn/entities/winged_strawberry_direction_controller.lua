local wingedStrawberryDirectionController = {}

wingedStrawberryDirectionController.name = "SorbetHelper/WingedStrawberryDirectionController"
wingedStrawberryDirectionController.texture = "editorSprites/SorbetHelper/wingedStrawberryDirectionController"
wingedStrawberryDirectionController.placements = {
    name = "wingedStrawberryController",
    data = {
        direction = "Up"
    }
}
wingedStrawberryDirectionController.fieldInformation = {
    direction = {
        options = {
            "Up",
            "Down",
            "Left",
            "Right",
            "UpLeft",
            "UpRight",
            "DownLeft",
            "DownRight"
        },
        editable = false
    }
}

return wingedStrawberryDirectionController