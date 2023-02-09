using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Battlefield : MonoBehaviour
{
    private const float CELL_SIZE = 2f;

    public delegate void OnCellOccupantChangedCallback(int x, int y, Permanent occupant);
    public OnCellOccupantChangedCallback onCellOccupied;
    public OnCellOccupantChangedCallback onCellAbandoned;

    private static Vector3 playerRotation = new Vector3(0, 90, 0);
    private static Vector3 opponentRotation = new Vector3(0, -90, 0);
    
    private GridNode[,] cellArray;

    [SerializeField] private int _width;
    [SerializeField] private int _depth;
    [SerializeField] private Transform _origin;
    [SerializeField] private GameObject nodeDisplay;
    [Space]
    [SerializeField] private Transform _playerDeck;
    [SerializeField] private Transform _playerHand, _playerDiscard;
    [SerializeField] private Transform _opponentDeck, _opponentHand, _opponentDiscard;

    #region - Public Variable References -
    public int Width => _width;
    public int Depth => _depth;
    public float CellSize => CELL_SIZE;
    public Transform Origin => _origin;
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
        cellArray = new GridNode[_width, _depth];
        for (int x = 0; x < cellArray.GetLength(0); x++)
        {
            for (int z = 0; z < cellArray.GetLength(1); z++)
            {
                var pos = GetGridPosition(x, z);
                pos.y += 0.001f;
                GameObject go = GameObject.Instantiate(nodeDisplay, pos, Quaternion.identity);
                go.transform.SetParent(_origin);

                cellArray[x, z] = go.GetComponentInChildren<GridNode>();
                cellArray[x, z].OnAssignCoordinates(x, z);
            }
        }
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
        return _origin.transform.position + new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
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
            if (GetNode(x, startY).occupant != null)
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
            if (GetNode(startX, y).occupant != null)
            {
                //Debug.Log(startX + "," + y + " is not clear");
                return false;
            }
            //Debug.Log(startX + "," + y + " is clear");
        }

        return true;
    }
    #endregion

    public Permanent PlacePermanent(int x, int z, GameObject prefab, bool isPlayer)
    {
        var node = GetNode(x, z);
        return PlacePermanent(node, prefab, isPlayer);
    }

    public Permanent PlacePermanent(GridNode node, GameObject prefab, bool isPlayer)
    {
        var unit = Instantiate(prefab, node.transform.position, Quaternion.identity);

        if (isPlayer) unit.transform.localEulerAngles = playerRotation;
        else unit.transform.localEulerAngles = opponentRotation;

        var permanent = unit.GetComponent<Permanent>();
        node.SetOccupant(permanent);

        return permanent;
    }

    public void RemovePermanent(int x, int z)
    {
        GetNode(x, z).SetOccupant(null);
    }
}
