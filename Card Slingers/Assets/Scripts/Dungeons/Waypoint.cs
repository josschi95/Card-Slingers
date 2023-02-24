using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour, IInteractable
{
    [SerializeField] private Direction _direction;
    [SerializeField] protected Transform _point;
    [SerializeField] protected Waypoint _neighborNode;
    [SerializeField] private DungeonRoom _room;
    [SerializeField] private ParticleSystem _hallwayFog;

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
        if (point != null && _neighborNode != null && point != _neighborNode)
        {
            Debug.LogWarning("Changing neighbor node");
            Debug.DrawLine(transform.position, _neighborNode.transform.position, Color.red, int.MaxValue);
        }
        
        if (point == null) Debug.LogWarning("nulling neighbor node at " + transform.position);

        _neighborNode = point;
        if (_neighborNode != null)
        {
            //Debug.DrawLine(transform.position, _neighborNode.transform.position, Color.green, int.MaxValue);
        }
       
    }

    public void OnLeftClick()
    {
        PlayerController.SetWaypoint(this);
    }

    public void OnRightClick()
    {
        //Do nothing
    }

    private void OnMouseEnter()
    {
        
    }

    private void OnMouseExit()
    {
        
    }

    public virtual Waypoint OnWaypointReached(Waypoint fromWaypoint)
    {
        _hallwayFog.Stop();

        if (fromWaypoint == _neighborNode)
        {
            _room.OnRoomEntered(_direction);
            return null;
        }
        else return _neighborNode;
    }

    private void OnDestroy()
    {
        //if (_neighborNode != null) _neighborNode.SetConnectedWaypoint(null);
    }
}
