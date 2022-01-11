local utils = require("utils")

local killbox = {}

killbox.name = "SorbetHelper/FlagToggledKillbox"
killbox.color = {0.8, 0.4, 0.4, 0.8}
killbox.canResize = {true, false}
killbox.placements = {
    name = "flag_toggled_killbox",
    data = {
        width = 8,
        flag = "",
        inverted = false
    }
}

function killbox.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width or 8, 32)
end

return killbox
