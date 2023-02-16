using System.Collections.Generic;
using UnityEngine;

public class Battlefield : MonoBehaviour
{
    private const float CELL_SIZE = 5f;
    
    private GridNode[,] gridArray;
    [SerializeField] private int[] _laneBalanceArray;
    [Space]
    [SerializeField] private Vector2Int _dimensions;
    [SerializeField] private GridNode node; //Move this to being pooled
    [SerializeField] private GameObject checkerboardWhite, checkerboardGray; //these won't be needed beyond testing
    [Space]
    [SerializeField] private Transform _playerCardsParent;
    [SerializeField] private Transform  _playerHand, _playerDeck, _playerDiscard, _playerExile;
    [Space]
    [SerializeField] private Transform _opponentCardsParent;
    [SerializeField] private Transform _opponentHand, _opponentDeck, _opponentDiscard, _opponentExile;
    private Vector3 origin;

    #region - Public Variable References -
    public int Width => _dimensions.x; //These are currently only being used for early testing
    public int Depth => _dimensions.y;
    public int[] LaneBalanceArray => _laneBalanceArray;
    #endregion

    //Along that same line, I feel that Battlefield should be broken up into two separate scripts since it's handling both grid management and "pathfinding" so BattleFieldManager, and Grid

    #region - Grid -
    public void CreateGrid()
    {
        origin = new Vector3((-Width * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f), 0, (-Depth * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f));

        var parentDist = Depth * CELL_SIZE * 0.5f + 2;
        _playerCardsParent.position = new Vector3(transform.position.x, transform.position.y + 0.25f, -parentDist);
        _opponentCardsParent.position = new Vector3(transform.position.x, transform.position.y + 0.25f, parentDist);

        float f = Depth; int playerDepth = Mathf.RoundToInt(f * 0.5f);

        gridArray = new GridNode[Width, Depth];
        _laneBalanceArray = new int[Width];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                var pos = GetGridPosition(x, z);
                TESTING_CREATE_CHECKERBOARD(pos, x, z);

                pos.y += 0.001f;
                var go = Instantiate(node, pos, Quaternion.identity);
                go.transform.SetParent(transform);

                gridArray[x, z] = go;
                gridArray[x, z].OnAssignCoordinates(x, z, z < playerDepth);
                gridArray[x, z].onNodeValueChanged += OnNodeValueChanged;
            }
        }

        float initZ = 25 + ((Depth - 6) * 2.5f);
        float aerialY = 5 * Depth - 5;
        var cam = Camera.main.GetComponent<FreeFlyCamera>();
        cam.SetInit(new Vector3(0, 12, -initZ), new Vector3(35, 0, 0));
        cam.SetAerialView(aerialY);
    }

    private void DestroyGrid()
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
        newgo.transform.SetParent(transform);
    }

    public GridNode GetNode(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < Width && z < Depth)
        {
            return gridArray[x, z];
        }

        throw new System.Exception("parameter " + x + "," + z + " outside bounds of array");
    }

    public int GetFrontRow(CommanderController commander)
    {
        float f = Depth;
        int playerFront = Mathf.RoundToInt(f * 0.5f);

        if (commander is PlayerCommander) return playerFront;
        else return playerFront + 1;
    }

    public Vector3 GetGridPosition(int x, int z)
    {
        return origin + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
        //return _origin.transform.position + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
    }

    public GridNode[] GetAllNodesInLane(int laneX)
    {
        var tempArray = new GridNode[Depth];

        for (int i = 0; i < tempArray.Length; i++)
        {
            tempArray[i] = gridArray[laneX, i];
        }

        return tempArray;
    }

    public List<GridNode> GetControlledNodesInLane(CommanderController commander, int lane)
    {
        var tempList = new List<GridNode>();
        int halfDepth = Mathf.RoundToInt(Depth * 0.5f);

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

    public GridNode GetUnoccupiedNodeInRange(GridNode fromNode, GridNode toNode, int range)
    {
        int modifier = 1; //run up or down the lane based on toNode relative position
        if (fromNode.gridZ > toNode.gridZ) modifier = -1;

        for (int i = fromNode.gridZ + modifier; i != toNode.gridZ; i += modifier)
        {
            //Debug.Log("Looking for unoccupied node at gridZ: " + i);
            var node = GetNode(fromNode.gridX, i);
            if (node.Occupant != null) continue; //node is occupied
            if (DuelManager.instance.ClaimedNodes.Contains(node)) continue; //another unit is moving here
            if (Mathf.Abs(node.gridZ - toNode.gridZ) > range) continue; //node not within range
            return node;
        }
        return null;
    }

    public GridNode[] GetNodePath(GridNode startNode, GridNode endNode)
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
    }

    private void OnNodeValueChanged(GridNode node)
    {
        int lane = node.gridX;
        int newLaneValue = 0;

        for (int i = 0; i < gridArray.GetLength(0); i++)
        {
            newLaneValue += gridArray[lane, i].occupantPower;
        }
        _laneBalanceArray[lane] = newLaneValue;
    }
    #endregion

    #region - Card Placement Parents - 

    //I'm likely going to end up moving these into a pooled object or just have a single one that I use for each dungeon,
    //and have them held by a dungeon manager instead of every single battlefield having their own
    public Transform GetHandParent(CommanderController commander)
    {
        if (commander is PlayerCommander) return _playerHand;
        return _opponentHand;
    }

    public Transform GetDeckParent(CommanderController commander)
    {
        if (commander is PlayerCommander) return _playerDeck;
        return _opponentDeck;
    }

    public Transform GetDiscardParent(CommanderController commander)
    {
        if (commander is PlayerCommander) return _playerDiscard;
        return _opponentDiscard;
    }

    public Transform GetExileParent(CommanderController commander)
    {
        if (commander is PlayerCommander) return _playerExile;
        return _opponentExile;
    }
    #endregion
}
