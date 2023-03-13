using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    public void GenerateObstacles(Obstacle[] obstacles, List<DungeonRoom> rooms)
    {
        Physics.SyncTransforms();

        for (int i = 1; i < rooms.Count; i++) //Skip the first room
        {
            //50% chance of no obstacles in room
            if (Random.value > 0.65f) continue;

            int numObstacles = Random.Range(1, rooms[i].BoardDimensions.x);
            var availableNodes = BattlefieldManager.instance.GetNodesInRoom(rooms[i]);

            for (int o = 0; o < numObstacles; o++)
            {
                var node = GetValidNode(availableNodes);
                if (node == null) break;

                var obs = Instantiate(obstacles[Random.Range(1, obstacles.Length)], node.Transform.position, Quaternion.identity, rooms[i].Transform);
                obs.OnOccupyNode(node);
            }
        }
    }

    private GridNode GetValidNode(List<GridNode> nodes)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].Obstacle != null) nodes.RemoveAt(i);
            else if (nodes[i].Occupant != null) nodes.RemoveAt(i);
        }

        if (nodes.Count == 0) return null;

        return nodes[Random.Range(0, nodes.Count)];
    }
}
