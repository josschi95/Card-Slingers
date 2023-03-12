using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Commander Encounter", menuName = "Scriptable Objects/Encounters/Commander Encounter")]
public class CommanderEncounterPreset : EnemyGroupPreset
{
    [SerializeField] private CommanderSO _commanderSO;
    public CommanderSO Commander => _commanderSO;

    private OpponentCommander commander;

    [SerializeField] private UnitSO[] _initialSummons;
    public UnitSO[] InitialSummons => _initialSummons;
}
