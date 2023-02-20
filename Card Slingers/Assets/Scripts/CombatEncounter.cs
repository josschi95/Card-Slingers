using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatEncounter : MonoBehaviour
{
    //Probably make this a subclass of CombatEncounter later, CommanderCombatEncounter
    [SerializeField] private Battlefield battlefield;
    [SerializeField] private CommanderSO enemyCommander;
    private OpponentCommander commander;

    private void Start()
    {
        if (enemyCommander != null) OnCommanderCombatEncounter();
    }

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
        DuelManager.instance.OnNewMatchStart(battlefield, commander);
    }
}
