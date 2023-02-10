using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour, IInteractable
{
    [Header("Card Display")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description, flavorText;
    [SerializeField] private Image display;
    [SerializeField] private GameObject[] costMarkers;

    [Header("Card Info")]
    [SerializeField] protected CommanderController _commander;
    [SerializeField] protected CardSO _cardInfo;
    protected CardLocation _location; //If the card is in the deck, discard, hand, or on the field
    protected bool _raiseCard, _isSelected;
    private Coroutine lerpCardUpCoroutine;

    #region - Public Variables -
    public bool isRevealed { get; private set; } //If the player is able to see the card
    public CommanderController Commander => _commander;
    public CardSO CardInfo => _cardInfo; //Scriptable object to hold the card stats 
    public CardLocation Location => _location;
    #endregion

    //Assign the card its information and its owner
    public void AssignCard(CardSO card, CommanderController commander, bool isRevealed = false)
    {
        _cardInfo = card;
        _commander = commander;

        SetCardDisplay();

        if (isRevealed) OnRevealCard();
    }

    //Update the display for the assigned card
    protected virtual void SetCardDisplay()
    {
        display.sprite = CardInfo.icon;
        title.text = CardInfo.name;
        description.text = CardInfo.description;
        flavorText.text = CardInfo.flavorText;

        for (int i = 0; i < costMarkers.Length; i++)
        {
            if (i < CardInfo.cost) costMarkers[i].SetActive(true);
            else costMarkers[i].SetActive(false);
        }
    }

    //Set the location of the card, for reference when selecting it
    public void SetCardLocation(CardLocation location)
    {
        _location = location;
    }

    //Reveal the card so the player is able to see it
    public void OnRevealCard()
    {
        isRevealed = true;
    }

    #region - IInteractable -
    public void OnMouseEnter()
    {
        if (_commander is PlayerCommander && _location == CardLocation.InHand && !_raiseCard)
        {
            _raiseCard = true;
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(LerpCardUpDown(true));
        }
    }

    public void OnMouseExit()
    {
        _raiseCard = false;
        if (_isSelected) return;

        if (_commander is PlayerCommander && _location == CardLocation.InHand)
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(LerpCardUpDown(false));
        }
    }

    public void OnLeftClick() => OnCardSelected();

    public void OnRightClick() => OnDisplayCardInfo();
    #endregion

    private void OnCardSelected()
    {
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
                _isSelected = true;
                DuelManager.instance.onCardInHandSelected?.Invoke(this);
                break;
            case CardLocation.OnField:
                _isSelected = true;
                DuelManager.instance.onCardInPlaySelected?.Invoke(this);
                break;
        }
    }

    private void OnDisplayCardInfo()
    {
        if (isRevealed) UIManager.instance.ShowCardDisplay(_cardInfo);
    }

    public void OnDeSelectCard()
    {
        _isSelected = false;
        if (!_raiseCard)
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(LerpCardUpDown(false));
        }
    }

    protected IEnumerator LerpCardUpDown(bool up)
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

    public virtual void OnSendToDiscardPile()
    {
        _commander.CardsInDiscard.Add(this);
        DuelManager.instance.Battlefield.PlaceCardInDiscard(_commander, this);
    }
}

public class MathParabola
{
    public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float time)
    {
        System.Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

        var midPoint = Vector3.Lerp(start, end, time);

        return new Vector3(midPoint.x, f(time) + Mathf.Lerp(start.y, end.y, time), midPoint.z);
    }

    public static Vector3 Parabola(Vector3 start, Vector3 end, float time)
    {
        float height = Vector3.Distance(start, end) * 0.25f;

        System.Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

        var midPoint = Vector3.Lerp(start, end, time);

        return new Vector3(midPoint.x, f(time) + Mathf.Lerp(start.y, end.y, time), midPoint.z);
    }
}

public enum CardLocation { InDeck, InDiscard, InHand, OnField }