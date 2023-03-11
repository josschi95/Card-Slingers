using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderController : MonoBehaviour
{
    #region - Callbacks -
    public delegate void OnCardsChangeCallback();
    public OnCardsChangeCallback onCardsInHandChange;

    public delegate void OnStatValueChangedCallback();
    public OnStatValueChangedCallback onHealthChange;
    public OnStatValueChangedCallback onManaChange;
    #endregion

    protected DuelManager duelManager;
    protected Animator commanderAnimator;

    [SerializeField] private CommanderSO _commanderInfo;
    [SerializeField] private Card_Commander _commanderCard;
    [SerializeField] private int _currentMana = 4;
    [Space]
    [SerializeField] protected List<Card> _cardsInHand;
    [SerializeField] protected List<Card> _cardsInDeck;
    [SerializeField] protected List<Card> _cardsInDiscardPile;
    [SerializeField] protected List<Card> _cardsInExile;
    [Space]
    [SerializeField] protected List<Card_Permanent> _permanentsOnField;
    protected HealthDisplay healthDisplay;
    protected Quaternion _defaultRotation;

    public bool isTurn { get; protected set; }
    private int _defaultMana = 4;
    private int _handSize = 4;

    #region - Properties -
    public bool isDrawingCards { get; private set; } //drawing cards, don't trigger raise card
    public CommanderSO CommanderInfo => _commanderInfo;
    public Card_Commander CommanderCard => _commanderCard;
    public int CurrentMana => _currentMana;
    public List<Card> CardsInHand => _cardsInHand;
    public List<Card_Permanent> CardsOnField => _permanentsOnField;
    public Quaternion DefaultRotation
    {
        get => _defaultRotation;
        set
        {
            _defaultRotation = value;
        }
    }
    #endregion

    protected virtual void Start()
    {
        duelManager = DuelManager.instance;

        healthDisplay = GetComponentInChildren<HealthDisplay>();
        healthDisplay.gameObject.SetActive(false);
    }

    #region - Initial Methods -
    public virtual void OnAssignCommander(CommanderSO commanderInfo)
    {
        _commanderInfo = commanderInfo;
        _commanderCard = new Card_Commander(commanderInfo, this is PlayerCommander);
    }

    public virtual void OnMatchStart(int startingHandSize = 4, int mana = 4)
    {
        SubscribeToMatchEvents();

        healthDisplay.gameObject.SetActive(true);

        _defaultMana = mana;
        _handSize = startingHandSize;

        _cardsInDeck = new List<Card>();
        _cardsInHand = new List<Card>();
        _cardsInDiscardPile = new List<Card>();
        _cardsInExile = new List<Card>();
        _permanentsOnField = new List<Card_Permanent>();
        _permanentsOnField.Add(CommanderCard);

        GenerateNewDeck();
        ShuffleDeck();
        StartCoroutine(DrawCards());
    }

    protected virtual void GenerateNewDeck()
    {
        if (_commanderInfo == null) throw new UnityException("CommanderSO is null. Cannot Generate Deck.");
        bool isPlayer = this is PlayerCommander;

        foreach (CardSO cardSO in _commanderInfo.Deck.cards)
        {
            Card newCard = null;
            
            switch (cardSO.type)
            {
                case CardType.Unit:
                    newCard = new Card_Unit(cardSO as UnitSO, isPlayer);
                    break;
                case CardType.Structure:
                    newCard = new Card_Structure(cardSO as StructureSO, isPlayer);
                    break;
                case CardType.Trap:
                    newCard = new Card_Trap(cardSO as TrapSO, isPlayer);
                    break;
                case CardType.Equipment:
                    throw new UnityException("Equipment has not been added!");
                    //newCard = new Card_Equipment();
                    //break;
                case CardType.Terrain:
                    throw new UnityException("Terrain has not been added!");
                    //newCard = new Card_Terrain();
                    //break;
                case CardType.Spell:
                    newCard = new Card_Spell(cardSO as SpellSO, isPlayer);
                    break;
                case CardType.Commander:
                    throw new UnityException("Should not be adding commander to deck!");
            }

            PlaceCardInDeck(newCard);
        }
    }
    #endregion

    #region - Phases -
    protected virtual void OnNewTurn(bool isPlayerTurn)
    {
        //Meant to be overwritten
    }

    protected virtual void OnTurnStart()
    {
        RefillMana();

        //isDrawingCards should only stop this at the start of the match
        if (!isDrawingCards) StartCoroutine(DrawCards());

        //For each card on the field, trigger OnTurnStart effects
        for (int i = 0; i < _permanentsOnField.Count; i++)
        {
            _permanentsOnField[i].OnTurnStart();
        }
    }
    #endregion

    #region - Card Movements -
    private IEnumerator DrawCards()
    {
        isDrawingCards = true;
        duelManager.onCardMovementStarted?.Invoke(null);

        while (_cardsInHand.Count < _handSize)
        {
            if (_cardsInDeck.Count <= 0)
            {
                Debug.Log("No Remaining Cards in Deck");
                ReturnDiscardPileToDeck();
                yield return new WaitForSeconds(1.5f); //give time for the deck to reshuffle
            }

            var cardToDraw = _cardsInDeck[0];
            _cardsInDeck.Remove(cardToDraw);
            PlaceCardInHand(cardToDraw);

            yield return new WaitForSeconds(0.5f);

            yield return null;
        }

        duelManager.onCardMovementEnded?.Invoke(null);
        isDrawingCards = false;
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

    private void PlaceCardInHand(Card card)
    {
        _cardsInHand.Add(card);
        card.SetCardLocation(CardLocation.InHand);
        onCardsInHandChange?.Invoke();
    }

    protected void PlaceCardInDeck(Card card)
    {
        _cardsInDeck.Add(card);
        card.SetCardLocation(CardLocation.InDeck);
    }

    private void PlaceCardInDiscard(Card card)
    {
        _cardsInDiscardPile.Add(card);
        card.SetCardLocation(CardLocation.InDiscard);
    }

    private void PlaceCardInExile(Card card)
    {
        _cardsInExile.Add(card);
        card.SetCardLocation(CardLocation.InExile);
    }

    private void DiscardCardFromHand(Card cardToDiscard)
    {
        if (!_cardsInHand.Contains(cardToDiscard)) return;
        _cardsInHand.Remove(cardToDiscard);
        PlaceCardInDiscard(cardToDiscard);
    }

    private void OnPermanentDestroyed(Card_Permanent permanent)
    {
        permanent.onPermanentDestroyed -= OnPermanentDestroyed;
        //Remove from list
        _permanentsOnField.Remove(permanent);
    }

    private void OnPermanentRemoved(Card_Permanent permanent)
    {
        permanent.onRemovedFromField -= OnPermanentRemoved;
        //Moves the card to the discard pile
        PlaceCardInDiscard(permanent);
    }

    //private void ReturnPermanentInPlayToHand(Card_Permanent card) { }
    #endregion

    #region - Card Play -
    public bool CanPlayCard(Card card)
    {
        if (card.CardInfo.cost > _currentMana) return false;
        return true;
    }

    public void OnCardPlayed(Card card, GridNode node)
    {
        if (card is Card_Spell spell) OnInstantPlayed(spell, node);
        else if (card is Card_Permanent perm) OnPermanentPlayed(perm, node);
        onCardsInHandChange?.Invoke();
    }

    private void OnInstantPlayed(Card_Spell spell, GridNode node)
    {
        _cardsInHand.Remove(spell);
        OnSpendMana(spell.CardInfo.cost);

        //Commander play casting animation
        //_commanderCard.PermanentObject.GetComponent<Animator>().SetTrigger("ability");
        commanderAnimator.SetTrigger("ability");

        StartCoroutine(ResolveInstantDelay(spell, node));

        //This works for now but I should have the card go to a sort of limbo position
    }

    private IEnumerator ResolveInstantDelay(Card_Spell spell, GridNode node)
    {
        yield return new WaitForSeconds(1f);

        Instantiate(spell.FX, node.Transform.position + spell.StartPos, Quaternion.identity);

        if (node.Occupant != null)
        {
            for (int i = 0; i < spell.Effects.Length; i++)
            {
                GameManager.OnApplyEffect(node.Occupant, spell.Effects[i]);
            }
        }

        PlaceCardInDiscard(spell);
    }

    private void OnPermanentPlayed(Card_Permanent card, GridNode node)
    {
        //Spend Mana cost of card
        OnSpendMana(card.CardInfo.cost);

        // !!MUST CHANGE LOCATION BEFORE DESELECTING!!
        card.SetCardLocation(CardLocation.OnField);

        //Remove from hand
        _cardsInHand.Remove(card);
        _permanentsOnField.Add(card);

        card.onPermanentDestroyed += OnPermanentDestroyed;
        card.onRemovedFromField += OnPermanentRemoved;

        //Display path line for visual cues
        //duelManager.DisplayLineArc(card.transform.position, node.transform.position);
        var info = card.CardInfo as PermanentSO;
        var summon = Instantiate(info.Prefab, node.Transform.position, _defaultRotation);
        card.OnSummoned(summon, node);
        //Move the card to its new position
        //StartCoroutine(MoveCardToField(card, node));
    }

    /*private IEnumerator MoveCardToField(Card_Permanent card, GridNode node)
    {
        //float dist = Vector3.Distance(card.transform.position, endPos);
        float timeToMove = Vector3.Distance(card.transform.position, node.transform.position) / 25f;
        //I actually think I want to increase the time for shorter distances


        float timeElapsed = 0f;
        var startPos = card.transform.position;
        var startRot = card.transform.rotation;
        var endRot = transform.rotation; //set to same rotation as commander

        if (this is not PlayerCommander) endRot = Quaternion.Euler(0, 180, 0);

        duelManager.onCardMovementStarted?.Invoke(card);
        while (timeElapsed < timeToMove)
        {
            timeElapsed += Time.deltaTime;

            card.transform.position = MathParabola.Parabola(startPos, node.transform.position, timeElapsed / timeToMove);
            card.transform.rotation = Quaternion.Slerp(startRot, endRot, timeElapsed / timeToMove);

            yield return null;
        }
        duelManager.onCardMovementEnded?.Invoke(card);

        //card.transform.position = endPos;
        card.transform.rotation = endRot;

        //Trigger the card for creating its permanent prefab
        card.OnSummoned(node);

        //Remove trail display
        //duelManager.ClearLineArc();
    }*/
    #endregion

    #region - Mana -
    private void RefillMana()
    {
        _currentMana = _defaultMana;
        onManaChange?.Invoke();
    }

    public void OnSpendMana(int points)
    {
        _currentMana -= points;
        if (_currentMana <= 0) _currentMana = 0; //End phase if all AP has been spent
        onManaChange?.Invoke();
    }

    public void OnGainMana(int mana)
    {
        _currentMana += mana;
        onManaChange?.Invoke();
    }
    #endregion

    #region - Off-Grid Movement -
    public bool isMoving { get; private set; }
    
    public void SetStartingNode(GridNode node, Vector3 front)
    {
        if (commanderAnimator == null)
        {
            commanderAnimator = GetComponentInChildren<Animator>();
        }

        StartCoroutine(MoveToPosition(node, front));
    }

    private IEnumerator MoveToPosition(GridNode node, Vector3 front)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, node.transform.position) > 0.2f)
        {
            commanderAnimator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
            FaceTarget(node.transform.position);
            yield return null;
        }

        transform.position = node.transform.position;
        commanderAnimator.SetFloat("speed", 0);

        _commanderCard.SetStartingNode(node);
        isMoving = false;

        StartCoroutine(TurnToFaceTarget(front));
    }

    private IEnumerator TurnToFaceTarget(Vector3 pos)
    {
        float t = 0, timeToMove = 0.5f;
        while (t < timeToMove)
        {
            FaceTarget(pos);
            t += Time.deltaTime;
            yield return null;
        }
    }

    private void FaceTarget(Vector3 pos) //update this to accept a Transform transform?
    {
        Vector3 direction = (pos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
    }
    #endregion

    #region - Match End -
    protected virtual void OnPlayerVictory()
    {
        MatcheEnd();

        //Meant to be overridden, but keep base
    }

    protected virtual void OnPlayerDefeat()
    {
        MatcheEnd();

        //Meant to be overridden, but keep base
    }

    protected void SubscribeToMatchEvents()
    {
        duelManager.onNewTurn += OnNewTurn;

        duelManager.onPlayerDefeat += OnPlayerDefeat;
        duelManager.onPlayerVictory += OnPlayerVictory;
    }

    protected void MatcheEnd()
    {
        duelManager.onNewTurn -= OnNewTurn;

        duelManager.onPlayerVictory -= OnPlayerVictory;
        duelManager.onPlayerDefeat -= OnPlayerDefeat;

        if (healthDisplay != null) healthDisplay.gameObject.SetActive(false);
    }
    #endregion
}