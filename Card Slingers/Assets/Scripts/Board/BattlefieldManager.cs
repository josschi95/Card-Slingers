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

    [SerializeField] private bool CREATE_TEST_CHECKERBOARD;

    private GridNode[,] gridArray;
    private Vector2Int _dimensions;
    [SerializeField] private int[] _laneThreatArray;
    [Space]
    [SerializeField] private Transform _nodeParent;
    [SerializeField] private GridNode node; //Move this to being pooled
    [SerializeField] private Transform _checkerBoardParent;
    [SerializeField] private GameObject checkerboardWhite, checkerboardGray; //these won't be needed beyond testing
    private Vector3 _origin;
    public Vector3 Center { get; private set; } //probably change this to a transform later on to handle rotations with turns

    #region - Public Variable References -
    public int Width => _dimensions.x; //These are currently only being used for early testing
    public int Depth => _dimensions.y;
    public float CellSize => CELL_SIZE;
    public int[] LaneThreatArray => _laneThreatArray;
    #endregion

    #region - Grid -
    public void CreateGrid(Vector3 center, Vector2Int dimensions)
    {
        Center = center;
        _dimensions = dimensions;

        _origin = new Vector3(
            (-Width * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f) + Center.x, 
            Center.y, 
            (-Depth * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f) + Center.z);

        float f = Depth; int playerDepth = Mathf.RoundToInt(f * 0.5f);

        gridArray = new GridNode[Width, Depth];
        _laneThreatArray = new int[Width];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                var pos = GetGridPosition(x, z);
                TESTING_CREATE_CHECKERBOARD(pos, x, z);

                pos.y += 0.001f;
                var go = Instantiate(node, pos, Quaternion.identity);
                go.transform.SetParent(_nodeParent);

                gridArray[x, z] = go;
                gridArray[x, z].OnAssignCoordinates(x, z, z < playerDepth);
                gridArray[x, z].onNodeValueChanged += OnNodeValueChanged;
            }
        }

        float initZ = 27 + ((Depth - 6) * 2.5f);
        CameraController.instance.SetHome(new Vector3(Center.x, Center.y + 12, Center.z - initZ), transform.rotation.y);
    }

    public void DestroyGrid()
    {
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                var node = gridArray[x, z];
                node.onNodeValueChanged -= OnNodeValueChanged;
                node.ReleaseToPool();
                //Destroy(node.gameObject);
            }
        }
    }

    //For testing only
    private void TESTING_CREATE_CHECKERBOARD(Vector3 pos, int x, int z)
    {
        if (!CREATE_TEST_CHECKERBOARD) return;

        var go = checkerboardGray;
        pos.y -= 0.01f;
        if (z%2 == 0) //row is even
        {
            if (x % 2 == 0) //column is even
            {
                //gray
            }
            else //column is odd
            {
                go = checkerboardWhite;
            }
        }
        else //row is odd
        {
            if (x % 2 == 0) //column is even
            {
                go = checkerboardWhite;
            }
            else //column is odd
            {
                //gray
            }
        }

        var newgo = Instantiate(go, pos, Quaternion.identity);
        newgo.transform.SetParent(_checkerBoardParent);
    }

    public GridNode GetNode(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < Width && z < Depth)
        {
            return gridArray[x, z];
        }

        throw new System.Exception("parameter " + x + "," + z + " outside bounds of array");
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
                if (vertical + horizontal <= range) nodeList.Add(gridArray[x, z]);
            }
        }
        return nodeList;
    }

    public List<GridNode> GetControlledNodesInLane(CommanderController commander, int lane)
    {
        var tempList = new List<GridNode>();
        int halfDepth = Mathf.RoundToInt(Depth * 0.5f);
        float half = Depth * 0.5f;

        if (commander is PlayerCommander)
        {
            //check all nodes starting from 0, going to halfway point
            for (int i = 0; i < halfDepth; i++)
            {
                var occupant = gridArray[lane, i].Occupant;
                if (occupant != null && occupant.Commander != commander) break;
                else tempList.Add(gridArray[lane, i]);
            }
        }
        else
        {
            //check all nodes starting from furthest node going backward to half point
            for (int i = Depth - 1; i >= halfDepth; i--)
            {
                var occupant = gridArray[lane, i].Occupant;
                if (occupant != null && occupant.Commander != commander) break;
                else tempList.Add(gridArray[lane, i]);
            }
        }

        return tempList;
    }

    public List<GridNode> GetSummonableLanes(CommanderController commander)
    {
        var tempList = new List<GridNode>();
        int halfDepth = Mathf.RoundToInt(Depth * 0.5f);

        if (commander is PlayerCommander)
        {
            //Goes lane by lane from 0 to width
            for (int x = 0; x < Width; x++)
            {
                string lanes = "Lane " + x + ": ";
                for (int z = 0; z < halfDepth; z++)
                {
                    var node = GetNode(x, z);

                    if (node.Occupant != null)
                    {
                        if (node.Occupant.Commander != commander) break;
                        else continue;
                    }

                    lanes += z + ", ";
                    tempList.Add(node);
                }
                //Debug.Log(lanes);
            }
        }
        else
        {
            //Goes lane by lane from 0 to width
            for (int x = 0; x < Width; x++)
            {
                string lanes = "Lane " + x + ": ";
                for (int z = Depth - 1; z >= halfDepth; z--)
                {
                    var node = GetNode(x, z);

                    if (node.Occupant != null)
                    {
                        if (node.Occupant.Commander != commander) break;
                        else continue;
                    }

                    lanes += z + ", ";
                    tempList.Add(node);
                }
                //Debug.Log(lanes);
            }
        }

        return tempList;
    }

    /// <summary>
    /// Returns a list of unoccupied lane nodes on the commander's side of the field.
    /// </summary>
    public List<GridNode> GetOpenNodesInLane(CommanderController commander, int lane)
    {
        var tempList = GetControlledNodesInLane(commander, lane);

        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            if (tempList[i].Occupant != null) tempList.RemoveAt(i);
        }

        return tempList;
    }

    private void OnNodeValueChanged(GridNode node)
    {
        int lane = node.gridX;
        int newLaneValue = 0;

        for (int i = 0; i < gridArray.GetLength(0); i++)
        {
            newLaneValue += gridArray[lane, i].occupantPower;
        }
        _laneThreatArray[lane] = newLaneValue;
    }
    #endregion

    #region - Pathfinding -
    private const int MOVE_STRAIGHT_COST = 1;

    private List<GridNode> openList; //nodes to search
    private List<GridNode> closedList; //already searched

    //Returns a list of nodes that can be travelled to reach a target destination
    public List<GridNode> FindNodePath(Card_Unit unit, GridNode endNode, bool ignoreEndNode = false, bool stopWithinRange = false)
    {
        GridNode startNode = unit.Node;
        //Debug.Log("Start: " + startNode.x + "," + startNode.y);
        //Debug.Log("End: " + endNode.x + "," + endNode.y);

        openList = new List<GridNode> { startNode };
        closedList = new List<GridNode>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Depth; y++)
            {
                GridNode pathNode = GetNode(x, y);
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

            if (stopWithinRange && GetDistanceInNodes(currentNode, endNode) <= unit.Range && currentNode.CanBeOccupied(unit))
            {
                //Don't need to move any further
                //Debug.Log("Found closest node within range, located at " + currentNode.gridX + "," + currentNode.gridZ);
                return CalculatePath(currentNode);
            }

            if (currentNode == endNode)
            {
                //Reached final node
                return CalculatePath(endNode);
            }

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
        Debug.Log("Path could not be found");
        return null;
    }

    //Issue 1. When finding a nodepath to an enemy, I'm only looking to get next to that unit, not just within range


    //
    //Add a method in here for something like FindPartialPath that allows the unit to at least move towards their intended target
    //

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
                if (pathNode.CanBeTraversed(unit) && pathNode.CanBeOccupied(unit) && GetDistanceInNodes(startNode, pathNode) <= unit.Speed + 1)
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
            else if (temp.Count > unit.Speed + 1) nodes.RemoveAt(i);

        }

        return nodes;
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
                //Add all walkable nodes which can be accessed 
                if (pathNode.CanBeAttacked(unit) && GetDistanceInNodes(startNode, pathNode) <= range + unit.Speed)
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
            var temp = FindNodePath(unit, nodes[i], true, true);

            /*Debug.Log("__________NEW PATH__________");
            string txt = "";
            for (int g = 0; g < temp.Count; g++)
            {
                txt += "_" + temp[g].gridX + "," + temp[g].gridZ + "_";
            }
            Debug.Log(txt);*/

            //There is no available path to that node
            if (temp == null) nodes.RemoveAt(i);

            //The path requires more moves than are available
            //else if (temp.Count > range + unit.Speed + 1) //+1 because it counts the unit's occupied node as the first node
            else if (temp.Count > unit.Speed + 1) //+1 because it counts the unit's occupied node as the first node
            {
                //Debug.Log("Removing path, number of nodes is greater than " + (range + unit.Speed + 1).ToString() + "(including own node)");
                nodes.RemoveAt(i);
            }
            //The node path has at least two nodes in it - Checking this to not throw an error for the next check
            //The node next to the target cannot be occupied
            //The node next to the target is not where the attacking unit currently is located
            else if (temp.Count >= 2 && !temp[temp.Count - 2].CanBeOccupied(unit) && temp[temp.Count - 2] != unit.Node)
            {
                //Debug.Log("Cannot occupy node needed to move to");
                nodes.RemoveAt(i);
            }
            //else Debug.Log("-----Valid Path-----");
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

    private int CalculateDistanceCost(GridNode a, GridNode b)
    {
        int xDistance = Mathf.Abs(a.gridX - b.gridX);
        int zDistance = Mathf.Abs(a.gridZ - b.gridZ);
        int remaining = Mathf.Abs(xDistance - zDistance);
        return MOVE_STRAIGHT_COST * remaining;
    }

    public int GetDistanceInNodes(GridNode fromNode, GridNode toNode)
    {
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
        if (currentNode.gridZ + 1 < Depth) neighborList.Add(GetNode(currentNode.gridX, currentNode.gridZ + 1));
        //Down
        if (currentNode.gridZ - 1 >= 0) neighborList.Add(GetNode(currentNode.gridX, currentNode.gridZ - 1));
        //Left
        if (currentNode.gridX - 1 >= 0) neighborList.Add(GetNode(currentNode.gridX - 1, currentNode.gridZ));
        //Right
        if (currentNode.gridX + 1 < Width) neighborList.Add(GetNode(currentNode.gridX + 1, currentNode.gridZ));

        return neighborList;
    }
    #endregion

    #region - Obsolete -
    //Return a list of nodes which can be targeted given the range, used for attacks
    /*public List<GridNode> FindTargetableNodes(Card_Unit unit, int range)
    {
        List<GridNode> nodes = new List<GridNode>();
        GridNode startNode = unit.Node;

        //Get all nodes in the grid
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Depth; y++)
            {
                GridNode pathNode = GetNode(x, y);
                //Add all walkable nodes which can be accessed via a straight line path
                if (pathNode.CanBeTraversed(unit) && GetDistanceInNodes(startNode, pathNode) <= range)
                {
                    //Note that this doesn't eliminate diagonals
                    nodes.Add(pathNode);
                    //Debug.Log(pathNode.x + "," + pathNode.y);
                }
            }
        }

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            var temp = FindNodePath(unit, nodes[i]);

            //There is no available path to that node
            if (temp == null) nodes.RemoveAt(i);
            //The path requires more moves than are available
            else if (temp.Count > range) nodes.RemoveAt(i);
        }

        return nodes;
    }*/

    //Replaced by FindNodePath
    /*public GridNode[] GetNodePath(GridNode startNode, GridNode endNode)
    {
        if (endNode.Occupant != null) return null;

        int length = Mathf.Abs(startNode.gridZ - endNode.gridZ) + 1;
        var nodePath = new GridNode[length];
        int mod = 1;
        if (startNode.gridZ > endNode.gridZ) mod = -1;

        for (int i = 0; i < nodePath.Length; i++)
        {
            //get the node in the same lane, with the gridZ increasing/decreasing based on direction
            var nextNode = GetNode(startNode.gridX, startNode.gridZ + i * mod);
            nodePath[i] = nextNode;
        }

        return nodePath;
    }*/

    /*public GridNode GetUnoccupiedNodeInRange(GridNode fromNode, GridNode toNode, int range)
    {
        int modifier = 1; //run up or down the lane based on toNode relative position
        if (fromNode.gridZ > toNode.gridZ) modifier = -1;

        for (int i = fromNode.gridZ + modifier; i != toNode.gridZ; i += modifier)
        {
            //Debug.Log("Looking for unoccupied node at gridZ: " + i);
            var node = GetNode(fromNode.gridX, i);
            if (node.Occupant != null) continue; //node is occupied
            if (Mathf.Abs(node.gridZ - toNode.gridZ) > range) continue; //node not within range
            return node;
        }
        return null;
    }*/

    /*public GridNode[] GetAllNodesInLane(int laneX)
    {
        var tempArray = new GridNode[Depth];

        for (int i = 0; i < tempArray.Length; i++)
        {
            tempArray[i] = gridArray[laneX, i];
        }

        return tempArray;
    }*/

    /*public int GetFrontRow(CommanderController commander)
    {
        float f = Depth;
        int playerFront = Mathf.RoundToInt(f * 0.5f);

        if (commander is PlayerCommander) return playerFront;
        else return playerFront + 1;
    }*/
    #endregion
}
