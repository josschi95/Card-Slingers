using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Structure : Card_Permanent
{
    [Header("Structure Info")]
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _currentHealth;
    [SerializeField] private Card_Unit _occupant;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public Card_Permanent Occupant => _occupant;
    public bool CanBeOccupied => StructureCanBeOccupied();
    public bool IsOccupied => _occupant != null;

    protected override void SetCardDisplay()
    {
        base.SetCardDisplay();

        //Also show indicator for current health

        //An indicator for current occupant
    }

    public override void OnSummoned(GridNode node)
    {
        base.OnSummoned(node);

        var info = CardInfo as StructureSO;
        _maxHealth = info.maxHealth;
        _currentHealth = _maxHealth;
    }

    private bool StructureCanBeOccupied()
    {
        var info = CardInfo as StructureSO;

        if (!info.canBeOccupied) return false;
        if (IsOccupied) return false;

        return true;
    }


}
