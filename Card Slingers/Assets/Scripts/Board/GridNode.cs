using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode : MonoBehaviour, IInteractable
{
    public enum MaterialType { Normal, Blue, Red };
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material blueHighlightedMaterial;
    [SerializeField] private Material redHighlightedMaterial;
    [Space]

    [SerializeField] private bool _isPlayerNode;
    [SerializeField] Card_Permanent _occupant;
    [SerializeField] Card _trap;
    [SerializeField] Card _terrain;

    private bool _lockedForDisplay; //ignore mouse movements to change the color

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

        DuelManager.instance.onNodeSelected?.Invoke(this);

    }

    public void OnRightClick()
    {
        if (Occupant != null) UIManager.instance.ShowCardDisplay(_occupant.CardInfo);
    }

    public void OnMouseEnter()
    {
        DuelManager.instance.onNodeMouseEnter?.Invoke(this);

        if (_lockedForDisplay || Occupant == null) return;
        else if (Occupant.Commander is PlayerCommander) SetColor(MaterialType.Blue);
        else SetColor(MaterialType.Red);
    }

    private void OnMouseExit()
    {
        DuelManager.instance.onNodeMouseExit?.Invoke(this);
        
        if (_lockedForDisplay) return;

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

    public void SetLockedDisplay(MaterialType color)
    {
        _lockedForDisplay = true;
        SetColor(color);
    }

    public void UnlockDisplay()
    {
        _lockedForDisplay = false;
        SetColor(MaterialType.Normal);
    }

    public bool CanMoveIntoNode(Card_Permanent card)
    {
        if (card == _occupant) return false; //same occupant

        if (_occupant == null) return true; //not occupied at all

        if (_occupant.Commander != card.Commander) return false; //occupied by an enemy

        if (_occupant is Card_Structure structure && structure.CanBeOccupied) return true; //can move into building

        return false; //there is an allied occupant here
    }
}
