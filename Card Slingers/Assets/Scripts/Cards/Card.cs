using System.Collections;
using UnityEngine;

public class Card
{
    protected CardSO _cardInfo;
    public bool isPlayerCard { get; protected set; }
    protected CardLocation _location; //If the card is in the deck, discard, hand, or on the field

    #region - Properties -
    public bool isRevealed { get; protected set;}//If the player is able to see the card
    public CardSO CardInfo => _cardInfo; //Scriptable object to hold the card stats 
    public CardLocation Location => _location;
    #endregion

    public Card(CardSO card, bool isPlayerCard)
    {
        _cardInfo = card;
        this.isPlayerCard = isPlayerCard;
    }

    //Set the location of the card, for reference when selecting it
    public void SetCardLocation(CardLocation location)
    {
        _location = location;

        switch (_location)
        {
            case CardLocation.InHand:
                isRevealed = isPlayerCard;
                break;
            case CardLocation.InDeck:
                isRevealed = false;
                break;
            case CardLocation.InDiscard:
                isRevealed = true;
                break;
            case CardLocation.OnField:
                isRevealed = true;
                break;
            case CardLocation.InExile:
                isRevealed = true;
                break;
        }
    }

    public void OnDeSelectCard()
    {
        //_display.OnDeSelectCard();

    }
}

public enum CardLocation { InHand, InDeck, InDiscard, OnField, InExile }