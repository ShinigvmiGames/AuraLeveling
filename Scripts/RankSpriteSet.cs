using UnityEngine;
[CreateAssetMenu(menuName = "AuraLeveling/Gates/Rank Sprite Set")]
public class RankSpriteSet : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public GateRank rank;
        public Sprite sprite;
    }

    public Entry[] entries;

    public Sprite Get(GateRank rank)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].rank == rank)
                return entries[i].sprite;
        return null;
    }
}