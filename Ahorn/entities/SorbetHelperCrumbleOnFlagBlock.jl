module SorbetHelperCrumbleOnFlagBlock

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/CrumbleOnFlagBlock" CrumbleOnFlagBlock(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockHeight,
    tiletype::String="3",
    blendin::Bool=true,
    playAudio::Bool=true,
    showDebris::Bool=true,
    flag::String="",
    inverted::Bool=false,
    depth::Integer=-10010,
)

const placements = Ahorn.PlacementDict(
    "Crumble On Flag Block (Sorbet Helper)" => Ahorn.EntityPlacement(
        CrumbleOnFlagBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer
    )
)

Ahorn.editingOptions(entity::CrumbleOnFlagBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::CrumbleOnFlagBlock) = 8, 8
Ahorn.resizable(entity::CrumbleOnFlagBlock) = true, true

Ahorn.selection(entity::CrumbleOnFlagBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CrumbleOnFlagBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

end
