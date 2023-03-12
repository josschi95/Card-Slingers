using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Card_Commander : Card_Unit 
{
    private CommanderSO _commanderInfo;

    public Card_Commander(CommanderSO commander, bool isPlayerCard) : base(commander, isPlayerCard)
    {
        _cardInfo = commander;
        _commanderInfo = commander;
        this.isPlayerCard = isPlayerCard;
    }

    public CommanderController OnCommanderCreated(bool isPlayer, GridNode node)
    {
        _summon = Object.Instantiate(_commanderInfo.Prefab, node.Transform.position, Quaternion.identity);
        Summon.Card = this;
        
        _animator = _summon.GetComponent<Animator>();
        Summon.GetComponent<AnimationEventHandler>().Unit = this;

        _currentHealth = NetMaxHealth();
        _movesLeft = Speed;
        _hasTakenAction = false;

        SetStartingNode(node);

        if (isPlayer)
        {
            var player = Summon.gameObject.AddComponent<PlayerCommander>();
            player.OnAssignCommander(_commanderInfo, this);
            return player;
        }
        else
        {
            var enemy = Summon.gameObject.AddComponent<OpponentCommander>();
            enemy.OnAssignCommander(_commanderInfo, this);
            return enemy;
        }
    }

    public void SetStartingNode(GridNode node)
    {
        OnOccupyNode(node);
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
