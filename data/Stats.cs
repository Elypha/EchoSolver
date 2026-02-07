using System.Runtime.CompilerServices;

namespace EchoSolver.data;

public enum SubStatType : byte
{
    None = 0,
    ATK = 1,
    HP = 2,
    DEF = 3,
    ATK_Percent = 4,
    HP_Percent = 5,
    DEF_Percent = 6,
    EnergyRegen = 7,
    CritRate = 8,
    CritDamage = 9,
    BasicAttackBonus = 10,
    HeavyAttackBonus = 11,
    ResonanceSkillBonus = 12,
    ResonanceLiberationBonus = 13,
}

public static class SubStatValueRNG
{
    // use 10000 as 100.00%

    // CDF

    // 7%, 52%, 38%, 3%
    private static readonly int[] ProbabilityFlatATK = [700, 5900, 9700, 10000];
    // 14%, 45%, 38%, 3%
    private static readonly int[] ProbabilityFlatDEF = [1400, 5900, 9700, 10000];
    // 6.5%, 7.5%, 20.0%, 25.0%, 17.0%, 15.0%, 6.0%, 3.0%
    private static readonly int[] ProbabilityStandard = [650, 1400, 3400, 5900, 7600, 9100, 9700, 10000];
    // 23.6%, 23.6%, 23.6%, 8.0%, 8.0%, 8.0%, 2.6%, 2.6%
    private static readonly int[] ProbabilityCrit = [2360, 4720, 7080, 7880, 8680, 9480, 9740, 10000];

    // Values

    // [(int)SubStatType][value]
    private static readonly float[][] ValueLookup = new float[14][];
    // 0=FlatATK, 1=FlatDEF, 2=Standard, 3=Crit
    private static readonly byte[] ProbGroupMap = new byte[14];


    static SubStatValueRNG()
    {
        // Flat ATK (30, 40, 50, 60)
        ValueLookup[(int)SubStatType.ATK] = [30f, 40f, 50f, 60f];
        ProbGroupMap[(int)SubStatType.ATK] = 0;

        // Flat DEF (40, 50, 60, 70)
        ValueLookup[(int)SubStatType.DEF] = [40f, 50f, 60f, 70f];
        ProbGroupMap[(int)SubStatType.DEF] = 1;

        // Flat HP
        ValueLookup[(int)SubStatType.HP] = [320f, 360f, 390f, 430f, 470f, 510f, 540f, 580f];
        ProbGroupMap[(int)SubStatType.HP] = 2;

        // Standard Percentages
        float[] standardPercentValues = [6.4f, 7.1f, 7.9f, 8.6f, 9.4f, 10.1f, 10.9f, 11.6f];
        var standardTypes = new[] {
            SubStatType.ATK_Percent,
            SubStatType.HP_Percent,
            SubStatType.BasicAttackBonus,
            SubStatType.HeavyAttackBonus,
            SubStatType.ResonanceSkillBonus,
            SubStatType.ResonanceLiberationBonus,
        };
        foreach (var t in standardTypes)
        {
            ValueLookup[(int)t] = standardPercentValues;
            ProbGroupMap[(int)t] = 2;
        }

        // DEF Percent
        ValueLookup[(int)SubStatType.DEF_Percent] = [8.1f, 9.0f, 10.0f, 10.9f, 11.8f, 12.8f, 13.8f, 14.7f];
        ProbGroupMap[(int)SubStatType.DEF_Percent] = 2;

        // Energy Regen
        ValueLookup[(int)SubStatType.EnergyRegen] = [6.8f, 7.6f, 8.4f, 9.2f, 10.0f, 10.8f, 11.6f, 12.4f];
        ProbGroupMap[(int)SubStatType.EnergyRegen] = 2;

        // Crit Rate
        ValueLookup[(int)SubStatType.CritRate] = [6.3f, 6.9f, 7.5f, 8.1f, 8.7f, 9.3f, 9.9f, 10.5f];
        ProbGroupMap[(int)SubStatType.CritRate] = 3;

        // Crit Damage
        ValueLookup[(int)SubStatType.CritDamage] = [12.6f, 13.8f, 15.0f, 16.2f, 17.4f, 18.6f, 19.8f, 21.0f];
        ProbGroupMap[(int)SubStatType.CritDamage] = 3;
    }


    /// <summary>
    /// Given a SubStatType, roll a random value for it based on its probability distribution.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetRandomValueFor(SubStatType type)
    {
        int typeIndex = (int)type;
        int probGroup = ProbGroupMap[typeIndex];

        int[] cdf = probGroup switch
        {
            0 => ProbabilityFlatATK,
            1 => ProbabilityFlatDEF,
            2 => ProbabilityStandard,
            3 => ProbabilityCrit,
            _ => ProbabilityStandard
        };

        // get random 0~9999
        int roll = Random.Shared.Next(10000);

        int valueIndex = 0;
        for (int i = 0; i < cdf.Length; i++)
        {
            if (roll < cdf[i])
            {
                valueIndex = i;
                break;
            }
        }

        return ValueLookup[typeIndex][valueIndex];
    }
}


public static class SubStatTypeRNG
{
    // Cache all SubStatType values to avoid reflection every time
    private static readonly SubStatType[] _allTypes;

    static SubStatTypeRNG()
    {
        // Get all SubStatType values except None
        var values = Enum.GetValues<SubStatType>();
        _allTypes = new SubStatType[values.Length - 1];
        int idx = 0;
        foreach (var v in values)
        {
            if (v != SubStatType.None) _allTypes[idx++] = v;
        }
    }

    /// <summary>
    /// Given the existing stats on the echo, roll a new SubStatType that is not already present.
    /// </summary>
    /// <param name="existingStats">Current list of substats already present on the echo</param>
    /// <param name="currentCount">Current number of substats present</param>
    public static SubStatType RollNextStatType(SubStatType[] existingStats, int currentCount)
    {
        // NOTE:
        // For high performance, we do not create a new List, but use a Swap algorithm or simple rejection sampling.
        // Since there are only 13 total substats and at most 5 existing ones, rejection sampling is efficient enough.

        while (true)
        {
            var candidate = _allTypes[Random.Shared.Next(_allTypes.Length)];

            bool exists = false;
            for (int i = 0; i < currentCount; i++)
            {
                if (existingStats[i] == candidate)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists) return candidate;
        }
    }
}
