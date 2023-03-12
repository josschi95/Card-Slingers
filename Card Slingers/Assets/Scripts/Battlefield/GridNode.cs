using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class GridNode : MonoBehaviour, IInteractable
{
    private IObjectPool<GridNode> _pool;

    [SerializeField] private Transform _transform;
    public Transform Transform => _transform;

    #region - Callbacks -
    public delegate void OnGridNodeValueChanged(GridNode node);
    public OnGridNodeValueChanged onNodeValueChanged;

    public delegate void OnGridNodeEnteredCallback(Card_Unit unit);
    public OnGridNodeEnteredCallback onNodeEntered;
    #endregion

    #region - Display -
    public enum MaterialType { Normal, Blue, Red, Yellow, Green };
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material[] highlightMats;
    private bool _lockedForDisplay; //ignore mouse movements to change the color
    [Space]
    #endregion

    #region - Node Contents -
    [SerializeField] private Card_Permanent _occupant = null;
    [SerializeField] private Card_Trap _trap = null;
    [SerializeField] private Card _terrain = null;
    [SerializeField] private Obstacle _obstacle = null;
    public Card_Permanent Occupant
    {
        get => _occupant;
        set
        {
            if (_occupant != null)
            {
                if (value != null) Debug.LogError("Node " + gridX + "," + gridZ + "is already occupied by " + _occupant.CardInfo.name);
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
    public Card Terrain => _terrain;
    public Obstacle Obstacle
    {
        get => _obstacle;
        set
        {
            _obstacle = value;
        }
    }
    public int occupantPower { get; private set; }
    #endregion

    #region - Pathfinding Properties -
    public int gridX { get; private set; }
    public int gridZ { get; private set; }

    [HideInInspector] public int gCost; //the movement cost to move from the start node to this node, following the existing path
    [HideInInspector] public int hCost; //the estimated movement cost to move from this node to the end node
    [HideInInspector] public int fCost; //the current best guess as to the cost of the path
    [HideInInspector] public GridNode cameFromNode;
    #endregion

    #region - Pooling -
    public void SetPool(IObjectPool<GridNode> pool) => _pool = pool;

    //Return the node to its pool when a match is finished
    public void ReleaseToPool()
    {
        if (_pool != null) _pool.Release(this);
        else Destroy(gameObject);
    }
    #endregion

    public void OnAssignCoordinates(int x, int z)
    {
        gridX = x; gridZ = z;
    }

    public void CalculateFCost()
    {
        fCost = gCost + hCost;
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
        PlayerController.instance.onNodeSelected?.Invoke(this);
        DuelManager.instance.onNodeSelected?.Invoke(this);
    }

    public void OnRightClick()
    {
        if (Occupant != null) DungeonUIManager.instance.ShowCardDisplay(_occupant.CardInfo);
    }

    public void OnMouseEnter()
    {
        DuelManager.instance.onNodeMouseEnter?.Invoke(this);

        if (_lockedForDisplay || Occupant == null) return;
        else if (Occupant.isPlayerCard) SetColor(MaterialType.Blue);
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
        if (meshRenderer == null) return;

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
    public bool CanBeOccupied()
    {
        if (_obstacle != null || _occupant != null) return false;
        return true; //there is an occupant here
    }

    public bool CanBeAttacked(Card_Permanent attacker)
    {
        if (_obstacle != null) return true;

        if (_occupant == null) return false; //nothing to attack
        if (_occupant.isPlayerCard == attacker.isPlayerCard) return false; //same team
        return true; //occupied by enemy
    }

    public bool CanBeTraversed(Card_Unit unit)
    {
        if (_obstacle != null) return false;

        if (_occupant == null) return true; //not occupied at all
        if (_occupant.isPlayerCard != unit.isPlayerCard) return false; //cannot walk through enemy space
        if (_occupant is Card_Structure structure && !structure.canBeTraversed) return false; //no unit can enter this space
        return true;
    }
    #endregion
}