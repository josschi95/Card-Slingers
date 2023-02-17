using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardHolder : MonoBehaviour
{
    [SerializeField] private CardPile _hand, _deck, _traps, _discard, _exile;
    
    public bool IsPlayer
    {
        set
        {
            _hand.isPlayer = value;
            _deck.isPlayer = value;
            _traps.isPlayer = value;
            _discard.isPlayer = value;
            _exile.isPlayer = value;
        }
    }

    public Transform Hand => _hand.transform;
    public Transform Deck => _deck.transform;
    public Transform Traps => _traps.transform;
    public Transform Discard => _discard.transform;
    public Transform Exile => _exile.transform;

}