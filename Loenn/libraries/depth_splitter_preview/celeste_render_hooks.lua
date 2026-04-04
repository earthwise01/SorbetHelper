local autotiler = require("autotiler")
local tasks = require("utils.tasks")
local utils = require("utils")
local atlases = require("atlases")
local matrix = require("utils.matrix")
local bit = require("bit")
local depths = require("consts.object_depths")
local logging = require("logging")
local mods = require("mods")
local tileDepthHelper = mods.requireFromPlugin("libraries.depth_splitter_preview.tile_depth_helper")

local tilesFgDepth = depths.fgTerrain
local tilesBgDepth = depths.bgTerrain

local celesteRenderHooks = {}

function celesteRenderHooks.load()
    local celesteRender = require("celeste_render")

    -- get tiletype depths from solid tiles depth splitters when loading tileset xmls
    local _orig_loadCustomTilesetAutotiler = celesteRender.loadCustomTilesetAutotiler
    function celesteRender.loadCustomTilesetAutotiler(state)
        _orig_loadCustomTilesetAutotiler(state)
        tileDepthHelper.updateTiletypeDepths(state)
    end
    logging.warning("[SorbetHelper/DepthSplitterPreview] hooked celesteRender.loadCustomTilesetAutotiler!")

    local _orig_getTilesBatch = celesteRender.getTilesBatch
    -- modified from https://github.com/CelestialCartographers/Loenn/blob/v1.0.5/src/celeste_render.lua#L410
    -- to add tiles to multiple batches depending on their depths in sorbet helper solid tiles depth splitters,
    -- and to keep compatibility with older loenn versions
    -- todo: check if this works on later versions/port any new changes over
    function celesteRender.getTilesBatch(room, tiles, meta, scenery, fg, randomMatrix, batchMode, shouldYield)
        if not tileDepthHelper.shouldEnableMultipleTileDepths() then
            return _orig_getTilesBatch(room, tiles, meta, scenery, fg, randomMatrix, batchMode, shouldYield)
        end

        batchMode = batchMode or "matrixDrawingBatch"

        local tilesMatrix = tiles.matrix

        -- Getting upvalues
        local tileCache = celesteRender.tilesSpriteMetaCache
        local autotiler = autotiler
        local meta = meta
        local checkTile = autotiler.checkTile
        local lshift = bit.lshift
        local bxor = bit.bxor
        local band = bit.band

        local canvasOrMatrixBatch = batchMode == "gridCanvasDrawingBatch" or batchMode == "matrixDrawingBatch"
        local tableBatch = batchMode == "table"

        local airTile = "0"
        local emptyTile = " "
        local wildcard = "*"

        local defaultQuad = {{0, 0}}
        local defaultSprite = ""

        local width, height = tilesMatrix:size()
        local batchWrapper = tileDepthHelper.createBatchWrapper({}, width, height, batchMode) -- edited

        local random = randomMatrix or celesteRender.getRoomRandomMatrix(room, fg and "tilesFg" or "tilesBg")

        local sceneryMatrix = scenery and scenery.matrix
        local sceneryMeta = celesteRender.getSceneryMeta()
        local sceneryWidth, sceneryHeight = sceneryMeta.realWidth, sceneryMeta.realHeight

        local gameplayAtlas = atlases.gameplay

        local missingTiles = {}

        for x = 1, width do
            for y = 1, height do
                local tile = tilesMatrix:getInbounds(x, y)
                local sceneryTile = sceneryMatrix and sceneryMatrix:getInbounds(x, y, -1) or -1

                if sceneryTile > -1 then
                    local quad = celesteRender.getOrCacheScenerySpriteQuad(sceneryTile)

                    if quad then
                        if canvasOrMatrixBatch then
                            batchWrapper:getBatch(fg and tilesFgDepth or tilesBgDepth):set(x, y, sceneryMeta, quad, x * 8 - 8, y * 8 - 8) -- edited

                        elseif tableBatch then
                            table.insert(batchWrapper.batches, {sceneryMeta, quad, x * 8 - 8, y * 8 - 8}) -- edited
                        end
                    end

                elseif tile and tile ~= airTile then
                    local tileMeta = meta[tile]
                    local texture = tileMeta and tileMeta.path

                    if texture then
                        -- TODO - Render overlay sprites
                        local quads, sprites = tileDepthHelper.autotiler_getQuads_compat(x, y, tilesMatrix, meta, tileMeta, airTile, emptyTile, wildcard, defaultQuad, defaultSprite, checkTile, lshift, bxor, band)
                        local quadCount = #quads

                        if quadCount > 0 then
                            local rng = random:getInbounds(x, y)
                            local randQuad = quads[tileDepthHelper.celesteRender_getRandQuadIndex_compat(rng, quadCount)]

                            local spriteMeta = gameplayAtlas[texture]

                            if spriteMeta then
                                local quad = celesteRender.getOrCacheTileSpriteQuad(tileCache, tile, texture, randQuad, fg)

                                if canvasOrMatrixBatch then
                                    local tileDepth = tileDepthHelper.getTiletypeDepth(tile, fg) -- added

                                    batchWrapper:getBatch(tileDepth):set(x, y, spriteMeta, quad, x * 8 - 8, y * 8 - 8) -- edited

                                elseif tableBatch then
                                    table.insert(batchWrapper.batches, {spriteMeta, quad, x * 8 - 8, y * 8 - 8}) -- edited
                                end

                            else
                                -- Missing texture, not found on disk
                                table.insert(missingTiles, {x, y})
                            end
                        end

                    else
                        -- Unknown tileset id
                        table.insert(missingTiles, {x, y})
                    end
                end
            end

            if shouldYield ~= false then
                tasks.yield()
            end
        end

        celesteRender.drawInvalidTiles(batchWrapper:getBatch(fg and tilesFgDepth or tilesBgDepth), missingTiles, fg) -- edited

        if shouldYield ~= false then
            tasks.update(batchWrapper.batches) -- edited
        end

        return batchWrapper.batches, missingTiles -- edited
    end
    logging.warning("[SorbetHelper/DepthSplitterPreview] hooked celesteRender.getTilesBatch!")

    ---

    return {
        unload = function()
            celesteRender.loadCustomTilesetAutotiler = _orig_loadCustomTilesetAutotiler
            celesteRender.getTilesBatch = _orig_getTilesBatch
        end
    }
end

return celesteRenderHooks
