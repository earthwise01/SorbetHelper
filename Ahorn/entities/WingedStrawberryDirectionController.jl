module SorbetHelperWingedStrawberryDirectionController

using ..Ahorn, Maple

const directions = String[
    "Up",
    "Down",
    "Left",
    "Right",
    "UpLeft",
    "UpRight",
    "DownLeft",
    "DownRight"
]

@mapdef Entity "SorbetHelper/WingedStrawberryDirectionController" WingedStrawberryDirectionController(x::Integer, y::Integer,
    direction::String="Up")

const placements = Ahorn.PlacementDict(
    "Winged Strawberry Direction Controller (Sorbet Helper)" => Ahorn.EntityPlacement(
        WingedStrawberryDirectionController
    )
)

function Ahorn.editingOptions(entity::WingedStrawberryDirectionController)
    return Dict{String, Any}(
        "direction" => directions
    )
end

function Ahorn.selection(entity::WingedStrawberryDirectionController)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 12, y - 12, 24, 24)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WingedStrawberryDirectionController, room::Maple.Room) = Ahorn.drawImage(ctx, "objects/SorbetHelper/wingedStrawberryDirectionController/icon", -12, -12)

end
