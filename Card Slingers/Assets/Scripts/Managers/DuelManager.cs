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

    public delegate void OnNodeSelected(GridNode node);
    public OnNodeSelected onNodeSelected;

    [SerializeField] private Battlefield battleField;
    [SerializeField] private CommanderSO _player, _opponent;

    public CommanderController playerController, opponentController;

    public CommanderSO Player => _player;
    public CommanderSO Opponent => _opponent;


    private void Start()
    {
        OnAssignCommanders(_player, _opponent);
    }

    private void PlaceCommanders()
    {
        float depth = battleField.Depth;

        int playerZ = Mathf.CeilToInt(depth * 0.5f) - 1;
        var playerPerm = battleField.PlacePermanent(0, playerZ, _player.CommanderPrefab, true);
        playerController = playerPerm.GetComponent<CommanderController>();

        int opponentZ = Mathf.RoundToInt(depth * 0.5f);
        var opponentPerm = battleField.PlacePermanent(battleField.Width - 1, opponentZ, _opponent.CommanderPrefab, false);
        opponentController = opponentPerm.GetComponent<CommanderController>();
    }

    private void OnAssignCommanders(CommanderSO player, CommanderSO opponent)
    {
        _player = player;
        _opponent = opponent;

        PlaceCommanders();
        OnMatchStart();
    }

    private void OnMatchStart()
    {
        if (Random.value >= 0.5f) onBeginPhase?.Invoke(_player);
        else onBeginPhase?.Invoke(_opponent);
    }
}
