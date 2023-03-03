using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Down the line, I can possibly make some Scriptable Objects for each dungeon which will house the room/hallway prefabs 
//As well as the encounters, and then I only need one dungeon scene which will read the dungeon type and then spawn the correct 
//items from there. Obviously I will still need separate scenes for the pre-built boss dungeons, but that's minor. 

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

    [SerializeField] private DungeonPresets[] _dungeonPresets;

    [SerializeField] private DungeonGenerator _generator;

    [Space]

    [SerializeField] private DungeonSize _dungeonSize; //For Testing Only

    [Space]

    [SerializeField] 
    private List<CombatEncounter> _encounters;

    public bool AllEncountersComplete()
    {
        if (_encounters.Count == 0) return true;
        return false;
    }

    private void Start()
    {
        DuelManager.instance.onMatchStarted += OnEncounterInitiated;
    }

    public void CreateDungeon(Dungeons dungeon, int floor)
    {
        _dungeonIsReady = false;
        //Change this to take in the level and set number of rooms and combats accordingly
        _generator.BeginGeneration(_dungeonPresets[(int)dungeon], _dungeonSize);
    }

    //FOR TESTING ONLY
    /*public void CreateDungeon(int floor)
    {
        _dungeonIsReady = false;
        Debug.Log("Floor set to " + floor);
        _generator.BeginGeneration(_dungeonSize);
        //Change this to take in the level and set number of rooms and combats accordingly
    }*/

    public void SetEncounters(List<CombatEncounter> encounters)
    {
        _encounters = new List<CombatEncounter>(encounters);
    }

    private void OnEncounterInitiated(CombatEncounter encounter)
    {
        _encounters.Remove(encounter); //There is currently no "fleeing" from combat
    }

    //Criteria to leave
    // 1. Defeat all
    // 2. Defeat most
    // 3. Defeat boss
}
