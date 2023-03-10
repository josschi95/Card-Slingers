using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Commander : Card_Unit 
{
    public Card_Commander(CommanderSO commander, bool isPlayerCard) : base(commander, isPlayerCard)
    {
        _cardInfo = commander;
        this.isPlayerCard = isPlayerCard;
    }

    public void SetStartingNode(GridNode node)
    {
        OnOccupyNode(node);
    }

    public void OnCommanderSummon(Transform parent)
    {
        _currentHealth = NetMaxHealth();
        _movesLeft = Speed;
        _hasTakenAction = false;

        //Instantiate permanent
        var permanent = CardInfo as PermanentSO;
        _summon = Object.Instantiate(permanent.Prefab, parent.position, parent.rotation, parent);
        _summon.Card = this;
        _animator = Summon.GetComponent<Animator>();
        _summon.GetComponent<AnimationEventHandler>().Unit = this;
    }

    public override void OnTakeDamage(int damage)
    {
        damage = Mathf.Clamp(damage - Defense, 0, int.MaxValue);
        _currentHealth -= damage;
        
        GameManager.instance.GetBloodParticles(_summon.transform.position + Vector3.up);

        if (_currentHealth <= 0) _animator.SetTrigger("death");
        else
        {
            _animator.SetTrigger("damage");
            if (UnitCanRetaliate())
            {
                //Debug.Log("Unit can retaliate");
                DuelManager.instance.onCardBeginAction?.Invoke(this);
                _summon.FaceTargetCoroutine(_attackTarget.Node.Transform.position);
                _animator.SetTrigger("attack");
                _canRetaliate = false;
            }
        } 

        onValueChanged?.Invoke();
    }

    public void OnCommanderDeath()
    {
        if (isPlayerCard) DuelManager.instance.onPlayerDefeat?.Invoke();
        else DuelManager.instance.onPlayerVictory?.Invoke();
    }
}
