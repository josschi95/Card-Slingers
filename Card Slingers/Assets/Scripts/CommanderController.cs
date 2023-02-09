using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderController : MonoBehaviour
{
    private DuelManager duelManager;

    [SerializeField] private Phase currentPhase;
    [Space]
    [SerializeField] private List<Card> _cardsInDeck;
    [SerializeField] private List<Card> _cardsInHand;
    //[SerializeField] private List<Card> _cardsInPlay;
    [SerializeField] private List<Card> _cardsInDiscardPile;
    [Space]
    [SerializeField] private List<Permanent> permanentsInPlay;
    [SerializeField] private int currentActionPoints = 4;

    public bool isTurn { get; private set; }

    public Transform deckPile, handPile, discardPile;

    public List<Card> CardsInDeck => _cardsInDeck;
    public List<Card> CardsInHand => _cardsInHand;
    public List<Card> CardsInDiscard => _cardsInDiscardPile;

    public virtual void OnMatchStart(CommanderSO commander, Transform deckPile, Transform hand, Transform discard, int startingHandSize = 4)
    {
        duelManager = DuelManager.instance;

        _cardsInDeck = new List<Card>();
        _cardsInDiscardPile = new List<Card>();
        //_cardsInPlay = new List<Card>();
        _cardsInHand = new List<Card>();
        permanentsInPlay = new List<Permanent>();

        this.deckPile = deckPile;
        handPile = hand;
        discardPile = discard;

        for (int i = 0; i < commander.Deck.cards.Count; i++)
        {
            var go = Instantiate(duelManager.cardPrefab);
            go.transform.SetParent(this.deckPile, false);

            var newCard = go.GetComponent<Card>();
            newCard.AssignCard(commander.Deck.cards[i], this == duelManager.playerController);
            _cardsInDeck.Add(newCard);
            newCard.SetCardLocation(CardLocation.InDeck);
        }

        ShuffleDeck();

        //Draw cards
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawCard();
        }
    }

    #region - Phases -
    public void SetPhase(Phase phase)
    {
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

    public void OnBeginPhase()
    {
        Debug.Log(gameObject.name + " entered Begin Phase");
        isTurn = true;
        currentActionPoints = 4;

        DrawCard();
        //This will eventually have an animation, so wait until that is finished

        //For each card on the field, invoke an OnBeginPhase event

        DuelManager.instance.OnCurrentPhaseFinished();
    }

    private void OnSummoningPhase()
    {
        Debug.Log(gameObject.name + " entered Summoning Phase");


    }

    private void OnDeclarationPhase()
    {
        Debug.Log(gameObject.name + " entered Declaration Phase");
    }

    private void OnResolutionPhase()
    {
        Debug.Log(gameObject.name + " entered Resolution Phase");
    }

    private void OnEndPhase()
    {
        Debug.Log(gameObject.name + " entered End Phase");

        isTurn = false;

        DuelManager.instance.OnCurrentPhaseFinished();
    }

    public void OnNextPhase()
    {
        if (currentPhase == Phase.End) OnBeginPhase();
        else SetPhase(currentPhase + 1);
    }
    #endregion

    #region - Card Movement -
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
            card.transform.SetParent(deckPile, false);
            card.SetCardLocation(CardLocation.InDeck);
        }
    }

    private void DrawCard()
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
        cardToDraw.transform.SetParent(handPile, false);
        cardToDraw.SetCardLocation(CardLocation.InHand);
    }

    private void DiscardCard(Card cardToDiscard)
    {
        if (!_cardsInHand.Contains(cardToDiscard)) return;

        _cardsInHand.Remove(cardToDiscard);
        _cardsInDiscardPile.Add(cardToDiscard);
        cardToDiscard.transform.SetParent(discardPile);
        cardToDiscard.SetCardLocation(CardLocation.InDiscard);
    }
    #endregion

    public bool CanPlayCard(int cardCost)
    {
        if (cardCost > currentActionPoints) return false;
        return true;
    }

    private void OnSpendActionPoints(int points)
    {
        currentActionPoints -= points;
        if (currentActionPoints <= 0)
        {
            currentActionPoints = 0;
            if (isTurn && currentPhase == Phase.Summoning)
            {
                //End phase if all AP has been spent
                duelManager.OnCurrentPhaseFinished();
            }
        }
    }

    public void OnInstantPlayed(Card card)
    {
        OnSpendActionPoints(card.cardInfo.cost);

        //If a target is needed, wait for a target to be selected
        //Trigger whatever effect the instant has

        //Send to discard pile
    }

    public void OnPermanentPlayed(int x, int z, Card card)
    {
        OnSpendActionPoints(card.cardInfo.cost);

        var permanent = card.cardInfo as PermanentSO;

        var go = Instantiate(permanent.Prefab, new Vector3(x, z), Quaternion.identity);
        var newPermanent = go.GetComponent<Permanent>();

        permanentsInPlay.Add(newPermanent);
        newPermanent.OnEnterField(this, card);

        _cardsInHand.Remove(card);
        card.transform.SetParent(go.transform, false);
        card.SetCardLocation(CardLocation.OnField);
    }

    public void OnPermanentRemovedFromField(Permanent permanent)
    {
        //Move card to discard

        //Removes from list of permanents under control
        permanentsInPlay.Remove(permanent);
        //Trigger any exit effects
        permanent.OnExitField();
        //Add to discard pile
        _cardsInDiscardPile.Add(permanent.Card);
        permanent.Card.SetCardLocation(CardLocation.InDiscard);
    }
}
