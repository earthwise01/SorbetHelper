local effect = {}

effect.name = "SorbetHelper/SpiralStars"

effect.defaultData = {
    colors = "ffffff",
    backgroundColor = "000000ff",
    speed = 70,
    rotationSpeed = -40,
    trailLength = 8,
    trailDelay = 0.0167,
    starCount = 100,
    spritePath = "bgs/02/stars",
    centerX = 160,
    centerY = 90,
    centerRadius = 70,
    spawnRadius = 190,
}

effect.fieldOrder = {
    "only", "exclude", "tag", "flag", 
    "spritePath", "colors", "backgroundColor", "notflag",
    "starCount", "centerX", "spawnRadius", "speed",
    "trailLength", "centerY", "centerRadius", "rotationSpeed",
    "trailDelay"
}

effect.fieldInformation = {
    colors = {
        fieldType = "list",
        elementOptions = {
            fieldType = "color",
            allowXNAColors = false
        }
    },
    backgroundColor = {
        fieldType = "color",
        allowXNAColors = false,
        showAlpha = true
    },
    trailLength = {
        fieldType = "integer",
        minimumValue = 0
    },
    starCount = {
        fieldType = "integer",
        minimumValue = 0
    },
    centerRadius = {
        minimumValue = 0
    },
    spawnRadius = {
        minimumValue = 0
    },
}

return effect