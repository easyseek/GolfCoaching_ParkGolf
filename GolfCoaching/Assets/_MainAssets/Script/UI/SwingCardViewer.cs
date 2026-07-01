using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SwingCardViewer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [System.Serializable]
    public class CardData
    {
        public string name;
        public Texture2D texture;

        public CardData(string name, Texture2D texture)
        {
            this.name = name;
            this.texture = texture;
        }
    }

    private List<CardData> cardDataList;

    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardParent;

    [SerializeField] private Transform leftPos;
    [SerializeField] private Transform centerPos;
    [SerializeField] private Transform rightPos;

    private Vector3 centerScale = Vector3.one;
    private Vector3 sideScale;

    private List<CaptureCardUI> cardUIs = new List<CaptureCardUI>();
    private int currentIndex = 0;

    private Vector2 dragStartPos;
    private bool isDragging = false;

    private void Awake()
    {
        sideScale = Vector3.one * 0.8f;
    }

    //void Start()
    //{
    //    UpdateCardStates(true);
    //}

    public void SetCardList(List<CardData> newCardDataList)
    {
        foreach (var card in cardUIs)
        {
            Destroy(card.gameObject);
        }
        cardUIs.Clear();

        cardDataList = newCardDataList;
        currentIndex = 0;

        for (int i = 0; i < cardDataList.Count; i++)
        {
            GameObject go = Instantiate(cardPrefab, cardParent);
            CaptureCardUI card = go.GetComponent<CaptureCardUI>();
            card.SetCard(cardDataList[i].texture, cardDataList[i].name);
            card.exitButton.onClick.RemoveAllListeners();
            card.exitButton.onClick.AddListener(() => gameObject.SetActive(false));

            cardUIs.Add(card);

            int index = i;
            go.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(index));
        }

        UpdateCardStates(true);
    }

    void OnCardClicked(int index)
    {
        if (index == currentIndex) return;
        currentIndex = index;
        UpdateCardStates();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPos = eventData.position;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        Vector2 delta = eventData.position - dragStartPos;
        float threshold = 50f;

        if (Mathf.Abs(delta.x) > threshold)
        {
            if (delta.x < 0 && currentIndex < cardUIs.Count - 1)
            {
                currentIndex++;
                UpdateCardStates();
            }
            else if (delta.x > 0 && currentIndex > 0)
            {
                currentIndex--;
                UpdateCardStates();
            }
        }
    }

    void UpdateCardStates(bool instant = false)
    {
        for (int i = 0; i < cardUIs.Count; i++)
        {
            CaptureCardUI card = cardUIs[i];

            Vector2 targetPos;
            Vector3 targetScale;
            bool isCenter = false;

            if (i == currentIndex)
            {
                targetPos = centerPos.localPosition;
                targetScale = centerScale;
                isCenter = true;
            }
            else if (i == currentIndex - 1)
            {
                targetPos = leftPos.localPosition;
                targetScale = sideScale;
            }
            else if (i == currentIndex + 1)
            {
                targetPos = rightPos.localPosition;
                targetScale = sideScale;
            }
            else if (i < currentIndex - 1)
            {
                targetPos = leftPos.localPosition + Vector3.left * 1000;
                targetScale = Vector3.zero;
            }
            else
            {
                targetPos = rightPos.localPosition + Vector3.right * 1000;
                targetScale = Vector3.zero;
            }

            card.gameObject.SetActive(i >= currentIndex - 1 && i <= currentIndex + 1);

            if (instant)
                card.SetTransform(targetPos, targetScale, isCenter);
            else
                card.AnimateTo(targetPos, targetScale, 0.3f, isCenter);
        }
    }

    public void ShowAtIndex(int index)
    {
        gameObject.SetActive(true);
        currentIndex = Mathf.Clamp(index, 0, cardUIs.Count - 1);
        UpdateCardStates(true);
    }
}