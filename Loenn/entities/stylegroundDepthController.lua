local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local stylegroundDepthController = {}

stylegroundDepthController.name = "SorbetHelper/StylegroundDepthController"
stylegroundDepthController.depth = -1000010
stylegroundDepthController.placements = {
    {
        name = "depth",
        data = {
            depth = 0,
            tag = ""
        }
    },
    {
        name = "aboveHud",
        data = {
            depth = "AboveHud",
            tag = ""
        }
    }
}

local depths = depths.getDepths()
table.insert(depths, {"————————", ""})
table.insert(depths, {"Above Colorgrade", "AboveColorgrade"})
table.insert(depths, {"Above HUD", "AboveHud"})
table.insert(depths, {"Above Pause HUD", "AbovePauseHud"})

stylegroundDepthController.fieldInformation = {
    depth = {
        fieldType = "sorbetHelper.integerAndEnum",
        options = depths,
        enum = {
            AboveColorgrade = true,
            AboveHud = true,
            AbovePauseHud = true
        },
        editable = true
    }
}

stylegroundDepthController.fieldOrder = {
    "x", "y",
    "tag", "depth",
}

function stylegroundDepthController.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local depth = entity.depth or 0

    local texture = type(depth) == "number" and "stylegroundEntityController" or "stylegroundOverHudController"
    return sorbetUtils.getControllerSprites(x, y, texture, true)
end

return stylegroundDepthController