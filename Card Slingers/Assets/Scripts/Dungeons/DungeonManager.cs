using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager instance;
    private void Awake()
    {
        instance = this;
    }

    private bool _dungeonIsReady = false;
    public bool DungeonIsReady
    {
        get => _dungeonIsReady;
        set
        {
            _dungeonIsReady = value;
        }
    }

    [SerializeField] private DungeonGenerator _generator;

    [Space]

    [SerializeField] private DungeonSize _dungeonSize;

    [Space]

    [SerializeField] private List<CombatEncounter> _encounters;

    private void Start()
    {
        DuelManager.instance.onMatchStarted += WatchMatch;
    }

    public void CreateDungeon(int floor)
    {
        _dungeonIsReady = false;
        Debug.Log("Floor set to " + floor);
        Debug.Log("Floor not setting anything yet.");
        _generator.BeginGeneration(_dungeonSize);

    }

    public void SetEncounters(List<CombatEncounter> encounters)
    {
        _encounters = new List<CombatEncounter>(encounters);
    }

    private void WatchMatch(CombatEncounter encounter)
    {
        //Wait until player victory/defeat
        DuelManager.instance.onPlayerVictory += delegate { OnMatchFinished(encounter); };
        //if criteria has been met for defeating the dungoen, allow the player to leave without penalty
    }

    private void OnMatchFinished(CombatEncounter encounter)
    {
        DuelManager.instance.onPlayerVictory -= delegate { OnMatchFinished(encounter); };

        _encounters.Remove(encounter);
        if (_encounters.Count == 0) Debug.Log("All Encounters complete! Return to town.");
    }

    //Criteria to leave
    // 1. Defeat all
    // 2. Defeat most
    // 3. Defeat boss
}
