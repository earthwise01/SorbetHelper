local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local displacementDepthFixer = {}

displacementDepthFixer.name = "SorbetHelper/DepthAdheringDisplacementWrapper"
displacementDepthFixer.placements = {
    {
        name = "entityOnly",
        alternativeName = "altName",
        data = {
            width = 8,
            height = 8,
            distortBehind = false,
            ignoreBounds = false,
            minDepth = "",
            maxDepth = "",
            affectedTypes = ""
        }
    },
    {
        name = "distortBehind",
        alternativeName = "altName",
        data = {
            width = 8,
            height = 8,
            distortBehind = true,
            ignoreBounds = false,
            minDepth = "",
            maxDepth = "",
            affectedTypes = ""
        }
    }
}

displacementDepthFixer.fieldInformation = function()
    return {
        ignoreBounds = {
            default = false
        },
        affectedTypes = {
            fieldType = "list",
            elementSeparator = ",",
            elementDefault = "",
            elementOptions = {
                 options = sorbetUtils.getAllSIDs(),
                 searchable = true,
            },

            default = ""
        },
        minDepth = {
            fieldType = "integer",
            allowEmpty = true
        },
        maxDepth = {
            fieldType = "integer",
            allowEmpty = true
        }
    }
end

displacementDepthFixer.fieldOrder = {
    "x", "y", "width", "height",
    "maxDepth", "affectedTypes",
    "minDepth", "ignoreBounds", "distortBehind"
}

displacementDepthFixer.depth = -1000010
displacementDepthFixer.fillColor = {100 / 255, 225 / 255, 245 / 255, 0.25}
displacementDepthFixer.borderColor = {183 / 255, 250 / 255, 221 / 255, 0.5}

return displacementDepthFixer
