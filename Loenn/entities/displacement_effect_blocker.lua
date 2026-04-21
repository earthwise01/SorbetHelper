local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local displacementEffectBlocker = {}

displacementEffectBlocker.name = "SorbetHelper/DisplacementEffectBlocker"
displacementEffectBlocker.fillColor = {225 / 255, 245 / 255, 100 / 255, 0.25}
displacementEffectBlocker.borderColor = {240 / 255, 210 / 255, 170 / 255, 0.5}
displacementEffectBlocker.depth = sorbetHelper.controllerDepth
displacementEffectBlocker.placements = {
    {
        name = "displacement_effect_blocker_full",
        data = {
            width = 8,
            height = 8,
            waterOnly = false,
            depthAdhering = false,
            flag = "",
        }
    },
    {
        name = "displacement_effect_blocker_water_only",
        data = {
            width = 8,
            height = 8,
            waterOnly = true,
            depthAdhering = false,
            flag = "",
        }
    },
    {
        name = "displacement_effect_blocker_depth_adhering",
        data = {
            width = 8,
            height = 8,
            waterOnly = false,
            depth = 0,
            depthAdhering = true,
            flag = "",
        }
    }
}

function displacementEffectBlocker.ignoredFields(entity)
    local ignored = {"_id", "_name", "depthAdhering"}
    if entity.depthAdhering == false then
        table.insert(ignored, "depth")
    end

    return ignored
end

displacementEffectBlocker.fieldOrder = {
    "x", "y",
    "width", "height",
    "flag", "waterOnly",
    "depth"
}

displacementEffectBlocker.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepths({
            {"Water & Waterfalls", -9999},
            {"FG Waterfalls", -49900}
        }),
        editable = true
    }
}

return displacementEffectBlocker
