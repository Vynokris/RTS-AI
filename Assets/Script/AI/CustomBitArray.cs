using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomBitArray
{
    public int value { get; private set; }

    public static bool operator == (CustomBitArray first, CustomBitArray second)
    {
        return first.value == second.value;
    }

    public static bool operator != (CustomBitArray first, CustomBitArray second)
    {
        return !(first == second);
    }

    public bool this[int index]
    {
        get => (value & (1 << index)) != 0;

        set
        {
            if (value) {
                this.value |= (1 << index);
            }
            else {
                this.value &= ~(1 << index);
            }
        }
    }

    public CustomBitArray And(CustomBitArray preconditionsState)
    {
        CustomBitArray newCustomBitArray = new CustomBitArray();
        newCustomBitArray.value = value & preconditionsState.value;

        return newCustomBitArray;
    }

    public CustomBitArray Or(CustomBitArray preconditionsState)
    {
        CustomBitArray newCustomBitArray = new CustomBitArray();
        newCustomBitArray.value = value | preconditionsState.value;

        return newCustomBitArray;
    }

    public override bool Equals(object obj)
    {
        if (obj is CustomBitArray otherWrapper)
        {
            return value == otherWrapper.value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}
