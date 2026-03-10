using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "AuraLeveling/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyDefinition> allEnemies = new List<EnemyDefinition>();

    public List<EnemyDefinition> GetEnemiesForPool(EnemyPool pool, GateRank rank)
    {
        var result = new List<EnemyDefinition>();
        foreach (var e in allEnemies)
        {
            if (e == null) continue;
            if (e.pool == pool && rank >= e.minRank)
                result.Add(e);
        }
        return result;
    }

    public EnemyDefinition GetRandomEnemy(EnemyPool pool, GateRank rank)
    {
        var candidates = GetEnemiesForPool(pool, rank);
        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }
}
