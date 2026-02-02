local depths = {}

function depths.getDepths()
    local list = {
        {"BG Terrain (10000)", 10000},
        {"BG Mirrors (9500)", 9500},
        {"BG Decals (9000)", 9000},
        {"BG Particles (8000)", 8000},
        {"Solids Below (5000)", 5000},
        {"Below (2000)", 2000},
        {"NPCs (1000)", 1000},
        {"Theo Crystal (100)", 100},
        {"Player (0)", 0},
        {"Dust (-50)", -50},
        {"Pickups (-100)", -100},
        {"Seeker (-200)", -200},
        {"Particles (-8000)", -8000},
        {"Above (-8500)", -8500},
        {"Solids (-9000)", -9000},
        {"FG Terrain (-10000)", -10000},
        {"FG Decals (-10500)", -10500},
        {"Dream Blocks (-11000)", -11000},
        {"Crystal Spinners (-11500)", -11500},
        {"Player Dream Dashing (-12000)", -12000},
        {"Enemy (-12500)", -12500},
        {"Fake Walls (-13000)", -13000},
        {"FG Particles (-50000)", -50000},
        {"Top (-1000000)", -1000000},
        {"Formation Sequences (-2000000)", -2000000}
    }

    return list
end

function depths.addDepths(list, toAdd)
    for i,p in ipairs(toAdd) do
        depths.addDepth(list, p[1], p[2])
    end

    return list
end

function depths.addDepth(list, name, depth)
    name = name .. " (" .. depth .. ")"

    for i,p in ipairs(list) do
        -- rename if depth already exists in the list
        if p[2] == depth then
            list[i][1] = name
            return list
        end

        -- otherwise insert before the first lower depth in the list
        if p[2] < depth then
            table.insert(list, i, {name, depth})
            return list
        end
    end

    -- if the list was empty or the new depth would be the lowest depth, insert it at the end of the list
    table.insert(list, {name, depth})
    return list
end

return depths
