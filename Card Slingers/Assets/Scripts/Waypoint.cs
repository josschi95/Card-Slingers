using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour, IInteractable
{
    [SerializeField] private CombatEncounter encounter;

    public void OnLeftClick()
    {
        PlayerController.SetWaypoint(this);
    }

    public void OnRightClick()
    {
        //Do nothing
    }

    public void OnWaypointReached()
    {
        if (encounter != null) encounter.TriggerCombat();

    }
}
