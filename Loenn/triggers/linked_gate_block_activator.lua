local linkedGateBlockActivator = {}

linkedGateBlockActivator.name = "SorbetHelper/LinkedGateBlockActivatorTrigger"
linkedGateBlockActivator.placements = {
    name = "linked_gate_block_activator_trigger",
    data = {
        linkTag = "",
        flag = "",
        inverted = false
    }
}

linkedGateBlockActivator.fieldOrder = {"x", "y", "width", "height", "linkTag", "flag", "inverted"}

return linkedGateBlockActivator