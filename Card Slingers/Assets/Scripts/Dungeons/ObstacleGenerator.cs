using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    [SerializeField] private Obstacle[] _obstacles;

    public void GenerateObstacles(List<DungeonRoom> rooms)
    {
        for (int i = 1; i < rooms.Count; i++) //Skip the first room
        {
            //50% chance of no obstacles in room
            //if (Random.value > 0.65f) continue;

            int numObstacles = Random.Range(1, rooms[i].BoardDimensions.x);

            for (int o = 0; o < numObstacles; o++)
            {
                //Grab a random location within the board excluding the outside rim
                int x = Random.Range(1, rooms[i].BoardDimensions.x - 1);
                int z = Random.Range(1, rooms[i].BoardDimensions.y - 1);

                //Need to find some way I'm not doubling up on the same node

                //Ths should initially be set to the center of the [0,0] node on the board
                var obstaclePos = rooms[i].Transform.position;
                obstaclePos.x -= Mathf.RoundToInt(rooms[i].BoardDimensions.x * 0.5f) * 5f - 2.5f;
                obstaclePos.z -= Mathf.RoundToInt(rooms[i].BoardDimensions.y * 0.5f) * 5f - 2.5f;

                obstaclePos.x += 5 * x;
                obstaclePos.z += 5 * z;

                var obs = Instantiate(_obstacles[Random.Range(1, _obstacles.Length)], obstaclePos, Quaternion.identity, rooms[i].Transform);
                rooms[i].AddObjstacle(obs);
            }
        }
    }
}
