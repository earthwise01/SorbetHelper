-- heavily references both loennextended by jatheplayer and anotherloennplugin by microlith57
local logging = require("logging")
local utils = require("utils")
local fileLocations = require("file_locations")
local serialize = require("utils.serialize")
local mods = require("mods")

local entities = require("entities")
local decals = require("decals")
local xmlHandler = require("lib.xml2lua.xmlhandler.tree")
local xml2lua = require("lib.xml2lua.xml2lua")

-- DECAL REGISTRY PARSING --

local hdDecalNames = {}
local decalDepths = {}

-- dont think this is really accurate at all but   good enough i guess idk
local function loadDecalRegistryXML(filename)
    local handler = xmlHandler:new()
    local parser = xml2lua.parser(handler)
    local xmlString = utils.readAll(filename, "rb")

    if not xmlString then
        logging.warning(string.format("[SorbetHelper] Unable to read decal registry xml from '%s'", filename))
        return
    end

    local xml = utils.stripByteOrderMark(xmlString)
    parser:parse(xml)

    local decalsRoot = handler.root.decals.decal

    if not decalsRoot then
        logging.warning(string.format("[SorbetHelper] Unable to read decal registry xml from '%s'", filename))
        return
    end

    for _, element in ipairs(decalsRoot) do
        local path = element._attr.path

        if element.depth then
            local depth = tonumber(element.depth._attr.value)

            if depth ~= nil then
                decalDepths["decals/" .. path] = depth
                logging.info(string.format("[SorbetHelper] Registered decal registry depth '" .. depth .. "' for decal '%s'", path))
            end
        end

        if element.sorbetHelper_hiRes then
            hdDecalNames["decals/" .. path] = true
            logging.info(string.format("[SorbetHelper] Registered hi-res decal '%s'", path))
        end
    end
end

local function parseDecalRegistry()
    local filenames = {}
    local modsDirectory = utils.joinpath(fileLocations.getCelesteDir(), "Mods")

    for modFolderName, _ in pairs(mods.loadedMods) do
        local path = utils.joinpath(modsDirectory, modFolderName, "DecalRegistry.xml")
        -- logging.info("[SorbetHelper] checking for decal registry " .. path)
        if utils.isFile(path) then
            logging.info(string.format("[SorbetHelper] Parsing decal registry xml from '%s' ", path))
            loadDecalRegistryXML(path)
        end
    end
end

parseDecalRegistry()

-- HOOKS --

-- undo any "hooks" after hot reloading
if entities.___sorbetHelperDecalHooks then
    entities.___sorbetHelperDecalHooks.unload()
end

--
local _orig_decals_getDrawable = decals.getDrawable
function decals.getDrawable(texture, handler, room, decal, viewport)
    local drawable, depth = _orig_decals_getDrawable(texture, handler, room, decal, viewport)
    
    if depth == nil then
        for registryName, registryDepth in pairs(decalDepths) do
            local i = string.find(texture, registryName)
            if i ~= nil and i == 1 then
                return drawable, registryDepth
            end
        end
    end

    return drawable, depth
end

-- scale down hi-res decals
local _orig_decals_getPlacements = decals.getPlacements
function decals.getPlacements(layer, specificMods)
    local placements = _orig_decals_getPlacements(layer, specificMods)
    
    for i, placement in ipairs(placements) do
        local itemTemplate = placement.itemTemplate

        if hdDecalNames[itemTemplate.texture] ~= nil then
            logging.info(string.format("[SorbetHelper] Modified placement scale for hi-res decal '%s'", itemTemplate.texture))
            itemTemplate.scaleX = 1 / 6
            itemTemplate.scaleY = 1 / 6
        end
    end

    return placements
end

-- unloads any "hooks"
local function unload()
    decals.getDrawable = _orig_decals_getDrawable
    decals.getPlacements = _orig_decals_getPlacements
end

entities.___sorbetHelperDecalHooks = {
    unload = unload
}

return { }