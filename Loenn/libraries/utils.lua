local drawableSprite = require("structs.drawable_sprite")
local colors = require("consts.colors")

local sorbetUtils = {}

function sorbetUtils.getGenericNodeSprite(x, y, color)
    local color = color or colors.selectionCompleteNodeLineColor

    local sprite = drawableSprite.fromTexture("editorSprites/SorbetHelper/nodeMarker", {x = x, y = y, color = color})
    sprite.rotation = math.pi / 4

    return sprite
end

return sorbetUtils