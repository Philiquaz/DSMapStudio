// Automatically Generated

using System.Diagnostics.CodeAnalysis;
using HKLib.hk2018;
using HKLib.hk2018.hkaiWorldCommands;

namespace HKLib.Reflection.hk2018;

internal class hkaiWorldCommandsAddDynamicUserEdgeSetupsData : HavokData<AddDynamicUserEdgeSetups> 
{
    public hkaiWorldCommandsAddDynamicUserEdgeSetupsData(HavokType type, AddDynamicUserEdgeSetups instance) : base(type, instance) {}

    public override bool TryGetField<TGet>(string fieldName, [MaybeNull] out TGet value)
    {
        value = default;
        switch (fieldName)
        {
            case "m_sizePaddedTo16":
            case "sizePaddedTo16":
            {
                if (instance.m_sizePaddedTo16 is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_filterBits":
            case "filterBits":
            {
                if (instance.m_filterBits is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_primaryType":
            case "primaryType":
            {
                if (instance.m_primaryType is TGet castValue)
                {
                    value = castValue;
                    return true;
                }
                if ((byte)instance.m_primaryType is TGet byteValue)
                {
                    value = byteValue;
                    return true;
                }
                return false;
            }
            case "m_secondaryType":
            case "secondaryType":
            {
                if (instance.m_secondaryType is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_identifier":
            case "identifier":
            {
                if (instance.m_identifier is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_setups":
            case "setups":
            {
                if (instance.m_setups is null)
                {
                    return true;
                }
                if (instance.m_setups is TGet castValue)
                {
                    value = castValue;
                    return true;
                }
                return false;
            }
            default:
            return false;
        }
    }

    public override bool TrySetField<TSet>(string fieldName, TSet value)
    {
        switch (fieldName)
        {
            case "m_sizePaddedTo16":
            case "sizePaddedTo16":
            {
                if (value is not ushort castValue) return false;
                instance.m_sizePaddedTo16 = castValue;
                return true;
            }
            case "m_filterBits":
            case "filterBits":
            {
                if (value is not byte castValue) return false;
                instance.m_filterBits = castValue;
                return true;
            }
            case "m_primaryType":
            case "primaryType":
            {
                if (value is hkCommand.PrimaryType castValue)
                {
                    instance.m_primaryType = castValue;
                    return true;
                }
                if (value is byte byteValue)
                {
                    instance.m_primaryType = (hkCommand.PrimaryType)byteValue;
                    return true;
                }
                return false;
            }
            case "m_secondaryType":
            case "secondaryType":
            {
                if (value is not ushort castValue) return false;
                instance.m_secondaryType = castValue;
                return true;
            }
            case "m_identifier":
            case "identifier":
            {
                if (value is not hkHandle<uint> castValue) return false;
                instance.m_identifier = castValue;
                return true;
            }
            case "m_setups":
            case "setups":
            {
                if (value is null)
                {
                    instance.m_setups = default;
                    return true;
                }
                if (value is hkaiReferencedArray<hkaiUserEdgeUtils.UserEdgeSetup> castValue)
                {
                    instance.m_setups = castValue;
                    return true;
                }
                return false;
            }
            default:
            return false;
        }
    }

}
