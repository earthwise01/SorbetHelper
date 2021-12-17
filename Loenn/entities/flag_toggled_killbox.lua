local killbox = {}

killbox.name = "SorbetHelper/FlagToggledKillbox"
killbox.fillColor = {0.8, 0.4, 0.4, 0.8}
killbox.borderColor = {0.0, 0.0, 0.0, 0.0}
killbox.canResize = {true, false}
killbox.placements = {
    name = "flag_toggled_killbox",
    data = {
        width = 8,
        height = 32,
        flag = "",
        inverted = false
    }
}

return killbox
