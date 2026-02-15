using UnityEngine;
[CreateAssetMenu(menuName = "AuraLeveling/Gates/Gate Sprite Set")]
public class GateSpriteSet : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public GateRank rank;
        public Sprite gateSprite;         // der runde Gate-Kreis
        public Sprite frameNormalSprite;  // normaler Frame
        public Sprite frameSRankSprite;   // spezieller S-Frame
    }

    public Entry[] entries;

    public bool TryGet(GateRank rank, out Entry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].rank == rank)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }
}