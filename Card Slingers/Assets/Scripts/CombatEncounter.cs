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

    public OpponentCommander Commander => commander;
    public Vector2Int Dimensions => _battlefieldDimensions;

    private void OnCommanderCombatEncounter()
    {
        commander = Instantiate(enemyCommander.cardPrefab).GetComponent<OpponentCommander>();
        commander.OnAssignCommander(enemyCommander);
        commander.transform.position = PlayerController.instance.currentRoom.Transform.position;
        commander.transform.eulerAngles = PlayerController.instance.currentRoom.Orientation;

        commander.CommanderCard.OnCommanderSummon();
    }

    public void TriggerCombat()
    {
        if (enemyCommander != null) OnCommanderCombatEncounter();
    }

    public void OnCombatStart()
    {
        //This will instantiate all of the enemies, and place them in their appropriate spots
    }
}
