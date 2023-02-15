using UnityEngine;

public abstract class StateBase : MonoBehaviour
{
    public abstract void OnStateEnter(OpponentCommander commander);
    public abstract void OnStateUpdate(OpponentCommander commander);
    public abstract void OnStateExit(OpponentCommander commander);
}
