using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component for permanents on the field. This includes units, buildings, equipment, and traps
/// </summary>
public class Card_Permanent : Card
{
    //Invoke this event when a unit's stats change
    public delegate void OnPermanentValueChangedCallback();
    public OnPermanentValueChangedCallback onValueChanged;

    [SerializeField] private GridNode _occupiedNode;
    [SerializeField] private GameObject _permanentObject;

    public GridNode OccupiedNode => _occupiedNode;
    public GameObject PermanentObject => _permanentObject;
    public int ThreatLevel => GetThreatLevel();

    public virtual void OnSummoned(GridNode node)
    {
        //Sets the card location as on the battlefield
        SetCardLocation(CardLocation.OnField);
        isRevealed = true;
        //Set as child to the battlefield
        transform.SetParent(DuelManager.instance.Battlefield.transform);

        OnOccupyNode(node); //Occupy the given node

        //Instantiate permanent
        var permanent = CardInfo as PermanentSO;
        _permanentObject = Instantiate(permanent.Prefab, transform.position, transform.rotation);
        _permanentObject.transform.SetParent(transform);
        cardGFX.SetActive(false); //Disable the physical card display
        GetComponent<Collider>().enabled = false; //Disable collider to not interfere with node selection
    }

    //Set current node and occupy it
    protected void OnOccupyNode(GridNode newNode)
    {
        _occupiedNode = newNode;
        _occupiedNode.SetOccupant(this);

        transform.position = newNode.transform.position;
    }

    //Abandon the currently occupied node
    public void OnAbandonNode()
    {
        _occupiedNode?.ClearOccupant();
        _occupiedNode = null;
    }

    protected virtual int GetThreatLevel()
    {
        return 0;
    }

    public virtual void OnBeginPhase()
    {
        //Trigger any relevant abilities
    }

    public void OnRemoveFromField() //Maybe change this to a method in the base Card class for OnEnterDiscard which will also set location
    {
        OnAbandonNode();
        cardGFX.SetActive(true); //Disable the physical card display
        GetComponent<Collider>().enabled = true; //re-enable collider for card selection
        //Play death animation <= this will probably be separate and only for units,
        //because I'll have to wait for the animation to finish
        //and then I can call OnRemovePermanentFromField(this) 

        //destroy _permanentObject or return to pool
    }

    public virtual void OnTargetEngaged(Card_Unit attacker)
    {
        //Meant to be overridden
    }

    public virtual void OnTakeDamage(int dmg)
    {
        //Meant to be overridden
    }

    protected virtual void OnPermanentDestroyed()
    {
        //Meant to be overridden
    }

    public virtual void OnCommanderVictory()
    {
        //Meant to be overridden
    }

    public virtual void OnCommanderDefeat()
    {
        //Meant to be overridden
    }
}
