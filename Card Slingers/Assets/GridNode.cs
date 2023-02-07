using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material blueHighlightedMaterial;
    [SerializeField] private Material redHighlightedMaterial;

    private enum MaterialType { Normal, Blue, Red };

    public Permanent occupant { get; private set; }

    private Battlefield field;
    public int gridX { get; private set; }
    public int gridZ { get; private set; }

    public void OnAssignCoordinates(int x, int z)
    {
        gridX = x;
        gridZ = z;
    }

    public void SetOccupant(Permanent occupant)
    {
        this.occupant = occupant;
    }

    private void OnMouseEnter()
    {
        if (occupant != null && occupant.Commander == DuelManager.instance.opponent) SetColor(MaterialType.Red);
        else SetColor(MaterialType.Blue);
    }

    private void OnMouseExit()
    {
        SetColor(MaterialType.Normal);
    }

    private void OnMouseDown()
    {
        Debug.Log(gridX + "," + gridZ);
        DuelManager.instance.onNodeSelected?.Invoke(this);
    }

    private void SetColor(MaterialType type)
    {
        switch (type)
        {
            case MaterialType.Blue:
                meshRenderer.material = blueHighlightedMaterial;
                break;
            case MaterialType.Red:
                meshRenderer.material = redHighlightedMaterial;
                break;
            default:
                meshRenderer.material = normalMaterial;
                break;
        }
    }
}
