using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [SerializeField] private TMP_Text title, description;
    [SerializeField] private Image display;
    [SerializeField] private GameObject[] costMarkers;

    public CardSO cardInfo;

    public void AssignCard(CardSO card)
    {
        cardInfo = card;

        title.text = cardInfo.name;

        for (int i = 0; i < costMarkers.Length; i++)
        {
            if (i < cardInfo.cost) costMarkers[i].SetActive(true);
            else costMarkers[i].SetActive(false);
        }
    }
}