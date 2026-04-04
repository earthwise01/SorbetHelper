local celesteRender = require("celeste_render")
local matrix = require("utils.matrix")
local utils = require("utils")
local autotiler = require("autotiler")
local atlases = require("atlases")
local bit = require("bit")
local depths = require("consts.object_depths")
local logging = require("logging")
local mods = require("mods")
local tileDepthHelper = mods.requireFromPlugin("libraries.depth_splitter_preview.tile_depth_helper")

local tilesFgDepth = depths.fgTerrain
local tilesBgDepth = depths.bgTerrain

local brushHelperHooks = {}

function brushHelperHooks.load()
    local brushHelper = require("brushes")

    -- taken from https://github.com/CelestialCartographers/Loenn/blob/v1.0.8/src/brushes.lua#L49
    -- (needed for the brushHelper.updateRender hook below)
    local function addNeighborIfMissing(x, y, needsUpdate, addedUpdate)
        if not addedUpdate or not addedUpdate:get(x, y) then
            table.insert(needsUpdate, x)
            table.insert(needsUpdate, y)

            if addedUpdate then
                addedUpdate:set(x, y, true)
            end
        end
    end

    -- taken from https://github.com/CelestialCartographers/Loenn/blob/v1.0.8/src/brushes.lua#L61
    -- (needed for the brushHelper.updateRender hook below)
    -- Inlined for 3x3 tilesets
    local function addMissingNeighbors(x, y, needsUpdate, addedUpdate)
        -- Around the target tile
        addNeighborIfMissing(x - 1, y - 1, needsUpdate, addedUpdate)
        addNeighborIfMissing(x, y - 1, needsUpdate, addedUpdate)
        addNeighborIfMissing(x + 1, y - 1, needsUpdate, addedUpdate)

        addNeighborIfMissing(x - 1, y, needsUpdate, addedUpdate)
        addNeighborIfMissing(x + 1, y, needsUpdate, addedUpdate)

        addNeighborIfMissing(x - 1, y + 1, needsUpdate, addedUpdate)
        addNeighborIfMissing(x, y + 1, needsUpdate, addedUpdate)
        addNeighborIfMissing(x + 1, y + 1, needsUpdate, addedUpdate)

        -- Tiles used to check for center/padding
        addNeighborIfMissing(x + 2, y, needsUpdate, addedUpdate)
        addNeighborIfMissing(x - 2, y, needsUpdate, addedUpdate)

        addNeighborIfMissing(x, y + 2, needsUpdate, addedUpdate)
        addNeighborIfMissing(x, y - 2, needsUpdate, addedUpdate)
    end

    -- taken from https://github.com/CelestialCartographers/Loenn/blob/v1.0.8/src/brushes.lua#L82
    -- (needed for the brushHelper.updateRender hook below)
    local function addMissingNeighborsCustomSize(x, y, tileMeta, needsUpdate, addedUpdate)
        local scanWidth, scanHeight = tileMeta.scanWidth, tileMeta.scanHeight
        local offsetX, offsetY = math.floor(scanWidth / 2), math.floor(scanHeight / 2)

        -- Around the target tile
        for tx = x - offsetX, x + offsetX do
            for ty = y - offsetY, y + offsetY do
                addNeighborIfMissing(tx, ty, needsUpdate, addedUpdate)
            end
        end

        -- Tiles used to check for center/padding
        addNeighborIfMissing(x + offsetX + 1, y, needsUpdate, addedUpdate)
        addNeighborIfMissing(x - offsetX - 1, y, needsUpdate, addedUpdate)

        addNeighborIfMissing(x, y + offsetY + 1, needsUpdate, addedUpdate)
        addNeighborIfMissing(x, y - offsetY - 1, needsUpdate, addedUpdate)
    end

    local _orig_updateRender = brushHelper.updateRender
    -- modified from https://github.com/CelestialCartographers/Loenn/blob/v1.0.8/src/brushes.lua#L105
    -- to match the changes made to celesteRender.getTilesBatch (since this function copies code from/has very similar code to it),
    -- and to keep compatibility with older loenn versions
    -- todo: check if this works on later versions/port any new changes over
    function brushHelper.updateRender(room, x, y, material, layer, randomMatrix)
        if not tileDepthHelper.shouldEnableMultipleTileDepths() then
            return _orig_updateRender(room, x, y, material, layer, randomMatrix)
        end

        local fg = layer == "tilesFg"

        local tiles = room[layer]
        local tilesMatrix = tiles.matrix

        -- Getting upvalues
        local gameplayAtlas = atlases.gameplay
        local cache = celesteRender.tilesSpriteMetaCache
        local autotiler = autotiler
        local meta = fg and celesteRender.tilesMetaFg or celesteRender.tilesMetaBg
        local scenery = fg and room.sceneryFg or room.sceneryBg
        local checkTile = autotiler.checkTile
        local lshift = bit.lshift
        local bxor = bit.bxor
        local band = bit.band

        local airTile = "0"
        local emptyTile = " "
        local wildcard = "*"

        local defaultQuad = {{0, 0}}
        local defaultSprite = ""

        local materialType = utils.typeof(material)

        -- No need to create matrix for single tile brushing
        local trackAddedUpdates = materialType == "matrix"
        local addedUpdate

        if trackAddedUpdates then
            local width, height = tilesMatrix:size()

            addedUpdate = matrix.filled(nil, width, height)
        end

        local needsUpdate = {}

        local random = randomMatrix or celesteRender.getRoomRandomMatrix(room, layer)
        local roomCache = celesteRender.getRoomCache(room.name, layer)
        local batches = roomCache and roomCache.result -- edited
        local batchWrapper = tileDepthHelper.createBatchWrapper(batches, width, height) -- added

        local sceneryMatrix = scenery and scenery.matrix
        local sceneryMeta = celesteRender.getSceneryMeta()

        if not batches then -- edited
            return false
        end

        if materialType == "matrix" then
            local materialWidth, materialHeight = material:size()
            local tilesWidth, tilesHeight = tilesMatrix:size()

            for i = 1, materialWidth do
                for j = 1, materialHeight do
                    local tx, ty = x + i - 1, y + j - 1

                    if tx >= 1 and ty >= 1 and tx <= tilesWidth and ty <= tilesHeight then
                        local target = tilesMatrix:get(tx, ty, " ")
                        local mat = material:getInbounds(i, j)

                        if mat ~= target and mat ~= " " then
                            tilesMatrix:set(tx, ty, mat)

                            -- Add the current tile and nearby tiles for redraw
                            -- Use inlined for 3x3 tilesets
                            if not meta[mat] or not meta[mat].customScanSize then
                                addNeighborIfMissing(tx, ty, needsUpdate, addedUpdate)
                                addMissingNeighbors(tx, ty, needsUpdate, addedUpdate)

                            else
                                addMissingNeighborsCustomSize(tx, ty, meta[mat], needsUpdate, addedUpdate)
                            end
                        end
                    end
                end
            end

        else
            local target = tilesMatrix:get(x, y, "0")

            if target ~= material and material ~= " " then
                tilesMatrix:set(x, y, material)

                -- Add the current tile and nearby tiles for redraw
                -- Use inlined for 3x3 tilesets
                if not meta[material] or not meta[material].customScanSize then
                    addNeighborIfMissing(x, y, needsUpdate, addedUpdate)
                    addMissingNeighbors(x, y, needsUpdate, addedUpdate)

                else
                    addMissingNeighborsCustomSize(x, y, meta[material], needsUpdate, addedUpdate)
                end
            end
        end

        local updateIndex = 1
        local missingTiles = {}

        while updateIndex < #needsUpdate do
            local x, y = needsUpdate[updateIndex], needsUpdate[updateIndex + 1]

            if tilesMatrix:inbounds(x, y) then
                local tile = tilesMatrix:getInbounds(x, y)
                local sceneryTile = sceneryMatrix:getInbounds(x, y, -1) or -1

                if sceneryTile > -1 then
                    local quad = celesteRender.getOrCacheScenerySpriteQuad(sceneryTile)

                    if quad then
                        batchWrapper:setSafe(fg and tilesFgDepth or tilesBgDepth, x, y, sceneryMeta, quad, x * 8 - 8, y * 8 - 8) -- edited
                    end

                elseif tile == airTile then
                    batchWrapper:removeSafe(x, y, sceneryMeta) -- edited

                else
                    -- TODO - Update overlay sprites
                    local tileMeta = meta[tile]
                    local texture = tileMeta and tileMeta.path

                    if texture then
                        local quads, sprites = tileDepthHelper.autotiler_getQuads_compat(x, y, tilesMatrix, meta, tileMeta, airTile, emptyTile, wildcard, defaultQuad, defaultSprite, checkTile, lshift, bxor, band) -- edited
                        local quadCount = #quads

                        if quadCount > 0 then
                            local rng = random:getInbounds(x, y)
                            local randQuad = quads[tileDepthHelper.getRandQuadIndex_compat(rng, quadCount)] -- edited

                            local spriteMeta = gameplayAtlas[texture]

                            if spriteMeta then
                                local quad = celesteRender.getOrCacheTileSpriteQuad(cache, tile, texture, randQuad, fg)
                                local tileDepth = tileDepthHelper.getTiletypeDepth(tile, fg) -- added

                                batchWrapper:setSafe(tileDepth, x, y, spriteMeta, quad, x * 8 - 8, y * 8 - 8) -- edited
                            end
                        end

                    else
                        table.insert(missingTiles, {x, y})
                    end
                end
            end

            updateIndex += 2
        end

        celesteRender.drawInvalidTiles(batchWrapper:getBatch(fg and tilesFgDepth or tilesBgDepth), missingTiles, fg) -- edited

        -- re-sort depth batches in case a new one was added
        celesteRender.invalidateRoomCache(room, "complete") -- added

        return batchWrapper.batches -- edited
    end
    logging.warning("[SorbetHelper/DepthSplitterPreview] hooked brushHelper.updateRender!")

    return {
        unload = function()
            brushHelper.updateRender = _orig_updateRender
        end
    }
end

return brushHelperHooks
