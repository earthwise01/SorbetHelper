-- based on https://github.com/CelestialCartographers/Loenn/blob/v1.0.9/src/ui/forms/fields/integer.lua

local utils = require("utils")
local stringField = require("ui.forms.fields.string")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local intSourceField = {}

intSourceField.fieldType = "sorbet_helper.int_source"

-- Any integers outside of this range are not safe to save
local largestInt = math.floor(2^31 - 1)
local smallestInt = math.floor(-2^31)

local function valueValidator(raw, value, allowEmpty, minimum, maximum)
    if raw == "" then
        return allowEmpty
    end

    if sorbetHelper.isCounterOrSessionExpression(value) then
        return true

    else
        local number = tonumber(value)
        return utils.isInteger(number) and number <= maximum and number >= minimum
    end
end

function intSourceField.getElement(name, value, options)
    local minimumValue = math.max(options.minimumValue or smallestInt, smallestInt)
    local maximumValue = math.min(options.maximumValue or largestInt, largestInt)
    local warningBelowValue = options.warningBelowValue or minimumValue
    local warningAboveValue = options.warningAboveValue or maximumValue
    local allowEmpty = options.allowEmpty or false

    options.valueTransformer = function(v)
        if sorbetHelper.isCounterOrSessionExpression(v) then
            return v
        end

        return tonumber(v)
    end
    options.displayTransformer = function(v)
        if type(v) == "number" or sorbetHelper.isCounterOrSessionExpression(v) then
            return tostring(v)
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

return intSourceField
