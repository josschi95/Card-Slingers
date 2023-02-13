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

            StartCoroutine(SmoothCardMovement(child.gameObject, new Vector3(cardXPos, 0, 0)));
            cardXPos += CARD_WIDTH + cardSpacing;
        }
    }

    private IEnumerator SmoothCardMovement(GameObject card, Vector3 endPos)
    {
        float timeElapsed = 0f, timeToMove = 0.5f;
        var startPos = card.transform.localPosition;
        var startRot = card.transform.localRotation;

        var endRot = Quaternion.Euler(Vector3.zero);
        if (!card.GetComponent<Card>().isRevealed) endRot = Quaternion.Euler(faceDown);

        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime;

            card.transform.localPosition = Vector3.Lerp(startPos, endPos, timeElapsed / timeToMove);
            //card.transform.localPosition = MathParabola.Parabola(startPos, endPos, timeElapsed / timeToMove);
            card.transform.localRotation = Quaternion.Slerp(startRot, endRot, timeElapsed / timeToMove);

            yield return null;
        }

        card.transform.localPosition = endPos;
        card.transform.localRotation = endRot;
    }
}
