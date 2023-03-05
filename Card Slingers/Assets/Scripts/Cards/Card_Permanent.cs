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
    [SerializeField] protected GameObject _permanentObject;

    public GridNode Node => _occupiedNode;
    public GameObject PermanentObject => _permanentObject;
    public int ThreatLevel => GetThreatLevel();
    //For ordering player cards based on their perceived threat based on power and position
    [SerializeField] private float _modifiedThreatLevel;
    public float ModifiedThreatLevel
    {
        get => _modifiedThreatLevel;
        set
        {
            _modifiedThreatLevel = Mathf.Clamp(value, 0, int.MaxValue);
        }
    }

    public override void AssignCardInfo(CardSO card, bool isPlayerCard)
    {
        base.AssignCardInfo(card, isPlayerCard);

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

    private void OnDestroy()
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

    public virtual void OnSummoned(GridNode node)
    {       
        //Set as child to the battlefield
        transform.SetParent(DuelManager.instance.Battlefield.transform);

        OnOccupyNode(node); //Occupy the given node

        //Instantiate permanent
        var permanent = CardInfo as PermanentSO;
        _permanentObject = Instantiate(permanent.Prefab, transform.position, transform.rotation);
        _permanentObject.transform.SetParent(transform);
        _display.gameObject.SetActive(false);
        GetComponent<Collider>().enabled = false; //Disable collider to not interfere with node selection
    }

    //Set current node and occupy it
    protected virtual void OnOccupyNode(GridNode newNode)
    {
        _occupiedNode = newNode;
        _occupiedNode.Occupant = this;
        //_occupiedNode.SetOccupant(this);

        transform.position = newNode.transform.position;
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

    public virtual void OnBeginPhase()
    {
        //Trigger any relevant abilities
    }

    /// <summary>
    /// Call this method when the card is destroyed. Abandons node, re-enables card GFX and collider
    /// </summary>
    protected void OnRemoveFromField() //Maybe change this to a method in the base Card class for OnEnterDiscard which will also set location
    {
        OnAbandonNode(); //I think I'm just going to move this stuff into OnPermanentDestroyed
        _display.gameObject.SetActive(true);
        GetComponent<Collider>().enabled = true; //re-enable collider for card selection
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
