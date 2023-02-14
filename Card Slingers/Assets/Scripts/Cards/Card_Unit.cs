using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Unit : Card_Permanent
{
    [Header("Unit Info")]
    [SerializeField] protected int _currentHealth;
    [SerializeField] private List<Card_Permanent> _equipment;

    [SerializeField] private List<int> _healthModifiers;
    [SerializeField] private List<int> _attackModifiers;
    [SerializeField] private List<int> _rangeModifiers;
    [SerializeField] private List<int> _defenseModifiers;
    [SerializeField] private List<int> _speedModifiers;

    private bool _isMoving;
    private bool _isAttacking;

    #region - Animation -
    protected Animator _animator;
    private bool _waitingForDeathAnim;
    #endregion

    #region - Public Reference Variables -
    public int MaxHealth => NetMaxHealth();
    public int CurrentHealth => _currentHealth;
    public int Damage => NetDamage();
    public int Range => NetRange();
    public int Defense => NetDefense();
    public int Speed => NetSpeed();
    public List<Card_Permanent> Equipment => _equipment;

    public bool IsActing => UnitIsActing();
    public bool CanAct => UnitCanAct();
    public bool CanMove => UnitCanMove();
    public bool CanAttack => UnitCanAttack();
    #endregion

    #region - Override Methods -
    protected override void SetCardDisplay()
    {
        base.SetCardDisplay();

        //Also show indicator for all stats

        //An indicator for current equipment

        //status effects, if that becomes a thing
    }

    public override void OnSummoned(GridNode node)
    {
        //set max health before occupying node
        _currentHealth = NetMaxHealth();

        base.OnSummoned(node);
        
        //Summon prefab before getting anim
        _animator = PermanentObject.GetComponent<Animator>();

        _equipment = new List<Card_Permanent>();
        _healthModifiers = new List<int>();
        _attackModifiers = new List<int>();
        _rangeModifiers = new List<int>();
        _defenseModifiers = new List<int>();
        _speedModifiers = new List<int>();
    }

    protected override int GetPowerLevel()
    {
        if (Commander is PlayerCommander) return _currentHealth + Damage + Defense;
        else return -(_currentHealth + Damage + Defense);
    }

    public override void OnCommanderVictory()
    {
        _animator.SetTrigger("victory");
        StartCoroutine(WaitForDeathAnim());
    }

    public override void OnCommanderDefeat()
    {
        OnPermanentDestroyed();
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
        if (!CanAttack) return 0;

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

    #region - Unit Conditions -
    private bool UnitIsActing()
    {
        if (_isMoving) return true;
        if (_isAttacking) return true;
        return false;
    }
    
    private bool UnitCanAct()
    {
        return true;
    }

    private bool UnitCanMove()
    {
        if (!CanAct) return false;
        if (Speed == 0) return false;

        return true;
    }

    private bool UnitCanAttack()
    {
        if (!CanAct) return false;
        return true;
    }
    #endregion

    #region - Movement -
    public void MoveToNode(GridNode newNode)
    {
        StartCoroutine(MoveUnit(newNode));
    }
    
    private IEnumerator MoveUnit(GridNode endNode)
    {
        _isMoving = true;
        var nodePath = new List<GridNode>(DuelManager.instance.Battlefield.GetNodePath(OccupiedNode, endNode));
        var currentNode = OccupiedNode; //the node that the unit is currently located at, changes each time they move

        float speed = 1; //set whether walking forwards or back
        if (OccupiedNode.gridZ > endNode.gridZ) speed = -1;

        //ignore the currently occupied node
        if (nodePath[0] == OccupiedNode) nodePath.RemoveAt(0);
        OnAbandonNode(); //abandon the currently occupied node

        while(nodePath.Count > 0)
        {         
            if (!CanMove)  //movement has been stopped by an effect
            {
                OnOccupyNode(currentNode);
                OnStopMovement();
                yield break;
            }
            else if (_currentHealth <= 0) //unit's health has been reduced to 0, likely a trap
            {
                OnStopMovement();
                yield break;
            }

            while (Vector3.Distance(transform.position, nodePath[0].transform.position) > 0.1f)
            {
                _animator.SetFloat("speed", speed, 0.1f, Time.deltaTime);
                //timeElaspsed += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            transform.position = nodePath[0].transform.position;
            nodePath[0].OnEnterNode(this);
            currentNode = nodePath[0];
            nodePath.RemoveAt(0);

            yield return null;
        }

        //occupies the new node
        OnOccupyNode(endNode);
        OnStopMovement();
    }

    private void OnStopMovement()
    {
        _animator.SetFloat("speed", 0);
        _isMoving = false;
    }
    #endregion

    public void AttackNode(GridNode node)
    {
        if (!CanAttack) return; //can't attack, shouldn't have gotten here if this is already false, but worth checking

        //out of range, movement was likely stopped before getting within range
        if (Mathf.Abs(OccupiedNode.gridZ - node.gridZ) > Range)
        {
            Debug.Log("Target out of range");
            return;
        }

        _animator.SetTrigger("attack");

        //should include some sort of delay here to trigger the damage when the attack would actually hit, instead of immediately

        node.Occupant.OnTakeDamage(Damage);
    }

    public override void OnTakeDamage(int damage)
    {
        _animator.SetTrigger("damage");
        damage = Mathf.Clamp(damage - Defense, 0, int.MaxValue);
        _currentHealth -= damage;
        Debug.Log(CardInfo.name + " takes " + damage + " damage!");
        if (_currentHealth <= 0) OnPermanentDestroyed();
        onValueChanged?.Invoke();
    }

    public void OnRegainHealth(int amount)
    {
        _currentHealth += amount;
        if (_currentHealth > MaxHealth) _currentHealth = MaxHealth;
    }

    protected override void OnPermanentDestroyed()
    {
        //Debug.Log("unit has been destroyed");
        _animator.SetTrigger("death");
        StartCoroutine(WaitForDeathAnim());
    }

    public void OnDeathAnimCompleted()
    {
        //Debug.Log("Death Anim Complete");
        _waitingForDeathAnim = false;
    }

    private IEnumerator WaitForDeathAnim()
    {
        _waitingForDeathAnim = true;
        while (_waitingForDeathAnim == true)
        {
            yield return null;
        }

        float timeElapsed = 0, timeToMove = 2f;
        
        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime; //slowly sink the unit beneath the playing field before destroying it
            PermanentObject.transform.localPosition = Vector3.Lerp(PermanentObject.transform.localPosition, Vector3.down, timeElapsed / timeToMove);
            yield return null;
        }

        cardGFX.SetActive(true); //Re-enable card
        Destroy(PermanentObject); //Destroy unit

        yield return new WaitForSeconds(0.1f);
        //Invoke an event for the commander to listen to
        Commander.onPermanentDestroyed?.Invoke(this);
    }
}

public enum UnitStat { Health, Attack, Range, Defense, Speed }
