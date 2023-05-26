module SorbetHelperKillZone

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/KillZone" KillZone(
    x::Integer,
    y::Integer,
    width::Integer=8,
    height::Integer=8,
    flag::String="",
    inverted::Bool=false,
    fastKill::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Kill Zone (Sorbet Helper)" => Ahorn.EntityPlacement(
        KillZone,
        "rectangle",
    )
)

Ahorn.minimumSize(entity::KillZone) = 8, 8
Ahorn.resizable(entity::KillZone) = true, true
Ahorn.editingOrder(entity::KillZone) = String["x", "y", "width", "height", "flag", "inverted", "fastKill"]

function Ahorn.selection(entity::KillZone)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
 
    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::KillZone, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    
    Ahorn.drawRectangle(ctx, 0, 0, width, height, (176 / 255, 99 / 255, 100 / 255, 0.45), (145 / 255, 59 / 255, 95 / 255, 0.82))
end

end
