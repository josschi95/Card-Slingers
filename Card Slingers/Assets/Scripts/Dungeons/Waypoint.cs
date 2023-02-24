using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour, IInteractable
{
    [SerializeField] private Direction _direction;
    [SerializeField] protected Transform _point;
    [SerializeField] protected Waypoint _neighborNode;
    [SerializeField] private DungeonRoom _room;

    public Direction direction => _direction;
    public DungeonRoom Room => _room;
    public Waypoint ConnectedNode => _neighborNode;
    public Transform Point => _point;

    public void SetRoom(DungeonRoom room)
    {
        _room = room;
    }

    public void SetConnectedWaypoint(Waypoint point)
    {
        _neighborNode = point;
        //Debug.DrawLine(transform.position, _neighborNode.transform.position, Color.green, int.MaxValue);

    }

    public void OnLeftClick()
    {
        PlayerController.SetWaypoint(this);
    }

    public void OnRightClick()
    {
        //Do nothing
    }

    public virtual void OnWaypointReached(Waypoint fromWaypoint)
    {
        if (fromWaypoint == _neighborNode)
        {
            _room.OnRoomEntered();
        }
        else
        {
            PlayerController.SetWaypoint(_neighborNode);
        }
    }

    private void OnDestroy()
    {
        if (_neighborNode != null) _neighborNode.SetConnectedWaypoint(null);
    }
}
