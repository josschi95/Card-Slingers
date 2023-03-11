using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandDisplay : MonoBehaviour
{
    [SerializeField] private PlayerCommander _player;
    [SerializeField] private CardDisplay[] _playerCards;

    [Space]

    [SerializeField] private float _cardWidth;
    [SerializeField] private float _cardSpacing;
    private int _cardsInHand;

    private void Start()
    {
        _player.onCardsInHandChange += UpdateCardDisplay;

        for (int i = 0; i < _playerCards.Length; i++)
        {
            _playerCards[i].gameObject.SetActive(false);
        }
    }



    private void UpdateCardDisplay()
    {
        for (int i = 0; i < _playerCards.Length; i++)
        {
            if (i >= _player.CardsInHand.Count)
            {
                //_playerCards[i].AssignedCard = null;
                _playerCards[i].gameObject.SetActive(false);
                continue;
            }

            _playerCards[i].AssignedCard = _player.CardsInHand[i];
            _playerCards[i].gameObject.SetActive(true);
        }

        OrderHand();
    }

    private void OrderHand()
    {
        int cardCount = transform.childCount;

        bool addedNewCard = false;
        if (cardCount > _cardsInHand) addedNewCard = true;
        _cardsInHand = cardCount;

        float width = cardCount * (_cardWidth + _cardSpacing);
        float cardXPos = -(width * 0.5f) + (_cardWidth + _cardSpacing) * 0.5f;

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);

            if (addedNewCard && i == transform.childCount - 1) //move card in parabola
                StartCoroutine(SmoothCardMovement(child.gameObject, Vector3.right * cardXPos, true));
            else StartCoroutine(SmoothCardMovement(child.gameObject, Vector3.right * cardXPos, false));
            cardXPos += _cardWidth + _cardSpacing;
        }
    }

    private IEnumerator SmoothCardMovement(GameObject card, Vector3 endPos, bool newCard = false)
    {
        float timeElapsed = 0f, timeToMove = 0.5f;
        var startPos = card.transform.localPosition;
        var startRot = card.transform.localRotation;

        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime;

            if (newCard) card.transform.localPosition = MathParabola.Parabola(startPos, endPos, 1f, timeElapsed / timeToMove);
            else card.transform.localPosition = Vector3.Lerp(startPos, endPos, timeElapsed / timeToMove);

            yield return null;
        }

        card.transform.localPosition = endPos;
    }
}
