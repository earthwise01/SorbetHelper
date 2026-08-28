local languageRegistry = require("language_registry")
local celesteEnums = require("consts.celeste_enums")

-- Inject SorbetHelper wipes into the dropdown in map metadata.
-- Based on FemtoHelper (ty sunset) and JungleHelper by proxy (ty maddie)

local sorbetHelperName = languageRegistry.getLanguage().mods.SorbetHelper.name

local function registerWipe(wipeId, wipeName)
    -- todo: use lang for wipe name
    celesteEnums.wipe_names[string.format("%s [%s]", wipeName, sorbetHelperName)] = wipeId
end

registerWipe("SorbetHelper/FourPointStarfieldWipe", "Four Point Starfield")

return {}
