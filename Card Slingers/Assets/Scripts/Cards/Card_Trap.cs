using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Trap : Card_Permanent
{
    private TrapSO _trapInfo;
    private Animator _trapAnim;

    public Card_Trap(TrapSO trap, bool isPlayerCard) : base (trap, isPlayerCard)
    {
        _cardInfo = trap;
        _trapInfo = trap;
        this.isPlayerCard = isPlayerCard;
    }

    public override void OnSummoned(Summon summon, GridNode node)
    {
        SetCardLocation(CardLocation.OnField);
        isRevealed = false;
        //transform.SetParent(DuelManager.instance.Battlefield.transform);

        OnOccupyNode(node); //Occupy the given node

        _summon = summon;
        _trapAnim = _summon.GetComponent<Animator>();
    }

    private void OnTrapTriggered(Card_Unit unit)
    {
        //same commander, trap doesn't trigger
        if (unit.isPlayerCard == isPlayerCard) return;
        _trapAnim.SetTrigger("trigger");

        //apply effects of trap
        for (int i = 0; i < _trapInfo.Effects.Length; i++)
        {
            GameManager.OnApplyEffect(unit, _trapInfo.Effects[i]);
        }

        OnPermanentDestroyed();
    }

    public override void OnOccupyNode(GridNode newNode)
    {
        _occupiedNode = newNode;
        newNode.Trap = this;
        _occupiedNode.onNodeEntered += OnTrapTriggered;
    }

    public override void OnAbandonNode()
    {
        _occupiedNode.onNodeEntered -= OnTrapTriggered;
        _occupiedNode.Trap = null;
        _occupiedNode = null;
    }

    protected override void OnPermanentDestroyed()
    {
        _summon.DestroyAfterDelay(1f);
        OnAbandonNode();
        onPermanentDestroyed?.Invoke(this);
        onRemovedFromField?.Invoke(this);
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
