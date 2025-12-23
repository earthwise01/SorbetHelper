local trigger = {}

trigger.name = "SorbetHelper/MiniPopupTrigger"
trigger.placements = {
    name = "miniPopup",
    data = {
        activeTime = 8.0,
        titleText = "AREA_7",
        subText = "CHECKPOINT_7_3",
        baseColor = "000000",
        accentColor = "f08080",
        titleColor = "ffffff",
        iconTexture = "",
        texturePath = "SorbetHelper/popup",

        mode = "OnPlayerEnter",
        onlyOnce = true,
        removeOnLeave = true,
        flag = ""
    }
}

trigger.fieldInformation = {
    activeTime = {
        minimumValue = 0.0
    },
    baseColor = {
        fieldType = "color",
        allowXNAColors = false
    },
    accentColor = {
        fieldType = "color",
        allowXNAColors = false
    },
    titleColor = {
        fieldType = "color",
        allowXNAColors = false
    },
    mode = {
        options = { "OnPlayerEnter", "OnFlagEnabled", "OnFlagDisabled", "WhilePlayerInside" },
        editable = false
    }
}

trigger.fieldOrder = {
    "x", "y",
    "width", "height",
    "titleText", "titleColor",
    "subText", "accentColor",
    "activeTime", "baseColor",
    "mode", "iconTexture",
    "flag", "texturePath",
    "onlyOnce", "removeOnLeave"
}

return trigger