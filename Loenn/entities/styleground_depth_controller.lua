local drawableText = require("structs.drawable_text")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local stylegroundDepthController = {}

local depthOptions = sorbetHelper.getDepths()
table.insert(depthOptions, {"————————", ""})
table.insert(depthOptions, {"Above Colorgrade", "AboveColorgrade"})
table.insert(depthOptions, {"Above HUD", "AboveHud"})
table.insert(depthOptions, {"Above Pause HUD", "AbovePauseHud"})

local depthOptionsEnum = {
    ["AboveColorgrade"] = true,
    ["AboveHud"] = true,
    ["AbovePauseHud"] = true
}

stylegroundDepthController.name = "SorbetHelper/StylegroundDepthController"
stylegroundDepthController.depth = sorbetHelper.controllerDepth
stylegroundDepthController.placements = {
    {
        name = "styleground_depth_controller",
        data = {
            depth = 0,
            tag = ""
        }
    },
    {
        name = "styleground_depth_controller_above_hud",
        data = {
            depth = "AboveHud",
            tag = ""
        }
    }
}

stylegroundDepthController.fieldOrder = {
    "x", "y",
    "tag", "depth"
}

stylegroundDepthController.fieldInformation = {
    depth = {
        fieldType = "sorbet_helper.integer_or_enum",
        options = depthOptions,
        enum = {
            ["AboveColorgrade"] = true,
            ["AboveHud"] = true,
            ["AbovePauseHud"] = true
        },
        editable = true
    }
}

function stylegroundDepthController.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local depth = entity.depth or 0

    local texture = type(tonumber(depth)) == "number" and "stylegroundEntityController" or "stylegroundOverHudController"
    local sprites = sorbetHelper.getControllerSprites(x, y, texture, true)

    if entity.tag ~= "" and sorbetHelper.checkForDuplicateInMap(entity, false, function(entity1, entity2) return entity1.tag == entity2.tag end) then
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
