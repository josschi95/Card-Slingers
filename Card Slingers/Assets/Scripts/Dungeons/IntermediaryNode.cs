using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntermediaryNode : Waypoint
{
    [SerializeField] private Waypoint _secondNeighbor;
    [SerializeField] private Transform _pointSecondary;
    public Transform PointTwo => _pointSecondary;

    public void SetAsIntermediate(Waypoint primary, Waypoint secondary)
    {
        _neighborNode = primary;
        _secondNeighbor = secondary;

        primary.SetConnectedWaypoint(this);
        secondary.SetConnectedWaypoint(this);

        //Debug.DrawLine(transform.position, _neighborNode.transform.position, Color.blue, int.MaxValue);
        //Debug.DrawLine(transform.position, _secondNeighbor.transform.position, Color.blue, int.MaxValue);
    }

    public override Waypoint OnWaypointReached(Waypoint fromWaypoint)
    {
        if (fromWaypoint == _neighborNode) return _secondNeighbor;
        else return _neighborNode;
    }
}
