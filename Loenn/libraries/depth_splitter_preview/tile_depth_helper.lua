local logging = require("logging")
local mods = require("mods")
local depths = require("consts.object_depths")
local smartDrawingBatch = require("structs.smart_drawing_batch")
local autotiler = require("autotiler")
local meta = require("meta")
local version = require("utils.version_parser")
local sorbetUtils = mods.requireFromPlugin("libraries.sorbet_utils")

local tilesFgDepth = depths.fgTerrain
local tilesBgDepth = depths.bgTerrain

local tileDepthHelper = {}

--- backwards compat with older loenn versions ---

local currentLoennVersion = meta.version
local beforeV106 = currentLoennVersion < version("v1.0.6")

function tileDepthHelper.autotiler_getQuads_compat(x, y, tiles, meta, tileMeta, airTile, emptyTile, wildcard, defaultQuad, defaultSprite, checkTile)
    -- versions before v1.0.6 had a `meta` argument instead of a `tileMeta` argument
    if beforeV106 then
        return autotiler.getQuads(x, y, tiles, meta, airTile, emptyTile, wildcard, defaultQuad, defaultSprite, checkTile)
    end

    return autotiler.getQuads(x, y, tiles, tileMeta, airTile, emptyTile, wildcard, defaultQuad, defaultSprite, checkTile)
end

function tileDepthHelper.getRandQuadIndex_compat(rng, quadCount)
    -- versions before v1.0.6 had a bug where the matrix used for tile randomization wasn't in the correct 0-1 range (i think?), which meant that the random quad index also had to be calculated differently
    if beforeV106 then
        return utils.mod1(rng, quadCount)
    end

    return 1 + math.floor(rng * (quadCount - 1))
end

function tileDepthHelper.smartDrawingBatch_createMatrixBatch_compat(default, width, height, cellWidth, cellHeight)
    -- versions before v1.0.6 had a `default` argument
    if beforeV106 then
        return smartDrawingBatch.createMatrixBatch(default, width, height, cellWidth, cellHeight)
    end

    return smartDrawingBatch.createMatrixBatch(width, height, cellWidth, cellHeight)
end

--- tiletype depths ---

-- todo: currently updated in celesteRender.loadCustomTilesetAutotiler but idk if thats enough or not
local enableMultipleTileDepths
local tiletypesFgToDepths
local tiletypesBgToDepths
function tileDepthHelper.updateTiletypeDepths(state)
    enableMultipleTileDepths = false
    tiletypesFgToDepths = {}
    tiletypesBgToDepths = {}

    -- these are already called everywhere celesteRender.loadCustomTilesetAutotiler is
    -- celesteRender.invalidateRoomCache()
    -- celesteRender.clearBatchingTasks()

    for _, room in pairs(state.map.rooms) do
        for _, entity in pairs(room.entities or {}) do
            if entity._name == "SorbetHelper/SolidTilesDepthSplitter" then
                local tiletypesToDepths = entity.backgroundTiles and tiletypesBgToDepths or tiletypesFgToDepths

                for _, splitTiletype in ipairs(sorbetUtils.UTF8ToCharArray(entity.tiletypes)) do
                    if tiletypesToDepths[splitTiletype] == nil then
                        tiletypesToDepths[splitTiletype] = tonumber(entity.depth)
                        enableMultipleTileDepths = true
                    end
                end
            end
        end
    end
end

function tileDepthHelper.shouldEnableMultipleTileDepths()
    return enableMultipleTileDepths
end

function tileDepthHelper.getTiletypeDepth(tiletype, fg)
    if fg then
        return tiletypesFgToDepths[tiletype] or tilesFgDepth

    else
        return tiletypesBgToDepths[tiletype] or tilesBgDepth
    end
end

--- tile depth batch wrapper ---

local function getOrCreateSmartTilesBatch(batches, depth, width, height, mode)
    batches[depth] = batches[depth] or (mode == "gridCanvasDrawingBatch" and smartDrawingBatch.createGridCanvasBatch(false, width, height, 8, 8) or tileDepthHelper.smartDrawingBatch_createMatrixBatch_compat(false, width, height, 8, 8))

    return batches[depth]
end

function tileDepthHelper.createBatchWrapper(batches, width, height, batchMode)
    local wrapper = {}
    wrapper.batches = batches
    wrapper.width = width
    wrapper.height = height
    wrapper.batchMode = batchMode or "matrixDrawingBatch"

    function wrapper.getBatch(self, depth)
        if self.batchMode == "gridCanvasDrawingBatch" or self.batchMode == "matrixDrawingBatch" then
            return getOrCreateSmartTilesBatch(self.batches, depth, self.width, self.height, self.batchMode)
        end
    end

    function wrapper.removeSafe(self, x, y, meta)
        for _, batch in pairs(self.batches) do
            -- todo: does it matter that this just uses 'meta' when normal brushes.lua uses specifically the scenery meta
            -- todo: this also doesn't remove batches if they become emptyy but maybe that's fine
            batch:remove(x, y, meta) 
        end
    end

    function wrapper.setSafe(self, depth, x, y, meta, quad, drawX, drawY, r, sx, sy, jx, jy, ox, oy)
        self:removeSafe(x, y, meta) 
        self:getBatch(depth):set(x, y, meta, quad, drawX, drawY, r, sx, sy, jx, jy, ox, oy)
    end

    return wrapper
end

---

return tileDepthHelper
