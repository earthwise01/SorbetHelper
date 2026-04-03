local miniPopupTrigger = {}

local modes = {
    "OnPlayerEnter",
    "OnFlagEnabled",
    "OnFlagDisabled",
    "WhilePlayerInside"
}

miniPopupTrigger.name = "SorbetHelper/MiniPopupTrigger"
miniPopupTrigger.placements = {
    name = "mini_popup",
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

miniPopupTrigger.fieldOrder = {
    "x", "y",
    "width", "height",
    "titleText", "titleColor",
    "subText", "accentColor",
    "activeTime", "baseColor",
    "mode", "iconTexture",
    "flag", "texturePath",
    "onlyOnce", "removeOnLeave"
}

miniPopupTrigger.fieldInformation = {
    activeTime = {
        minimumValue = 0.0
    },
    baseColor = {
        fieldType = "color"
    },
    accentColor = {
        fieldType = "color"
    },
    titleColor = {
        fieldType = "color"
    },
    mode = {
        options = modes,
        editable = false
    }
}

return miniPopupTrigger
