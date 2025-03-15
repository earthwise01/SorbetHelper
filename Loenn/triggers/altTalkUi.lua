local trigger = {}

trigger.name = "SorbetHelper/AlternateInteractPrompt"
trigger.placements = {
    name = "altTalkUI",
    alternativeName = "altNameAltTalkUI",
    data = {
        dialogId = "sorbethelper_ui_talk",
        onLeft = false,
        playHighlightSfx = false
    }
}

trigger.fieldInformation = {
    dialogId = {
        -- debatable how useful most of these are but  i feel like theyre pretty general enough n its good to have presets so ppl wont have to duplicate the same thing everywhere
        options = {
            { "Talk", "sorbethelper_ui_talk" },
            { "Use", "sorbethelper_ui_use" },
            { "Pet", "sorbethelper_ui_pet" },
            { "Check", "sorbethelper_ui_check" },
            { "Inspect", "sorbethelper_ui_inspect" },
            { "Toggle", "sorbethelper_ui_toggle" },
        }
    },
}

return trigger