using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCommander : CommanderController
{
    private bool _deckIsCreated;
    [SerializeField] private List<Card> _existingDeck = new List<Card>();

    public override void OnMatchStart(int startingHandSize = 4, int mana = 4)
    {
        isTurn = true;
        base.OnMatchStart(startingHandSize, mana);
        Debug.Log("OnMatchStart Player");
    }

    protected override void OnNewTurn(bool isPlayerTurn)
    {
        isTurn = isPlayerTurn;
        if (isTurn) OnTurnStart();
    }

    protected override void GenerateNewDeck()
    {
        if (_deckIsCreated)
        {
            for (int i = 0; i < _existingDeck.Count; i++)
            {
                PlaceCardInDeck(_existingDeck[i]);
            }

            return;
        }

        base.GenerateNewDeck();
        _deckIsCreated = true;
        _existingDeck.AddRange(_cardsInDeck);
    }

    private void HideDeck()
    {
        for (int i = 0; i < _existingDeck.Count; i++)
        {
            _existingDeck[i].SetCardLocation(CardLocation.InDeck);
        }
        
        _cardsInDeck.Clear();
        _cardsInHand.Clear();
        _cardsInDiscardPile.Clear();
        _cardsInExile.Clear();
        _permanentsOnField.Clear();
    }

    protected override void OnPlayerVictory()
    {
        base.OnPlayerVictory();

        Invoke("HideDeck", 3f); //Allow cards to move about first
    }

    protected override void OnPlayerDefeat()
    {
        base.OnPlayerDefeat();

        //Display defeat screen

        //Allow the player to view the battlefield

        //Defeat screen has button to return to town
    }
}
