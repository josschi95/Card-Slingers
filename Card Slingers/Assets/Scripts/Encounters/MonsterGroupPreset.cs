using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Monster Group Preset", menuName = "Scriptable Objects/Group Preset/Monster Preset")]
public class MonsterGroupPreset : EnemyGroupPreset
{
    [SerializeField] private UnitSO[] _monsterPool;
    public UnitSO[] MonsterPool => _monsterPool;
}
