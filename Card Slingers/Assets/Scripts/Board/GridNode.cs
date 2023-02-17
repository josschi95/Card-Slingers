using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class GridNode : MonoBehaviour, IInteractable
{
    private IObjectPool<GridNode> _pool;
    
    public delegate void OnGridNodeValueChanged(GridNode node);
    public OnGridNodeValueChanged onNodeValueChanged;

    public delegate void OnGridNodeEnteredCallback(Card_Unit unit);
    public OnGridNodeEnteredCallback onNodeEntered;

    private void TEST_SUMMON_ENEMY()
    {
        DuelManager.instance.SummonTestEnemy(this);
    }

    public enum MaterialType { Normal, Blue, Red, Yellow };
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material[] highlightMats;
    [Space]

    [SerializeField] private Card_Permanent _occupant = null;
    [SerializeField] private Card_Trap _trap;
    [SerializeField] private Card _terrain;
    private bool _isPlayerNode; //located on player half of the grid

    public int occupantPower { get; private set; }

    private bool _lockedForDisplay; //ignore mouse movements to change the color

    public int gridX { get; private set; }
    public int gridZ { get; private set; }
    public bool IsPlayerNode => _isPlayerNode;
    public Card Terrain => _terrain;

    public void SetPool(IObjectPool<GridNode> pool) => _pool = pool;

    //Return the node to its pool when a match is finished
    public void ReleaseToPool()
    {
        if (_pool != null) _pool.Release(this);
        else Destroy(gameObject);
    }

    public void OnAssignCoordinates(int x, int z, bool isPlayerNode)
    {
        gridX = x; gridZ = z;
        _isPlayerNode = isPlayerNode;
    }

    public Card_Permanent Occupant
    {
        get => _occupant;
        set
        {
            if (_occupant != null)
            {
                if (value != null) Debug.LogError("Node " + gridX + "," + gridZ + "is already occupied by " + _occupant.name);
                _occupant.onValueChanged -= UpdateOccupantPower;
            }

            _occupant = value;
            UpdateOccupantPower();

            if (_occupant != null) _occupant.onValueChanged += UpdateOccupantPower;

            onNodeValueChanged?.Invoke(this);
        }
    }

    public Card_Trap Trap
    {
        get => _trap;
        set
        {
            if (_trap != null && value != null) return;

            _trap = value;
        }
    }

    private void UpdateOccupantPower()
    {
        if (_occupant == null) occupantPower = 0;
        else occupantPower = _occupant.ThreatLevel;

        onNodeValueChanged?.Invoke(this);
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
        if (_occupant is Card_Structure structure && structure.CanBeOccupied) return true; //can move into structure

        return false; //there is an (allied) occupant here
    }

    public bool CanBeAttacked(Card_Permanent attacker)
    {
        if (_occupant == null) return false; //nothing to attack
        if (_occupant.Commander == attacker.Commander) return false; //same team
        return true; //occupied by enemy
    }
    #endregion
}

/* Old Methods
 * 
    public void SetOccupant(Card_Permanent occupant)
    {
        if (_occupant != null) throw new System.Exception("Node " + gridX + "," + gridZ + "is already occupied by " + _occupant.name);
        _occupant = occupant;
        UpdateOccupantPower();
        _occupant.onValueChanged += UpdateOccupantPower;

        onNodeValueChanged?.Invoke(this);
    }

    public void ClearOccupant()
    {
        if (_occupant != null) _occupant.onValueChanged -= UpdateOccupantPower;
        _occupant = null;
        UpdateOccupantPower();

        onNodeValueChanged?.Invoke(this);
    }
 * 
 */