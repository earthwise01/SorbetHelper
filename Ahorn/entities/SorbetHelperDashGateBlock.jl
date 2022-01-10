module SorbetHelperDashGateBlock

using ..Ahorn, Maple

# Most of this is copy-pasted from MaxHelpingHand's Flag Switch Gates since I dislike writing Ahorn plugins
# https://github.com/max4805/MaxHelpingHand/blob/master/Ahorn/entities/maxHelpingHandFlagSwitchGate.jl

@pardef DashGateBlock(x1::Integer, y1::Integer, x2::Integer=x1+16, y2::Integer=y1, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    blockSprite::String="block", iconSprite::String="SorbetHelper/gateblock/dash/icon", inactiveColor::String="F86593", activeColor::String="FFFFFF", finishColor::String="62A1F5",
    shakeTime::Number=0.5, moveTime::Number=1.8, moveEased::Bool=true, allowWavedash::Bool=false, moveSound::String="event:/game/general/touchswitch_gate_open", finishedSound::String="event:/game/general/touchswitch_gate_finish", smoke::Bool=true, persistent::Bool=false, linked::Bool=false, linkTag::String="") =
    Entity("SorbetHelper/DashGateBlock", x=x1, y=y1, nodes=Tuple{Int, Int}[(x2, y2)], width=width, height=height, blockSprite=blockSprite, iconSprite=iconSprite,
    inactiveColor=inactiveColor, activeColor=activeColor, finishColor=finishColor, shakeTime=shakeTime, moveTime=moveTime, moveEased=moveEased, allowWavedash=allowWavedash, moveSound=moveSound, finishedSound=finishedSound, smoke=smoke, persistent=persistent, linked=linked, linkTag=linkTag)

function gateFinalizer(entity)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    entity.data["nodes"] = [(x + width, y)]
end

const textures = String["block", "mirror", "temple", "stars"]

const placements = Ahorn.PlacementDict(
    "Dash Gate Block ($(uppercasefirst(texture))) (Sorbet Helper)" => Ahorn.EntityPlacement(
        DashGateBlock,
        "rectangle",
        Dict{String, Any}(
            "blockSprite" => texture
        ),
        gateFinalizer
    ) for texture in textures
)

Ahorn.editingOrder(entity::DashGateBlock) = String["x", "y", "width", "height", "inactiveColor", "activeColor", "finishColor", "moveSound", "finishedSound", "shakeTime", "moveTime", "moveEased", "blockSprite", "iconSprite", "allowWavedash", "smoke", "persistent", "linked", "linkTag"]

Ahorn.editingOptions(entity::DashGateBlock) = Dict{String, Any}(
    "blockSprite" => textures,
    "iconSprite" => Dict{String, String}(
        "Vanilla" => "switchgate/icon",
        "Dash" => "SorbetHelper/gateblock/dash/icon",
        "Touch" => "SorbetHelper/gateblock/touch/icon",
    ),
)

Ahorn.nodeLimits(entity::DashGateBlock) = 1, 1

Ahorn.minimumSize(entity::DashGateBlock) = 16, 16
Ahorn.resizable(entity::DashGateBlock) = true, true

function Ahorn.selection(entity::DashGateBlock)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

function renderGateSwitch(ctx::Ahorn.Cairo.CairoContext, entity::DashGateBlock, x::Number, y::Number, width::Number, height::Number, sprite::String)
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

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::DashGateBlock, room::Maple.Room)
    sprite = get(entity.data, "blockSprite", "block")
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderGateSwitch(ctx, entity, stopX, stopY, width, height, sprite)
    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DashGateBlock, room::Maple.Room)
    sprite = get(entity.data, "blockSprite", "block")

    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderGateSwitch(ctx, entity, x, y, width, height, sprite)
end

end