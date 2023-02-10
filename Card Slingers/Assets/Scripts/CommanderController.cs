using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderController : MonoBehaviour
{
    public delegate void OnManaChangeCallback();
    public OnManaChangeCallback onManaChange;

    private DuelManager duelManager;

    [SerializeField] private CommanderSO _commanderInfo;
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
    public List<Card> CardsInDeck => _cardsInDeck;
    public List<Card> CardsInDiscard => _cardsInDiscardPile;
    public List<Card> CardsInHand => _cardsInHand;
    public List<Card_Permanent> PermanentsOnField => _permanentsOnField;
    public int CurrentMana => _currentMana;
    #endregion

    private void Start()
    {
        duelManager = DuelManager.instance;
    }

    public virtual void OnMatchStart(int startingHandSize = 4)
    {
        //Should only need this here for testing
        duelManager = DuelManager.instance;

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
        _cardsInDeck = new List<Card>();
        _cardsInDiscardPile = new List<Card>();
        _cardsInHand = new List<Card>();
        _permanentsOnField = new List<Card_Permanent>();

        foreach (CardSO card in _commanderInfo.Deck.cards)
        {
            /***Later on I'll have to check for each subtype, but this should work for now***/
            Card newCard;
            if (card is PermanentSO) newCard = Instantiate(duelManager.cardPermanentPrefab).GetComponent<Card>();
            else newCard = Instantiate(duelManager.cardPrefab).GetComponent<Card>();

            newCard.AssignCard(card, this, this == duelManager.playerController);
            _cardsInDeck.Add(newCard);
            DuelManager.instance.Battlefield.PlaceCardInDeck(this, newCard);
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
            case Phase.Declaration:
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

    private void ShuffleDiscardPile()
    {
        for (int i = _cardsInDiscardPile.Count - 1; i >= 0; i--)
        {
            var card = _cardsInDiscardPile[i];
            _cardsInDiscardPile.Remove(card);
            _cardsInDeck.Add(card);
            duelManager.Battlefield.PlaceCardInDeck(this, card);
        }
    }

    private void DrawCards(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (_cardsInDeck.Count <= 0)
            {
                Debug.Log("No Remaining Cards in Deck");
                ShuffleDiscardPile();
                return;
            }

            var cardToDraw = _cardsInDeck[0];
            _cardsInDeck.Remove(cardToDraw);
            _cardsInHand.Add(cardToDraw);
            DuelManager.instance.Battlefield.PlaceCardInHand(this, cardToDraw);
        }
    }

    private void DiscardCard(Card cardToDiscard)
    {
        if (!_cardsInHand.Contains(cardToDiscard)) return;

        _cardsInHand.Remove(cardToDiscard);
        cardToDiscard.OnSendToDiscardPile();
    }
    #endregion

    public bool CanPlayCard(int cardCost)
    {
        if (cardCost > _currentMana) return false;
        return true;
    }

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

    public void OnInstantPlayed(Card card)
    {
        OnSpendMana(card.CardInfo.cost);

        //If a target is needed, wait for a target to be selected
        //Trigger whatever effect the instant has

        //Send to discard pile
    }

    public void OnPermanentPlayed(GridNode node, Card_Permanent card)
    {
        //Spend Mana cost of card
        OnSpendMana(card.CardInfo.cost);

        //Remove from hand
        _cardsInHand.Remove(card);
        _permanentsOnField.Add(card);

        //Child to the battlefield
        //card.transform.SetParent(duelManager.Battlefield.transform);
        card.transform.SetParent(null);

        //Move the card to its new position
        StartCoroutine(MoveCard(card, node.transform.position, node));
    }

    private IEnumerator MoveCard(Card_Permanent card, Vector3 endPos, GridNode node = null)
    {
        float timeElapsed = 0f;
        float timeToMove = 1.5f;
        var startPos = card.transform.localPosition;
        var startRot = card.transform.rotation;
        var endRot = Quaternion.Euler(Vector3.zero);

        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime;

            card.transform.localPosition = MathParabola.Parabola(startPos, endPos, timeElapsed / timeToMove);
            card.transform.rotation = Quaternion.Slerp(startRot, endRot, timeElapsed / timeToMove);

            yield return new WaitForEndOfFrame();
        }

        //card.transform.SetParent(duelManager.Battlefield.transform);
        card.transform.localPosition = endPos;
        card.transform.rotation = endRot;

        //Trigger the card for creating its permanent prefab
        card.OnEnterField(node);
    }

    public void OnRemovePermanentFromField(Card_Permanent permanent)
    {
        //Trigger any exit effects
        permanent.OnRemoveFromField();
        
        //Moves the card to the discard pile
        permanent.OnSendToDiscardPile();
    }
}
