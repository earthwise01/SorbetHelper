module SorbetHelperDashFallingBlock

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/DashFallingBlock" DashFallingBlock(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockHeight,
    tiletype::String="3",
    shakeSfx::String="event:/game/general/fallblock_shake",
    impactSfx::String="event:/game/general/fallblock_impact",
    fallOnTouch::Bool=false,
    climbFall::Bool=true,
    fallOnStaticMover::Bool=false,
    depth::Integer=-9000,
    allowWavedash::Bool=false,
    dashCornerCorrection::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Dash Falling Block (Sorbet Helper)" => Ahorn.EntityPlacement(
        DashFallingBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer
    ),
)

Ahorn.editingOptions(entity::DashFallingBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions(),
    "shakeSfx" => Dict{String, String}(
        "Default" => "event:/game/general/fallblock_shake",
        "Snow" => "event:/game/01_forsaken_city/fallblock_ice_shake",
        "Wood" => "event:/game/03_resort/fallblock_wood_shake",
        "Reflection" => "event:/game/06_reflection/fallblock_boss_shake"
    ),
    "impactSfx" => Dict{String, String}(
        "Default" => "event:/game/general/fallblock_impact",
        "Snow" => "event:/game/01_forsaken_city/fallblock_ice_impact",
        "Wood" => "event:/game/03_resort/fallblock_wood_impact",
        "Reflection" => "event:/game/06_reflection/fallblock_boss_impact"
    )
)

Ahorn.minimumSize(entity::DashFallingBlock) = 8, 8
Ahorn.resizable(entity::DashFallingBlock) = true, true

Ahorn.selection(entity::DashFallingBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DashFallingBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

Ahorn.editingOrder(entity::DashFallingBlock) = String["x", "y", "width", "height", "shakeSfx", "impactSfx", "tiletype", "depth", "fallOnTouch", "climbFall", "fallOnStaticMover", "allowWavedash", "dashCornerCorrection"]

end