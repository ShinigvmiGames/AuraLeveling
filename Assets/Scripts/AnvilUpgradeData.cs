[System.Serializable]
public class AnvilUpgradeData
{
    public int level;
    public int costGold;
    public float durationSeconds;

    /// <summary>
    /// Creates upgrade data for a given level using AnvilSystem's formula.
    /// </summary>
    public static AnvilUpgradeData ForLevel(int level)
    {
        float duration = AnvilSystem.GetUpgradeDurationSeconds(level);
        // Gold cost scales with duration: ~10 gold per minute, rounded nicely
        int gold = UnityEngine.Mathf.RoundToInt(duration / 6f);
        // Round to nearest 5 for clean numbers
        gold = UnityEngine.Mathf.Max(10, (gold / 5) * 5);

        return new AnvilUpgradeData
        {
            level = level,
            costGold = gold,
            durationSeconds = duration
        };
    }
}
