local displacementArea = {}

displacementArea.name = "SorbetHelper/DisplacementEffectArea"
displacementArea.depth = -55000
displacementArea.placements = {
    {
        name = "normal",
        data = {
            width = 8,
            height = 8,
            horizontalDisplacement = 0.0,
            verticalDisplacement = 0.0,
            waterDisplacement = 0.25,
            alpha = 1.0,
            depthAdhering = false,
        }
    },
    {
        name = "depthAdhering",
        data = {
            width = 8,
            height = 8,
            depth = 0,
            horizontalDisplacement = 0.0,
            verticalDisplacement = 0.0,
            waterDisplacement = 0.25,
            alpha = 1.0,
            depthAdhering = true,
        }
    }
}

function displacementArea.ignoredFields(entity)
    local ignored = { "_id", "_name", "depthAdhering" }
    if entity.depthAdhering == false then
        table.insert(ignored, "depth")
    end

    return ignored
end

displacementArea.fieldInformation = {
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
        options = depths.addDepths(depths.getDepths(), {
            {"Water & Waterfalls", -9999}, {"FG Waterfalls", -49900}
        }),
        editable = true
    },
}

displacementArea.fieldOrder = {
    "x", "y",
    "width", "height",
    "horizontalDisplacement", "verticalDisplacement",
    "waterDisplacement", "alpha",
    "depth"
}

displacementArea.fillColor = {240 / 255, 100 / 255, 180 / 255, 0.25}
displacementArea.borderColor = {255 / 255, 189 / 255, 193 / 255, 0.5}

return displacementArea