// Automatically Generated

using System.Diagnostics.CodeAnalysis;
using HKLib.hk2018;

namespace HKLib.Reflection.hk2018;

internal class hknpCompoundShapeInternalsCdDynamicTreeKeyMaskData : HavokData<hknpCompoundShapeInternalsCdDynamicTreeKeyMask> 
{
    public hknpCompoundShapeInternalsCdDynamicTreeKeyMaskData(HavokType type, hknpCompoundShapeInternalsCdDynamicTreeKeyMask instance) : base(type, instance) {}

    public override bool TryGetField<TGet>(string fieldName, [MaybeNull] out TGet value)
    {
        value = default;
        switch (fieldName)
        {
            case "m_shape":
            case "shape":
            {
                if (instance.m_shape is null)
                {
                    return true;
                }
                if (instance.m_shape is TGet castValue)
                {
                    value = castValue;
                    return true;
                }
                return false;
            }
            case "m_instanceMasks":
            case "instanceMasks":
            {
                if (instance.m_instanceMasks is not TGet castValue) return false;
                value = castValue;
                return true;
            }
            case "m_filter":
            case "filter":
            {
                if (instance.m_filter is not TGet castValue) return false;
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
            case "m_shape":
            case "shape":
            {
                if (value is null)
                {
                    instance.m_shape = default;
                    return true;
                }
                if (value is hknpCompoundShape castValue)
                {
                    instance.m_shape = castValue;
                    return true;
                }
                return false;
            }
            case "m_instanceMasks":
            case "instanceMasks":
            {
                if (value is not List<hknpShapeKeyMask?> castValue) return false;
                instance.m_instanceMasks = castValue;
                return true;
            }
            case "m_filter":
            case "filter":
            {
                if (value is not List<uint> castValue) return false;
                instance.m_filter = castValue;
                return true;
            }
            default:
            return false;
        }
    }

}
