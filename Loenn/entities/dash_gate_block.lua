local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")
local drawing = require("utils.drawing")
local entities = require("entities")

-- somewhat based on flag switch gates from maddiehelpinghand as is the case for most game block stuff :p
-- https://github.com/maddie480/MaddieHelpingHand/blob/master/Loenn/entities/flagSwitchGate.lua
-- (rendering is mostly mine though and based on exclamation blocks if anything)

local dashGateBlock = {}

dashGateBlock.name = "SorbetHelper/DashGateBlock"
dashGateBlock.depth = 0
dashGateBlock.nodeLimits = {1, 1}
dashGateBlock.nodeLineRenderType = false
dashGateBlock.warnBelowSize = {16, 16}
dashGateBlock.placements = {
    {
        name = "normal",
        alternativeName = "normalaltname",
        data = {
            width = 16,
            height = 16,
            blockSprite = "SorbetHelper/gateblock/dash/block",
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
            axes = "Both",
            allowWavedash = false,
            dashCornerCorrection = false,
            linkTag = "" -- this is flag on activate in editor
        }
    }
}

dashGateBlock.fieldOrder = {"x", "y", "width", "height", "inactiveColor", "activeColor", "finishColor", "moveSound", "blockSprite", "finishedSound", "iconSprite", "shakeTime", "axes", "moveTime", "linkTag", "moveEased", "persistent",  "smoke", "allowWavedash", "dashCornerCorrection"}

dashGateBlock.fieldInformation = {
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
            "SorbetHelper/gateblock/dash/block"
        },
        editable = true
    },
    iconSprite = {
        options = {
            "switchgate/icon",
            -- "SorbetHelper/gateblock/dash/icon"        
        },
        editable = true
    },
    axes = {
        options = {
            "Both",
            "Horizontal",
            "Vertical",
        },
        editable = false
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

    local blockPath = entity.blockSprite or "SorbetHelper/gateblock/dash/block"
    local iconPath = entity.iconSprite or "switchgate/icon"

    local axes = entity.axes or "Both"
    if axes == "Horizontal" then
        blockPath ..= "_h"
    elseif axes == "Vertical" then
        blockPath ..= "_v"
    end

    local blockTexture = "objects/" .. blockPath
    local iconTexture = "objects/" .. iconPath .. "00"
    
    -- main block
    addNinePatchSprite(sprites, x, y, width, height, blockTexture)
    -- lights
    addNinePatchSprite(sprites, x, y, width, height, blockTexture .. "_lights", color)

    local iconSprite = drawableSprite.fromTexture(iconTexture, {x = x, y = y})

    iconSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    table.insert(sprites, iconSprite)

    return sprites
end

function dashGateBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    color = utils.getColor(entity.inactiveColor or "ffffff")

    local sprites = createSprites(entity, x, y, width, height, color)

    return sprites
end

function dashGateBlock.nodeSprite(room, entity, node, nodeIndex, viewport)
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

function dashGateBlock.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nx, ny = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 24, entity.height or 24

    return utils.rectangle(x, y, width, height), {utils.rectangle(nx, ny, width, height)}
end

return dashGateBlock
