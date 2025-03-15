local ui = require("ui")
local uiElements = require("ui.elements")
local uiUtils = require("ui.utils")
local contextMenu = require("ui.context_menu")
local grid = require("ui.widgets.grid")

local buttonField = {}

buttonField.fieldType = "sorbetHelper.infoButton"

buttonField._MT = {}
buttonField._MT.__index = {}

-- these all do nothing since this field isn't intended for actually storing data
function buttonField._MT.__index:setValue(value)
    -- self.currentValue = value
end

function buttonField._MT.__index:getValue()
    return true
end

function buttonField._MT.__index:fieldValid()
    return true
end

local function fieldChanged(formField)
    return function(element, new)
        local old = formField.currentValue

        formField.currentValue = new

        if formField.currentValue ~= old then
            formField:notifyFieldChanged()
        end
    end
end

function buttonField.getElement(name, value, options)
    local formField = {}

    local minWidth = options.minWidth or options.width or 160
    local maxWidth = options.maxWidth or options.width or 160

    local button = uiElements.button(options.displayName or name, function () end)
    -- button.tooltipText = "Click button to open menu."

    local description = options.tooltipText or "Error! No description found!"

    local buttonWithContext = contextMenu.addContextMenu(
        button,
        function ()
            return grid.getGrid({uiElements.label(description)}, 1)
        end,
        {
            shouldShowMenu = function (self, x, y, button, istouch) return true end,
            mode = "focused"
        }
    )

    formField.button = buttonWithContext
    formField.name = name
    formField.initialValue = value
    formField.currentValue = value
    formField.sortingPriority = 10
    formField.width = 4
    formField.elements = {
        buttonWithContext
    }

    return setmetatable(formField, buttonField._MT)
end

return buttonField