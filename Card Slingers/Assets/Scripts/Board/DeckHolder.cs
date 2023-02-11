using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckHolder : MonoBehaviour
{
    private const float MAX_HAND_WIDTH = 20f;
    private const float CARD_WIDTH = 2f;

    public enum HolderType { Deck, Hand, Discard };

    [SerializeField] private HolderType pileType;
    [SerializeField] private Transform[] objectsToIgnore;
    [SerializeField] private float cardSpacing = 0.01f;
    [SerializeField] private bool isPlayer;
    private Vector3 faceDown = new Vector3(0, 0, 180);

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
            transform.GetChild(i).localPosition = newPos;
            if (isDeck) transform.GetChild(i).localEulerAngles = faceDown;
            height -= cardSpacing;

        }
    }

    private void OrderHand()
    {
        int cardCount = transform.childCount - objectsToIgnore.Length;
        float width = cardCount * (CARD_WIDTH + cardSpacing);
        float cardXPos = -(width * 0.5f);

        for (int i = 0; i < transform.childCount; i++)
        {
            if (i < objectsToIgnore.Length) continue;
            var child = transform.GetChild(i);
            
            child.localPosition = new Vector3(cardXPos, 0, 0);
            cardXPos += CARD_WIDTH + cardSpacing;

            if (isPlayer) child.localEulerAngles = Vector3.zero;
            else
            {
                if (child.GetComponent<Card>().isRevealed) child.localEulerAngles = Vector3.zero;
                else child.localEulerAngles = faceDown;
            }
        }
    }

}
