using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private RectTransform playerHand;
    [SerializeField] private Vector2 hiddenPos;

    public void ShowPlayerHand()
    {
        playerHand.anchoredPosition = Vector2.zero;
    }

    public void HidePlayerHand()
    {
        playerHand.anchoredPosition = hiddenPos;
    }
}
