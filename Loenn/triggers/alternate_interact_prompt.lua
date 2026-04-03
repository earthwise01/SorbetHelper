local alternateInteractPromptTrigger = {}

local defaultPrompts = {
    { "(None)", ""},
    { "Talk", "sorbethelper_ui_talk" },
    { "Use", "sorbethelper_ui_use" },
    { "Pet", "sorbethelper_ui_pet" },
    { "Check", "sorbethelper_ui_check" },
    { "Inspect", "sorbethelper_ui_inspect" },
    { "Toggle", "sorbethelper_ui_toggle" }
}

local styles = {
    { "Bottom Corner", "BottomCorner" },
    { "Small Arrow", "SmallArrow" },
    { "Vanilla", "Vanilla" }
}

-- this reallyy should've been an entity but weh
alternateInteractPromptTrigger.name = "SorbetHelper/AlternateInteractPrompt"
alternateInteractPromptTrigger.placements = {
    {
        name = "alternate_interact_prompt_bottom_corner",
        alternativeName = "alt_style_interact_prompt",
        data = {
            dialogId = "",
            style = "BottomCorner",
            playHighlightSfx = true,
            onLeft = false,
            useUpInput = false
        }
    },
    {
        name = "alternate_interact_prompt_small_arrow",
        alternativeName = "alt_style_interact_prompt",
        data = {
            dialogId = "",
            style = "SmallArrow",
            playHighlightSfx = true,
            onLeft = false,
            useUpInput = false
        }
    },
    {
        name = "alternate_interact_prompt_vanilla_use_up_input",
        alternativeName = "alt_style_interact_prompt",
        data = {
            dialogId = "",
            style = "Vanilla",
            playHighlightSfx = true,
            onLeft = false,
            useUpInput = true
        }
    }
}

alternateInteractPromptTrigger.fieldOrder = {
    "x", "y",
    "width", "height",
    "style", "dialogId",
    "useUpInput", "playHighlightSfx", "onLeft"
}

alternateInteractPromptTrigger.fieldInformation = {
    dialogId = {
        options = defaultPrompts
    },
    style = {
        options = styles,
        editable = false
    }
}

return alternateInteractPromptTrigger
