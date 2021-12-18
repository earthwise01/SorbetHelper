module SorbetHelperReturnBerry

using ..Ahorn, Maple

# Return Berry Ahorn plugin
# Probably quite messy in places as it's mostly the Strawberry and Key Ahorn plugins smashed together.

@mapdef Entity "SorbetHelper/ReturnBerry" ReturnBerry(
    x::Integer,
    y::Integer,
    winged::Bool=false,
    checkpointID::Integer=-1,
    order::Integer=-1,
)

const placements = Ahorn.PlacementDict(
    "Strawberry (With Return) (Sorbet Helper)" => Ahorn.EntityPlacement(
        ReturnBerry,
        "point",
        Dict{String, Any}(),
        function(entity::ReturnBerry)
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

const sprites = Dict{Bool, String}(
    (false) => "collectables/strawberry/normal00",
    (true) => "collectables/strawberry/wings01",
)

# set the node limits
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

        Ahorn.drawArrow(ctx, px, py, nx, ny, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)
        px, py = nx, ny
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry, room::Maple.Room)
    x, y = Ahorn.position(entity)

    winged = get(entity.data, "winged", false)

    sprite = sprites[(winged)]
    
    Ahorn.drawSprite(ctx, sprite, x, y)
    
end

end