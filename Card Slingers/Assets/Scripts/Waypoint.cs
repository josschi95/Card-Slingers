using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour, IInteractable
{
    [SerializeField] private Waypoint _neighborWaypoint;
    [SerializeField] private DungeonRoom _room;

    public void SetRoom(DungeonRoom room)
    {
        _room = room;
    }

    public void SetConnectedWaypoint(Waypoint point)
    {
        _neighborWaypoint = point;
    }

    public void OnLeftClick()
    {
        PlayerController.SetWaypoint(this);
    }

    public void OnRightClick()
    {
        //Do nothing
    }

    public void OnWaypointReached(Waypoint fromWaypoint)
    {
        if (fromWaypoint == _neighborWaypoint)
        {
            _room.OnRoomEntered();
        }
        else
        {
            PlayerController.SetWaypoint(_neighborWaypoint);
        }
    }
}
