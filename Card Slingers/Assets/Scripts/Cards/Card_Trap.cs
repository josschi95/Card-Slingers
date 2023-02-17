using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Trap : Card_Permanent
{
    private TrapSO _trapInfo => CardInfo as TrapSO;

    public override void OnSummoned(GridNode node)
    {
        SetCardLocation(CardLocation.OnField);
        isRevealed = false;
        //transform.SetParent(DuelManager.instance.Battlefield.transform);

        OnOccupyNode(node); //Occupy the given node

        _permanentObject = Instantiate(_trapInfo.Prefab, node.transform.position, Quaternion.identity);
    }

    private void OnTrapTriggered(Card_Unit unit)
    {
        Debug.Log("Trap has been activated!");
        //same commander, trap doesn't trigger
        if (unit.Commander == Commander) return;

        //apply effects of trap
        for (int i = 0; i < _trapInfo.Effects.Length; i++)
        {
            GameManager.OnApplyEffect(unit, _trapInfo.Effects[i]);
        }

        //Will likely have some animations in here, in that event, I'll add an OnAnimationComplete method similar to units

        //Invoke after 1 second delay
        Invoke("OnPermanentDestroyed", 1f);
    }

    protected override void OnOccupyNode(GridNode newNode)
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
        cardGFX.SetActive(true); //Re-enable card
        Destroy(PermanentObject); //Destroy trap

        Commander.onPermanentDestroyed?.Invoke(this);
    }
}
