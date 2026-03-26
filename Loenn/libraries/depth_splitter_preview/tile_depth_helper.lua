local logging = require("logging")
local mods = require("mods")
local depths = require("consts.object_depths")
local smartDrawingBatch = require("structs.smart_drawing_batch")
local sorbetUtils = mods.requireFromPlugin("libraries.utils")

local tilesFgDepth = depths.fgTerrain
local tilesBgDepth = depths.bgTerrain

local tileDepthHelper = {}

---

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

---

local function getOrCreateSmartTilesBatch(batches, depth, width, height, mode)
    batches[depth] = batches[depth] or (mode == "gridCanvasDrawingBatch" and smartDrawingBatch.createGridCanvasBatch(false, width, height, 8, 8) or smartDrawingBatch.createMatrixBatch(false, width, height, 8, 8))

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
