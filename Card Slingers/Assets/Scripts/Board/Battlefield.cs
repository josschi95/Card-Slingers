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

    [SerializeField] private int _width;
    [SerializeField] private int _depth;
    [SerializeField] private Transform _origin;
    [SerializeField] private GameObject nodeDisplay;

    public int Width => _width;
    public int Depth => _depth;
    public float CellSize => CELL_SIZE;
    public Transform Origin => _origin;

    private GridNode[,] cellArray;

    private void Start()
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
                GameObject go = GameObject.Instantiate(nodeDisplay, GetGridPosition(x, z), Quaternion.identity);
                go.transform.SetParent(_origin);

                cellArray[x, z] = go.GetComponentInChildren<GridNode>();
                cellArray[x, z].OnAssignCoordinates(x, z);
            }
        }
    }

    public GridNode GetCell(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < _width && z < _depth)
        {
            return cellArray[x, z];
        }

        throw new System.Exception("parameter " + x + "," + z + " outside bounds of array");
    }

    public Vector3 GetGridPosition(int x, int z)
    {
        return GetCell(x, z).transform.position;
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
            if (GetCell(x, startY).occupant != null)
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
            if (GetCell(startX, y).occupant != null)
            {
                //Debug.Log(startX + "," + y + " is not clear");
                return false;
            }
            //Debug.Log(startX + "," + y + " is clear");
        }

        return true;
    }
    #endregion

    public void PlacePermanent(int x, int z, Permanent permanent)
    {
        GetCell(x, z).SetOccupant(permanent);
    }

    public void RemovePermanent(int x, int z)
    {
        GetCell(x, z).SetOccupant(null);
    }
}
