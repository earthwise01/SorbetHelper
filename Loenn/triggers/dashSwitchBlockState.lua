local trigger = {}

trigger.name = "SorbetHelper/DashSwitchBlockStateTrigger"
trigger.placements = {
    name = "dashSwitchBlockState",
    data = {
        index = 0,
        mode = "OnPlayerEnter",
    }
}


local colorNames = {
    ["Cyan"] = 0,
    ["Yellow"] = 1,
}

trigger.fieldInformation = {
    index = {
        fieldType = "integer",
        options = colorNames,
        editable = false
    },
    mode = {
        options = { "OnPlayerEnter", "OnPlayerSpawn", "OnLevelLoad" },
        editable = false
    }
}

return trigger