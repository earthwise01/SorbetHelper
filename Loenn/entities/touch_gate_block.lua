local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local drawing = require("utils.drawing")
local entities = require("entities")

-- somewhat based on flag switch gates from maddiehelpinghand as is the case for most game block stuff :p
-- https://github.com/maddie480/MaddieHelpingHand/blob/master/Loenn/entities/flagSwitchGate.lua
-- (rendering is mostly mine though and based on exclamation blocks if anything)

local touchGateBlock = {}

touchGateBlock.name = "SorbetHelper/TouchGateBlock"
touchGateBlock.depth = 0
touchGateBlock.nodeLimits = {1, 1}
touchGateBlock.nodeLineRenderType = false
touchGateBlock.warnBelowSize = {16, 16}
touchGateBlock.placements = {
    {
        name = "normal",
        alternativeName = "normalaltname",
        data = {
            width = 16,
            height = 16,
            blockSprite = "SorbetHelper/gateblock/touch/block",
            iconSprite = "switchgate/icon",
            inactiveColor = "5fcde4",
            activeColor = "ffffff",
            finishColor = "f141df",
            smoke = true,
            moveSound = "event:/sorbethelper/sfx/gateblock_open",
            finishedSound = "event:/sorbethelper/sfx/gateblock_finish",
            shakeTime = 0.5,
            moveTime = 1.8,
            moveEased = true,
            persistent = false,
            allowReturn = false,
            moveOnGrab = true,
            moveOnStaticMoverInteract = true,
            linkTag = "" -- this is flag on activate in editor
        }
    }
}

touchGateBlock.fieldOrder = {
    "x", "y",
    "width", "height",
    "inactiveColor", "activeColor",
    "finishColor", "moveSound",
    "blockSprite", "finishedSound",
    "iconSprite", "shakeTime",
    "linkTag", "moveTime",
    "moveOnGrab", "moveOnStaticMoverInteract", "moveOnStaticMover", "moveEased", "allowReturn",
    "smoke", "drawOutline", "persistent"
}

touchGateBlock.fieldInformation = {
    inactiveColor = {
        fieldType = "color"
    },
    activeColor = {
        fieldType = "color"
    },
    finishColor = {
        fieldType = "color"
    },
    blockSprite = {
        options = {
            "SorbetHelper/gateblock/touch/block"
        },
        editable = true
    },
    iconSprite = {
        options = {
            "switchgate/icon",
            -- "SorbetHelper/gateblock/touch/icon"        
        },
        editable = true
    },
    drawOutline = {
        default = true
    },
}

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

local function addNinePatchSprite(spriteTable, x, y, width, height, texture, color)
    color = color or {1.0, 1.0, 1.0, 1.0}

    local ninePatch = drawableNinePatch.fromTexture(texture, ninePatchOptions, x, y, width, height)
    ninePatch.color = color
    local sprites = ninePatch:getDrawableSprite()

    for _,v in ipairs(sprites) do
        table.insert(spriteTable, v)
    end
end

local function createSprites(entity, x, y, width, height, color)
    local sprites = {}

    local blockPath = entity.blockSprite or "SorbetHelper/gateblock/touch/block"
    local iconPath = entity.iconSprite or "switchgate/icon"

    local blockTexture = "objects/" .. blockPath
    local iconTexture = "objects/" .. iconPath .. "00"
    local fillRectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, color)

    -- main block
    table.insert(sprites, fillRectangle:getDrawableSprite())
    addNinePatchSprite(sprites, x, y, width, height, blockTexture)

    local iconSprite = drawableSprite.fromTexture(iconTexture, {x = x, y = y})

    iconSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    table.insert(sprites, iconSprite)

    return sprites
end

function touchGateBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    color = utils.getColor(entity.inactiveColor or "ffffff")

    local sprites = createSprites(entity, x, y, width, height, color)

    return sprites
end

function touchGateBlock.nodeSprite(room, entity, node, nodeIndex, viewport)
    local x, y = entity.x or 0, entity.y or 0
    local nx, ny = node.x or 0, node.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local color = utils.getColor(entity.finishColor or "ffffff")
    
    local sprites = createSprites(entity, nx, ny, width, height, color)

    local lineColor = utils.getColor(entity.activeColor or "ffffff")
    lineColor[4] = 0.75
    table.insert(sprites, drawableLine.fromPoints({x + math.floor(width / 2), y + math.floor(height / 2), nx + math.floor(width / 2), ny + math.floor(height / 2)}, lineColor))

    return sprites
end

function touchGateBlock.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nx, ny = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 24, entity.height or 24

    return utils.rectangle(x, y, width, height), {utils.rectangle(nx, ny, width, height)}
end

return touchGateBlock
