local displacementEffectBlocker = {}

displacementEffectBlocker.name = "SorbetHelper/DisplacementEffectBlocker"
displacementEffectBlocker.depth = -15000
displacementEffectBlocker.placements = {
    {
        name = "normal",
        data = {
            width = 8,
            height = 8,
            depthAdhering = false,
        }
    },
    {
        name = "depthAdhering",
        data = {
            width = 8,
            height = 8,
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
        fieldType = "integer"
    }
}

displacementEffectBlocker.fillColor = {225 / 255, 245 / 255, 100 / 255, 0.3}
displacementEffectBlocker.borderColor = {240 / 255, 210 / 255, 170 / 255, 0.6}

return displacementEffectBlocker