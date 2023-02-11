using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Commander", menuName = "Scriptable Objects/Cards/Commander")]
public class CommanderSO : UnitSO
{
    private void Reset()
    {
        type = CardType.Commander;
    }

    [Header("Commander Properties")]
    [SerializeField] private Deck _deck;
    public Deck Deck => _deck;

}