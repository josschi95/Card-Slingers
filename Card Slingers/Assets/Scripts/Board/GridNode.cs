using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode : MonoBehaviour, IInteractable
{
    private enum MaterialType { Normal, Blue, Red };
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material blueHighlightedMaterial;
    [SerializeField] private Material redHighlightedMaterial;
    private Battlefield field;

    [Space]

    [SerializeField] Card_Permanent _occupant;
    [SerializeField] Card _trap;
    [SerializeField] Card _terrain;


    public int gridX { get; private set; }
    public int gridZ { get; private set; }
    public Card_Permanent Occupant => _occupant;
    public Card Trap => _trap;
    public Card Terrain => _terrain;

    public void OnAssignCoordinates(int x, int z)
    {
        gridX = x;
        gridZ = z;
    }

    public void SetOccupant(Card_Permanent occupant)
    {
        _occupant = occupant;
    }

    public void OnLeftClick()
    {
        //if (Occupant != null) Debug.Log(gridX + "," + gridZ + ": " + Occupant.gameObject.name);
        //else Debug.Log(gridX + "," + gridZ);

        if (DuelManager.instance.WaitingForNodeSelection) DuelManager.instance.onNodeSelected?.Invoke(this);

    }

    public void OnRightClick()
    {
        if (Occupant != null) UIManager.instance.ShowCardDisplay(_occupant.CardInfo);
    }

    private void OnMouseExit()
    {
        SetColor(MaterialType.Normal);
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

    void IInteractable.OnMouseEnter()
    {
        if (DuelManager.instance.WaitingForNodeSelection)
        {
            if (Occupant != null && Occupant.Commander == DuelManager.instance.opponentController) SetColor(MaterialType.Red);
            else SetColor(MaterialType.Blue);
        }
    }
}
