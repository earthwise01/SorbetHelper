local utils = require("utils")
local logging = require("logging")
local mods = require("mods")
local meta = require("meta")
local version = require("utils.version_parser")

local celesteRenderHooks = mods.requireFromPlugin("libraries.depth_splitter_preview.celeste_render_hooks")
local brushHelperHooks = mods.requireFromPlugin("libraries.depth_splitter_preview.brushes_hooks")

local depthSplitterPreview = {}

-- check loenn version
local currentLoennVersion = meta.version
local expectedLoennVersion = version("v1.0.5")
if currentLoennVersion > expectedLoennVersion then
    logging.error("[SorbetHelper/DepthSplitterPreview] expected loenn version <= " .. tostring(expectedLoennVersion) .. " while current version is " .. tostring(currentLoennVersion) .. "! refusing to load.")
    return {}
end

-- unload "hooks" if they're already loaded
if utils.___depthSplitterHooks then
    utils.___depthSplitterHooks.unload()
    utils.___depthSplitterHooks = nil
end

-- load "hooks"
local hook_celesteRender = celesteRenderHooks.load()
local hook_brushHelper = brushHelperHooks.load()

-- prepare "hook" unloading
utils.___depthSplitterHooks = {
    unload = function()
        hook_celesteRender.unload()
        hook_celesteRender = nil
        hook_brushHelper.unload()
        hook_brushHelper = nil
    end
}

return depthSplitterPreview
