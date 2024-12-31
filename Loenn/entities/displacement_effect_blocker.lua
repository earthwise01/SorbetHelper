local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")

local displacementEffectBlocker = {}

displacementEffectBlocker.name = "SorbetHelper/DisplacementEffectBlocker"
displacementEffectBlocker.depth = -55000
displacementEffectBlocker.placements = {
    {
        name = "normal",
        data = {
            width = 8,
            height = 8,
            waterOnly = false,
            depthAdhering = false,
        }
    },
    {
        name = "waterOnly",
        data = {
            width = 8,
            height = 8,
            waterOnly = true,
            depthAdhering = false,
        }
    },
    {
        name = "depthAdhering",
        data = {
            width = 8,
            height = 8,
            waterOnly = false,
            depth = 0,
            depthAdhering = true,
        }
    }
}

function displacementEffectBlocker.ignoredFields(entity)
    local ignored = { "_id", "_name", "depthAdhering" }
    if entity.depthAdhering == false then
        table.insert(ignored, "depth")
    end

    return ignored
end

displacementEffectBlocker.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = depths.addDepths(depths.getDepths(), {
            {"Water & Waterfalls", -9999}, {"FG Waterfalls", -49900}
        }),
        editable = true
    }
}

displacementEffectBlocker.fillColor = {225 / 255, 245 / 255, 100 / 255, 0.25}
displacementEffectBlocker.borderColor = {240 / 255, 210 / 255, 170 / 255, 0.5}

return displacementEffectBlocker