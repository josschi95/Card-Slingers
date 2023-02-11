using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderController : MonoBehaviour
{
    public delegate void OnManaChangeCallback();
    public OnManaChangeCallback onManaChange;

    private DuelManager duelManager;

    [SerializeField] private CommanderSO _commanderInfo;
    [SerializeField] private Card_Permanent _commanderCard;
    [SerializeField] private Phase currentPhase;
    [SerializeField] private int _currentMana = 4;
    [Space]
    [SerializeField] private List<Card> _cardsInDeck;
    [SerializeField] private List<Card> _cardsInDiscardPile;
    [SerializeField] private List<Card> _cardsInHand;
    [Space]
    [SerializeField] private List<Card_Permanent> _permanentsOnField;
    public bool isTurn { get; private set; }

    #region - Public Variable References -
    public CommanderSO CommanderInfo => _commanderInfo;
    public Card_Permanent CommanderCard => _commanderCard;
    public int CurrentMana => _currentMana;
    #endregion

    private void Start()
    {
        duelManager = DuelManager.instance;
    }

    public virtual void OnAssignCommander(CommanderSO commanderInfo)
    {
        _commanderInfo = commanderInfo;
        _commanderCard.AssignCard(commanderInfo, this, true);
    }

    public virtual void OnMatchStart(int startingHandSize = 4)
    {
        //Should only need this here for testing
        duelManager = DuelManager.instance;

        _cardsInDeck = new List<Card>();
        _cardsInDiscardPile = new List<Card>();
        _cardsInHand = new List<Card>();
        _permanentsOnField = new List<Card_Permanent>();

        GenerateDeck();

        ShuffleDeck();

        DrawCards(startingHandSize);
    }

    //Set the commander as the first to go, normal but don't draw a card
    public void OnFirstTurn()
    {
        currentPhase = Phase.Begin;
        OnBeginPhase(false);
    }

    protected virtual void GenerateDeck()
    {
        if (_commanderInfo == null) Debug.Log("CommanderSO is null");

        foreach (CardSO cardSO in _commanderInfo.Deck.cards)
        {
            Card newCard = Instantiate(cardSO.cardPrefab).GetComponent<Card>();
            newCard.AssignCard(cardSO, this, this is PlayerCommander);
            PlaceCardInDeck(newCard);
        }
    }

    #region - Phases -
    public void SetPhase(Phase phase)
    {
        Debug.Log(CommanderInfo.name + " entering " + phase.ToString() + " phase");
        currentPhase = phase;
        switch (phase)
        {
            case Phase.Begin:
                OnBeginPhase();
                break;
            case Phase.Summoning:
                OnSummoningPhase();
                break;
            case Phase.Attack:
                OnDeclarationPhase();
                break;
            case Phase.Resolution:
                OnResolutionPhase();
                break;
            case Phase.End:
                OnEndPhase();
                break;
        }
    }

    public void OnBeginPhase(bool drawCard = true)
    {
        isTurn = true;
        _currentMana = 4;

        if (drawCard) DrawCards();
        //This will eventually have an animation, so wait until that is finished

        //For each card on the field, invoke an OnBeginPhase event

        DuelManager.instance.OnCurrentPhaseFinished();
    }

    private void OnSummoningPhase()
    {

    }

    private void OnDeclarationPhase()
    {

    }

    private void OnResolutionPhase()
    {

    }

    private void OnEndPhase()
    {
        isTurn = false;

        DuelManager.instance.OnCurrentPhaseFinished();
    }

    public void OnNextPhase()
    {
        if (currentPhase == Phase.End) OnBeginPhase();
        else SetPhase(currentPhase + 1);
    }
    #endregion

    #region - Card Movements -
    private void DrawCards(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (_cardsInDeck.Count <= 0)
            {
                Debug.Log("No Remaining Cards in Deck");
                ReturnDiscardPileToDeck();
                return;
            }

            var cardToDraw = _cardsInDeck[0];
            _cardsInDeck.Remove(cardToDraw);
            _cardsInHand.Add(cardToDraw);
            PlaceCardInHand(cardToDraw);
        }
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < _cardsInDeck.Count; i++)
        {
            Card temp = _cardsInDeck[i];
            int randomIndex = Random.Range(i, _cardsInDeck.Count);
            _cardsInDeck[i] = _cardsInDeck[randomIndex];
            _cardsInDeck[randomIndex] = temp;
        }
    }

    private void ReturnDiscardPileToDeck()
    {
        for (int i = _cardsInDiscardPile.Count - 1; i >= 0; i--)
        {
            var card = _cardsInDiscardPile[i];
            _cardsInDiscardPile.Remove(card);
            PlaceCardInDeck(card);
        }
        ShuffleDeck();
    }

    //

    private void PlaceCardInDeck(Card card)
    {
        _cardsInDeck.Add(card);
        card.SetCardLocation(CardLocation.InDeck);
        card.transform.SetParent(duelManager.Battlefield.GetDeckParent(this));
    }

    private void PlaceCardInDiscard(Card card)
    {
        _cardsInDiscardPile.Add(card);
        card.SetCardLocation(CardLocation.InDiscard);
        card.transform.SetParent(duelManager.Battlefield.GetDiscardParent(this));
    }

    private void PlaceCardInHand(Card card)
    {
        _cardsInHand.Add(card);
        card.SetCardLocation(CardLocation.InHand);
        card.transform.SetParent(duelManager.Battlefield.GetHandParent(this));
    }

    //

    private void DiscardCardFromHand(Card cardToDiscard)
    {
        if (!_cardsInHand.Contains(cardToDiscard)) return;
        _cardsInHand.Remove(cardToDiscard);
        PlaceCardInDiscard(cardToDiscard);
    }

    public void SendPermanentToDiscard(Card_Permanent permanent)
    {
        //Trigger any exit effects
        permanent.OnRemoveFromField();

        _permanentsOnField.Remove(permanent);

        //Moves the card to the discard pile
        PlaceCardInDiscard(permanent);
    }

    //private void ReturnPermanentInPlayToHand(Card_Permanent card) { }
    #endregion

    public bool CanPlayCard(Card card)
    {
        if (card.CardInfo.cost > _currentMana) return false;
        return true;
    }

    public void OnInstantPlayed(Card card)
    {
        OnSpendMana(card.CardInfo.cost);

        //If a target is needed, wait for a target to be selected
        //Trigger whatever effect the instant has

        //Send to discard pile
        PlaceCardInDiscard(card);
    }

    public void OnPermanentPlayed(GridNode node, Card_Permanent card)
    {
        //Spend Mana cost of card
        OnSpendMana(card.CardInfo.cost);

        // !!MUST CHANGE LOCATION BEFORE DESELECTING!!
        card.SetCardLocation(CardLocation.OnField);

        //Display path line for visual cues
        duelManager.DisplayLineArc(card.transform.position, node.transform.position);

        //Remove from hand
        _cardsInHand.Remove(card);
        _permanentsOnField.Add(card);

        //Move the card to its new position
        StartCoroutine(MoveCard(card, node.transform.position, node));
    }

    private IEnumerator MoveCard(Card_Permanent card, Vector3 endPos, GridNode node = null)
    {
        //float dist = Vector3.Distance(card.transform.position, endPos);
        float timeToMove = Vector3.Distance(card.transform.position, endPos) / 25f;
        //I actually think I want to increase the time for shorter distances


        float timeElapsed = 0f;
        var startPos = card.transform.position;
        var startRot = card.transform.rotation;
        var endRot = Quaternion.Euler(Vector3.zero);

        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime;

            card.transform.position = MathParabola.Parabola(startPos, endPos, timeElapsed / timeToMove);
            card.transform.rotation = Quaternion.Slerp(startRot, endRot, timeElapsed / timeToMove);

            yield return null;
        }

        //card.transform.position = endPos;
        card.transform.rotation = endRot;

        //Trigger the card for creating its permanent prefab
        card.OnSummoned(node);

        //Remove trail display
        duelManager.ClearLineArc();
    }

    #region - Mana -
    public void OnSpendMana(int points)
    {
        _currentMana -= points;
        if (_currentMana <= 0) //End phase if all AP has been spent
        {
            _currentMana = 0;
            if (isTurn && currentPhase == Phase.Summoning) duelManager.OnCurrentPhaseFinished();
        }
        onManaChange?.Invoke();
    }

    public void OnGainMana(int mana)
    {
        _currentMana += mana;
        onManaChange?.Invoke();
    }
    #endregion
}

/*
 * So during the declaration phase, the player is able to select their units and order them to move, attack, or use abilities
 * moving occurs first (you cannot move after you attack or use an ability)
 * then attacks and abilities
 * 
 * So what do I need to store for the resolution phase....
 * the units which are taking actions
 * each unit that is taking an action will also need to store what its actions will be
 * I can store this in the card itself!
 * 
 * So then I only need a list of cards that will be taking actions
 * 
 * 
*/