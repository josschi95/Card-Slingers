using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Card _card;

    [SerializeField] protected GameObject cardGFX;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description, flavorText, manaCostText;
    [SerializeField] private Image cardArt;

    [Header("Unit Info")]
    [SerializeField] private GameObject _healthIcon;
    [SerializeField] private GameObject _defenseIcon, attackIcon, speedIcon;
    [SerializeField] private TMP_Text _healthText, _defenseText, _attackText, _speedText;

    protected bool _isSelected;
    private float _raiseAmount = 265f;
    private Coroutine lerpCardUpCoroutine;

    public Card AssignedCard
    {
        get => _card;
        set
        {
            _card = value;
            if (_card != null)
            {
                if (_card.CardInfo == null)
                {
                    throw new UnityException("Not Assigning SO to card!");
                }
                SetDisplay(_card.CardInfo);
            }
        }
    }

    #region - Display - 
    private void SetDisplay(CardSO card)
    {
        if (card == null) throw new UnityException("Cannot set display for a null card!");

        cardArt.sprite = card.icon;
        title.text = card.name;
        description.text = card.description;
        flavorText.text = card.flavorText;
        manaCostText.text = card.cost.ToString();

        if (card is UnitSO unit) SetUnitInfo(unit);
        else if (card is StructureSO structure) SetStructureInfo(structure);
        else HideStats();
    }

    private void HideStats()
    {
        _healthIcon.SetActive(false);
        _defenseIcon.SetActive(false);
        attackIcon.SetActive(false);
        speedIcon.SetActive(false);
    }

    private void SetUnitInfo(UnitSO unit)
    {
        _healthIcon.SetActive(true);
        _defenseIcon.SetActive(true);
        attackIcon.SetActive(true);
        if (unit.Range > 1) _attackText.text += " (" + unit.Range.ToString() + ")";
        speedIcon.SetActive(true);

        _healthText.text = unit.MaxHealth.ToString();
        _defenseText.text = unit.Defense.ToString();
        _attackText.text = unit.Attack.ToString();
        _speedText.text = unit.Speed.ToString();
    }

    private void SetStructureInfo(StructureSO structure)
    {
        _healthIcon.SetActive(true);
        _defenseIcon.SetActive(true);

        attackIcon.SetActive(false);
        speedIcon.SetActive(false);

        _healthText.text = structure.MaxHealth.ToString();
        _defenseText.text = structure.Defense.ToString();
    }
    #endregion

    #region - Interaction -
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_card.Location != CardLocation.InHand || !_card.isPlayerCard) return;

        if (!DuelManager.instance.CardsInTransition) //Will prevent the card from moving when cards are in motion
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(RaiseCardInHand(true));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isSelected || !_card.isPlayerCard) return;
        if (_card.Location != CardLocation.InHand) return;

        if (!DuelManager.instance.CardsInTransition)
        {
            if (lerpCardUpCoroutine != null) StopCoroutine(lerpCardUpCoroutine);
            lerpCardUpCoroutine = StartCoroutine(RaiseCardInHand(false));
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnCardSelected();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnDisplayCardInfo();
        }
    }

    private void OnCardSelected()
    {
        switch (_card.Location)
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
                DuelManager.instance.onCardInHandSelected?.Invoke(_card);
                //I need to set some check to make sure that the card CAN be selected
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
        if (_card.isRevealed) DungeonUIManager.instance.ShowCardDisplay(_card.CardInfo);
    }

    public void OnDeSelectCard()
    {
        //Will have to subscribe to a callback event
        _isSelected = false;

        switch (_card.Location)
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
        if (_card.Location != CardLocation.InHand) yield break;

        float timeElapsed = 0;
        float timeToMove = 0.25f;
        var endPos = transform.localPosition;
        endPos.y = 0;
        if (up) endPos.y += _raiseAmount;

        while (timeElapsed < timeToMove)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, endPos, (timeElapsed / timeToMove));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = endPos;
    }
    #endregion
}
