local ui = require("ui")
local uiElements = require("ui.elements")
local uiUtils = require("ui.utils")

local booleanField = {}

booleanField.fieldType = "sorbetHelper.boolean_with_padding"

booleanField._MT = {}
booleanField._MT.__index = {}

function booleanField._MT.__index:setValue(value)
    self.currentValue = value
end

function booleanField._MT.__index:getValue()
    return self.currentValue
end

function booleanField._MT.__index:fieldValid()
    return type(self:getValue()) == "boolean"
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

-- kinda janky maybe but it gets the job done ig
function booleanField.getElement(name, value, options)
    local formField = {}

    local minWidth = options.minWidth or options.width or 160
    local maxWidth = options.maxWidth or options.width or 160

    local width = options.paddingWidth or 1
    local left = options.padLeft or false

    local checkbox = uiElements.checkbox(options.displayName or name, value, fieldChanged(formField))

    if options.tooltipText then
        checkbox.interactive = 1
        checkbox.tooltipText = options.tooltipText
    end

    checkbox.centerVertically = true

    local elements = {}

    if not left then table.insert(elements, checkbox) end

    local label = uiElements.label("")
    for i = 1, width do
        table.insert(elements, label)
    end

    if left then table.insert(elements, checkbox) end

    formField.checkbox = checkbox
    formField.name = name
    formField.initialValue = value
    formField.currentValue = value
    formField.sortingPriority = 10
    formField.width = 1 + width
    formField.elements = elements

    return setmetatable(formField, booleanField._MT)
end

return booleanField