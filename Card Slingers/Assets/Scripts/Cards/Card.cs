using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [SerializeField] private TMP_Text title, description, flavorText;
    [SerializeField] private Image display;
    [SerializeField] private GameObject[] costMarkers;
    private CardLocation _location;
    private bool _isPlayerCard;

    public CardSO cardInfo;
    public bool isRevealed { get; private set; }
    public CardLocation Location => _location;
    public bool IsPlayerCard => _isPlayerCard;

    private Coroutine lerpCardUpCoroutine;

    public void AssignCard(CardSO card, bool isPlayerCard)
    {
        cardInfo = card;
        _isPlayerCard = isPlayerCard;

        display.sprite = cardInfo.icon;
        title.text = cardInfo.name;
        description.text = cardInfo.description;
        flavorText.text = cardInfo.flavorText;

        for (int i = 0; i < costMarkers.Length; i++)
        {
            if (i < cardInfo.cost) costMarkers[i].SetActive(true);
            else costMarkers[i].SetActive(false);
        }
    }

    public void SetCardLocation(CardLocation location)
    {
        _location = location;
    }

    public void OnRevealCard()
    {
        isRevealed = true;
    }

    public void OnMouseEnter()
    {
        if (_isPlayerCard && _location == CardLocation.InHand)
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(LerpCardUpDown(true));
        }

    }

    public void OnMouseExit()
    {
        if (_isPlayerCard && _location == CardLocation.InHand)
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(LerpCardUpDown(false));
        }

    }

    public void OnMouseDown()
    {
        OnCardSelected();
    }

    private void OnCardSelected()
    {
        Debug.Log("OnCardSelected");

        switch (_location)
        {
            case CardLocation.InDeck:
                //I don't think there's anything to do here
                //Player draws automatically
                //I guess if the player gets to select a card from their deck... but that will probably be a UI effect to keep things simple
                break;
            case CardLocation.InDiscard:
                //Same thing here as the deck. Honestly I'm heavily leaning towards what MtG does (I think) and just having a static gameObject to represent the deck and the discard pile
                //Selecting either commander's discard pile should give a similar UI display of all cards in that pile, since they're public
                break;
            case CardLocation.InHand:
                DuelManager.instance.onCardInHandSelected?.Invoke(this);
                break;
            case CardLocation.OnField:
                break;
        }
    }

    private IEnumerator LerpCardUpDown(bool up)
    {
        float timeElapsed = 0;
        float timeToMove = 0.25f;
        var endPos = transform.localPosition;
        endPos.z = 0;
        if (up) endPos.z += 0.75f;

        while (timeElapsed < timeToMove)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, endPos, (timeElapsed / timeToMove));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = endPos;
    }
}

public enum CardLocation { InDeck, InDiscard, InHand, OnField }