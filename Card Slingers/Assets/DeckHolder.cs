using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckHolder : MonoBehaviour
{
    private const float CARD_WIDTH = 2f;

    public enum HolderType { Deck, Hand, Discard };

    [SerializeField] private HolderType pileType;
    [SerializeField] private Transform[] objectsToIgnore;
    [SerializeField] private float cardSpacing = 0.01f;

    private Vector3 faceDown = new Vector3(180, 0, 0);

    public void OnTransformChildrenChanged()
    {
        switch (pileType)
        {
            case HolderType.Deck:
                OrderDeckPile();
                break;
            case HolderType.Hand:
                OrderHand();
                break;
            case HolderType.Discard:
                OrderDeckPile(false);
                break;
        }
    }

    private void OrderDeckPile(bool isDeck = true)
    {
        int cardCount = transform.childCount - objectsToIgnore.Length;
        float height = cardSpacing * cardCount;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (i < objectsToIgnore.Length) continue;

            var newPos = Vector3.zero;
            newPos.y = height;
            transform.GetChild(i).transform.localPosition = newPos;
            if (isDeck) transform.GetChild(i).transform.localEulerAngles = faceDown;
            height -= cardSpacing;

        }
    }

    private void OrderHand()
    {
        int cardCount = transform.childCount - objectsToIgnore.Length;
        float width = cardCount * (CARD_WIDTH + cardSpacing);


    }

}
