using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Commander Encounter", menuName = "Scriptable Objects/Encounters/Commander Encounter")]
public class CommanderEncounter : CombatEncounter
{
    [SerializeField] private CommanderSO enemyCommander;
    private OpponentCommander commander;
    public OpponentCommander Commander => commander;

    public override void OnCombatTriggered()
    {
        base.OnCombatTriggered();

        if (enemyCommander != null) OnCommanderCombatEncounter();
    }

    public void PlaceCommander()
    {

    }

    private void OnCommanderCombatEncounter()
    {
        commander = Instantiate(enemyCommander.cardPrefab).GetComponent<OpponentCommander>();
        commander.OnAssignCommander(enemyCommander);

        commander.transform.position = DuelManager.instance.Battlefield.Center.position;
        commander.transform.eulerAngles = DuelManager.instance.Battlefield.Center.eulerAngles;

        //commander.transform.eulerAngles = PlayerController.instance.currentRoom.Orientation;

        commander.CommanderCard.OnCommanderSummon();
    }
}
