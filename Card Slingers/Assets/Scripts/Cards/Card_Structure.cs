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

    public Card_Structure(StructureSO structure, bool isPlayerCard) : base(structure, isPlayerCard)
    {
        _cardInfo = structure;
        this.isPlayerCard = isPlayerCard;
    }

    #region - Override Methods -
    public override void OnSummoned(Summon summon, GridNode node)
    {
        var info = CardInfo as StructureSO;
        _maxHealth = info.MaxHealth;
        _currentHealth = _maxHealth;

        base.OnSummoned(summon, node);
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
        if (isPlayerCard) return _currentHealth;
        else return -_currentHealth;
    }

    public override void OnTakeDamage(int damage)
    {
        _currentHealth -= damage;
        _summon.OnDamage();
        if (_currentHealth <= 0) OnPermanentDestroyed();
        onValueChanged?.Invoke();
    }

    protected override void OnPermanentDestroyed()
    {
        onPermanentDestroyed?.Invoke(this);

        OnRemoveFromField();

        //explosions or something

        _summon.DestroyAfterDelay(2);

        onRemovedFromField?.Invoke(this);
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

    protected override void OnCommanderVictory()
    {
        if (_location != CardLocation.OnField) return;
        OnPermanentDestroyed();
    }

    protected override void OnCommanderDefeat()
    {
        if (_location != CardLocation.OnField) return;
        OnPermanentDestroyed();
    }
}

