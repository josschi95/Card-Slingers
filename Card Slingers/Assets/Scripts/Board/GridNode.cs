using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode : MonoBehaviour, IInteractable
{
    public delegate void OnGridNodeValueChanged();
    public OnGridNodeValueChanged onNodeValueChanged;

    private void TEST_SUMMON_ENEMY()
    {
        DuelManager.instance.SummonTestEnemy(this);
    }

    public enum MaterialType { Normal, Blue, Red, Yellow };
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material[] highlightMats;
    [Space]

    //[SerializeField] 
    private bool _isPlayerNode;
    [SerializeField] private Card_Permanent _occupant = null;
    [SerializeField] private Card _trap;
    [SerializeField] private Card _terrain;

    public int OccupantPower => CalculateOccupantPower();

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
        if (_occupant != null) throw new System.Exception("Node " + gridX + "," + gridZ + "is already occupied by " + _occupant.name);
        _occupant = occupant;
    }

    public void ClearOccupant()
    {
        _occupant = null;
    }

    private int CalculateOccupantPower()
    {
        if (_occupant == null) return 0;
        else return _occupant.PowerLevel;
    }

    #region - Interactions -
    public void OnLeftClick()
    {
        //if (Occupant != null) Debug.Log(gridX + "," + gridZ + ": " + Occupant.gameObject.name);
        //else Debug.Log(gridX + "," + gridZ);

        DuelManager.instance.onNodeSelected?.Invoke(this);

    }

    public void OnRightClick()
    {
        if (Occupant != null) UIManager.instance.ShowCardDisplay(_occupant.CardInfo);

        TEST_SUMMON_ENEMY();
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
    #endregion

    #region - Display -
    private void SetColor(MaterialType type)
    {
        meshRenderer.material = highlightMats[(int)type];
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
    #endregion

    #region - Queries -
    public bool CanBeOccupied(Card_Permanent card)
    {
        if (_occupant == null) return true; //not occupied at all
        if (_occupant.Commander != card.Commander) return false; //occupied by an enemy unit/structure
        if (_occupant is Card_Structure structure && structure.CanBeOccupied) return true; //can move into building

        return false; //there is an (allied) occupant here
    }

    public bool CanBeAttacked(Card_Permanent attacker)
    {
        if (_occupant == null) return false; //nothing to attack
        if (_occupant.Commander == attacker.Commander) return false; //same team
        return true; //occupied by enemy
    }

    public void OnEnterNode(Card_Unit unit)
    {
        if (_trap != null && _trap.Commander != unit.Commander)
        {
            Debug.Log("Trap has been activated!");

            //apply effects of trap
            //could be damage, could be a persistent effect, could be counters, could be StopMovement
            //don't deal with speed -1 while the unit is already mvoing
        }
    }
    #endregion
}
