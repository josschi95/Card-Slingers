using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour, IInteractable
{
    [Header("Card Display")]
    [SerializeField] protected GameObject cardGFX;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description, flavorText;
    [SerializeField] private Image display;
    [SerializeField] private GameObject[] costMarkers;

    [Header("Card Info")]
    [SerializeField] protected CommanderController _commander;
    [SerializeField] protected CardSO _cardInfo;
    protected CardLocation _location; //If the card is in the deck, discard, hand, or on the field
    protected bool _isSelected;
    private Coroutine lerpCardUpCoroutine;

    #region - Public Variables -
    public bool isRevealed { get; private set; } //If the player is able to see the card
    public CommanderController Commander => _commander;
    public CardSO CardInfo => _cardInfo; //Scriptable object to hold the card stats 
    public CardLocation Location => _location;

    public object Current => throw new System.NotImplementedException();
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
        if (!_commander.isDrawingCards && _commander is PlayerCommander && _location == CardLocation.InHand)
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(RaiseCardInHand(true));
        }
    }

    public void OnMouseExit()
    {
        if (_isSelected) return;

        if (!_commander.isDrawingCards && _commander is PlayerCommander && _location == CardLocation.InHand)
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(RaiseCardInHand(false));
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
            case CardLocation.OnField: //cards on the field wiill never be selected, the nodes they are occupying will be selected
                _isSelected = true;
                //DuelManager.instance.onCardInPlaySelected?.Invoke(this);
                break;
            case CardLocation.InExile:
                //Maybe show display, the current route is that cards in exile are removed from the game for the current match
                //I could make it so that they're removed from the deck entirely, for the rest of the dungeon crawl,
                //but that would make exile effects against the player significantly more impactful than those used by the player
                //if that were the case, then the player must also be able to add cards to their deck during the dungeon crawl.
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

        switch (_location)
        {
            case CardLocation.InHand:
                if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
                lerpCardUpCoroutine = StartCoroutine(RaiseCardInHand(false));
                break;
            case CardLocation.OnField:

                break;
        }

    }

    protected IEnumerator RaiseCardInHand(bool up)
    {
        //Ignore this if not placed in hand
        if (_location != CardLocation.InHand) yield break;

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

public enum CardLocation { InHand, InDeck, InDiscard, OnField, InExile }