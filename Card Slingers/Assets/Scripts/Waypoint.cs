using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour, IInteractable
{
    [SerializeField] private Waypoint _neighborWaypoint;
    [SerializeField] private DungeonRoom _neighborRoom;

    public void SetRoom(DungeonRoom room)
    {
        _neighborRoom = room;
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
            _neighborRoom.OnRoomEntered();
        }
        else
        {
            PlayerController.SetWaypoint(_neighborWaypoint);
        }
    }
}
