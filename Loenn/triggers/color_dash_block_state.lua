local colorDashBlockStateTrigger = {}

local colorNames = {
    ["Cyan"] = 0,
    ["Yellow"] = 1,
}

local modes = {
    "OnPlayerEnter",
    "OnPlayerSpawn",
    "OnLevelLoad"
}

colorDashBlockStateTrigger.name = "SorbetHelper/ColorDashBlockStateTrigger"
colorDashBlockStateTrigger.placements = {
    name = "color_dash_block_state",
    alternativeName = "colour_dash_block_state",
    data = {
        index = 0,
        mode = "OnPlayerEnter",
    }
}

colorDashBlockStateTrigger.fieldOrder = {
    "x", "y",
    "width", "height",
    "index", "mode"
}

colorDashBlockStateTrigger.fieldInformation = {
    index = {
        fieldType = "integer",
        options = colorNames,
        editable = false
    },
    mode = {
        options = modes,
        editable = false
    }
}

return colorDashBlockStateTrigger
