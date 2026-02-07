namespace EchoSolver.data;

public class Echo
{
    private SubStatType[] _subStats = new SubStatType[5];
    private float[] _subStatValues = new float[5];
    private int _unlockedCount = 0;

    public int ExpConsumed = 0;
    public int TunersConsumed = 0;

    private static readonly int[] ExpCostTable = [4400, 12100, 23100, 39500, 63500];

    public void Reset()
    {
        Array.Clear(_subStats, 0, _subStats.Length);
        Array.Clear(_subStatValues, 0, _subStatValues.Length);
        _unlockedCount = 0;
        ExpConsumed = 0;
        TunersConsumed = 0;
    }

    public void Tune()
    {
        LevelUp();
        UnlockNextSlot();
    }

    public bool HasStat(SubStatType type, float minValue = 0)
    {
        for (int i = 0; i < _unlockedCount; i++)
        {
            if (_subStats[i] == type && _subStatValues[i] >= minValue)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasStat(SubStatType[] types, float[] minValues)
    {
        for (int i = 0; i < _unlockedCount; i++)
        {
            for (int j = 0; j < types.Length; j++)
            {
                if (_subStats[i] == types[j] && _subStatValues[i] >= minValues[j])
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void LevelUp()
    {
        ExpConsumed += ExpCostTable[_unlockedCount];
    }

    private void UnlockNextSlot()
    {
        var newStatType = SubStatTypeRNG.RollNextStatType(_subStats, _unlockedCount);
        var newStatValue = SubStatValueRNG.GetRandomValueFor(newStatType);
        _subStats[_unlockedCount] = newStatType;
        _subStatValues[_unlockedCount] = newStatValue;

        TunersConsumed += 10;
        _unlockedCount++;
    }

}
