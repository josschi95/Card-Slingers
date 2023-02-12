using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Unit : Card_Permanent
{
    [Header("Unit Info")]
    [SerializeField] private Animator _animator;
    [SerializeField] private int _currentHealth;
    [SerializeField] private List<Card_Permanent> _equipment;

    [SerializeField] private List<int> _healthModifiers;
    [SerializeField] private List<int> _attackModifiers;
    [SerializeField] private List<int> _rangeModifiers;
    [SerializeField] private List<int> _defenseModifiers;
    [SerializeField] private List<int> _movementModifiers;


    public int MaxHealth => NetMaxHealth();
    public int CurrentHealth => _currentHealth;
    public int Damage => NetDamage();
    public int Range => NetRange();
    public int Defense => NetDefense();
    public int Speed => NetMoveRange();
    public List<Card_Permanent> Equipment => _equipment;

    protected override void SetCardDisplay()
    {
        base.SetCardDisplay();

        //Also show indicator for all stats

        //An indicator for current equipment

        //status effects, if that becomes a thing
    }

    public override void OnSummoned(GridNode node)
    {
        base.OnSummoned(node);
        _animator = PermanentObject.GetComponent<Animator>();

        _equipment = new List<Card_Permanent>();
        _healthModifiers = new List<int>();
        _attackModifiers = new List<int>();
        _rangeModifiers = new List<int>();
        _defenseModifiers = new List<int>();
        _movementModifiers = new List<int>();

        _currentHealth = NetMaxHealth();
    }

    private int NetMaxHealth()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.MaxHealth;
        _healthModifiers.ForEach(x => value += x);
        if (value < 0) value = 0;
        return value;
    }

    private int NetDamage()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Attack;
        _attackModifiers.ForEach(x => value += x);
        if (value < 0) value = 0;
        return value;
    }

    private int NetRange()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Range;
        _rangeModifiers.ForEach(x => value += x);
        if (value < 0) value = 0;
        return value;
    }

    private int NetDefense()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Defense;
        _defenseModifiers.ForEach(x => value += x);
        if (value < 0) value = 0;
        return value;
    }

    private int NetMoveRange()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Speed;
        _movementModifiers.ForEach(x => value += x);
        if (value < 0) value = 0;
        return value;
    }

    public void TakeDamage(int damage)
    {
        _animator.SetTrigger("damage");

        _currentHealth -= damage;
        if (_currentHealth <= 0) OnUnitDestroyed();
    }

    public void RegainHealth(int amount)
    {
        _currentHealth += amount;
        if (_currentHealth > MaxHealth) _currentHealth = MaxHealth;
    }

    private void OnUnitDestroyed()
    {
        Debug.Log("unit has been destroyed");
        _animator.SetTrigger("death");

        //start a coroutine to wait until anim is finished playing
        StartCoroutine(WaitToRemove());
    }

    private IEnumerator WaitToRemove()
    {
        while (_animator.speed > 0.1) //IDK... 
        {
            yield return null;
        }

        //include a part where the gameObject sinks beneath the battlefield

        Destroy(PermanentObject);

        //Invoke an event for the commander to listen to
        Commander.onPermanentDestroyed?.Invoke(this);
    }
}

public enum UnitStat { Attack, Defense, Speed }
