using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPile : MonoBehaviour
{
    private const float MAX_HAND_WIDTH = 20f;
    private const float CARD_WIDTH = 2f;

    public enum HolderType { Deck, Hand, Discard };

    [SerializeField] private HolderType pileType;
    [SerializeField] private Transform[] objectsToIgnore;
    [SerializeField] private float cardSpacing = 0.01f;
    //[SerializeField] 
    public bool isPlayer;
    private Vector3 faceDown = new Vector3(0, 0, 180);
    private int _cardsInHand;
    private int _cardsInDiscard;

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
                OrderDiscardPile();
                break;
        }
    }

    private void OrderDeckPile()
    {
        int cardCount = transform.childCount - objectsToIgnore.Length;
        float height = cardSpacing * cardCount;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (i < objectsToIgnore.Length) continue;

            var newPos = Vector3.zero;
            newPos.y = height;
            transform.GetChild(i).localPosition = newPos;
            transform.GetChild(i).localEulerAngles = faceDown;
            height -= cardSpacing;
        }
    }

    private void OrderDiscardPile()
    {
        int cardCount = transform.childCount - objectsToIgnore.Length;
        float height = cardSpacing * cardCount;
        bool addedNewCard = false;
        if (cardCount > _cardsInDiscard) addedNewCard = true;
        _cardsInDiscard = cardCount;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (i < objectsToIgnore.Length) continue;
            var child = transform.GetChild(i);

            var newPos = Vector3.zero;
            newPos.y = height;

            StartCoroutine(SmoothCardMovement(child.gameObject, newPos, (addedNewCard && i == transform.childCount - 1)));
            height -= cardSpacing;
        }
    }

    private void OrderHand()
    {
        int cardCount = transform.childCount - objectsToIgnore.Length;

        bool addedNewCard = false;
        if (cardCount > _cardsInHand) addedNewCard = true;
        _cardsInHand = cardCount;

        float width = cardCount * (CARD_WIDTH + cardSpacing);
        float cardXPos = -(width * 0.5f);

        for (int i = 0; i < transform.childCount; i++)
        {
            if (i < objectsToIgnore.Length) continue;
            var child = transform.GetChild(i);

            if (addedNewCard && i == transform.childCount - 1) //move card in parabola
                StartCoroutine(SmoothCardMovement(child.gameObject, new Vector3(cardXPos, 0, 0), true));
            else StartCoroutine(SmoothCardMovement(child.gameObject, new Vector3(cardXPos, 0, 0), false));
            cardXPos += CARD_WIDTH + cardSpacing;
        }
    }

    private IEnumerator SmoothCardMovement(GameObject card, Vector3 endPos, bool newCard = false)
    {
        float timeElapsed = 0f, timeToMove = 0.5f;
        var startPos = card.transform.localPosition;
        var startRot = card.transform.localRotation;

        var endRot = Quaternion.Euler(360,360,360);
        if (!card.GetComponent<Card>().isRevealed) endRot = Quaternion.Euler(faceDown);

        DuelManager.instance.onCardMovementStarted?.Invoke();
        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime;

            if (newCard) card.transform.localPosition = MathParabola.Parabola(startPos, endPos, 1f, timeElapsed / timeToMove);
            else card.transform.localPosition = Vector3.Lerp(startPos, endPos, timeElapsed / timeToMove);
            card.transform.localRotation = Quaternion.Slerp(startRot, endRot, timeElapsed / timeToMove);

            yield return null;
        }
        DuelManager.instance.onCardMovementEnded?.Invoke();

        card.transform.localPosition = endPos;
        card.transform.localRotation = endRot;
    }
}
