local drawableText = require("structs.drawable_text")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local controller = {}

controller.name = "SorbetHelper/SparklingWaterColorController"
controller.depth = -1000010
controller.placements = {
    name = "normal",
    data = {
        outlineColor = "9ce4f7de",
        edgeColor = "8dceeb79",
        fillColor = "4289bd97",
        detailTexture = "objects/SorbetHelper/sparklingWater/detail",
        causticScale = 0.8,
        causticAlpha = 0.15,
        bubbleAlpha = 0.3,
        displacementSpeed = 0.25,
        -- affectedDepth = nil, -- appears automatically since allowEmpty is set to true in the field information
        global = false
    }
}

controller.fieldOrder = {
    "x", "y",
    "outlineColor", "edgeColor",
    "detailTexture", "fillColor",
    "causticAlpha", "bubbleAlpha",
    "causticScale", "displacementSpeed",
    "affectedDepth", "global"
}

controller.fieldInformation = {
    outlineColor = {
        fieldType = "color",
        showAlpha = true
    },
    edgeColor = {
        fieldType = "color",
        showAlpha = true
    },
    fillColor = {
        fieldType = "color",
        showAlpha = true
    },
    causticAlpha = {
        minimumValue = 0,
        maximumValue = 1
    },
    bubbleAlpha = {
        minimumValue = 0,
        maximumValue = 1
    },
    displacementSpeed = {
        minimumValue = 0,
        maximumValue = 1
    },
    affectedDepth = {
        fieldType = "integer",
        allowEmpty = true
    }
}

function controller.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0

    local sprites = sorbetUtils.getControllerSprites(x, y, "sparklingWaterColorController", entity.global or false)

    if entity.affectedDepth then
        -- little silly but i think its cool mayb
        table.insert(sprites, drawableText.fromText(entity.affectedDepth, x - 8.5, y + 2.5, 16, 8, nil, 0.5))
    end

    return sprites
end

return controller
