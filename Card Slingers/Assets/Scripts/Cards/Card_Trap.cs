using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Trap : Card_Permanent
{
    private TrapSO _trapInfo => CardInfo as TrapSO;
    private Animator _trapAnim;

    public override void OnSummoned(GridNode node)
    {
        SetCardLocation(CardLocation.OnField);
        isRevealed = false;
        //transform.SetParent(DuelManager.instance.Battlefield.transform);

        OnOccupyNode(node); //Occupy the given node

        _permanentObject = Instantiate(_trapInfo.Prefab, node.transform.position, Quaternion.identity);
        _trapAnim = _permanentObject.GetComponent<Animator>();
    }

    private void OnTrapTriggered(Card_Unit unit)
    {
        //same commander, trap doesn't trigger
        if (unit.Commander == Commander) return;
        _trapAnim.SetTrigger("trigger");

        //apply effects of trap
        for (int i = 0; i < _trapInfo.Effects.Length; i++)
        {
            GameManager.OnApplyEffect(unit, _trapInfo.Effects[i]);
        }

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
        OnAbandonNode();
        cardGFX.SetActive(true); //Re-enable card
        Destroy(PermanentObject); //Destroy trap
        Commander.onPermanentDestroyed?.Invoke(this);
    }

    protected override void OnCommanderVictory()
    {
        if (_location != CardLocation.OnField) return;
        StartCoroutine(WaitToRemove());
    }

    protected override void OnCommanderDefeat()
    {
        if (_location != CardLocation.OnField) return;
        OnPermanentDestroyed();
    }

    private IEnumerator WaitToRemove()
    {
        yield return new WaitForSeconds(2);

        OnAbandonNode();
        cardGFX.SetActive(true); //Re-enable card
        Destroy(PermanentObject); //Destroy trap

        //Invoke an event for the commander to listen to
        Commander.onSendToDiscard?.Invoke(this);
    }
}
