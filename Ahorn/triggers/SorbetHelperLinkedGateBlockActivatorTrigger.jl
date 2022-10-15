module SorbetHelperLinkedGateBlockActivatorTrigger

#=

using ..Ahorn, Maple

@mapdef Trigger "SorbetHelper/LinkedGateBlockActivatorTrigger" LinkedGateBlockActivatorTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, linkTag::String="", flag::String="", inverted::Bool=false)

const placements = Ahorn.PlacementDict(
    "Linked Gate Block Activator (Sorbet Helper)" => Ahorn.EntityPlacement(
        LinkedGateBlockActivatorTrigger,
        "rectangle"
    )
)

Ahorn.editingOrder(entity::LinkedGateBlockActivatorTrigger) = String["x", "y", "width", "height", "linkTag", "flag", "inverted"]

=#

end
