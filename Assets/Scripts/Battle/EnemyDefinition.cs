using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "AuraLeveling/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    public string enemyName;
    public Sprite portrait;
    public PlayerClass enemyClass;
    public WeaponType weaponType;
    public EnemyPool pool;

    [Header("Gate Rank Range (inclusive)")]
    [Tooltip("Lowest rank this enemy can appear in")]
    public GateRank minRank = GateRank.ERank;

    [Tooltip("Highest rank this enemy can appear in")]
    public GateRank maxRank = GateRank.SRank;
}
