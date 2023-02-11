using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Battlefield : MonoBehaviour
{
    private const float CELL_SIZE = 5f;

    public delegate void OnCellOccupantChangedCallback(int x, int y, Card_Permanent occupant);
    public OnCellOccupantChangedCallback onCellOccupied;
    public OnCellOccupantChangedCallback onCellAbandoned;

    private static Vector3 playerRotation = Vector3.zero;
    private static Vector3 opponentRotation = new Vector3(0, 180, 0);
    
    private GridNode[,] cellArray;

    [SerializeField] private int _width;
    [SerializeField] private int _depth;
    [SerializeField] private GameObject node; //Move this to being pooled
    [SerializeField] private GameObject checkerboardWhite, checkerboardGray;
    [Space]
    [SerializeField] private Transform _playerCardsParent;
    [SerializeField] private Transform _playerDeck, _playerHand, _playerDiscard;
    [Space]
    [SerializeField] private Transform _opponentCardsParent;
    [SerializeField] private Transform _opponentDeck, _opponentHand, _opponentDiscard;
    private Vector3 origin;

    #region - Public Variable References -
    public int Width => _width;
    public int Depth => _depth;
    public float CellSize => CELL_SIZE;
    public Transform PlayerDeck => _playerDeck;
    public Transform PlayerHand => _playerHand;
    public Transform PlayerDiscard => _playerDiscard;
    public Transform OpponentDeck => _opponentDeck;
    public Transform OpponentHand => _opponentHand;
    public Transform OpponentDiscard => _opponentDiscard;
    #endregion

    private void Awake()
    {
        CreateGrid();
    }

    #region - Grid -
    private void CreateGrid()
    {
        origin = new Vector3((-_width * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f), 0, (-_depth * CELL_SIZE * 0.5f) + (CELL_SIZE * 0.5f));

        var parentDist = _width * CELL_SIZE * 0.5f + 2;
        _playerCardsParent.position = new Vector3(transform.position.x, transform.position.y + 0.25f, -parentDist);
        _opponentCardsParent.position = new Vector3(transform.position.x, transform.position.y + 0.25f, parentDist);

        float f = _depth;
        int playerDepth = Mathf.RoundToInt(f * 0.5f);

        cellArray = new GridNode[_width, _depth];
        for (int x = 0; x < cellArray.GetLength(0); x++)
        {
            for (int z = 0; z < cellArray.GetLength(1); z++)
            {
                var pos = GetGridPosition(x, z);
                CreateCheckerboard(pos, x, z);

                pos.y += 0.001f;
                GameObject go = Instantiate(node, pos, Quaternion.identity);
                go.transform.SetParent(transform);

                cellArray[x, z] = go.GetComponentInChildren<GridNode>();
                cellArray[x, z].OnAssignCoordinates(x, z, z < playerDepth);
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
            return cellArray[x, z];
        }

        throw new System.Exception("parameter " + x + "," + z + " outside bounds of array");
    }

    public Vector3 GetGridPosition(int x, int z)
    {
        return origin + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
        //return _origin.transform.position + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
    }

    public Vector3 GetNodePosition(int x, int z)
    {
        return GetNode(x, z).transform.position;
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
    #endregion

    public void PlaceCommander(int x, int z, GameObject go, bool isPlayer)
    {
        var node = GetNode(x, z);

        go.transform.SetParent(transform, false);
        go.transform.position = node.transform.position;
        if (isPlayer) go.transform.localEulerAngles = playerRotation;
        else go.transform.localEulerAngles = opponentRotation;

        var permanent = go.GetComponent<Card_Permanent>();
        node.SetOccupant(permanent);
        permanent.SetNode(node);
    }

    public bool NodeBelongsToCommander(GridNode node, CommanderController commander)
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
    }

    #region - Card Placement - 
    public void PlaceCardInDeck(CommanderController commander, Card card)
    {
        if (commander == DuelManager.instance.playerController) card.transform.SetParent(_playerDeck, false);
        else card.transform.SetParent(_opponentDeck, false);
        card.SetCardLocation(CardLocation.InDeck);
    }

    public void PlaceCardInDiscard(CommanderController commander, Card card)
    {
        if (commander == DuelManager.instance.playerController) card.transform.SetParent(_playerDiscard, false);
        else card.transform.SetParent(_opponentDiscard, false);
        card.SetCardLocation(CardLocation.InDiscard);
    }

    public void PlaceCardInHand(CommanderController commander, Card card)
    {
        if (commander == DuelManager.instance.playerController) card.transform.SetParent(_playerHand, false);
        else card.transform.SetParent(_opponentHand, false);
        card.SetCardLocation(CardLocation.InHand);
    }
    #endregion
}
