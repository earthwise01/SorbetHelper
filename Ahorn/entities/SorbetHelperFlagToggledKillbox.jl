module SorbetHelperFlagToggledKillbox

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/FlagToggledKillbox" FlagToggledKillbox(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    flag::String= "",
    inverted::Bool=false,
    )

const placements = Ahorn.PlacementDict(
    "Flag Toggled Killbox (Sorbet Helper)" => Ahorn.EntityPlacement(
        FlagToggledKillbox,
        "rectangle"
    ),
)

Ahorn.editingOrder(entity::FlagToggledKillbox) = String["x", "y", "width", "flag", "inverted"]

Ahorn.minimumSize(entity::FlagToggledKillbox) = 8, 0
Ahorn.resizable(entity::FlagToggledKillbox) = true, false

function Ahorn.selection(entity::FlagToggledKillbox)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = 32

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FlagToggledKillbox, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = 32

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.8, 0.4, 0.4, 0.8), (0.0, 0.0, 0.0, 0.0))
end

end