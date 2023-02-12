using System.Collections.Generic;
using UnityEngine;

public class Battlefield : MonoBehaviour
{
    private const float CELL_SIZE = 5f;
    
    private GridNode[,] gridArray;

    [SerializeField] private int _width;
    [SerializeField] private int _depth;
    [SerializeField] private GameObject node; //Move this to being pooled
    [SerializeField] private GameObject checkerboardWhite, checkerboardGray; //these won't be needed beyond testing
    [Space]
    [SerializeField] private Transform _playerCardsParent;
    [SerializeField] private Transform  _playerHand, _playerDeck, _playerDiscard, _playerExile;
    [Space]
    [SerializeField] private Transform _opponentCardsParent;
    [SerializeField] private Transform _opponentHand, _opponentDeck, _opponentDiscard, _opponentExile;
    private Vector3 origin;

    #region - Public Variable References -
    public int Width => _width; //These are currently only being used for early testing
    public int Depth => _depth;
    #endregion

    #region - Grid -
    public void CreateGrid()
    {
        origin = new Vector3((-_width * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f), 0, (-_depth * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f));

        var parentDist = _width * CELL_SIZE * 0.5f + 2;
        _playerCardsParent.position = new Vector3(transform.position.x, transform.position.y + 0.25f, -parentDist);
        _opponentCardsParent.position = new Vector3(transform.position.x, transform.position.y + 0.25f, parentDist);

        float f = _depth;
        int playerDepth = Mathf.RoundToInt(f * 0.5f);

        gridArray = new GridNode[_width, _depth];
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                var pos = GetGridPosition(x, z);
                CreateCheckerboard(pos, x, z);

                pos.y += 0.001f;
                GameObject go = Instantiate(node, pos, Quaternion.identity);
                go.transform.SetParent(transform);

                gridArray[x, z] = go.GetComponentInChildren<GridNode>();
                gridArray[x, z].OnAssignCoordinates(x, z, z < playerDepth);
            }
        }

        float initZ = 25 + ((_depth - 6) * 2.5f);
        float aerialY = 5 * _depth - 5;
        var cam = Camera.main.GetComponent<FreeFlyCamera>();
        cam.SetInit(new Vector3(0, 12, -initZ), new Vector3(35, 0, 0));
        cam.SetAerialView(aerialY);
    }

    //For testing only
    private void CreateCheckerboard(Vector3 pos, int x, int z)
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
        if (x >= 0 && z >= 0 && x < _width && z < _depth)
        {
            return gridArray[x, z];
        }

        throw new System.Exception("parameter " + x + "," + z + " outside bounds of array");
    }

    public Vector3 GetGridPosition(int x, int z)
    {
        return origin + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
        //return _origin.transform.position + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
    }

    public GridNode[] GetAllNodesInLane(int laneX)
    {
        var tempArray = new GridNode[_depth];

        for (int i = 0; i < tempArray.Length; i++)
        {
            tempArray[i] = gridArray[laneX, i];
        }

        return tempArray;
    }

    public GridNode[] GetLaneNodesInRange(GridNode node, int range)
    {
        var tempList = new List<GridNode>();

        for (int i = 0; i < _depth; i++)
        {
            var laneNode = gridArray[node.gridX, i];
            if (laneNode == node) continue;

            if (Mathf.Abs(node.gridZ - laneNode.gridZ) <= range) tempList.Add(laneNode);
        }

        return tempList.ToArray();
    }

    public bool OnValidateNewPosition(GridNode newNode, int width, int height)
    {
        int startX = newNode.gridX;
        int startY = newNode.gridZ;

        for (int x = startX; x < startX + width; x++)
        {
            if (x >= _width)
            {
                //Debug.Log(x + "," + startY + " is out of bounds");
                return false;
            }
            if (GetNode(x, startY).Occupant != null)
            {
                //Debug.Log(x + "," + startY + " is not clear");
                return false;
            }
            //Debug.Log(x + "," + startY + " is clear");
        }
        for (int y = startY; y < startY + height; y++)
        {
            if (y >= _depth)
            {
                //Debug.Log(startX + "," + y + " is out of bounds");
                return false;
            }
            if (GetNode(startX, y).Occupant != null)
            {
                //Debug.Log(startX + "," + y + " is not clear");
                return false;
            }
            //Debug.Log(startX + "," + y + " is clear");
        }

        return true;
    }

    //used to divide the field into two halves
    /*public bool NodeBelongsToCommander(GridNode node, CommanderController commander)
    {
        float tempDepth = _depth;
        int halfDepth = Mathf.RoundToInt(tempDepth * 0.5f);

        if (commander is PlayerCommander)
        {
            if (node.gridZ < halfDepth) return true;
            return false;
        }
        else
        {
            if (node.gridZ >= halfDepth) return true;
            return false;
        }
    }*/
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
