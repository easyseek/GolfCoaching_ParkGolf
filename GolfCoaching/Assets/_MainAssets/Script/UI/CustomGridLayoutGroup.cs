using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class CustomGridLayoutGroup : LayoutGroup
{
    public Vector2 spacing = Vector2.zero;
    public int columns = 2; // 한 행에 배치할 자식 개수
    public float defaultWidth = 100f;
    public float defaultHeight = 100f;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetCells();
    }

    public override void CalculateLayoutInputVertical() { }

    public override void SetLayoutHorizontal()
    {
        SetCells();
    }

    public override void SetLayoutVertical()
    {
        SetCells();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetDirty();
    }
#endif

    private void SetCells()
    {
        int childCount = rectChildren.Count;
        if (childCount == 0)
            return;

        int rows = Mathf.CeilToInt(childCount / (float)columns);
        float yOffset = padding.top;
        int childIndex = 0;

        for (int row = 0; row < rows; row++)
        {
            float xOffset = padding.left;

            for (int col = 0; col < columns; col++)
            {
                if (childIndex >= childCount)
                    break;

                RectTransform child = rectChildren[childIndex];
                LayoutElement layoutElem = child.GetComponent<LayoutElement>();
                
                float childWidth = (layoutElem != null && layoutElem.preferredWidth > 0) ? layoutElem.preferredWidth : defaultWidth;
                float childHeight = (layoutElem != null && layoutElem.preferredHeight > 0) ? layoutElem.preferredHeight : defaultHeight;

                SetChildAlongAxis(child, 0, xOffset, childWidth);
                SetChildAlongAxis(child, 1, yOffset, childHeight);
                xOffset += childWidth + spacing.x;
                childIndex++;
            }

            yOffset += defaultHeight + spacing.y;
        }
    }
}