// Automatically Generated

using System.Diagnostics.CodeAnalysis;
using HKLib.hk2018;

namespace HKLib.Reflection.hk2018;

internal class hkbBehaviorGraphStringDataData : HavokData<hkbBehaviorGraphStringData> 
{
    public hkbBehaviorGraphStringDataData(HavokType type, hkbBehaviorGraphStringData instance) : base(type, instance) {}

    public override bool TryGetField<TGet>(string fieldName, [MaybeNull] out TGet value)
    {
        value = default;
        switch (fieldName)
        {
            case "m_propertyBag":
            case "propertyBag":
            {
                if (instance.m_propertyBag is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_eventNames":
            case "eventNames":
            {
                if (instance.m_eventNames is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_attributeNames":
            case "attributeNames":
            {
                if (instance.m_attributeNames is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_variableNames":
            case "variableNames":
            {
                if (instance.m_variableNames is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_characterPropertyNames":
            case "characterPropertyNames":
            {
                if (instance.m_characterPropertyNames is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_animationNames":
            case "animationNames":
            {
                if (instance.m_animationNames is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            default:
            return false;
        }
    }

    public override bool TrySetField<TSet>(string fieldName, TSet value)
    {
        switch (fieldName)
        {
            case "m_propertyBag":
            case "propertyBag":
            {
                if (value is not hkPropertyBag castValue) return false;
                instance.m_propertyBag = castValue;
                return true;
            }
            case "m_eventNames":
            case "eventNames":
            {
                if (value is not List<string?> castValue) return false;
                instance.m_eventNames = castValue;
                return true;
            }
            case "m_attributeNames":
            case "attributeNames":
            {
                if (value is not List<string?> castValue) return false;
                instance.m_attributeNames = castValue;
                return true;
            }
            case "m_variableNames":
            case "variableNames":
            {
                if (value is not List<string?> castValue) return false;
                instance.m_variableNames = castValue;
                return true;
            }
            case "m_characterPropertyNames":
            case "characterPropertyNames":
            {
                if (value is not List<string?> castValue) return false;
                instance.m_characterPropertyNames = castValue;
                return true;
            }
            case "m_animationNames":
            case "animationNames":
            {
                if (value is not List<string?> castValue) return false;
                instance.m_animationNames = castValue;
                return true;
            }
            default:
            return false;
        }
    }

}
