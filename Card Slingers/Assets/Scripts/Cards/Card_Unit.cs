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
    [SerializeField] private List<int> _speedModifiers;

    private bool _canMove = true, _canAttack = true;

    public bool isActing { get; private set; } //reference for if the unit is moving/attacking 

    #region - Public Reference Variables -
    public int MaxHealth => NetMaxHealth();
    public int CurrentHealth => _currentHealth;
    public int Damage => NetDamage();
    public int Range => NetRange();
    public int Defense => NetDefense();
    public int Speed => NetSpeed();
    public List<Card_Permanent> Equipment => _equipment;
    #endregion

    #region - Inherited Methods -
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
        _speedModifiers = new List<int>();

        _currentHealth = NetMaxHealth();
    }
    #endregion

    #region - Unit Stats -
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

    private int NetSpeed()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Speed;
        _speedModifiers.ForEach(x => value += x);
        if (value < 0) value = 0;
        return value;
    }
    #endregion

    public void MoveToNode(GridNode newNode)
    {
        StartCoroutine(MoveCard(newNode));
        //abandon first node

        //run while loop for node

        //occupy new node
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

    private IEnumerator MoveCard(GridNode endNode)
    {
        isActing = true;

        var nodePath = new List<GridNode>(DuelManager.instance.Battlefield.GetNodePath(OccupiedNode, endNode));
        var currentNode = OccupiedNode; //the node that the unit is currently located at, changes each time they move

        /*!!! Major Flaw: If the unit moves into a space occupied by an ally, and then the next node has a trap that prevents entrance
         * Then the units end position would be the same as that of their ally, which is not allowed. 
         * So final verdict is that you can always move into the space of a trap and occupy it, but they'll often also stop movement!!!*/

        //ignore the currently occupied node
        if (nodePath[0] == OccupiedNode) nodePath.RemoveAt(0);

        OnAbandonNode(); //abandon the currently occupied node

        while(nodePath.Count > 0)
        {
            if (!_canMove)  //movement has been stopped by an effect
            {
                endNode = currentNode;
                yield break;
            }

            nodePath[0].OnEnterNode(this);
            transform.position = nodePath[0].transform.position;
            currentNode = nodePath[0];

            //Remove the current node from the list
            nodePath.RemoveAt(0);
            Debug.Log("still moving");
            yield return new WaitForSeconds(3);
        }

        //occupies the new node
        OnOccupyNode(endNode);
        isActing = false;
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
