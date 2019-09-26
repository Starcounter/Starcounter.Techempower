using System;

namespace Starcounter.Techempower
{
    public interface IRandom
    {
        int Next(int minValue, int maxValue);
    }
}
