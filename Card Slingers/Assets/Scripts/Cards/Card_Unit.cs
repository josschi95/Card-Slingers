using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class Card_Unit : Card_Permanent
{
    public delegate void OnAnimatorEventCallback();
    public OnAnimatorEventCallback onAbilityAnimation;
    protected Animator _animator;
    private UnitSO _unitInfo;
    [Space]

    [SerializeField] protected int _currentHealth;
    [SerializeField] private List<Card_Permanent> _equipment;

    [SerializeField] private int[] _statModifiers = new int[System.Enum.GetNames(typeof(UnitStat)).Length];

    private bool _canUseAbility;
    private bool _isMoving;
    private bool _isAttacking;

    protected Card_Permanent _attackTarget;

    protected bool _hasTakenAction;
    protected bool _canRetaliate;
    private bool _isDestroyed;

    protected int _movesLeft;

    #region - Properties -
    public int MaxHealth => NetMaxHealth();
    public int CurrentHealth => _currentHealth;
    public int Attack => NetDamage();
    public int Range => NetRange();
    public int Defense => NetDefense();
    public int Speed => NetSpeed();
    public List<Card_Permanent> Equipment => _equipment;

    public bool IsActing => _isAttacking || _isMoving;
    public bool CanAct => UnitCanAct();
    public bool CanMove => UnitCanMove();
    public bool CanAttack => UnitCanAttack();
    public bool CanUseAbility => UnitCanUseAbility();
    public int MovesLeft => _movesLeft;
    #endregion

    public Card_Unit(UnitSO unit, bool isPlayerCard) : base(unit, isPlayerCard)
    {
        _cardInfo = unit;
        _unitInfo = unit;
        this.isPlayerCard = isPlayerCard;
    }

    #region - Override Methods -
    public override void OnSummoned(Summon summon, GridNode node)
    {
        //set max health before occupying node
        _isDestroyed = false;
        _movesLeft = Speed;
        _hasTakenAction = false;

        _currentHealth = NetMaxHealth();

        base.OnSummoned(summon, node);
        
        //Summon prefab before getting anim
        _summon.Card = this;
        _animator = Summon.GetComponent<Animator>();
        _summon.GetComponent<AnimationEventHandler>().Unit = this;
        _equipment = new List<Card_Permanent>();
    }

    protected override int GetThreatLevel()
    {
        return _currentHealth + Attack + Defense;
    }

    protected override void OnCommanderVictory()
    {
        if (_location != CardLocation.OnField) return;
        _animator.SetTrigger("victory");
        //StartCoroutine(OnRemoveUnit(false));
    }

    protected override void OnCommanderDefeat()
    {
        if (_location != CardLocation.OnField) return;
        OnPermanentDestroyed();
    }
    #endregion

    #region - Unit Stats -
    private void ResetAllStats()
    {
        _currentHealth = _unitInfo.MaxHealth;

        for (int i = 0; i < _statModifiers.Length; i++)
        {
            _statModifiers[i] = 0;
        }
    }

    public void AddModifier(UnitStat stat, int modifier = 1)
    {
        _statModifiers[(int)stat] += modifier;
    }

    protected int NetMaxHealth()
    {
        int value = _unitInfo.MaxHealth;
        value += _statModifiers[(int)UnitStat.Health];
        if (value < 0) value = 0;
        return value;
    }

    private int NetDamage()
    {
        int value = _unitInfo.Attack;
        value += _statModifiers[(int)UnitStat.Attack];
        if (value < 0) value = 0;
        return value;
    }

    private int NetRange()
    {
        if (!CanAttack) return 0;

        int value = _unitInfo.Range;
        value += _statModifiers[(int)UnitStat.Range];
        if (value < 0) value = 0;
        return value;
    }

    private int NetDefense()
    {
        int value = _unitInfo.Defense;
        value += _statModifiers[(int)UnitStat.Defense];
        if (value < 0) value = 0;
        return value;
    }

    private int NetSpeed()
    {
        int value = _unitInfo.Speed;
        value += _statModifiers[(int)UnitStat.Speed];
        if (value < 0) value = 0;
        return value;
    }
    #endregion

    #region - Unit Conditions -   
    private bool UnitCanAct()
    {
        if (_currentHealth <= 0) return false;
        //return false if under some sort of effect such as stun... whatever
        return true;
    }

    private bool UnitCanMove()
    {
        if (!CanAct) return false;
        if (_movesLeft <= 0) return false;
        if (Speed == 0) return false;

        return true;
    }

    private bool UnitCanAttack()
    {
        if (!CanAct) return false;
        return true;
    }

    protected bool UnitCanRetaliate()
    {
        if (!CanAttack) return false;
        if (!_canRetaliate) return false;
        if (_attackTarget == null) return false;
        if (DuelManager.instance.Battlefield.GetDistanceInNodes(Node, _attackTarget.Node) > Range) return false;
        return true;
    }

    private bool UnitCanUseAbility()
    {
        if (!CanAct) return false;

        //if within a cooldown window

        //if there is no valid target

        //if cannot act

        return false;
    }
    #endregion

    #region - Effects -
    public void OnHalt()
    {
        _movesLeft = 0; //Will stop unit from moving anymore
    }

    #endregion

    #region - Movement -   
    public IEnumerator MoveUnit(List<GridNode> nodePath)
    {
        _isMoving = true;
        DuelManager.instance.onCardBeginAction?.Invoke(this);
        var currentNode = Node; //the node that the unit is currently located at, changes each time they move
        var endNode = nodePath[nodePath.Count - 1];

        //ignore the currently occupied node
        if (nodePath[0] == Node) nodePath.RemoveAt(0);
        OnAbandonNode(); //abandon the currently occupied node

        while (nodePath.Count > 0)
        {
            if (!CanMove || _currentHealth <= 0)  //movement has been stopped by an effect, or unit's health was reduced to 0
            {
                if (_currentHealth > 0) OnOccupyNode(currentNode);
                OnStopMovement();
                yield break;
            }

            while (Vector3.Distance(_summon.Transform.position, nodePath[0].transform.position) > 0.1f)
            {
                _animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
                _summon.FaceTarget(nodePath[0].Transform.position);
                yield return null;
            }

            _movesLeft--;

            _summon.Transform.position = nodePath[0].transform.position;
            nodePath[0].onNodeEntered?.Invoke(this);
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
        DuelManager.instance.onCardEndAction?.Invoke(this);
    }
    #endregion

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        _movesLeft = Speed;
        _hasTakenAction = false;
    }

    public bool HasActed
    {
        get => _hasTakenAction;
        set
        {
            _hasTakenAction = value;
        }
    }

    #region - Combat -
    //resolve an attack action which has been declared 
    public void OnAttack(GridNode node)
    {
        if (!CanAttack) return; //shouldn't have gotten here if this is already false, but worth checking
        //out of range, movement was likely stopped before getting within range
        if (DuelManager.instance.Battlefield.GetDistanceInNodes(Node, node) > Range)
        {
            Debug.Log("Target located at " + node.gridX + "," + node.gridZ + " is further than " 
                + Range + " units from " + Node.gridX + "," + Node.gridZ);
            return;
        }

        _isAttacking = true;
        _canRetaliate = false;
        DuelManager.instance.onCardBeginAction?.Invoke(this);
        _summon.FaceTargetCoroutine(node.Transform.position);
        node.Occupant.OnTargetEngaged(this);
        
        _attackTarget = node.Occupant;
        _animator.SetTrigger("attack");
        _hasTakenAction = true;
    }

    public void OnAttackAnimationTrigger()
    {
        if (_attackTarget == null)
        {
            Debug.LogWarning("Attack target is null for attacker at " + Node.gridX + "," + Node.gridZ);
            return;
        }
        _attackTarget.OnTakeDamage(Attack);
        _isAttacking = false;
        _attackTarget = null;
        DuelManager.instance.onCardEndAction?.Invoke(this);
    }

    public override void OnTakeDamage(int damage)
    {
        damage = Mathf.Clamp(damage - Defense, 0, int.MaxValue);
        _currentHealth -= damage;

        _summon.OnDamage();

        if (_currentHealth <= 0) OnPermanentDestroyed();
        else
        {
            _animator.SetTrigger("damage");
            if (_attackTarget != null)
            {
                if (UnitCanRetaliate())
                {
                    DuelManager.instance.onCardBeginAction?.Invoke(this);
                    _summon.FaceTargetCoroutine(_attackTarget.Node.Transform.position);
                    _animator.SetTrigger("attack");
                    _canRetaliate = false;
                }
                else
                {
                    _attackTarget = null;
                    _canRetaliate = false;
                }
            }
        }

        onValueChanged?.Invoke();
    }

    public override void OnTargetEngaged(Card_Unit attacker)
    {
        //another unit has engaged this unit
        _canRetaliate = true;
        _attackTarget = attacker;
    }

    public void OnRegainHealth(int amount)
    {
        _currentHealth += amount;
        if (_currentHealth > MaxHealth) _currentHealth = MaxHealth;
    }
    #endregion

    #region - Card Destroyed -
    protected override void OnPermanentDestroyed()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        OnRemoveFromField();
        onPermanentDestroyed?.Invoke(this);

        _animator.SetTrigger("death");
        DuelManager.instance.onCardMovementStarted?.Invoke(this);
    }

    public virtual IEnumerator OnRemoveUnit(bool sinkUnit = true)
    {
        float timeElapsed = 0, timeToMove = 1.5f;
        if (!sinkUnit) timeToMove = 2f;
        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime; //slowly sink the unit beneath the playing field before destroying it
            if (sinkUnit) Summon.transform.localPosition = Vector3.Lerp(Summon.transform.localPosition, Vector3.down, timeElapsed / timeToMove);
            yield return null;
        }

        OnRemoveFromField();
        Object.Destroy(Summon); //Destroy unit

        //Invoke an event for the commander to listen to
        onRemovedFromField?.Invoke(this);
        DuelManager.instance.onCardMovementEnded?.Invoke(this);
    }
    #endregion
}

public enum UnitStat { Health, Attack, Range, Defense, Speed }
