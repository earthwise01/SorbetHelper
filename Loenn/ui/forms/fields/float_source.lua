-- based on https://github.com/CelestialCartographers/Loenn/blob/v1.0.9/src/ui/forms/fields/number.lua

local utils = require("utils")
local stringField = require("ui.forms.fields.string")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local floatSourceField = {}

floatSourceField.fieldType = "sorbet_helper.float_source"

local function valueValidator(raw, value, allowEmpty, minimum, maximum)
    if raw == "" then
        return allowEmpty
    end

    if sorbetHelper.isSliderOrSessionExpression(value) then
        return true

    else
        local number = tonumber(value)
        return number ~= nil and number <= maximum and number >= minimum
    end
end

function floatSourceField.getElement(name, value, options)
    local minimumValue = options.minimumValue or -math.huge
    local maximumValue = options.maximumValue or math.huge
    local warningBelowValue = options.warningBelowValue or minimumValue
    local warningAboveValue = options.warningAboveValue or maximumValue
    local allowEmpty = options.allowEmpty or false

    options.valueTransformer = function(v)
        if sorbetHelper.isSliderOrSessionExpression(v) then
            return v
        end

        return tonumber(v)
    end
    options.displayTransformer = function(v)
        if type(v) == "number" then
            return utils.prettifyFloat(v)

        elseif sorbetHelper.isSliderOrSessionExpression(v) then
            return v
        end

        return ""
    end
    options.warningValidator = function(v, raw)
        return valueValidator(raw, v, allowEmpty, warningBelowValue, warningAboveValue)
    end
    options.validator = function(v, raw)
        return valueValidator(raw, v, allowEmpty, minimumValue, maximumValue)
    end

    return stringField.getElement(name, value, options)
end

return floatSourceField