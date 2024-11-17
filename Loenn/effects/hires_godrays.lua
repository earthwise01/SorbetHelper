local effect = {}

effect.name = "SorbetHelper/HiResGodrays"
effect.canBackground = false
effect.canForeground = true

effect.defaultData = {
    only = "*", exclude = "", tag = "", flag = "",
    colors = "f52b6380", fadex = "", fadey = "", notflag = "",
    scrollX = 1.1, scrollY = 1.1, speedX = 0, speedY = 8,
    durationBase = 4, durationRange = 8, minWidth = 8, maxWidth = 16,
    minScale = 1, maxScale = 1, minLength = 20, maxLength = 40,
    texturePath = "", texStartRotated = true, texMinRotate = -22.5, texMaxRotate = 22.5,
    rayCount = 6, offscreenPadding = 32, fadeInOut = true, fadeNearPlayer = true,
}

effect.fieldOrder = {
    "only", "exclude", "tag", "flag", 
    "colors", "fadex", "fadey", "notflag",
    "scrollX", "scrollY", "durationBase", "durationRange", 
    "speedX", "speedY", "minWidth", "maxWidth",
    "minScale", "maxScale", "minLength", "maxLength",
    "rayCount", "texturePath", "texMinRotate", "texMaxRotate", 
    "offscreenPadding", "fadeNearPlayer",  "fadeInOut", "texStartRotated"
}

effect.fieldInformation = {
    colors = {
        fieldType = "list",
        minimumElements = 1,
        elementDefault = "f52b6380",
        elementOptions = {
            fieldType = "color",
            allowXNAColors = false,
            showAlpha = true
        }
    },
    texturePath = {
        options = {
            { "Normal Godrays", "" },
            { "star", "star" }
            { "snow", "snow" },
        }
    },
    rayCount = {
        fieldType = "integer",
        minimumValue = 1,
    },
    offscreenPadding = {
        fieldType = "integer",
        minimumValue = 0,
    },
}

return effect