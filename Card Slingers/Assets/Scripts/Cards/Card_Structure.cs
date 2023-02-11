using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Structure : Card_Permanent
{
    [SerializeField] private bool _canBeOccupied;
    [SerializeField] private bool _isOccupied;
    [SerializeField] private Card_Permanent _occupant;

    public bool CanBeOccupied => StructureCanBeOccupied();
    public bool IsOccupied => _isOccupied;
    public Card_Permanent Occupant => _occupant;


    private bool StructureCanBeOccupied()
    {
        if (!_canBeOccupied) return false;
        if (_isOccupied) return false;

        return true;
    }
}
