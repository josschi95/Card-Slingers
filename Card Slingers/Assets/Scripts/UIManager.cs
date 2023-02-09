using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private void Awake()
    {
        instance = this;
    }

    [Header("Commander Banner")]
    [SerializeField] private TMP_Text playerCommanderName;
    [SerializeField] private TMP_Text playerHealth, playerMana, opponentCommanderName, opponentHealth, opponentMana; 

    [Header("Card Display")]
    [SerializeField] private RectTransform cardDisplayRect;
    [SerializeField] private Button hideCardDisplayButton;
    [Space]
    [SerializeField] private Image cardIcon;
    [SerializeField] private GameObject[] cardCostMarkers;
    [SerializeField] private TMP_Text cardTitle, cardDescription, cardFlavorText;
    private Coroutine lerpCardDisplayCoroutine;
    private Vector3 cardDisplayShownPos = Vector3.zero;
    private Vector3 cardDisplayHiddenPos = new Vector3(500, 0, 0);

    private void Start()
    {
        cardDisplayRect.anchoredPosition = cardDisplayHiddenPos;
        hideCardDisplayButton.onClick.AddListener(HideCardDisplay);
    }

    private void OnMatchStart()
    {
        //Show top display
        DuelManager.instance.playerController.onManaChange += OnCommanderValuesChanged;
        DuelManager.instance.opponentController.onManaChange += OnCommanderValuesChanged;
    }

    private void OnCommanderValuesChanged()
    {
        playerCommanderName.text = DuelManager.instance.playerController.gameObject.name;
        playerHealth.text = DuelManager.instance.playerController.name;
        playerMana.text = DuelManager.instance.playerController.CurrentMana.ToString();
        
        opponentCommanderName.text = DuelManager.instance.opponentController.gameObject.name;
        opponentHealth.text = DuelManager.instance.opponentController.name;
        opponentMana.text = DuelManager.instance.opponentController.CurrentMana.ToString();
    }

    public void ShowCardDisplay(CardSO card)
    {
        cardIcon.sprite = card.icon;
        cardTitle.text = card.name;
        cardDescription.text = card.description;
        cardFlavorText.text = card.flavorText;

        for (int i = 0; i < cardCostMarkers.Length; i++)
        {
            if (i < card.cost) cardCostMarkers[i].SetActive(true);
            else cardCostMarkers[i].SetActive(false);
        }

        if (lerpCardDisplayCoroutine != null) StopCoroutine(lerpCardDisplayCoroutine);
        lerpCardDisplayCoroutine = StartCoroutine(LerpCardDisplay(true));
    }

    public void HideCardDisplay()
    {
        if (lerpCardDisplayCoroutine != null) StopCoroutine(lerpCardDisplayCoroutine);
        lerpCardDisplayCoroutine = StartCoroutine(LerpCardDisplay(false));
    }

    private IEnumerator LerpCardDisplay(bool showDisplay)
    {
        float timeElapsed = 0;
        float timeToMove = 0.35f;

        var endPos = cardDisplayHiddenPos;
        if (showDisplay) endPos = cardDisplayShownPos;

        while (timeElapsed < timeToMove)
        {
            cardDisplayRect.anchoredPosition = Vector3.Lerp(cardDisplayRect.anchoredPosition, endPos, (timeElapsed / timeToMove));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        cardDisplayRect.anchoredPosition = endPos;
    }
}
