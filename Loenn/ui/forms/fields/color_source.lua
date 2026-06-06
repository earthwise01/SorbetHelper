-- based on https://github.com/CelestialCartographers/Loenn/blob/v1.0.9/src/ui/forms/fields/color.lua

local ui = require("ui")
local uiElements = require("ui.elements")
local uiUtils = require("ui.utils")

local contextMenu = require("ui.context_menu")
local utils = require("utils")
local colorPicker = require("ui.widgets.color_picker")
local configs = require("configs")
local xnaColors = require("consts.xna_colors")
local utf8 = require("utf8")

local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local colorSourceField = {}

colorSourceField.fieldType = "sorbet_helper.color_source"

colorSourceField._MT = {}
colorSourceField._MT.__index = {}

local fallbackHexColor = "ffffff"

local invalidStyle = {
    normalBorder = {0.65, 0.2, 0.2, 0.9, 2.0},
    focusedBorder = {0.9, 0.2, 0.2, 1.0, 2.0}
}

local function fixNumberColor(value)
    -- make sure colors stored as numbers are formatted correctly (makes e.g. 0 -> "000000")
    if type(value) == "number" then
        return string.format("%06d", value)
    end

    return value
end

function colorSourceField._MT.__index:setValue(value)
    self.currentValue = fixNumberColor(value) or fallbackHexColor
    self.field:setText(self.currentValue)
    self.field.index = utf8.len(self.currentValue)
end

function colorSourceField._MT.__index:getValue()
    return self.currentValue or fallbackHexColor
end

function colorSourceField._MT.__index:fieldValid(...)
    local current = self:getValue()

    if current == nil or #current == 0 then
        return self._allowEmpty
    end

    local counterOrExpression = sorbetHelper.isCounterOrSessionExpression(current)
    local colorParsed = utils.parseHexColor(current)
    return counterOrExpression or colorParsed
end

local function getValueOrEmptyFallback(element, value)
    if (value == nil or #value == 0) and element._allowEmpty then
        return fallbackHexColor
    end

    return value
end

local function updateFieldPreview(element, new)
    local counterOrExpression = sorbetHelper.isCounterOrSessionExpression(new)
    local colorParsed, r, g, b = utils.parseHexColor(getValueOrEmptyFallback(element, new))

    element._counterOrExpression = counterOrExpression
    element._colorParsed = colorParsed
    element._r, element._g, element._b = r, g, b

    return counterOrExpression, colorParsed, r, g, b
end

local function getFieldChangedFunction(formField)
    return function(element, new, old)
        local counterOrExpression, colorParsed = updateFieldPreview(element, new)
        local wasValid = formField:fieldValid()
        local valid = counterOrExpression or colorParsed

        formField.currentValue = new

        if wasValid ~= valid then
            if valid then
                formField.field.style = nil

            else
                formField.field.style = invalidStyle
            end

            formField.field:repaint()
        end

        formField:notifyFieldChanged()
    end
end

local function getColorPreviewArea(element)
    local x, y = element.screenX, element.screenY
    local width, height = element.width, element.height
    local padding = element.style:get("padding") or 0
    local previewSize = height - padding * 2
    local drawX, drawY = x + width - previewSize - padding, y + padding

    return drawX, drawY, previewSize, previewSize
end

local function drawColorPreview(element)
    if not element or element._counterOrExpression then
        return
    end

    local colorParsed = element._colorParsed
    local r, g, b, a = element._r or 0, element._g or 0, element._b or 0, colorParsed and 1 or 0
    local pr, pg, pb, pa = love.graphics.getColor()

    local drawX, drawY, width, height = getColorPreviewArea(element)

    love.graphics.setColor(0, 0, 0)
    love.graphics.rectangle("fill",  drawX, drawY, width, height)
    love.graphics.setColor(1, 1, 1)
    love.graphics.rectangle("fill",  drawX + 1, drawY + 1, width - 2, height - 2)
    love.graphics.setColor(r, g, b, a)
    love.graphics.rectangle("fill",  drawX + 2, drawY + 2, width - 4, height - 4)
    love.graphics.setColor(pr, pg, pb, pa)
end

local function shouldShowMenu(element, x, y, button)
    local menuButton = configs.editor.contextMenuButton
    local actionButton = configs.editor.toolActionButton

    if button == menuButton then
        return true

    elseif button == actionButton then
        local drawX, drawY, width, height = getColorPreviewArea(element)

        return utils.aabbCheckInline(x, y, 1, 1, drawX, drawY, width, height)
    end

    return false
end

function colorSourceField.getElement(name, value, options)
    local formField = {}

    value = fixNumberColor(value)

    local minWidth = options.minWidth or options.width or 160
    local maxWidth = options.maxWidth or options.width or 160
    local allowEmpty = options.allowEmpty

    local label = uiElements.label(options.displayName or name)
    local field = uiElements.field(value or fallbackHexColor, getFieldChangedFunction(formField)):with({
        minWidth = minWidth,
        maxWidth = maxWidth,
        _allowEmpty = allowEmpty
    }):hook({
        draw = function(orig, element)
            orig(element)
            drawColorPreview(element)
        end
    })
    local fieldWithContext = contextMenu.addContextMenu(
        field,
        function()
            local text = field:getText() or ""

            if sorbetHelper.isCounterOrSessionExpression(text) then
                return nil
            end

            local pickerOptions = {
                callback = function(data)
                    field:setText(data.hexColor)
                    field.index = utf8.len(data.hexColor)
                end,
                showAlpha = options.showAlpha or options.useAlpha,
                showHex = options.showHex,
                showHSV = options.showHSV,
                showRGB = options.showRGB,
            }

            return colorPicker.getColorPicker(text, pickerOptions)
        end,
        {
            shouldShowMenu = shouldShowMenu,
            mode = "focused"
        }
    )

    updateFieldPreview(field, value or "")
    field:setPlaceholder(value)

    if options.tooltipText then
        label.interactive = 1
        label.tooltipText = options.tooltipText
    end

    label.centerVertically = true

    formField.label = label
    formField.field = field
    formField.name = name
    formField.initialValue = value
    formField.currentValue = value
    formField._allowEmpty = allowEmpty
    formField.width = 2
    formField.elements = {
        label, fieldWithContext
    }

    return setmetatable(formField, colorSourceField._MT)
end

return colorSourceField
