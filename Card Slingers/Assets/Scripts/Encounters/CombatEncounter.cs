using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Combat Encounter", menuName = "Scriptable Objects/Encounter")]
public class CombatEncounter : ScriptableObject
{
    //Probably make this a subclass of CombatEncounter later, CommanderCombatEncounter
    [SerializeField] private CommanderSO enemyCommander;
    [SerializeField] private Vector2Int _battlefieldDimensions;
    private OpponentCommander commander;

    public Vector2Int Dimensions => _battlefieldDimensions;

    public virtual void OnCombatTriggered()
    {
        //Meant to be overriden
        //Instantiate all of the enemies, and place them in their appropriate spots
    }
}
