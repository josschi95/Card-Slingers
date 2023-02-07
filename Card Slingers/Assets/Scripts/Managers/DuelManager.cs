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

    public CommanderController player, opponent;

    public CommanderSO Player => _player;
    public CommanderSO Opponent => _opponent;


    private void Start()
    {
        Invoke("PlaceCommanders", 0.01f);
    }

    private void PlaceCommanders()
    {
        float depth = battleField.Depth;
        int playerZ = Mathf.CeilToInt(depth * 0.5f) - 1;
        var playerPrefab = Instantiate(_player.CommanderPrefab, battleField.GetCell(0, playerZ).transform.position, Quaternion.identity);
        battleField.PlacePermanent(0, playerZ, playerPrefab.GetComponent<Permanent>());

        int opponentZ = Mathf.RoundToInt(depth * 0.5f);
        var enemyPrefab = Instantiate(_opponent.CommanderPrefab, battleField.GetCell(battleField.Width - 1, opponentZ).transform.position, Quaternion.identity);
        battleField.PlacePermanent(0, opponentZ, enemyPrefab.GetComponent<Permanent>());
    }

    private void OnAssignCommanders(CommanderSO player, CommanderSO opponent)
    {
        this._player = player;
        this._opponent = opponent;

        PlaceCommanders();
        OnMatchStart();
    }

    private void OnMatchStart()
    {
        if (Random.value >= 0.5f) onBeginPhase?.Invoke(_player);
        else onBeginPhase?.Invoke(_opponent);
    }
}
