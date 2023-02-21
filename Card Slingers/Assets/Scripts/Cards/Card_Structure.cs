using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Structure : Card_Permanent
{
    [Header("Structure Info")]
    [SerializeField] private Card_Unit _occupant;
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _currentHealth;

    public CardFocus Focus => StructurePurpose();
    public int MaxHealth => _maxHealth;
    public int Defense => NetDefense();
    public int CurrentHealth => _currentHealth;
    public Card_Permanent Occupant => _occupant;
    public bool CanBeOccupied => StructureCanBeOccupied();
    public bool IsOccupied => _occupant != null;
    public bool canBeTraversed => CanBeTraversed();

    #region - Override Methods -
    protected override void SetCardDisplay()
    {
        base.SetCardDisplay();

        //Also show indicator for current health

        //An indicator for current occupant
    }

    public override void OnSummoned(GridNode node)
    {
        var info = CardInfo as StructureSO;
        _maxHealth = info.MaxHealth;
        _currentHealth = _maxHealth;

        base.OnSummoned(node);
    }

    private int NetDefense()
    {
        var unit = CardInfo as StructureSO;
        int value = unit.Defense;
        if (value < 0) value = 0;
        return value;
    }

    protected override int GetThreatLevel()
    {
        if (Commander is PlayerCommander) return _currentHealth;
        else return -_currentHealth;
    }

    public override void OnTakeDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0) OnPermanentDestroyed();
        onValueChanged?.Invoke();
    }

    protected override void OnPermanentDestroyed()
    {
        Debug.Log("unit has been destroyed");

        //explosions or something

        //start a coroutine to wait until anim is finished playing
        StartCoroutine(WaitToRemove());
    }
    #endregion
    
    private CardFocus StructurePurpose()
    {
        var card = CardInfo as StructureSO;
        return card.Focus;
    }

    private bool StructureCanBeOccupied()
    {
        var info = CardInfo as StructureSO;

        if (!info.canBeOccupied) return false;
        if (IsOccupied) return false;

        return true;
    }

    private bool CanBeTraversed()
    {
        var card = CardInfo as StructureSO;
        return card.CanBeTraversed;
    }

    private IEnumerator WaitToRemove()
    {
        yield return new WaitForSeconds(2);
        //include a part where the gameObject sinks beneath the battlefield
        cardGFX.SetActive(true); //Re-enable card
        Destroy(PermanentObject); //Destroy unit

        //Invoke an event for the commander to listen to
        Commander.onPermanentDestroyed?.Invoke(this);
    }

    protected override void OnCommanderVictory()
    {
        if (_location != CardLocation.OnField) return;
        StartCoroutine(WaitToRemove());
    }

    protected override void OnCommanderDefeat()
    {
        if (_location != CardLocation.OnField) return;
        OnPermanentDestroyed();
    }
}

