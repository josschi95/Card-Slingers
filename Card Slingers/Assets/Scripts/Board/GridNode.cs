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
    [Space]

    [SerializeField] private bool _isPlayerNode;
    [SerializeField] Card_Permanent _occupant;
    [SerializeField] Card _trap;
    [SerializeField] Card _terrain;


    public int gridX { get; private set; }
    public int gridZ { get; private set; }
    public bool IsPlayerNode => _isPlayerNode;
    public Card_Permanent Occupant => _occupant;
    public Card Trap => _trap;
    public Card Terrain => _terrain;

    public void OnAssignCoordinates(int x, int z, bool isPlayerNode)
    {
        gridX = x;
        gridZ = z;
        _isPlayerNode = isPlayerNode;
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

    public void OnMouseEnter()
    {
        DuelManager.instance.onNodeMouseEnter?.Invoke(this);

        if (DuelManager.instance.WaitingForNodeSelection)
        {
            if (Occupant != null && Occupant.Commander == DuelManager.instance.opponentController) SetColor(MaterialType.Red);
            else SetColor(MaterialType.Blue);
        }
    }

    private void OnMouseExit()
    {
        SetColor(MaterialType.Normal);
        DuelManager.instance.onNodeMouseExit?.Invoke(this);
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
