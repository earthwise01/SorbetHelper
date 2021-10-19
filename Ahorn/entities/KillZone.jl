module SorbetHelperKillZone

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/KillZone" KillZone(
    x::Integer,
    y::Integer,
    width::Integer=8,
    height::Integer=8,
)

const placements = Ahorn.PlacementDict(
    "Kill Zone (Sorbet Helper)" => Ahorn.EntityPlacement(
        KillZone,
        "rectangle",
    )
)

Ahorn.minimumSize(entity::KillZone) = 8, 8
Ahorn.resizable(entity::KillZone) = true, true

end
