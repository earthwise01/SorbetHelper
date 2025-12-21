local trigger = {}

trigger.name = "SorbetHelper/AlternateInteractPrompt"
trigger.placements = {
    {
        name = "altTalkUIBottomCorner",
        alternativeName = "altNameAltTalkUI",
        data = {
            dialogId = "",
            style = "BottomCorner",
            playHighlightSfx = true,
            onLeft = false,
        }
    },
    {
        name = "altTalkUISmallArrow",
        alternativeName = "altNameAltTalkUI",
        data = {
            dialogId = "",
            style = "SmallArrow",
            playHighlightSfx = true,
            onLeft = false,
        }
    }
}

trigger.fieldInformation = {
    dialogId = {
        -- debatable how useful most of these are but  i feel like theyre pretty general enough n its good to have presets so ppl wont have to duplicate the same thing everywhere
        options = {
            { "(None)", ""},
            { "Talk", "sorbethelper_ui_talk" },
            { "Use", "sorbethelper_ui_use" },
            { "Pet", "sorbethelper_ui_pet" },
            { "Check", "sorbethelper_ui_check" },
            { "Inspect", "sorbethelper_ui_inspect" },
            { "Toggle", "sorbethelper_ui_toggle" },
        }
    },
    style = {
        options = {
            { "Bottom Corner", "BottomCorner" },
            { "Small Arrow", "SmallArrow" },
        },
        editable = false
    }
}

function trigger.ignoredFields(entity)
    local ignored = { "_id", "_name" }
    
    if entity.style ~= "BottomCorner" then
        table.insert(ignored, "onLeft")
    end

    return ignored
end

return trigger