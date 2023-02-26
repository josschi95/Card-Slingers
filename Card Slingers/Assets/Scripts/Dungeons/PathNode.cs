using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour, IInteractable
{
    [SerializeField] private Direction _direction;
    [SerializeField] private DungeonRoom _room;
    [SerializeField] protected Transform _point;
    [SerializeField] private ParticleSystem _hallwayFog;
    [Space]
    [SerializeField] protected PathNode _neighborNode;

    public Direction direction => _direction;
    public DungeonRoom Room => _room;
    public Transform Point => _point;
    public PathNode ConnectedNode
    {
        get => _neighborNode;
        set
        {
            if (value != null && _neighborNode != null && value != _neighborNode)
            {
                Debug.LogWarning("Changing neighbor node");
            }

            _neighborNode = value;
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
        //Outline highlight
    }

    private void OnMouseExit()
    {
        //Remove highlight
    }

    public virtual PathNode OnWaypointReached(PathNode fromWaypoint)
    {
        _hallwayFog.Stop();

        if (fromWaypoint == _neighborNode)
        {
            _room.OnRoomEntered(_direction);
            return null;
        }
        else return _neighborNode;
    }
}
