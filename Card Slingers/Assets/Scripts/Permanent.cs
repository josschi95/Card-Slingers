using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component for permanents on the field. This includes units, buildings, equipment, traps, and terrains
/// </summary>
public class Permanent : MonoBehaviour
{
    [SerializeField] private CommanderController _commander;
    [SerializeField] private Card _card;

    public CommanderController Commander => _commander;
    public Card Card => _card;


    public void OnEnterField(CommanderController commander, Card assignedCard)
    {
        _commander = commander;
        _card = assignedCard;

        //Play enter animation
    }

    public void OnBeginPhase()
    {

    }

    public void OnExitField()
    {

        //Play death animation
        //Then destroy gameObject
    }
}
