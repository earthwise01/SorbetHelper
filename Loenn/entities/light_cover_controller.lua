local lightCover = {}

lightCover.name = "SorbetHelper/LightCoverController"
lightCover.texture = "editorSprites/SorbetHelper/lightCoverController"
lightCover.depth = -1000010
lightCover.placements = {
    name = "controller",
    data = {
        classNames = "",
        useFullClassNames = false,
        minDepth = "",
        maxDepth = "",
        global = false,

        alpha = 1,
    }
}

lightCover.fieldInformation = {
    classNames = {
        fieldType = "list",
        minimumElements = 0,
        elementDefault = "",
    },
    minDepth = {
        fieldType = "integer",
        allowEmpty = true
    },
    maxDepth = {
        fieldType = "integer",
        allowEmpty = true
    },
    alpha = {
        minimumValue = 0,
        maximumValue = 1
    }
}

lightCover.fieldOrder = {
    "x", "y",
    "alpha", "maxDepth",
    "classNames", "minDepth",
    "useFullClassNames"
}

return lightCover