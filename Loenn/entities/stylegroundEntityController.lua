local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local stylegroundEntityController = {}

stylegroundEntityController.name = "SorbetHelper/StylegroundEntityController"
stylegroundEntityController.sprite = sorbetUtils.getControllerSpriteFunction("stylegroundEntityController", true)
stylegroundEntityController.depth = -1000010
-- Replaced by the Styleground Depth Controller 
--
-- stylegroundEntityController.placements = {
--     name = "controller",
--     alternativeName = "altname",
--     data = {
--         depth = 0,
--         tag = "",

--         -- hey so like   i don't   really feel like i should show this normally??
--         -- this is supposed to fix a "bug" that happens when used alongside world portals from bits & bolts, but causes the code to be messier & probably laggier
--         -- user-side enabling this should still work the same, but given That and how niche the bug is i think ill leave this here commented idk
--         -- if you're running into said issue uncomment the following line, place a new styleground entity controller, and check no consume
--         -- noConsume = false
--     }
-- }

stylegroundEntityController.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = depths.getDepths(),
        editable = true
    }
}

stylegroundEntityController.fieldOrder = {
    "x", "y",
    "tag", "depth",
}

return stylegroundEntityController