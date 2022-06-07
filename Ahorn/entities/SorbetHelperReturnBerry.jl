module SorbetHelperReturnBerry

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/ReturnBerry" ReturnBerry(
    x::Integer,
    y::Integer,
    winged::Bool=false,
    delay::Number=0.3,
    checkpointID::Integer=-1,
    order::Integer=-1,
)

const placements = Ahorn.PlacementDict(
    "Strawberry (With Node Based Return) (Sorbet Helper)" => Ahorn.EntityPlacement(
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
    "Strawberry (Winged, With Node Based Return) (Sorbet Helper)" => Ahorn.EntityPlacement(
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

const sprites = Dict{Tuple{Bool, Bool}, String}(
    (false, false) => "collectables/strawberry/normal00",
    (true, false) => "collectables/strawberry/wings01",
    (false, true) => "collectables/ghostberry/idle00",
    (true, true) => "collectables/ghostberry/wings01",
)

Ahorn.nodeLimits(entity::ReturnBerry) = 2, -1

function Ahorn.selection(entity::ReturnBerry)
    x, y = Ahorn.position(entity)

    nodes = get(entity.data, "nodes", Tuple{Int, Int}[])
    winged = get(entity.data, "winged", false)
    seeded = length(nodes) > 2

    sprite = sprites[(winged, seeded)]
    seedSprite = "collectables/strawberry/seed00"
    bubbleSprite = "characters/player/bubble"

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]

    for (i, node) in enumerate(nodes)
        nx, ny = node
        if i < 3
            push!(res, Ahorn.getSpriteRectangle(bubbleSprite, nx, ny))
        else
            push!(res, Ahorn.getSpriteRectangle(seedSprite, nx, ny))
        end
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry)
    x, y = Ahorn.position(entity)
    px, py = x, y
    nodes = get(entity.data, "nodes", Tuple{Int, Int}[])
    winged = get(entity.data, "winged", false)
    seeded = length(nodes) > 2

    bubbleSprite = "characters/player/bubble"

    for (i, node) in enumerate(nodes)
        nx, ny = Int.(node)

        if i < 3
            Ahorn.drawArrow(ctx, px, py, nx, ny, (1.0, 1.0, 1.0, 0.5), headLength=6)
            Ahorn.drawSprite(ctx, bubbleSprite, nx, ny)
            px, py = nx, ny
        else
            Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
        end
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry, room::Maple.Room)
    x, y = Ahorn.position(entity)
    px, py = x, y
    nodes = get(entity.data, "nodes", Tuple{Int, Int}[])
    winged = get(entity.data, "winged", false)
    seeded = length(nodes) > 2

    sprite = sprites[(winged, seeded)]
    seedSprite = "collectables/strawberry/seed00"
    bubbleSprite = "characters/player/bubble"

    for (i, node) in enumerate(nodes)
        nx, ny = Int.(node)
        if i < 3
            Ahorn.drawArrow(ctx, px, py, nx, ny, (1.0, 1.0, 1.0, 0.3), headLength=6)
            Ahorn.drawSprite(ctx, bubbleSprite, nx, ny, alpha=0.35)
            px, py = nx, ny
        else
            Ahorn.drawSprite(ctx, seedSprite, nx, ny)
        end
    end
    
    Ahorn.drawSprite(ctx, sprite, x, y)
    
end

end