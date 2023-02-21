using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntermediaryNode : Waypoint
{
    [SerializeField] private Waypoint _secondNeighbor;

    public void SetAsIntermediate(Waypoint primary, Waypoint secondary)
    {
        _neighborNode = primary;
        _secondNeighbor = secondary;

        _neighborNode.SetConnectedWaypoint(this);
        _secondNeighbor.SetConnectedWaypoint(this);

        Debug.DrawLine(transform.position, _neighborNode.transform.position, Color.green, int.MaxValue);
        Debug.DrawLine(transform.position, _secondNeighbor.transform.position, Color.green, int.MaxValue);
    }

    public override void OnWaypointReached(Waypoint fromWaypoint)
    {
        if (fromWaypoint == _neighborNode) PlayerController.SetWaypoint(_secondNeighbor);
        else PlayerController.SetWaypoint(_neighborNode);
    }
}
