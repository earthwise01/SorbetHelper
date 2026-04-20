local pufferTweaksController = {}

pufferTweaksController.name = "SorbetHelper/PufferTweaksController"
pufferTweaksController.texture = "editorSprites/SorbetHelper/pufferTweaksController"
pufferTweaksController.placements = {
    name = "puffer_tweaks_controller",
    data = {
        fixSquishExplode = true,
        snapToSpring = true,
        springXSpeedThreshold = 60,
        springYSpeedThreshold = 0,
        canBePushedWhileExploded = false,
        canRespawnWhenHomeBlocked = false,
        moreExplodeParticles = false
    }
}

pufferTweaksController.fieldOrder = {
    "x", "y",
    "springXSpeedThreshold", "springYSpeedThreshold",
    "canBePushedWhileExploded", "moreExplodeParticles", "snapToSpring", "fixSquishExplode",
    "canRespawnWhenHomeBlocked"
}

return pufferTweaksController
