local drawableSprite = require("structs.drawable_sprite")
local drawableText = require("structs.drawable_text")
local colors = require("consts.colors")
local loadedState = require("loaded_state")

local sorbetUtils = {}

function sorbetUtils.getGenericNodeSprite(x, y, color)
    local color = color or colors.selectionCompleteNodeLineColor

    local sprite = drawableSprite.fromTexture("editorSprites/SorbetHelper/nodeMarker", {x = x, y = y, color = color})
    sprite.rotation = math.pi / 4
    sprite:setScale(0.9428090415820632)

    return sprite
end

-- praying this isnt stupidly laggy
function sorbetUtils.checkForDuplicateInMap(self, isTrigger, duplicateCheck)
    isTrigger = isTrigger or false
    local map = loadedState.map
    if not map then return false end

    local name = self._name

    for _, room in pairs(map.rooms) do
        local list = isTrigger and room.triggers or room.entities

        for _, entity in pairs(list) do
            if entity ~= self and entity._name == name then
                if not duplicateCheck or duplicateCheck(self, entity) then
                    return true
                end
            end
        end
    end

    return false
end

function sorbetUtils.getControllerSprites(x, y, texture, global, warning)
    texture = texture and "editorSprites/SorbetHelper/" .. texture or "@Internal@/northern_lights"

    local sprite = drawableSprite.fromTexture(texture, {x = x, y = y})
    local sprites = { sprite }

    if global then
        table.insert(sprites, drawableText.fromText("Global", x - 16, y - 21, 32, 8, nil, 1))
    end

    if warning then
        table.insert(sprites, drawableText.fromText(warning, x - 48, y + 11, 96, 8, nil, 1, {1.0, 0.0, 0.0, 1.0}))
    end

    return sprites
end

function sorbetUtils.getControllerSpriteFunction(textureName, globalCheck, noDuplicates)
    globalCheck = globalCheck or function (room, entity) return entity.global or false end

    return function (room, entity)
        local warning = noDuplicates and sorbetUtils.checkForDuplicateInMap(entity) and "!Duplicate!" or nil
        local global = type(globalCheck) == "boolean" and globalCheck or globalCheck(room, entity)
        return sorbetUtils.getControllerSprites(entity.x or 0, entity.y or 0, textureName, global, warning)
    end
end

return sorbetUtils
