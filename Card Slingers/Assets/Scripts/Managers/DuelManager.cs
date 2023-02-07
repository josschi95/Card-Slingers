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

    [SerializeField] private Battlefield battleField;
    [SerializeField] private CommanderSO player, opponent;


    private void Start()
    {
        Invoke("PlaceCommanders", 0.01f);
    }

    private void PlaceCommanders()
    {
        float depth = battleField.Grid.GetDepth();
        int playerZ = Mathf.CeilToInt(depth * 0.5f) - 1;
        var playerPrefab = Instantiate(player.CommanderPrefab, battleField.Grid.GetCell(0, playerZ).transform.position, Quaternion.identity);

        int opponentZ = Mathf.RoundToInt(depth * 0.5f);
        var enemyPrefab = Instantiate(opponent.CommanderPrefab, battleField.Grid.GetCell(battleField.Grid.GetWidth() - 1, opponentZ).transform.position, Quaternion.identity);
    }

    private void OnAssignCommanders(CommanderSO player, CommanderSO opponent)
    {
        this.player = player;
        this.opponent = opponent;

        PlaceCommanders();
        OnMatchStart();
    }

    private void OnMatchStart()
    {
        if (Random.value >= 0.5f) onBeginPhase?.Invoke(player);
        else onBeginPhase?.Invoke(opponent);
    }
}
