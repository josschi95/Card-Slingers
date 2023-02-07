using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Deck
{
    public List<CardSO> cards;

    public Deck(List<CardSO> newCards)
    {
        cards = new List<CardSO>();
        cards.AddRange(newCards);
    }

    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            CardSO temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}
