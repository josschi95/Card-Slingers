using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [SerializeField] private Waypoint _startingWaypoint;

    [SerializeField] private List<CombatEncounter> encounters = new List<CombatEncounter>();

    private void Start()
    {
        DuelManager.instance.onMatchStarted += WatchMatch;

        GameObject.Find("PlayerController").GetComponent<PlayerController>().SetStartingWaypoint(_startingWaypoint);
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

        encounters.Remove(encounter);
        if (encounters.Count == 0) Debug.Log("All Encounters complete! Return to town.");
    }

    //Criteria to leave
    // 1. Defeat all
    // 2. Defeat most
    // 3. Defeat boss
}
