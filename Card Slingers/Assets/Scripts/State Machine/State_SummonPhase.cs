using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State_SummonPhase : StateBase
{
    public override void OnStateEnter(OpponentCommander commander)
    {
        //Find invaded lanes
    }

    public override void OnStateUpdate(OpponentCommander commander)
    {

    }

    public override void OnStateExit(OpponentCommander commander)
    {
        DuelManager.instance.OnCurrentPhaseFinished();
    }
}
