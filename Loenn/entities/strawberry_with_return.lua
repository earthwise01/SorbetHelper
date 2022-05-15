local return_berry = {}

return_berry.name = "SorbetHelper/ReturnBerry"
return_berry.depth = -100
return_berry.nodeLineRenderType = "line"
return_berry.nodeLimits = {2, 2}

function return_berry.texture(room, entity)
    local winged = entity.winged
    if winged then
        return "collectables/strawberry/wings01"
    else
        return "collectables/strawberry/normal00"
    end
end

return_berry.placements = {
    {
        name = "normal",
        placementType = "point",
        data = {
            winged = false,
            delay = 0.3,
            nodes = {
                {x = 0, y = 0},
                {x = 0, y = 0}
            }
        }
    },
    {
        name = "winged",
        placementType = "point",
        data = {
            winged = true,
            delay = 0.3,
            nodes = {
                {x = 0, y = 0},
                {x = 0, y = 0}
            }
        }
    }
}

return return_berry