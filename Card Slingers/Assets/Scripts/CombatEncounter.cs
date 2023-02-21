using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatEncounter : MonoBehaviour
{
    //Probably make this a subclass of CombatEncounter later, CommanderCombatEncounter
    [SerializeField] private CommanderSO enemyCommander;
    [SerializeField] private Vector2Int battlefieldDimensions;
    private OpponentCommander commander;
    private bool _hasBeenTriggered;

    public OpponentCommander Commander => commander;
    public Vector2Int Dimensions => battlefieldDimensions;

    private void OnCommanderCombatEncounter()
    {
        commander = Instantiate(enemyCommander.cardPrefab).GetComponent<OpponentCommander>();
        commander.OnAssignCommander(enemyCommander);
        commander.transform.position = transform.position;
        commander.transform.rotation = transform.rotation;

        commander.CommanderCard.OnCommanderSummon();
    }

    public void TriggerCombat()
    {
        if (_hasBeenTriggered) return;

        if (enemyCommander != null) OnCommanderCombatEncounter();
        DuelManager.instance.onMatchStarted?.Invoke(this);

        _hasBeenTriggered = true;
    }
}
