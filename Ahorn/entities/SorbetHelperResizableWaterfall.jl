# as usual this is mostly just a low effort copy-paste of the default ahorn big waterfall plugin because i don't think i particularly care to put in much effort for ahorn plugins anymore unfortunately

module SorbetHelperWaterfall

using ..Ahorn, Maple

@mapdef Entity "SorbetHelper/BigWaterfall" ResizableWaterfall(
    x::Integer,
    y::Integer,
    width::Integer=16,
    height::Integer=8,
    color::String="87CEFA",
    ignoreSolids::Bool=false,
    lines::Bool=true,
    depth::Integer=-9999,
)

const placements = Ahorn.PlacementDict(
    "Resizable Waterfall (Sorbet Helper)" => Ahorn.EntityPlacement(
        ResizableWaterfall,
        "rectangle",
        Dict{String, Any}()
    ),
    "Resizable Waterfall (Above Foreground) (Sorbet Helper)" => Ahorn.EntityPlacement(
        ResizableWaterfall,
        "rectangle",
        Dict{String, Any}(
            "ignoreSolids" => true,
            "depth" => -49900
        )
    ),
)

const fillColor = Ahorn.XNAColors.LightBlue .* 0.3
const surfaceColor = Ahorn.XNAColors.LightBlue .* 0.8

const waterSegmentLeftMatrix = [
    1 1 1 0 1 0;
    1 1 1 0 1 0;
    1 1 1 0 1 0;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 1 0 1;
    1 1 1 0 1 0;
    1 1 1 0 1 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
    1 1 0 1 0 0;
]

const waterSegmentLeft = Ahorn.matrixToSurface(
    waterSegmentLeftMatrix,
    [
        fillColor,
        surfaceColor
    ]
)

const waterSegmentRightMatrix = [
    0 1 0 1 1 1;
    0 1 0 1 1 1;
    0 1 0 1 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 0 1 0 1 1;
    0 1 0 1 1 1;
    0 1 0 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
    1 0 1 1 1 1;
]

const waterSegmentRight = Ahorn.matrixToSurface(
    waterSegmentRightMatrix,
    [
        fillColor,
        surfaceColor
    ]
)

Ahorn.minimumSize(entity::ResizableWaterfall) = 16, 0

Ahorn.editingOrder(entity::ResizableWaterfall) = String["x", "y", "width", "color", "depth", "lines", "ignoreSolids"]

Ahorn.resizable(entity::ResizableWaterfall) = true, false

function  Ahorn.selection(entity::ResizableWaterfall, room::Maple.Room)
    entityHeightHackfix = entity.height
    entity.height = Int(getHeight(entity, room))
    rect = Ahorn.getEntityRectangle(entity)
    entity.height = entityHeightHackfix
    return rect
end

Ahorn.editingOptions(entity::ResizableWaterfall) = Dict{String, Any}(
    "layer" => String["FG", "BG"]
)

function getHeight(entity::ResizableWaterfall, room::Maple.Room)
    waterEntities = filter(e -> e.name == "water", room.entities)
    waterRects = Ahorn.Rectangle[
        Ahorn.Rectangle(
            Int(get(e.data, "x", 0)),
            Int(get(e.data, "y", 0)),
            Int(get(e.data, "width", 8)),
            Int(get(e.data, "height", 8))
        ) for e in waterEntities
    ]

    width, height = room.size
    x, y = Int(get(entity.data, "x", 0)), Int(get(entity.data, "y", 0))
    tx, ty = floor(Int, x / 8) + 1, floor(Int, y / 8) + 1

    wantedHeight = 8 - y % 8
    while wantedHeight < height - y
        rect = Ahorn.Rectangle(x, y + wantedHeight, 8, 8)

        if any(Ahorn.checkCollision.(waterRects, Ref(rect)))
            break
        end

        if get(room.fgTiles.data, (ty + 1, tx), '0') != '0'
            break
        end

        wantedHeight += 8
        ty += 1
    end

    return wantedHeight
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ResizableWaterfall, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 16))
    height = Int(getHeight(entity, room))

    segmentHeightLeft, segmentWidthLeft = size(waterSegmentLeftMatrix)
    segmentHeightRight, segmentWidthRight = size(waterSegmentRightMatrix)

    Ahorn.Cairo.save(ctx)

    Ahorn.rectangle(ctx, 0, 0, width, height)
    Ahorn.clip(ctx)

    for i in 0:segmentHeightLeft:ceil(Int, height / segmentHeightLeft) * segmentHeightLeft
        Ahorn.drawImage(ctx, waterSegmentLeft, 0, i)
        Ahorn.drawImage(ctx, waterSegmentRight, width - segmentWidthRight, i)
    end

    # Drawing a rectangle normally doesn't guarantee that its the same color as above
    if height >= 0 && width >= segmentWidthLeft + segmentWidthRight
        fillRectangle = Ahorn.matrixToSurface(fill(0, (height, width - segmentWidthLeft - segmentWidthRight)), [fillColor])
        Ahorn.drawImage(ctx, fillRectangle, segmentWidthLeft, 0)
    end
    
    Ahorn.restore(ctx)
end

end
