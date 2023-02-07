using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelManager : MonoBehaviour
{
    public static DuelManager instance;

    private void Awake()
    {
        instance = this;
    }

    public delegate void OnPhaseChange(CommanderSO commander);
    public OnPhaseChange onBeginPhase;
    public OnPhaseChange onSummoningPhase;
    public OnPhaseChange onPlanningPhase;
    public OnPhaseChange onResolutionPhase;
    public OnPhaseChange onEndPhase;

    [SerializeField] private FieldGrid<GridNode> grid;
    [SerializeField] private CommanderSO player, opponent;


    private void Start()
    {
        
    }

    private void OnAssignCommanders(CommanderSO player, CommanderSO opponent)
    {
        this.player = player;
        this.opponent = opponent;

        OnMatchStart();
    }

    private void OnMatchStart()
    {
        if (Random.value >= 0.5f) onBeginPhase?.Invoke(player);
        else onBeginPhase?.Invoke(opponent);
    }
}
