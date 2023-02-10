using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component for permanents on the field. This includes units, buildings, equipment, and traps
/// </summary>
public class Card_Permanent : Card
{
    [SerializeField] private GridNode _occupiedNode;
    [SerializeField] private GameObject _permanentObject;
    private bool _firstTurn; //Prevent the card from taking any actions same turn it was summoned
    //private Animator anim; //this will only be for Card_Unit ...we'll see. buildings may just have VFX, traps may have a "Sprung" anim

    public GridNode OccupiedNode => _occupiedNode;
    public GameObject PermanentObject => _permanentObject;
    public bool FirstTurn => _firstTurn;

    public void OnEnterField(GridNode node)
    {
        //Sets the card location as on the battlefield
        SetCardLocation(CardLocation.OnField);

        //Set current node and occupy it
        _occupiedNode = node;
        _occupiedNode.SetOccupant(this);

        //Instantiate permanent
        var permanent = CardInfo as PermanentSO;
        _permanentObject = Instantiate(permanent.Prefab, transform.position, Quaternion.identity);
        //anim = _permanentObject.GetComponent<Animator>(); //This will only be for units, not buildings, traps, or equipment
        //Play enter animation

        _firstTurn = true;
    }

    public void SetNode(GridNode newNode)
    {
        _occupiedNode = newNode;
    }

    public void OnBeginPhase()
    {
        _firstTurn = false;
        //Trigger any relevant abilities
    }

    public void OnRemoveFromField() //Maybe change this to a method in the base Card class for OnEnterDiscard which will also set location
    {
        _occupiedNode.SetOccupant(null);
        //Play death animation <= this will probably be separate and only for units,
        //because I'll have to wait for the animation to finish
        //and then I can call OnRemovePermanentFromField(this) 

        //destroy _permanentObject or return to pool

        //Removes from commander's list of permanents under control
        _commander.PermanentsOnField.Remove(this);
    }
}
