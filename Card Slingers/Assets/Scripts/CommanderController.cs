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
    [SerializeField] private List<Card> _cardsInPlay;
    [SerializeField] private List<Card> _discardPile;
    [Space]
    [SerializeField] private List<Permanent> permanentsInPlay;
    [SerializeField] private int currentActionPoints = 4;

    public List<Card> CardsInDeck => _cardsInDeck;
    public List<Card> CardsInHand => _cardsInHand;
    public List<Card> DiscardPile => _discardPile;

    private void Start()
    {
        duelManager = DuelManager.instance;
        duelManager.onBeginPhase += delegate { OnBeginPhase(); };
        duelManager.onSummoningPhase += delegate { OnSummoningPhase(); };
        duelManager.onPlanningPhase += delegate { OnDeclarationPhase(); };
        duelManager.onResolutionPhase += delegate { OnResolutionPhase(); };
        duelManager.onEndPhase += delegate { OnEndPhase(); };
    }

    public virtual void OnMatchStart(CommanderSO commander, int startingHandSize = 4)
    {
        _cardsInDeck = new List<Card>();
        _discardPile = new List<Card>();
        _cardsInPlay = new List<Card>();
        _cardsInHand = new List<Card>();
        permanentsInPlay = new List<Permanent>();

        for (int i = 0; i < commander.Deck.cards.Count; i++)
        {
            var go = Instantiate(duelManager.cardPrefab);
            var newCard = go.GetComponent<Card>();
            newCard.AssignCard(commander.Deck.cards[i]);
            _cardsInDeck.Add(newCard);
        }

        ShuffleDeck();

        //Draw cards
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawCard();
        }
    }

    private void SetPhase(Phase phase)
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
        Debug.Log(gameObject.name + " Begin Phase");

        currentActionPoints = 4;

        DrawCard();

        //For each card on the field, invoke an OnBeginPhase event
    }

    public void OnSummoningPhase()
    {
        Debug.Log(gameObject.name + " Summoning Phase");


    }

    public void OnDeclarationPhase()
    {
        Debug.Log(gameObject.name + " Declaration Phase");
    }

    public void OnResolutionPhase()
    {
        Debug.Log(gameObject.name + " Resolution Phase");
    }

    public void OnEndPhase()
    {
        Debug.Log(gameObject.name + " End Phase");
    }

    public void OnNextPhase()
    {
        if (currentPhase == Phase.End) OnBeginPhase();
        else SetPhase(currentPhase + 1);
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

    private void DrawCard()
    {
        _cardsInHand.Add(_cardsInDeck[0]);
        _cardsInDeck.RemoveAt(0);
    }

    private void DiscardCard(Card cardToDiscard)
    {
        if (_cardsInHand.Contains(cardToDiscard))
        {
            _cardsInHand.Remove(cardToDiscard);
            _discardPile.Add(cardToDiscard);
        }
    }

    public bool CanPlayCard(int cardCost)
    {
        if (cardCost > currentActionPoints) return false;
        return true;
    }

    public void OnInstantPlayed(Card card)
    {
        currentActionPoints -= card.cardInfo.cost;


    }

    public void OnPermanentPlayed(int x, int z, Card card)
    {
        currentActionPoints -= card.cardInfo.cost;

        var permanent = card.cardInfo as PermanentSO;

        var go = Instantiate(permanent.Prefab, new Vector3(x, z), Quaternion.identity);
        var newPermanent = go.GetComponent<Permanent>();

        permanentsInPlay.Add(newPermanent);
        newPermanent.OnEnterField(this, card);
        _cardsInHand.Remove(card);
    }

    public void OnPermanentRemovedFromField(Permanent permanent)
    {
        //Move card to discard

        //Removes from list of permanents under control
        permanentsInPlay.Remove(permanent);
        //Trigger any exit effects
        permanent.OnExitField();
        //Add to discard pile
        _discardPile.Add(permanent.Card);
    }
}
