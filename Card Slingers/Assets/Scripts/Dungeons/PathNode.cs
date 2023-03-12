using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private Direction _direction;
    [SerializeField] private DungeonRoom _room;
    [SerializeField] protected Transform _point;
    protected PathNode _neighborNode;

    public Transform Transform
    {
        get
        {
            if (_transform == null)
            {
                _transform = transform;
            }
            return _transform;
        }
    }

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
}
