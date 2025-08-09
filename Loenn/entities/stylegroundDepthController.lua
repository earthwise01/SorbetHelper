local drawableText = require("structs.drawable_text")
local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local stylegroundDepthController = {}

stylegroundDepthController.name = "SorbetHelper/StylegroundDepthController"
stylegroundDepthController.depth = -1000010
stylegroundDepthController.placements = {
    {
        name = "depth",
        data = {
            depth = 0,
            tag = ""
        }
    },
    {
        name = "aboveHud",
        data = {
            depth = "AboveHud",
            tag = ""
        }
    }
}

local depths = depths.getDepths()
table.insert(depths, {"————————", ""})
table.insert(depths, {"Above Colorgrade", "AboveColorgrade"})
table.insert(depths, {"Above HUD", "AboveHud"})
table.insert(depths, {"Above Pause HUD", "AbovePauseHud"})

stylegroundDepthController.fieldInformation = {
    depth = {
        fieldType = "sorbetHelper.integerAndEnum",
        options = depths,
        enum = {
            AboveColorgrade = true,
            AboveHud = true,
            AbovePauseHud = true
        },
        editable = true
    }
}

stylegroundDepthController.fieldOrder = {
    "x", "y",
    "tag", "depth",
}

function stylegroundDepthController.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local depth = entity.depth or 0

    local texture = type(tonumber(depth)) == "number" and "stylegroundEntityController" or "stylegroundOverHudController"
    local sprites = sorbetUtils.getControllerSprites(x, y, texture, true)

    if entity.tag ~= "" and sorbetUtils.checkForDuplicateInMap(entity, false, function(entity1, entity2) return entity1.tag == entity2.tag end) then
        local text = "!Duplicate Tag!\n" .. entity.tag

        -- guess lönn doesn't support changing how drawableText gets justified? so umm    calculate the offset and remove it i guess
        -- https://github.com/CelestialCartographers/Loenn/blob/master/src/utils/drawing.lua#L78
        local font = love.graphics.getFont()
        local fontHeight = font:getHeight()
        local fontLineHeight = font:getLineHeight()
        local longest, lines = font:getWrap(text, 96)
        local textHeight = (#lines - 1) * (fontHeight * fontLineHeight) + fontHeight
        local offsetY = math.floor((16 - textHeight) / 2)

        table.insert(sprites, drawableText.fromText(text, x - 48, y - offsetY + 12, 96, 16, nil, 1, {1.0, 0.0, 0.0, 1.0}))
    end

    return sprites
end

return stylegroundDepthController