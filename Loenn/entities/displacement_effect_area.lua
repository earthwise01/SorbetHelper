local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local displacementEffectArea = {}

displacementEffectArea.name = "SorbetHelper/DisplacementEffectArea"
displacementEffectArea.fillColor = {240 / 255, 100 / 255, 180 / 255, 0.25}
displacementEffectArea.borderColor = {255 / 255, 189 / 255, 193 / 255, 0.5}
displacementEffectArea.depth = sorbetHelper.controllerDepth
displacementEffectArea.placements = {
    {
        name = "displacement_effect_area",
        data = {
            width = 8,
            height = 8,
            horizontalDisplacement = 0.0,
            verticalDisplacement = 0.0,
            waterDisplacement = 0.25,
            alpha = 1.0,
            flag = "",
            depthAdhering = false
        }
    },
    {
        name = "displacement_effect_area_depth_adhering",
        data = {
            width = 8,
            height = 8,
            horizontalDisplacement = 0.0,
            verticalDisplacement = 0.0,
            waterDisplacement = 0.25,
            alpha = 1.0,
            flag = "",
            depthAdhering = true,
            depth = 0
        }
    }
}

function displacementEffectArea.ignoredFields(entity)
    local ignored = {"_id", "_name", "depthAdhering"}
    if entity.depthAdhering == false then
        table.insert(ignored, "depth")
    end

    return ignored
end

displacementEffectArea.fieldOrder = {
    "x", "y",
    "width", "height",
    "horizontalDisplacement", "verticalDisplacement",
    "waterDisplacement", "alpha",
    "depth"
}

displacementEffectArea.fieldInformation = {
    horizontalDisplacement = {
        minimumValue = -1.0,
        maximumValue = 1.0
    },
    verticalDisplacement = {
        minimumValue = -1.0,
        maximumValue = 1.0
    },
    waterDisplacement = {
        minimumValue = 0.0,
        maximumValue = 1.0
    },
    alpha = {
        minimumValue = 0.0,
        maximumValue = 1.0
    },
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepths({
            {"Water & Waterfalls", -9999},
            {"FG Waterfalls", -49900}
        }),
        editable = true
    }
}

return displacementEffectArea
