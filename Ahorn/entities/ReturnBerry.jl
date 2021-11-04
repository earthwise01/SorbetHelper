module SorbetHelperReturnBerry

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/ReturnBerry" ReturnBerry(
    x::Integer,
    y::Integer,
    winged::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Return Berry (Sorbet Helper)" => Ahorn.EntityPlacement(
        ReturnBerry,
    )
)

# Ahorn.minimumSize(entity::KillZone) = 8, 8
# Ahorn.resizable(entity::KillZone) = true, true

function Ahorn.selection(entity::ReturnBerry)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x, y, 16, 16)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ReturnBerry, room::Maple.Room)
    # width = Int(get(entity.data, "width", 32))
    # height = Int(get(entity.data, "height", 32))
    
    Ahorn.drawRectangle(ctx, 0, 0, 16, 16, (1.0, 0.8, 0.85, 0.8), (0.0, 1.0, 1.0, 0.0))
end

end
