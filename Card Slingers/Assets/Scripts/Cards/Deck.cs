using System.Collections.Generic;

[System.Serializable]
public class Deck
{
    public List<CardSO> cards;

    public Deck(List<CardSO> newCards)
    {
        cards = new List<CardSO>();
        cards.AddRange(newCards);
    }
}
