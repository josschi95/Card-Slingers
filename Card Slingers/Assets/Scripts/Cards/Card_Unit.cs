using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Unit : Card_Permanent
{
    public delegate void OnAnimatorEventCallback();
    public OnAnimatorEventCallback onAttackAnimation;
    public OnAnimatorEventCallback onAbilityAnimation;
    public OnAnimatorEventCallback onDeathAnimation;
    protected Animator _animator;

    [Header("Unit Info")]
    [SerializeField] protected int _currentHealth;
    [SerializeField] private List<Card_Permanent> _equipment;

    [SerializeField] private int[] _statModifiers = new int[System.Enum.GetNames(typeof(UnitStat)).Length];

    private bool _isMoving;
    private bool _isAttacking;
    protected Card_Permanent _attackTarget;
    protected bool _canRetaliate;
    private bool _actedThisTurn;

    #region - Properties -
    public int MaxHealth => NetMaxHealth();
    public int CurrentHealth => _currentHealth;
    public int Damage => NetDamage();
    public int Range => NetRange();
    public int Defense => NetDefense();
    public int Speed => NetSpeed();
    public List<Card_Permanent> Equipment => _equipment;

    public bool IsActing => _isAttacking || _isMoving;
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

        //stat tokens
    }

    public override void OnSummoned(GridNode node)
    {
        //set max health before occupying node
        _currentHealth = NetMaxHealth();

        base.OnSummoned(node);
        
        //Summon prefab before getting anim
        _animator = PermanentObject.GetComponent<Animator>();

        _equipment = new List<Card_Permanent>();

        onAttackAnimation += OnAttackAnimationTrigger;
        onDeathAnimation += OnUnitDeathAnimationComplete;
    }

    protected override int GetThreatLevel()
    {
        if (Commander is PlayerCommander) return _currentHealth + Damage + Defense;
        else return -(_currentHealth + Damage + Defense);
    }

    protected override void OnCommanderVictory()
    {
        if (_location != CardLocation.OnField) return;
        _animator.SetTrigger("victory");
        StartCoroutine(OnRemoveUnit(false));
    }

    protected override void OnCommanderDefeat()
    {
        if (_location != CardLocation.OnField) return;
        OnPermanentDestroyed();
    }
    #endregion

    #region - Unit Stats -
    public void AddModifier(UnitStat stat, int modifier = 1)
    {
        _statModifiers[(int)stat] += modifier;
    }

    protected int NetMaxHealth()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.MaxHealth;
        value += _statModifiers[(int)UnitStat.Health];
        if (value < 0) value = 0;
        return value;
    }

    private int NetDamage()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Attack;
        value += _statModifiers[(int)UnitStat.Attack];
        if (value < 0) value = 0;
        return value;
    }

    private int NetRange()
    {
        if (!CanAttack) return 0;

        var unit = CardInfo as UnitSO;
        int value = unit.Range;
        value += _statModifiers[(int)UnitStat.Range];
        if (value < 0) value = 0;
        return value;
    }

    private int NetDefense()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Defense;
        value += _statModifiers[(int)UnitStat.Defense];
        if (value < 0) value = 0;
        return value;
    }

    private int NetSpeed()
    {
        var unit = CardInfo as UnitSO;
        int value = unit.Speed;
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
    #endregion

    #region - Movement -
    public void MoveToNode(GridNode newNode)
    {
        var nodePath = new List<GridNode>(DuelManager.instance.Battlefield.FindNodePath(this, newNode));
        StartCoroutine(MoveUnit(nodePath));
    }

    public void MoveAlongNodePath(List<GridNode> nodePath)
    {
        StartCoroutine(MoveUnit(nodePath));
    }
    
    private IEnumerator MoveUnit(List<GridNode> nodePath)
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

            while (Vector3.Distance(transform.position, nodePath[0].transform.position) > 0.1f)
            {
                _animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
                FaceTarget(nodePath[0].transform.position);
                yield return null;
            }

            transform.position = nodePath[0].transform.position;
            nodePath[0].onNodeEntered?.Invoke(this);
            currentNode = nodePath[0];
            nodePath.RemoveAt(0);

            yield return null;
        }

        //occupies the new node
        OnOccupyNode(endNode);
        OnStopMovement();
    }

    protected IEnumerator TurnToFaceTarget(Vector3 pos)
    {
        float t = 0, timeToMove = 0.5f;
        while (t < timeToMove)
        {
            FaceTarget(pos);
            t += Time.deltaTime;
            yield return null;
        }
    }

    private void FaceTarget(Vector3 pos) //update this to accept a Transform transform?
    {
        Vector3 direction = (pos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 25f);
    }

    private void OnStopMovement()
    {
        _animator.SetFloat("speed", 0);
        _isMoving = false;
        DuelManager.instance.onCardEndAction?.Invoke(this);
    }
    #endregion

    protected override void OnBeginPhase()
    {
        base.OnBeginPhase();
        _actedThisTurn = false;
    }

    public bool HasActed
    {
        get => _actedThisTurn;
        set
        {
            _actedThisTurn = value;
        }
    }

    #region - Combat -
    //resolve an attack action which has been declared 
    public void OnAttack(GridNode node)
    {
        if (!CanAttack) return; //shouldn't have gotten here if this is already false, but worth checking

        //out of range, movement was likely stopped before getting within range
        //if (Mathf.Abs(Node.gridZ - node.gridZ) > Range)
        if (DuelManager.instance.Battlefield.GetDistanceInNodes(Node, node) > Range)
        {
            Debug.Log("Target located at " + node.gridX + "," + node.gridZ + " is further than " 
                + Range + " units from " + Node.gridX + "," + Node.gridZ);
            return;
        }

        _isAttacking = true;
        _canRetaliate = false;
        DuelManager.instance.onCardBeginAction?.Invoke(this);
        StartCoroutine(TurnToFaceTarget(node.transform.position));
        node.Occupant.OnTargetEngaged(this);
        
        _attackTarget = node.Occupant;
        _animator.SetTrigger("attack");
    }

    protected void OnAttackAnimationTrigger()
    {
        _attackTarget.OnTakeDamage(Damage);
        _isAttacking = false;
        DuelManager.instance.onCardEndAction?.Invoke(this);
    }

    public override void OnTakeDamage(int damage)
    {
        damage = Mathf.Clamp(damage - Defense, 0, int.MaxValue);
        _currentHealth -= damage;

        GameManager.instance.GetBloodParticles(transform.position + Vector3.up);

        if (_currentHealth <= 0) OnPermanentDestroyed();
        else
        {
            _animator.SetTrigger("damage");
            if (UnitCanRetaliate())
            {
                Debug.Log("Unit can retaliate");
                DuelManager.instance.onCardBeginAction?.Invoke(this);
                StartCoroutine(TurnToFaceTarget(_attackTarget.transform.position));
                _animator.SetTrigger("attack");
                _canRetaliate = false;
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
        //Debug.Log("unit has been destroyed");
        _animator.SetTrigger("death");
        DuelManager.instance.onCardMovementStarted?.Invoke(this);
    }

    protected virtual void OnUnitDeathAnimationComplete()
    {
        StartCoroutine(OnRemoveUnit());
    }

    protected virtual IEnumerator OnRemoveUnit(bool sinkUnit = true)
    {
        float timeElapsed = 0, timeToMove = 1.5f;
        if (!sinkUnit) timeToMove = 2f;
        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime; //slowly sink the unit beneath the playing field before destroying it
            if (sinkUnit) PermanentObject.transform.localPosition = Vector3.Lerp(PermanentObject.transform.localPosition, Vector3.down, timeElapsed / timeToMove);
            yield return null;
        }

        cardGFX.SetActive(true); //Re-enable card GFX
        Destroy(PermanentObject); //Destroy unit

        //Invoke an event for the commander to listen to
        Commander.onPermanentDestroyed?.Invoke(this);
        DuelManager.instance.onCardMovementEnded?.Invoke(this);
    }
    #endregion
}

public enum UnitStat { Health, Attack, Range, Defense, Speed }
