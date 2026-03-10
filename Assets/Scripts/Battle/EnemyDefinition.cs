using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "AuraLeveling/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    public string enemyName;
    public Sprite portrait;
    public PlayerClass enemyClass;
    public WeaponType weaponType;
    public EnemyPool pool;
    public GateRank minRank;
}
