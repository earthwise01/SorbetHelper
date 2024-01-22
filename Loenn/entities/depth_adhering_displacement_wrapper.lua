local depthAdheringDisplacementWrapper = {}

depthAdheringDisplacementWrapper.name = "SorbetHelper/DepthAdheringDisplacementWrapper"
depthAdheringDisplacementWrapper.depth = -15000
depthAdheringDisplacementWrapper.placements = {
    {
        name = "normal",
        data = {
            width = 8,
            height = 8,
            distortBehind = false,
            -- onlyCollideTopLeft = false
        }
    },
    {
        name = "distortBehind",
        data = {
            width = 8,
            height = 8,
            distortBehind = true,
            -- onlyCollideTopLeft = false
        }
    }
}

depthAdheringDisplacementWrapper.canResize = {true, true}

depthAdheringDisplacementWrapper.fillColor = {100 / 255, 225 / 255, 245 / 255, 0.3}
depthAdheringDisplacementWrapper.borderColor = {170 / 255, 240 / 255, 210 / 255, 0.6}

return depthAdheringDisplacementWrapper