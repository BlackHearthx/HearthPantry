using System;

namespace HearthPantry
{
    internal static class PantryPolicy
    {
        internal static float ValidMultiplier(float multiplier)
        {
            return float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f
                ? 1f : multiplier;
        }

        internal static float ScaleRate(float rate, float multiplier)
        {
            return rate / ValidMultiplier(multiplier);
        }

    }
}
