local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local displacementDepthFixer = {}

displacementDepthFixer.name = "SorbetHelper/DepthAdheringDisplacementWrapper"
displacementDepthFixer.fillColor = {100 / 255, 225 / 255, 245 / 255, 0.25}
displacementDepthFixer.borderColor = {183 / 255, 250 / 255, 221 / 255, 0.5}
displacementDepthFixer.depth = sorbetHelper.controllerDepth
displacementDepthFixer.placements = {
    {
        name = "displacement_depth_fixer",
        alternativeName = "depth_adhering_displacment_wrapper",
        data = {
            width = 8,
            height = 8,
            distortBehind = true,
            ignoreBounds = false,
            minDepth = "",
            maxDepth = "",
            affectedTypes = ""
        }
    },
    {
        name = "displacement_depth_fixer_entity_only",
        alternativeName = "depth_adhering_displacment_wrapper",
        data = {
            width = 8,
            height = 8,
            distortBehind = false,
            ignoreBounds = false,
            minDepth = "",
            maxDepth = "",
            affectedTypes = ""
        }
    }
}

displacementDepthFixer.fieldOrder = {
    "x", "y",
    "width", "height",
    "maxDepth", "affectedTypes",
    "minDepth", "ignoreBounds", "distortBehind"
}

displacementDepthFixer.fieldInformation = {
    ignoreBounds = {
        default = false
    },
    affectedTypes = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return sorbetHelper.getMapSIDs() end,
             searchable = true
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

return displacementDepthFixer
