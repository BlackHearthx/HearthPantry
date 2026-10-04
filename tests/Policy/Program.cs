using System;
using HearthPantry;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
Check(PantryPolicy.ScaleRate(1f, 4f) == .25f, "4x days preserve quarter countdown rate");
Check(PantryPolicy.ScaleRate(2f, 4f) == .5f, "world food rate composes with TimeControl");
foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
    Check(PantryPolicy.ScaleRate(1f, invalid) == 1f, "invalid multiplier fallback");
float remaining = 1800f;
for (int second = 0; second < 3960; second++) remaining -= PantryPolicy.ScaleRate(1f, 4f);
Check(Math.Abs(remaining / 1800f * 100f - 45f) < .01f, "45 percent renewal at 45 percent of extended lifetime");
Check(PantryPolicy.ScaleRate(1f, .5f) == 2f, "shorter days speed countdown");
float weakLong = PantryPolicy.Score(10, 10, 2400, 1, 0, 50, 50, 50, 50, 50);
float strong = PantryPolicy.Score(100, 100, 1800, 5, 0, 50, 50, 50, 50, 50);
Check(strong > weakLong, "long weak meals do not dominate balanced meals");
Check(PantryPolicy.Score(10, 10, 1800, 1, 100, 0, 0, 0, 0, 100) == 100f, "magic preference selects eitr");
Check(PantryPolicy.Score(10, 10, 1800, 1, 100, 0, 0, 0, 0, 0) == 0f, "eitr can be disabled");
Console.WriteLine($"Passed {checks} regression checks.");
