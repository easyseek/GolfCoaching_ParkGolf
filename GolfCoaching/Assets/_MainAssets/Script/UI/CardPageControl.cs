using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardPageControl<TCard, TData> where TCard : MonoBehaviour
{
    private Func<TCard, TData, int, Action<TData>, TCard> setupCardCallback;

    private RectTransform prevGroup;
    private RectTransform curGroup;
    private RectTransform nextGroup;
    private RectTransform hideGroup;

    private GameObject cardPrefab;

    private Action onPageChanged;
    private Action<TData> onCardClicked;

    private List<TCard> prevCards = new();
    private List<TCard> curCards = new();
    private List<TCard> nextCards = new();
    private List<TData> dataList = new();

    private Vector2 prevGroupPos;
    private Vector2 curGroupPos;
    private Vector2 nextGroupPos;
    
    private int cardsPerPage;
    private int currentPage = 0;
    private int totalPages = 1;

    private bool isMoving = false;

    public int CurrentPage {
        get { return currentPage; }
    }

    public int TotalPages {
        get { return totalPages; }
    }

    public void Initialize(GameObject prefab, RectTransform prev, RectTransform cur, RectTransform next, RectTransform hide, int perPage, Func<TCard, TData, int, Action<TData>, TCard> setupCallback, Action onPageChange = null)
    {
        cardPrefab = prefab;
        prevGroup = prev;
        curGroup = cur;
        nextGroup = next;
        hideGroup = hide;
        cardsPerPage = perPage;
        setupCardCallback = setupCallback;
        onPageChanged = onPageChange;

        prevGroupPos = prev.anchoredPosition;
        curGroupPos = cur.anchoredPosition;
        nextGroupPos = next.anchoredPosition;

        prevCards = InstantiateCards(prevGroup);
        curCards = InstantiateCards(curGroup);
        nextCards = InstantiateCards(nextGroup);
    }

    public void SetCardClickAction(Action<TData> callback)
    {
        onCardClicked = callback;
    }

    public void SetData(List<TData> newData)
    {
        dataList = newData;
        totalPages = Mathf.Max(1, Mathf.CeilToInt(dataList.Count / (float)cardsPerPage));
        currentPage = 0;

        LoadPage();
    }

    private List<TCard> InstantiateCards(RectTransform parent)
    {
        var list = new List<TCard>();

        foreach (Transform child in parent)
            UnityEngine.Object.Destroy(child.gameObject);

        for (int i = 0; i < cardsPerPage; i++)
        {
            var obj = GameObject.Instantiate(cardPrefab, parent);

            if (obj.TryGetComponent<TCard>(out var card))
                list.Add(card);
        }

        return list;
    }

    private void LoadCards(List<TCard> cards, int pageIndex, RectTransform targetGroup)
    {
        if (pageIndex < 0 || pageIndex >= totalPages)
        {
            foreach (var card in cards)
                card.transform.SetParent(hideGroup, false);

            return;
        }

        int start = pageIndex * cardsPerPage;
        int count = Mathf.Min(cardsPerPage, dataList.Count - start);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < count)
            {
                var data = dataList[start + i];

                var card = setupCardCallback(cards[i], data, i, ApplyData);
                card.transform.SetParent(targetGroup, false);

                if (!card.gameObject.activeSelf)
                    card.gameObject.SetActive(true);
            }
            else
            {
                cards[i].transform.SetParent(hideGroup, false);
            }
        }
    }

    private void LoadPage()
    {
        LoadCards(prevCards, currentPage - 1, prevGroup);
        LoadCards(curCards, currentPage, curGroup);
        LoadCards(nextCards, currentPage + 1, nextGroup);

        onPageChanged?.Invoke();
    }

    private void ApplyData(TData data)
    {
        onCardClicked?.Invoke(data);
    }

    public void NextPage()
    {
        if (currentPage >= totalPages - 1 || isMoving) 
            return;

        isMoving = true;
        currentPage++;

        curGroup.DOAnchorPos(prevGroupPos, 0.3f);
        nextGroup.DOAnchorPos(curGroupPos, 0.3f).OnComplete(() => {
            RectTransform tempGroup = prevGroup;
            prevGroup = curGroup;
            curGroup = nextGroup;
            nextGroup = tempGroup;

            List<TCard> tempList = prevCards;
            prevCards = curCards;
            curCards = nextCards;
            nextCards = tempList;

            nextGroup.anchoredPosition = nextGroupPos;
            LoadCards(nextCards, currentPage + 1, nextGroup);

            isMoving = false;
            onPageChanged?.Invoke();
        });
    }

    public void PrevPage()
    {
        if (currentPage <= 0 || isMoving) return;
        isMoving = true;
        currentPage--;

        curGroup.DOAnchorPos(nextGroupPos, 0.3f);
        prevGroup.DOAnchorPos(curGroupPos, 0.3f).OnComplete(() => {
            RectTransform tempGroup = nextGroup;
            nextGroup = curGroup;
            curGroup = prevGroup;
            prevGroup = tempGroup;

            List<TCard> tempList = nextCards;
            nextCards = curCards;
            curCards = prevCards;
            prevCards = tempList;

            prevGroup.anchoredPosition = prevGroupPos;
            LoadCards(prevCards, currentPage - 1, prevGroup);

            isMoving = false;
            onPageChanged?.Invoke();
        });
    }
}
