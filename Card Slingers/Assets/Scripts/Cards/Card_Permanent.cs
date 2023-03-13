using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component for permanents on the field. This includes units, buildings, equipment, and traps
/// </summary>
public class Card_Permanent : Card
{
    public delegate void OnPermanentDestroyedCallback(Card_Permanent card);
    public OnPermanentDestroyedCallback onPermanentDestroyed;
    public OnPermanentDestroyedCallback onRemovedFromField;

    //Invoke this event when a unit's stats change
    public delegate void OnPermanentValueChangedCallback();
    public OnPermanentValueChangedCallback onValueChanged;

    [SerializeField] protected GridNode _occupiedNode;
    [SerializeField] protected Summon _summon;

    public GridNode Node => _occupiedNode;
    public Summon Summon => _summon;

    public int ThreatLevel => GetThreatLevel();
    //For ordering player cards based on their perceived threat based on power and position
    private float _modifiedThreatLevel;
    public float ModifiedThreatLevel
    {
        get => _modifiedThreatLevel;
        set
        {
            _modifiedThreatLevel = Mathf.Clamp(value, 0, int.MaxValue);
        }
    }

    public Card_Permanent(PermanentSO card, bool isPlayerCard) : base (card, isPlayerCard)
    {
        _cardInfo = card;
        this.isPlayerCard = isPlayerCard;
    }

    public void SubscribeToEvents()
    {
        if (isPlayerCard)
        {
            DuelManager.instance.onPlayerVictory += OnCommanderVictory;
            DuelManager.instance.onPlayerDefeat += OnCommanderDefeat;
        }
        else
        {
            DuelManager.instance.onPlayerVictory += OnCommanderDefeat;
            DuelManager.instance.onPlayerDefeat += OnCommanderVictory;
        }
    }

    protected void UnsubscribeFromEvents()
    {
        if (isPlayerCard)
        {
            DuelManager.instance.onPlayerVictory -= OnCommanderVictory;
            DuelManager.instance.onPlayerDefeat -= OnCommanderDefeat;
        }
        else
        {
            DuelManager.instance.onPlayerVictory -= OnCommanderDefeat;
            DuelManager.instance.onPlayerDefeat -= OnCommanderVictory;
        }
    }

    public virtual void OnSummoned(Summon summon, GridNode node)
    {
        _summon = summon;
        OnOccupyNode(node); //Occupy the given node
        //SubscribeToEvents();
    }

    //Set current node and occupy it
    public virtual void OnOccupyNode(GridNode newNode)
    {
        _occupiedNode = newNode;
        _occupiedNode.Occupant = this;
        //_occupiedNode.SetOccupant(this);
        if (_summon == null) Debug.LogError("Summon is null.");
        if (newNode == null) Debug.LogError("New Node is null.");
        _summon.transform.position = newNode.transform.position;
    }

    //Abandon the currently occupied node
    public virtual void OnAbandonNode()
    {
        //should only be null if occupying a structure
        if (_occupiedNode != null) _occupiedNode.Occupant = null;
        //_occupiedNode?.ClearOccupant();
        _occupiedNode = null;
    }

    protected virtual int GetThreatLevel()
    {
        return 0;
    }

    public virtual void OnTurnStart()
    {
        //Trigger any relevant abilities
    }

    /// <summary>
    /// Call this method when the card is destroyed. Abandons node, unsubscribes from events
    /// </summary>
    protected void OnRemoveFromField() //Maybe change this to a method in the base Card class for OnEnterDiscard which will also set location
    {
        OnAbandonNode(); //I think I'm just going to move this stuff into OnPermanentDestroyed
        UnsubscribeFromEvents();
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

    protected virtual void OnCommanderVictory()
    {
        if (_location != CardLocation.OnField) return;
        //Meant to be overridden
    }

    protected virtual void OnCommanderDefeat()
    {
        if (_location != CardLocation.OnField) return;
        //Meant to be overridden
    }
}
