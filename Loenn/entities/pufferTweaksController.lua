local controller = {}

controller.name = "SorbetHelper/PufferTweaksController"
controller.texture = "editorSprites/SorbetHelper/pufferTweaksController"
controller.placements = {
    name = "controller",
    data = {
        fixSquishExplode = true,
        snapToSpring = true,
        springXSpeedThreshold = 60,
        springYSpeedThreshold = 0,
        canBePushedWhileExploded = false,
        canRespawnWhenHomeBlocked = false,
        moreExplodeParticles = false,
    }
}

controller.fieldOrder = {
    "x", "y",
    "springXSpeedThreshold", "springYSpeedThreshold",
    "canBePushedWhileExploded", "moreExplodeParticles", "snapToSpring", "fixSquishExplode",
    "canRespawnWhenHomeBlocked"
}

return controller