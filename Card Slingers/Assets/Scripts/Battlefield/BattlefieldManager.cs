using System.Collections.Generic;
using UnityEngine;

public class BattlefieldManager : MonoBehaviour
{
    public static BattlefieldManager instance;
    private void Awake()
    {
        instance = this;
    }

    private const float CELL_SIZE = 5f;

    private GridNode[,] gridArray;
    private Vector2Int _dimensions;
    [Space]
    [SerializeField] private Transform _center;
    [SerializeField] private Transform _cameraHome;
    [SerializeField] private GridNode node; //Move this to being pooled

    public Transform Center => _center;
    private Vector3 _origin; //The center of the [0,0] node on the grid

    #region - Properties -
    public int Width => _dimensions.x; //These are currently only being used for early testing
    public int Depth => _dimensions.y;
    public float CellSize => CELL_SIZE;
    #endregion

    #region - Grid -
    public void CreateGrid(Vector3 origin, Vector2Int dimensions)
    {
        Physics.SyncTransforms();

        _dimensions = dimensions;
        _origin = origin;

        gridArray = new GridNode[Width, Depth];
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                var nodePos = GetGridPosition(x, z);

                if (!Physics.CheckSphere(nodePos, 0.5f)) continue;

                var go = Instantiate(node, nodePos, Quaternion.identity, Center);

                gridArray[x, z] = go;
                gridArray[x, z].OnAssignCoordinates(x, z);
            }
        }
    }

    public GridNode GetNode(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < Width && z < Depth)
        {
            return gridArray[x, z];
        }
        return null;

        //throw new System.Exception("parameter " + x + "," + z + " outside bounds of array");
    }

    public GridNode GetNode(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(((Mathf.Abs(worldPosition.x - _origin.x)) + 2.5f) / CELL_SIZE);
        int z = Mathf.FloorToInt(((Mathf.Abs(worldPosition.z - _origin.z)) + 2.5f) / CELL_SIZE);
        return GetNode(x, z);
    }

    public GridNode GetNode(float worldXPos, float worldZPos)
    {
        int x = Mathf.FloorToInt(((Mathf.Abs(worldXPos - _origin.x)) + 2.5f) / CELL_SIZE);
        int z = Mathf.FloorToInt(((Mathf.Abs(worldZPos - _origin.z)) + 2.5f) / CELL_SIZE);
        return GetNode(x, z);
    }

    private Vector3 GetLocalGridPosition(int x, int z)
    {
        return new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
    }

    public Vector3 GetGridPosition(int x, int z)
    {
        return _origin + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
    }

    public List<GridNode> GetAllNodesInArea(GridNode originNode, int range)
    {
        var nodeList = new List<GridNode>();

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                var vertical = Mathf.Abs(originNode.gridZ - z);
                var horizontal = Mathf.Abs(originNode.gridX - x);
                if (vertical + horizontal > range) continue;
                var node = GetNode(x, z);
                if (node == null) continue;
                nodeList.Add(node);
            }
        }
        return nodeList;
    }

    public List<GridNode> GetSummonableNodes(GridNode originNode, int range)
    {
        var nodeList = new List<GridNode>();

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                var vertical = Mathf.Abs(originNode.gridZ - z);
                var horizontal = Mathf.Abs(originNode.gridX - x);
                if (vertical + horizontal > range) continue;

                var node = GetNode(x, z);

                if (node == null) continue;
                if (node.Occupant != null || node.Obstacle != null) continue;
                nodeList.Add(node);
            }
        }
        return nodeList;
    }

    public List<GridNode> GetControlledNodesInLane(bool isPlayer, int lane)
    {
        var tempList = new List<GridNode>();
        int halfDepth = Mathf.RoundToInt(Depth * 0.5f);
        float half = Depth * 0.5f;

        if (isPlayer)
        {
            //check all nodes starting from 0, going to halfway point
            for (int i = 0; i < halfDepth; i++)
            {
                var occupant = gridArray[lane, i].Occupant;
                if (occupant != null && !occupant.isPlayerCard) break;
                else tempList.Add(gridArray[lane, i]);
            }
        }
        else
        {
            //check all nodes starting from furthest node going backward to half point
            for (int i = Depth - 1; i >= halfDepth; i--)
            {
                var occupant = gridArray[lane, i].Occupant;
                if (occupant != null && occupant.isPlayerCard) break;
                else tempList.Add(gridArray[lane, i]);
            }
        }

        return tempList;
    }

    public List<GridNode> GetNodesInRoom(DungeonRoom room, bool excludePerimeter = false, bool excludeEntrances = true)
    {
        var nodeList = new List<GridNode>();
        //Debug.Log("Center: " + room.transform.position);
        var origin = room.Transform.position;
        origin.x -= (room.RoomDimensions.x - CELL_SIZE) * 0.5f;
        origin.z -= (room.RoomDimensions.y - CELL_SIZE) * 0.5f;
        //Debug.Log("Origin: " + origin);

        for (int x = 0; x < room.BoardDimensions.x; x++)
        {
            for (int z = 0; z < room.BoardDimensions.y; z++)
            {
                if (excludePerimeter) //Don't include the outside ring of nodes
                {
                    if (x == 0 || z == 0) continue;
                    if (x == room.BoardDimensions.x - 1 || z == room.BoardDimensions.y - 1) continue; //break?
                }

                if (excludeEntrances) //Don't include the nodes in the outside ring in the middle of that side
                {
                    int middleX = Mathf.CeilToInt(room.BoardDimensions.x);
                    int middleZ = Mathf.CeilToInt(room.BoardDimensions.y);

                    if (z == middleZ && x == 0) continue;
                    else if (z == middleZ && x == room.BoardDimensions.x - 1) continue;
                    else if (x == middleX && z == 0) continue;
                    else if (x == middleX && z == room.BoardDimensions.y - 1) continue;
                }

                nodeList.Add(GetNode(new Vector3(origin.x + x * CELL_SIZE, 0, origin.z + z * CELL_SIZE)));
            }
        }

        return nodeList;
    }

    /// <summary>
    /// Returns a list of unoccupied lane nodes on the commander's side of the field.
    /// </summary>
    public List<GridNode> GetOpenNodesInLane(bool isPlayer, int lane)
    {
        var tempList = GetControlledNodesInLane(isPlayer, lane);

        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            if (tempList[i].Occupant != null) tempList.RemoveAt(i);
        }

        return tempList;
    }
    #endregion

    #region - Pathfinding -
    private const int MOVE_STRAIGHT_COST = 1;

    private List<GridNode> openList; //nodes to search
    private List<GridNode> closedList; //already searched

    //Returns a list of nodes that can be travelled to reach a target destination
    public List<GridNode> FindNodePath(Card_Unit unit, GridNode endNode, bool ignoreEndNode = false, bool stopInRange = false)
    {
        GridNode startNode = unit.Node;

        openList = new List<GridNode> { startNode };
        closedList = new List<GridNode>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Depth; y++)
            {
                GridNode pathNode = GetNode(x, y);
                if (pathNode == null) continue;
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.cameFromNode = null;
            }
        }

        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            GridNode currentNode = GetLowestFCostNode(openList);

            if (stopInRange && GetDistanceInNodes(currentNode, endNode) <= unit.Range && currentNode.CanBeOccupied())
            {
                //Don't need to move any further
                //Debug.Log("Found closest node within range, located at " + currentNode.gridX + "," + currentNode.gridZ);
                return CalculatePath(currentNode);
            }

            //Reached final node
            if (currentNode == endNode) return CalculatePath(endNode);

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (GridNode neighbour in GetNeighbourList(currentNode))
            {
                if (closedList.Contains(neighbour)) continue;

                //if the neighbor is the endNode and choosing to ignore whether it is walkable, add it to the closed list
                if (neighbour == endNode && ignoreEndNode)
                {
                    //Do nothing here, bypass the next if statement
                    //Debug.Log("Ignoring End Node");
                }
                else if (!neighbour.CanBeTraversed(unit))
                {
                    //Debug.Log("Removing unwalkable/occupied tile " + neighbour.x + "," + neighbour.y);
                    closedList.Add(neighbour);
                    continue;
                }

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbour);

                if (tentativeGCost < neighbour.gCost)
                {
                    //If it's lower than the cost previously stored on the neightbor, update it
                    neighbour.cameFromNode = currentNode;
                    neighbour.gCost = tentativeGCost;
                    neighbour.hCost = CalculateDistanceCost(neighbour, endNode);
                    neighbour.CalculateFCost();

                    if (!openList.Contains(neighbour)) openList.Add(neighbour);
                }
            }
        }

        //Out of nodes on the openList
        //Debug.Log("Path could not be found from " + unit.Node.gridX + "," + unit.Node.gridZ + " to " + endNode.gridX + "," + endNode.gridZ);
        return null;
    }

    //Return a list of nodes which can be reached given the available number of moves
    public List<GridNode> FindReachableNodes(Card_Unit unit)
    {
        List<GridNode> nodes = new List<GridNode>();
        GridNode startNode = unit.Node;

        //Get all nodes in the grid within range that can be occupied
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Depth; y++)
            {
                GridNode pathNode = GetNode(x, y);
                if (pathNode == null) continue;

                if (pathNode.CanBeTraversed(unit) && pathNode.CanBeOccupied() && GetDistanceInNodes(startNode, pathNode) <= unit.MovesLeft + 1)
                {
                    nodes.Add(pathNode);
                    //Debug.Log(pathNode.x + "," + pathNode.y);
                }
            }
        }
        
        //Check that there is a valid path from unit's node to that node
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            var temp = FindNodePath(unit, nodes[i]);

            //There is no available path to that node
            if (temp == null) nodes.RemoveAt(i);
            //The path requires more moves than are available
            else if (temp.Count > unit.MovesLeft + 1) nodes.RemoveAt(i);
        }

        return nodes;
    }

    public ReachableNodes FindNodesWithinReach(Card_Unit unit)
    {
        List<GridNode> walkNodes = new List<GridNode>();
        List<GridNode> attackNodes = new List<GridNode>();
        GridNode startNode = unit.Node;

        //Get all nodes in the grid within range that can be occupied
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Depth; y++)
            {
                GridNode pathNode = GetNode(x, y);
                if (pathNode == null) continue;

                var dist = GetDistanceInNodes(startNode, pathNode);
                if (dist <= unit.MovesLeft + unit.Range && pathNode.CanBeAttacked(unit)) attackNodes.Add(pathNode);
                if (dist <= unit.MovesLeft && pathNode.CanBeOccupied()) walkNodes.Add(pathNode);
            }
        }


        return new ReachableNodes(walkNodes, attackNodes);
    }

    //Grab all nodes within speed + range distance, and if there is a target there, add it to the list
    public List<GridNode> FindTargetableNodes(Card_Unit unit, int range)
    {
        List<GridNode> nodes = new List<GridNode>();
        GridNode startNode = unit.Node;

        //Get all nodes in the grid
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Depth; y++)
            {
                GridNode pathNode = GetNode(x, y);
                if (pathNode == null) continue;
                //Add all walkable nodes which can be accessed 
                if (pathNode.CanBeAttacked(unit) && GetDistanceInNodes(startNode, pathNode) <= range + unit.MovesLeft)
                {
                    nodes.Add(pathNode);
                    //Debug.Log(pathNode.gridX + "," + pathNode.gridZ + " can be attacked");
                }
                else
                {
                    //Debug.Log(pathNode.gridX + "," + pathNode.gridZ + " cannot be attacked");
                }
            }
        }

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (GetDistanceInNodes(unit.Node, nodes[i]) <= range) continue;

            //Find a node path to that node, ignoring the final node, and only needing to move within range
            var path = FindNodePath(unit, nodes[i], true, true);

            //There is no available path to that node
            if (path == null) nodes.RemoveAt(i);

            //The path requires more moves than are available
            else if (path.Count > unit.MovesLeft + 1) nodes.RemoveAt(i); //+1 because it counts the unit's occupied node as the first node

            //The node path has at least two nodes in it - Checking this to not throw an error for the next check
            //The final node in the path can be occupied
            else if (path.Count >= 2 && !path[path.Count - 1].CanBeOccupied())
            {
                //Ok this is where I'm running into an issue
                //Debug.Log("Cannot occupy node needed to move to");
                nodes.RemoveAt(i);
            }
        }

        return nodes;
    }

    private List<GridNode> CalculatePath(GridNode endNode)
    {
        List<GridNode> path = new List<GridNode>();
        path.Add(endNode);
        GridNode currentNode = endNode;
        while (currentNode.cameFromNode != null)
        {
            //Start at the end and work backwards
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }
        path.Reverse();
        return path;
    }

    private int CalculateDistanceCost(GridNode a, GridNode b)
    {
        int xDistance = Mathf.Abs(a.gridX - b.gridX);
        int zDistance = Mathf.Abs(a.gridZ - b.gridZ);
        int remaining = Mathf.Abs(xDistance - zDistance);
        return MOVE_STRAIGHT_COST * remaining;
    }

    public int GetDistanceInNodes(GridNode fromNode, GridNode toNode)
    {
        if (fromNode == null) throw new UnityException("FromNode is null!");
        if (toNode == null) throw new UnityException("ToNode is null!");
        var vertical = Mathf.Abs(fromNode.gridZ - toNode.gridZ);
        var horizontal = Mathf.Abs(fromNode.gridX - toNode.gridX);
        return vertical + horizontal;
    }

    private GridNode GetLowestFCostNode(List<GridNode> pathNodeList)
    {
        GridNode lowestFCostNode = pathNodeList[0];

        for (int i = 0; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].fCost < lowestFCostNode.fCost)
                lowestFCostNode = pathNodeList[i];
        }

        return lowestFCostNode;
    }

    //Return a list of all neighbors, up/down/left/right
    private List<GridNode> GetNeighbourList(GridNode currentNode)
    {
        List<GridNode> neighborList = new List<GridNode>();

        //Up
        if (currentNode.gridZ + 1 < Depth)
        {
            var neighbor = GetNode(currentNode.gridX, currentNode.gridZ + 1);
            if (neighbor != null) neighborList.Add(neighbor);
        }
        //Down
        if (currentNode.gridZ - 1 >= 0)
        {
            var neighbor = GetNode(currentNode.gridX, currentNode.gridZ - 1);
            if (neighbor != null) neighborList.Add(neighbor);
        }
        //Left
        if (currentNode.gridX - 1 >= 0)
        {
            var neighbor = GetNode(currentNode.gridX - 1, currentNode.gridZ);
            if (neighbor != null) neighborList.Add(neighbor);
        }
        //Right
        if (currentNode.gridX + 1 < Width)
        {
            var neighbor = GetNode(currentNode.gridX + 1, currentNode.gridZ);
            if (neighbor != null) neighborList.Add(neighbor);
        }

        return neighborList;
    }
    #endregion

    private List<GridNode> CalculateShortestPathWithinRange(Card_Unit unit, GridNode endNode)
    {
        List<GridNode> path = new List<GridNode>();
        path.Add(endNode);
        GridNode currentNode = endNode;
        while (currentNode.cameFromNode != null)
        {
            //Start at the end and work backwards
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }
        path.Reverse();

        //I now have a list of nodes from start to end leading the unit to the target
        //Starting from the second to last node, check if it is within range of the endnode
        //if it is, remove the node above it
        for (int i = path.Count - 2; i >= 0; i--)
        {
            if (GetDistanceInNodes(path[i], endNode) <= unit.Range) path.RemoveAt(i + 1);
        }

        return path;
    }
}

public struct ReachableNodes
{
    public List<GridNode> walkNodes;
    public List<GridNode> attackNodes;

    public ReachableNodes(List<GridNode> walk, List<GridNode> attack)
    {
        walkNodes = walk;
        attackNodes = attack;
    }
}