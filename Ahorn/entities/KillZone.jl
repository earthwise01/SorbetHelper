module SorbetHelperKillZone

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/KillZone" KillZone(
    x::Integer,
    y::Integer,
    width::Integer=8,
    height::Integer=8,
    flag::String="",
    inverted::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Kill Zone (Sorbet Helper)" => Ahorn.EntityPlacement(
        KillZone,
        "rectangle",
    )
)

Ahorn.minimumSize(entity::KillZone) = 8, 8
Ahorn.resizable(entity::KillZone) = true, true

function Ahorn.selection(entity::KillZone)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
 
    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::KillZone, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    
    Ahorn.drawRectangle(ctx, 0, 0, width, height, (1.0, 0.8, 0.85, 0.8), (0.0, 1.0, 1.0, 0.0))
end

end
