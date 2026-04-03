local drawableSprite = require("structs.drawable_sprite")
local drawableText = require("structs.drawable_text")
local colors = require("consts.colors")
local loadedState = require("loaded_state")
local entities = require("entities")

local sorbetUtils = {}

--- util functions

-- adapted from https://forums.solar2d.com/t/split-utf-8-string-word-with-foreign-characters-to-letters/320463/2
function sorbetUtils.UTF8ToCharArray(str)
    local charArray = {}
    local iStart = 0
    local strLen = str:len()

    local function bit(b)
        return 2 ^ (b - 1)
    end
    local function hasBit(w, b)
        return w % (b + b) >= b
    end
    local function checkMultiByte(i)
        if iStart ~= 0 then
            charArray[#charArray + 1] = str:sub(iStart, i - 1)
            iStart = 0
        end
    end

    for i = 1, strLen do
        local b = str:byte(i)
        local multiStart = hasBit(b, bit(7)) and hasBit(b, bit(8))
        local multiTrail = not hasBit(b, bit(7)) and hasBit(b, bit(8))

        if multiStart then
            checkMultiByte(i)
            iStart = i

        elseif not multiTrail then
            checkMultiByte(i)
            charArray[#charArray + 1] = str:sub(i, i)
        end
    end

    -- process if last character is multi-byte
    checkMultiByte(strLen + 1)

    return charArray
end

--- field information options

function sorbetUtils.getAllSIDs()
    local sids = {}
    for k, v in pairs(entities.registeredEntities) do
        table.insert(sids, k)
    end
    table.sort(sids)

    return sids
end

function sorbetUtils.getMapSIDs()
    if not loadedState.map then return sorbetUtils.getAllSIDs() end

    local sidsInMap = {}
    for _, room in pairs(loadedState.map.rooms) do
        for _, entity in pairs(room.entities) do
            sidsInMap[entity._name] = true
        end
    end

    local sids = {}
    for k, v in pairs(sidsInMap) do
        table.insert(sids, k)
    end
    table.sort(sids)

    return sids
end

function sorbetUtils.getDepths(extraOptions)
    local depthOptions = {
        {"BG Terrain (10000)", 10000},
        {"BG Mirrors (9500)", 9500},
        {"BG Decals (9000)", 9000},
        {"BG Particles (8000)", 8000},
        {"Solids Below (5000)", 5000},
        {"Below (2000)", 2000},
        {"NPCs (1000)", 1000},
        {"Theo Crystal (100)", 100},
        {"Player (0)", 0},
        {"Dust (-50)", -50},
        {"Pickups (-100)", -100},
        {"Seeker (-200)", -200},
        {"Particles (-8000)", -8000},
        {"Above (-8500)", -8500},
        {"Solids (-9000)", -9000},
        {"FG Terrain (-10000)", -10000},
        {"FG Decals (-10500)", -10500},
        {"Dream Blocks (-11000)", -11000},
        {"Crystal Spinners (-11500)", -11500},
        {"Player Dream Dashing (-12000)", -12000},
        {"Enemy (-12500)", -12500},
        {"Fake Walls (-13000)", -13000},
        {"FG Particles (-50000)", -50000},
        {"Top (-1000000)", -1000000},
        {"Formation Sequences (-2000000)", -2000000}
    }

    if extraOptions then
        for _, option in ipairs(extraOptions) do
            local depth = option[2]
            local name = option[1] .. " (" .. depth .. ")"

            for i, origOption in ipairs(depthOptions) do
                local origDepth = origOption[2]

                -- rename if depth already exists
                if origDepth == depth then
                    depthOptions[i][1] = name
                    break
                end

                -- otherwise insert before the first lower depth in the list
                if depth > origDepth then
                    table.insert(depthOptions, i, {name, depth})
                    break
                end

                -- if there are no lower depths, insert it at the end of the list
                if i == #depthOptions then
                    table.insert(depthOptions, {name, depth})
                    break
                end
            end
        end
    end

    return depthOptions
end

sorbetUtils.simpleEasings = {
    "Linear",
    "SineIn", "SineOut", "SineInOut",
    "QuadIn", "QuadOut", "QuadInOut",
    "CubeIn", "CubeOut", "CubeInOut",
    "QuintIn", "QuintOut", "QuintInOut",
    "ExpoIn", "ExpoOut", "ExpoInOut"
}

sorbetUtils.allEasings = {
    "Linear",
    "SineIn", "SineOut", "SineInOut",
    "QuadIn", "QuadOut", "QuadInOut",
    "CubeIn", "CubeOut", "CubeInOut",
    "QuintIn", "QuintOut", "QuintInOut",
    "ExpoIn", "ExpoOut", "ExpoInOut",
    "BackIn", "BackOut", "BackInOut",
    "BigBackIn", "BigBackOut", "BigBackInOut",
    "ElasticIn", "ElasticOut", "ElasticInOut",
    "BounceIn", "BounceOut", "BounceInOut"
}

--- sprite utils

sorbetUtils.controllerDepth = -1000010

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

--- sprite functions

function sorbetUtils.getGenericNodeSprite(x, y, color)
    local color = color or colors.selectionCompleteNodeLineColor

    local sprite = drawableSprite.fromTexture("editorSprites/SorbetHelper/nodeMarker", {x = x, y = y, color = color})
    sprite.rotation = math.pi / 4
    sprite:setScale(0.9428090415820632)

    return sprite
end

function sorbetUtils.getControllerSprites(x, y, texture, global, warning)
    texture = texture and "editorSprites/SorbetHelper/" .. texture or "@Internal@/northern_lights"

    local sprite = drawableSprite.fromTexture(texture, {x = x, y = y})
    local sprites = {sprite}

    if global then
        table.insert(sprites, drawableText.fromText("Global", x - 16, y - 21, 32, 8, nil, 1))
    end

    if warning then
        table.insert(sprites, drawableText.fromText(warning, x - 48, y + 11, 96, 8, nil, 1, {1.0, 0.0, 0.0, 1.0}))
    end

    return sprites
end

function sorbetUtils.getControllerSpriteFunction(textureName, globalCheck, noDuplicates)
    globalCheck = globalCheck or function(room, entity) return entity.global or false end

    return function(room, entity)
        local warning = noDuplicates and sorbetUtils.checkForDuplicateInMap(entity) and "!Duplicate!" or nil
        local global = type(globalCheck) == "boolean" and globalCheck or globalCheck(room, entity)
        return sorbetUtils.getControllerSprites(entity.x or 0, entity.y or 0, textureName, global, warning)
    end
end

---

return sorbetUtils
