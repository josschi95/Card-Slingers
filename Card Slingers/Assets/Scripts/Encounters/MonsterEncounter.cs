using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Monster Encounter", menuName = "Scriptable Objects/Encounters/Monster Encounter")]
public class MonsterEncounter : CombatEncounter
{
    [SerializeField] private UnitSO[] _monsterPool;
    public UnitSO[] MonsterPool => _monsterPool;
}
