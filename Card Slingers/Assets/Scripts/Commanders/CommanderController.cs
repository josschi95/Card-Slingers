using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderController : MonoBehaviour
{
    public delegate void OnCommanderPhaseChangeCallback(Phase phase);
    public OnCommanderPhaseChangeCallback onNewPhase;

    public delegate void OnStatValueChangedCallback();
    public OnStatValueChangedCallback onHealthChange;
    public OnStatValueChangedCallback onManaChange;

    public delegate void OnPermanentDestroyedCallback(Card_Permanent card);
    public OnPermanentDestroyedCallback onPermanentDestroyed;

    protected DuelManager duelManager;
    private CardHolder _cardHolder;
    [HideInInspector] public Animator animator;

    [SerializeField] private CommanderSO _commanderInfo;
    [SerializeField] private Card_Commander _commanderCard;
    [SerializeField] protected Phase currentPhase;
    [SerializeField] private int _currentMana = 4;
    [Space]
    [SerializeField] protected List<Card> _cardsInHand;
    [SerializeField] protected List<Card> _cardsInDeck;
    [SerializeField] private List<Card> _cardsInDiscardPile;
    [SerializeField] private List<Card> _cardsInExile;
    [Space]
    [SerializeField] protected List<Card_Permanent> _permanentsOnField;
    protected HealthDisplay healthDisplay;

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
    #endregion

    protected virtual void Start()
    {
        duelManager = DuelManager.instance;

        onPermanentDestroyed += SendPermanentToDiscard;
        healthDisplay = GetComponentInChildren<HealthDisplay>();
        healthDisplay.gameObject.SetActive(false);

    }

    #region - Initial Methods -
    public virtual void OnAssignCommander(CommanderSO commanderInfo)
    {
        _commanderInfo = commanderInfo;
        _commanderCard.AssignCard(commanderInfo, this);
    }

    public virtual void OnMatchStart(CardHolder holder, int startingHandSize = 4, int mana = 4)
    {
        SubscribeToMatchEvents();

        _cardHolder = holder;
        healthDisplay.gameObject.SetActive(true);

        _defaultMana = mana;
        _handSize = startingHandSize;

        _cardsInDeck = new List<Card>();
        _cardsInDiscardPile = new List<Card>();
        _cardsInHand = new List<Card>();
        _permanentsOnField = new List<Card_Permanent>();
        _permanentsOnField.Add(CommanderCard);

        GenerateNewDeck();
        ShuffleDeck();
        StartCoroutine(DrawCards());
    }

    protected virtual void GenerateNewDeck()
    {
        if (_commanderInfo == null)
        {
            Debug.Log("CommanderSO is null");
            return;
        }

        foreach (CardSO cardSO in _commanderInfo.Deck.cards)
        {
            Card newCard = Instantiate(cardSO.cardPrefab);
            newCard.AssignCard(cardSO, this);
            PlaceCardInDeck(newCard);
        }
    }
    #endregion

    #region - Phases -
    private void SetPhase(Phase phase)
    {
        if (!isTurn) return;
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
                OnAttackPhase();
                break;
            case Phase.End:
                OnEndPhase();
                break;
        }
    }

    protected virtual void OnBeginPhase()
    {
        RefillMana();

        //isDrawingCards should only stop this at the start of the match
        if (!isDrawingCards) StartCoroutine(DrawCards());

        //For each card on the field, invoke an OnBeginPhase event
        onNewPhase?.Invoke(Phase.Begin);

        duelManager.OnCurrentPhaseFinished();
    }

    protected virtual void OnSummoningPhase()
    {

    }

    protected virtual void OnAttackPhase()
    {

    }

    protected virtual void OnEndPhase()
    {
        
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
        card.transform.SetParent(_cardHolder.Hand);
    }

    protected void PlaceCardInDeck(Card card)
    {
        _cardsInDeck.Add(card);
        card.SetCardLocation(CardLocation.InDeck);
        card.transform.SetParent(_cardHolder.Deck);
    }

    private void PlaceCardInDiscard(Card card)
    {
        _cardsInDiscardPile.Add(card);
        card.SetCardLocation(CardLocation.InDiscard);
        card.transform.SetParent(_cardHolder.Discard);
    }

    private void PlaceCardInExile(Card card)
    {
        _cardsInExile.Add(card);
        card.SetCardLocation(CardLocation.InExile);
        card.transform.SetParent(_cardHolder.Exile);
    }

    private void DiscardCardFromHand(Card cardToDiscard)
    {
        if (!_cardsInHand.Contains(cardToDiscard)) return;
        _cardsInHand.Remove(cardToDiscard);
        PlaceCardInDiscard(cardToDiscard);
    }

    private void SendPermanentToDiscard(Card_Permanent permanent)
    {
        //Trigger any exit effects
        permanent.OnRemoveFromField();

        //Remove from list
        _permanentsOnField.Remove(permanent);

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
    }

    private void OnInstantPlayed(Card_Spell spell, GridNode node)
    {
        OnSpendMana(spell.CardInfo.cost);

        //Commander play casting animation
        _commanderCard.PermanentObject.GetComponent<Animator>().SetTrigger("ability");
        _commanderCard.onAbilityAnimation += delegate { OnInstantResolved(spell, node); };

        //This works for now but I should have the card go to a sort of limbo position
    }

    private void OnInstantResolved(Card_Spell spell, GridNode node)
    {
        Instantiate(spell.FX, node.transform.position + spell.StartPos, Quaternion.identity);

        if (node.Occupant != null)
        {
            for (int i = 0; i < spell.Effects.Length; i++)
            {
                GameManager.OnApplyEffect(node.Occupant, spell.Effects[i]);
            }
        }

        _commanderCard.onAbilityAnimation -= delegate { OnInstantResolved(spell, node); };

        //Send to discard pile
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

        if (card is Card_Trap trap)
        {
            OnTrapPlayed(trap, node);
            return;
        }

        //Display path line for visual cues
        duelManager.DisplayLineArc(card.transform.position, node.transform.position);

        //Move the card to its new position
        StartCoroutine(MoveCardToField(card, node));
    }

    private void OnTrapPlayed(Card_Trap trap, GridNode node)
    {
        trap.transform.SetParent(_cardHolder.Traps);

        trap.OnSummoned(node);
    }

    private IEnumerator MoveCardToField(Card_Permanent card, GridNode node)
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
        duelManager.ClearLineArc();
    }
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
        //if (isTurn && currentPhase == Phase.Summoning) duelManager.OnCurrentPhaseFinished();
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
        StartCoroutine(MoveToPosition(node, front));
    }

    private IEnumerator MoveToPosition(GridNode node, Vector3 front)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, node.transform.position) > 0.2f)
        {
            animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
            FaceTarget(node.transform.position);
            yield return null;
        }

        transform.position = node.transform.position;
        animator.SetFloat("speed", 0);

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
        duelManager.onPhaseChange += SetPhase;
        duelManager.onNewTurn += delegate { isTurn = !isTurn; };

        duelManager.onPlayerDefeat += OnPlayerDefeat;
        duelManager.onPlayerVictory += OnPlayerVictory;
    }

    protected void MatcheEnd()
    {
        duelManager.onPhaseChange -= SetPhase;
        duelManager.onNewTurn -= delegate { isTurn = !isTurn; };

        duelManager.onPlayerVictory -= OnPlayerVictory;
        duelManager.onPlayerDefeat -= OnPlayerDefeat;

        if (healthDisplay != null) healthDisplay.gameObject.SetActive(false);

        //need to let coroutines finish, and let player pocket their cards
        if (_cardHolder != null) Destroy(_cardHolder.gameObject, 5f); 
        //get rid of card holders. Probbly lerp them down eventually
    }
    #endregion
}