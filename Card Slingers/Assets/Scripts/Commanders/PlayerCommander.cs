using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCommander : CommanderController
{
    private bool _deckIsCreated;
    [SerializeField] private List<Card> _existingDeck = new List<Card>();

    public override void OnMatchStart(CardHolder holder, int startingHandSize = 4, int mana = 4)
    {
        base.OnMatchStart(holder, startingHandSize, mana);
        
        isTurn = true;
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
        PlayerController.PlaceDeckInPocket(_existingDeck);
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
