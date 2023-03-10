using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandDisplay : MonoBehaviour
{
    [SerializeField] private PlayerCommander _player;
    [SerializeField] private CardDisplay[] _playerCards;

    private void Start()
    {
        _player.onCardsInHandChange += UpdateCardDisplay;

        for (int i = 0; i < _playerCards.Length; i++)
        {
            _playerCards[i].gameObject.SetActive(false);
        }
    }

    private void UpdateCardDisplay()
    {
        for (int i = 0; i < _playerCards.Length; i++)
        {
            if (i >= _player.CardsInHand.Count)
            {
                _playerCards[i].AssignedCard = null;
                _playerCards[i].gameObject.SetActive(false);
                continue;
            }

            _playerCards[i].AssignedCard = _player.CardsInHand[i];
            _playerCards[i].gameObject.SetActive(true);
        }
    }
}
