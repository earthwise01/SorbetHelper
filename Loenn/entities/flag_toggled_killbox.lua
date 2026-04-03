local utils = require("utils")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableText = require("structs.drawable_text")

local flagToggledKillbox = {}

flagToggledKillbox.name = "SorbetHelper/FlagToggledKillbox"
flagToggledKillbox.depth = -1000005
flagToggledKillbox.canResize = {true, false}
flagToggledKillbox.placements = {
    {
        name = "flag_toggled_killbox",
        data = {
            width = 8,
            flag = "",
            inverted = false,
            flagOnly = false,
            playerAboveThreshold = 32,
            lenientHitbox = false,
            updateOnLoad = false
        }
    },
    {
        name = "accurate_killbox",
        data = {
            width = 8,
            flag = "",
            inverted = false,
            flagOnly = false,
            playerAboveThreshold = 32,
            lenientHitbox = true,
            updateOnLoad = true
        }
    }
}

flagToggledKillbox.fieldOrder = {
    "x", "y",
    "width", "flag",
    "playerAboveThreshold", "inverted", "flagOnly",
    "lenientHitbox", "updateOnLoad"
}

function flagToggledKillbox.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width or 8, 8)
end

function flagToggledKillbox.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, 32
    local flag = entity.flag or ""
    local inverted = entity.inverted or false

    local sprites = {}

    table.insert(sprites, drawableRectangle.fromRectangle("fill", x, y, width, 8, {0.8, 0.4, 0.4, 0.8}))
    table.insert(sprites, drawableRectangle.fromRectangle("fill", x, y, width, height, {0.8, 0.4, 0.4, 0.25}))

    if flag ~= "" then
        table.insert(sprites, drawableText.fromText("(" .. (inverted and "!" .. flag or flag) .. ")", x, y, width, height, nil, 1))
    end

    return sprites
end

return flagToggledKillbox
