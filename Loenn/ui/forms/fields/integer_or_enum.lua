local stringField = require("ui.forms.fields.string")
local utils = require("utils")

local integerOrEnumField = {}

integerOrEnumField.fieldType = "sorbet_helper.integer_or_enum"

-- any integers outside of this range are not safe to save
local largestInt = math.floor(2^31 - 1)
local smallestInt = math.floor(-2^31)

-- not sure why this didn't seem to like being set directly in the field information ?
local function valueValidator(raw, value, enum, allowEmpty, minimum, maximum)
    if raw == "" then
        return allowEmpty
    end

    if enum[value] ~= nil then
        return true
    end

    local number = tonumber(value)
    return utils.isInteger(number) and number <= maximum and number >= minimum
end

function integerOrEnumField.getElement(name, value, options)
    -- add extra options and pass it onto string field

    local minimumValue = math.max(options.minimumValue or smallestInt, smallestInt)
    local maximumValue = math.min(options.maximumValue or largestInt, largestInt)
    local warningBelowValue = options.warningBelowValue or minimumValue
    local warningAboveValue = options.warningAboveValue or maximumValue
    local allowEmpty = options.allowEmpty or false
    local enum = options.enum or { }

    options.displayTransformer = function(v)
        if v == nil or (type(v) ~= "number" and enum[v] == nil) then
            return ""
        end

        return tostring(v)
    end
    options.warningValidator = function(v, raw)
        return valueValidator(raw, v, enum, allowEmpty, warningBelowValue, warningAboveValue)
    end
    options.validator = function(v, raw)
        return valueValidator(raw, v, enum, allowEmpty, minimumValue, maximumValue)
    end

    return stringField.getElement(name, value, options)
end

return integerOrEnumField
