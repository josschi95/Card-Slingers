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

    #region - Override Methods -
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

    protected override int GetPowerLevel()
    {
        return _currentHealth;
    }

    public override void OnTakeDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0) OnPermanentDestroyed();
    }

    protected override void OnPermanentDestroyed()
    {
        Debug.Log("unit has been destroyed");

        //explosions or something

        //start a coroutine to wait until anim is finished playing
        StartCoroutine(WaitToRemove());
    }
    #endregion
    private bool StructureCanBeOccupied()
    {
        var info = CardInfo as StructureSO;

        if (!info.canBeOccupied) return false;
        if (IsOccupied) return false;

        return true;
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

    public override void OnCommanderVictory()
    {
        StartCoroutine(WaitToRemove());
    }

    public override void OnCommanderDefeat()
    {
        OnPermanentDestroyed();
    }
}

