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

        internal static float Score(float health, float stamina, float duration, float regen, float eitr,
            int healthWeight, int staminaWeight, int durationWeight, int regenWeight, int eitrWeight)
        {
            // Comparable reference portions: 100 health/stamina/eitr, 30 minutes, 5 regen.
            return health / 100f * healthWeight + stamina / 100f * staminaWeight
                + duration / 1800f * durationWeight + regen / 5f * regenWeight
                + eitr / 100f * eitrWeight;
        }
    }
}
