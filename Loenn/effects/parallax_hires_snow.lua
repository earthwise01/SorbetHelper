local effect = {}

effect.name = "SorbetHelper/ParallaxHiResSnow"
effect.canBackground = false
effect.canForeground = true

effect.defaultData = {
    only = "*", exclude = "", fadex = "", fadey = "", fadeInOut = true,
    tag = "", flag = "", notflag = "",
    texturePath = "snow", randomRotation = true, fadeTowardsForeground = true,
    color = "ffffff", alpha = 1, additive = 0,
    directionX = -1, directionY = 0,
    minScale = 0.05, maxScale = 0.8,
    minSpeed = 2000, maxSpeed = 4000,
    minScrollX = 1, minScrollY = 1,
    maxScrollX = 1.25, maxScrollY = 1.25,
    sineAmplitude = 100, sineFrequency = 10,
    particleCount = 50,

}

effect.fieldOrder = {
    "only", "exclude", "tag", "flag", 
    "color", "alpha", "additive", "notflag",
    "texturePath", "particleCount", "fadex", "fadey", 
    "minScrollX", "minScrollY", "directionX", "directionY",
    "maxScrollX", "maxScrollY", "minSpeed", "maxSpeed",
    "sineAmplitude", "sineFrequency", "minScale", "maxScale",
    "randomRotation", "fadeTowardsForeground", "fadeInOut"
}

effect.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = false
    },
    alpha = {
        maximumValue = 1.0,
        minimumValue = 0.0
    },
    additive = {
        maximumValue = 1.0,
        minimumValue = 0.0
    },
    texturePath = {
        options = { "snow", "star" }
    },
    particleCount = {
        fieldType = "integer",
        minimumValue = 1
    }
}

return effect