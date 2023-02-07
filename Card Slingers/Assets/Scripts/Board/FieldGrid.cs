using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class FieldGrid<TGridObject>
{
    //This event is called whenever a grid value changes
    public event EventHandler<OnGridValueChangedEventArgs> onGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int z;
    }

    public delegate void OnNodeSelected(GridNode node);
    public OnNodeSelected onNodeSelected;

    private int width; //The number of columns
    private int height; //The number of rows
    private float cellSize; //The size of each grid element
    private Vector3 originPosition; //The bottom left corner of the grid, tile [0,0]
    private TGridObject[,] gridArray;

    private GameObject nodeDisplay;

    public FieldGrid(Battlefield display, int width, int height, float cellSize, Transform origin, Func<FieldGrid<TGridObject>, int, int, TGridObject> createdGridObject, GameObject nodeDisplay, bool showDebug = false)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = origin.position;

        this.nodeDisplay = nodeDisplay;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createdGridObject(this, x, z);
            }
        }

        GridCellDisplay[,] nodeDisplayArray = new GridCellDisplay[width, height];
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                GameObject go = GameObject.Instantiate(this.nodeDisplay, GetGridPosition(x, z), Quaternion.identity);
                go.transform.SetParent(origin);
                go.transform.SetParent(display.Origin);

                nodeDisplayArray[x, z] = go.GetComponentInChildren<GridCellDisplay>();
                nodeDisplayArray[x, z].OnAssignNode(gridArray[x, z] as GridNode);
            }
        }

        if (showDebug)
        {
            TMP_Text[,] debugTextArray = new TMP_Text[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < gridArray.GetLength(1); y++)
                {
                    GameObject go = GameObject.Instantiate(this.nodeDisplay, GetGridPosition(x, y), Quaternion.identity);
                    go.transform.SetParent(display.Origin);

                    debugTextArray[x, y] = go.GetComponentInChildren<TMP_Text>();
                    debugTextArray[x, y].text = gridArray[x, y]?.ToString();
                }
            }

            onGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) =>
            {
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
                var node = display.GetNode(eventArgs.x, eventArgs.z);
                if (node.isOccupied) debugTextArray[eventArgs.x, eventArgs.z].color = Color.red;
                else debugTextArray[eventArgs.x, eventArgs.z].color = Color.black;
            };
        }
    }

    private Vector3 GetGridPosition(int x, int z)
    {
        return new Vector3(x, 0.001f, z) * cellSize + originPosition;
    }

    public void SetGridObject(int x, int z, TGridObject value)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            gridArray[x, z] = value;
            TriggerGridObjectChanged(x, z);
        }
    }

    //method to update the grid upon value change
    public void TriggerGridObjectChanged(int x, int z)
    {
        if (onGridValueChanged != null) onGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, z = z });
    }

    public TGridObject GetGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            return gridArray[x, z];
        }

        //Invalid, outside of array
        return default(TGridObject);
    }

    public int GetWidth()
    {
        return width;
    }

    public int GetHeight()
    {
        return height;
    }
}