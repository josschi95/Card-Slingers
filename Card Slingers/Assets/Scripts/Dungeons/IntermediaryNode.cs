using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntermediaryNode : PathNode
{
    [SerializeField] private PathNode _secondNeighbor;
    [SerializeField] private Transform _pointSecondary;
    public Transform PointTwo => _pointSecondary;

    public void SetAsIntermediate(PathNode primary, PathNode secondary)
    {
        _neighborNode = primary;
        _secondNeighbor = secondary;

        primary.ConnectedNode = this;
        secondary.ConnectedNode = this;

        //Debug.DrawLine(transform.position, _neighborNode.transform.position, Color.blue, int.MaxValue);
        //Debug.DrawLine(transform.position, _secondNeighbor.transform.position, Color.blue, int.MaxValue);
    }

    public override PathNode OnWaypointReached(PathNode fromWaypoint)
    {
        if (fromWaypoint == _neighborNode) return _secondNeighbor;
        else return _neighborNode;
    }
}
