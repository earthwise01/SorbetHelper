module SorbetHelperTouchGateBlock

using ..Ahorn, Maple

# Most of this is copy-pasted from MaxHelpingHand's Flag Switch Gates since I dislike writing Ahorn plugins
# https://github.com/max4805/MaxHelpingHand/blob/master/Ahorn/entities/maxHelpingHandFlagSwitchGate.jl

@pardef TouchGateBlock(x1::Integer, y1::Integer, x2::Integer=x1+16, y2::Integer=y1, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    blockSprite::String="block", iconSprite::String="SorbetHelper/gateblock/touch/icon", inactiveColor::String="4EF3CF", activeColor::String="FFFFFF", finishColor::String="FFF175",
    shakeTime::Number=0.5, moveTime::Number=1.8, moveEased::Bool=true, moveOnGrab::Bool=true, moveOnStaticMoverInteract::Bool=false, moveSound::String="event:/game/general/touchswitch_gate_open", finishedSound::String="event:/game/general/touchswitch_gate_finish", smoke::Bool=true, persistent::Bool=false, linked::Bool=false, linkTag::String="") =
    Entity("SorbetHelper/TouchGateBlock", x=x1, y=y1, nodes=Tuple{Int, Int}[(x2, y2)], width=width, height=height, blockSprite=blockSprite, iconSprite=iconSprite,
    inactiveColor=inactiveColor, activeColor=activeColor, finishColor=finishColor, shakeTime=shakeTime, moveTime=moveTime, moveEased=moveEased, moveOnGrab=moveOnGrab, moveOnStaticMoverInteract=moveOnStaticMoverInteract, moveSound=moveSound, finishedSound=finishedSound, smoke=smoke, persistent=persistent, linked=linked, linkTag=linkTag)

function gateFinalizer(entity)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    entity.data["nodes"] = [(x + width, y)]
end

const textures = String["block", "mirror", "temple", "stars"]

const placements = Ahorn.PlacementDict(
    "Gate Block (Touch Activated, $(uppercasefirst(texture))) (Sorbet Helper)" => Ahorn.EntityPlacement(
        TouchGateBlock,
        "rectangle",
        Dict{String, Any}(
            "blockSprite" => texture
        ),
        gateFinalizer
    ) for texture in textures
)

Ahorn.editingOrder(entity::TouchGateBlock) = String["x", "y", "width", "height", "inactiveColor", "activeColor", "finishColor", "moveSound", "finishedSound", "shakeTime", "moveTime", "moveEased", "blockSprite", "iconSprite", "moveOnGrab", "moveOnStaticMoverInteract", "smoke", "persistent", "linked", "linkTag"]

Ahorn.editingOptions(entity::TouchGateBlock) = Dict{String, Any}(
    "blockSprite" => textures,
    "iconSprite" => Dict{String, String}(
        "Vanilla" => "switchgate/icon",
        "Dash" => "SorbetHelper/gateblock/dash/icon",
        "Touch" => "SorbetHelper/gateblock/touch/icon",
        "Linked" => "SorbetHelper/gateblock/linked/icon",
    ),
)

Ahorn.nodeLimits(entity::TouchGateBlock) = 1, 1

Ahorn.minimumSize(entity::TouchGateBlock) = 16, 16
Ahorn.resizable(entity::TouchGateBlock) = true, true

function Ahorn.selection(entity::TouchGateBlock)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

function renderGateSwitch(ctx::Ahorn.Cairo.CairoContext, entity::TouchGateBlock, x::Number, y::Number, width::Number, height::Number, sprite::String)
    icon = get(entity.data, "iconSprite", "SorbetHelper/gateblock/touch/icon") * "00"

    iconResource = "objects/$(icon)"

    iconSprite = Ahorn.getSprite(iconResource, "Gameplay")
    
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    frame = "objects/switchgate/$sprite"

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + (j - 1) * 8, 8, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)

    Ahorn.drawImage(ctx, iconSprite, x + div(width - iconSprite.width, 2), y + div(height - iconSprite.height, 2))
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::TouchGateBlock, room::Maple.Room)
    sprite = get(entity.data, "blockSprite", "block")
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderGateSwitch(ctx, entity, stopX, stopY, width, height, sprite)
    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::TouchGateBlock, room::Maple.Room)
    sprite = get(entity.data, "blockSprite", "block")

    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderGateSwitch(ctx, entity, x, y, width, height, sprite)
end

end