module SorbetHelperReturnBerry

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/ReturnBerry" ReturnBerry(
    x::Integer,
    y::Integer,
    winged::Bool=false,
)

println("adding placements")
const placements = Ahorn.PlacementDict(
    "Strawberry (With Return) (Sorbet Helper)" => Ahorn.EntityPlacement(
        ReturnBerry,
        "point",
        Dict{String, Any}(),
        function(entity::ReturnBerry)
            println("adding nodes")
            entity.data["nodes"] = [
                (Int(entity.data["x"]) + 32, Int(entity.data["y"])),
                (Int(entity.data["x"]) + 64, Int(entity.data["y"]))
            ]
        end
    ),
    "Strawberry (Winged, With Return) (Sorbet Helper)" => Ahorn.EntityPlacement(
        ReturnBerry,
        "point",
        Dict{String, Any}(
            "winged" => true
        ),
        function(entity::ReturnBerry)
             entity.data["nodes"] = [
                 (Int(entity.data["x"]) + 32, Int(entity.data["y"])),
                 (Int(entity.data["x"]) + 64, Int(entity.data["y"]))
             ]
        end
    )
)


# function setSprite(entity::ReturnBerry)   
#     if entity.data("winged") == true
#         println("using winged sprite")
#         sprite = "collectables/strawberry/wings01"
#     else 
#         println("using normal sprite")
#         sprite = "collectables/strawberry/normal00"
#     end
# end

const sprites = Dict{Bool, String}(
    (false) => "collectables/strawberry/normal00",
    (true) => "collectables/strawberry/wings01",
)

Ahorn.nodeLimits(entity::ReturnBerry) = length(get(entity.data, "nodes", [])) == 2 ? (2, 2) : (0, 0)

function Ahorn.selection(entity::ReturnBerry)
    x, y = Ahorn.position(entity)

    winged = get(entity.data, "winged", false)

    sprite = sprites[(winged)]

    if haskey(entity.data, "nodes") && length(entity["nodes"]) >= 2
        controllX, controllY = Int.(entity.data["nodes"][1])
        endX, endY = Int.(entity.data["nodes"][2])

        return [
            Ahorn.getSpriteRectangle(sprite, x, y),
            Ahorn.getSpriteRectangle(sprite, controllX, controllY),
            Ahorn.getSpriteRectangle(sprite, endX, endY)
        ]
        else
            return Ahorn.getSpriteRectangle(sprite, x, y)
    end

end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry)
    px, py = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", Tuple{Int, Int}[])
    winged = get(entity.data, "winged", false)

    sprite = sprites[(winged)]

    for node in nodes
        nx, ny = Int.(node)

        println("drawing arrows")
        Ahorn.drawArrow(ctx, px, py, nx, ny, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)
        px, py = nx, ny
    end
end

println("running Ahorn.render")
# Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)
function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry, room::Maple.Room)
    x, y = Ahorn.position(entity)

    winged = get(entity.data, "winged", false)

    sprite = sprites[(winged)]
    
    Ahorn.drawSprite(ctx, sprite, x, y)
    
end

end
# Ahorn.minimumSize(entity::KillZone) = 8, 8
# Ahorn.resizable(entity::KillZone) = true, true

# function Ahorn.selection(entity::ReturnBerry)
#     x, y = Ahorn.position(entity)

#     return Ahorn.Rectangle(x, y, 16, 16)
# end

# function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry, room::Maple.Room)
#     # width = Int(get(entity.data, "width", 32))
#     # height = Int(get(entity.data, "height", 32))
    
#     Ahorn.drawRectangle(ctx, 0, 0, 16, 16, (1.0, 0.8, 0.85, 0.8), (0.0, 1.0, 1.0, 0.0))
# end

#end
