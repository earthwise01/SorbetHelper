local depthAdheringDisplacementWrapper = {}

depthAdheringDisplacementWrapper.name = "SorbetHelper/DepthAdheringDisplacementWrapper"
depthAdheringDisplacementWrapper.depth = -55000
depthAdheringDisplacementWrapper.placements = {
    {
        name = "entityOnly",
        alternativeName = "entityOnlyAlt",
        data = {
            width = 8,
            height = 8,
            distortBehind = false,
            -- onlyCollideTopLeft = false
        }
    },
    {
        name = "distortBehind",
        alternativeName = "distortBehindAlt",
        data = {
            width = 8,
            height = 8,
            distortBehind = true,
            -- onlyCollideTopLeft = false
        }
    }
}

depthAdheringDisplacementWrapper.canResize = {true, true}

depthAdheringDisplacementWrapper.fillColor = {100 / 255, 225 / 255, 245 / 255, 0.25}
depthAdheringDisplacementWrapper.borderColor = {183 / 255, 250 / 255, 221 / 255, 0.5}

return depthAdheringDisplacementWrapper