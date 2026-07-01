using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UIElements;

public class DrawLandmark : MonoBehaviour
{
    [SerializeField] WebcamTracker tracker;
    [SerializeField] Transform screen;
    [SerializeField] RectTransform RectMaskScreen;

    //랜드마크 그리기
    [SerializeField] GameObject NodePrefab;
    [SerializeField] GameObject ConnectPrefab;
    [SerializeField] GameObject HeadPrefab;
    RectTransform poseHead;
    RectTransform[] poseNode;
    UILineRenderer[] poseConnect;
    [SerializeField] bool Draw = true;

    readonly int[] POSE_LANDMARK = { 16, 14, 12, 11, 13, 15, 24, 23, 26, 25, 28, 27 };
    readonly int[,] POSE_CONNECTION = { {11, 13}, {13, 15}, {11, 23}, {23, 25}, {25, 27},
                                        {11, 12}, {12, 24}, {23, 24}, {12, 14}, {14, 16},
                                        {24, 26}, {26, 28} };
    [SerializeField] float lineThickness = 5f;
    float ScreenScale = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(RectMaskScreen == null)
            RectMaskScreen = screen.GetComponent<RectTransform>();

        ScreenScale = RectMaskScreen.sizeDelta.x / 1920f;
        lineThickness = lineThickness * ScreenScale;

        SetDraw(Draw);

    }

    public void SetDraw(bool isDraw)
    {
        Draw = isDraw;
        if (Draw)
        {
            //헤드
            GameObject head = Instantiate(HeadPrefab);
            head.transform.SetParent(screen.transform);
            head.transform.localScale = Vector3.one * ScreenScale;
            head.transform.localRotation = Quaternion.identity;
            poseHead = head.GetComponent<RectTransform>();
            head.SetActive(false);

            //노드, 연결부
            poseNode = new RectTransform[12];
            poseConnect = new UILineRenderer[12];
            for (int i = 0; i < 12; i++)
            {
                GameObject connect = Instantiate(ConnectPrefab);
                connect.transform.SetParent(screen.transform);
                connect.GetComponent<RectTransform>().sizeDelta = RectMaskScreen.sizeDelta;
                connect.transform.localPosition = Vector3.zero;
                connect.transform.localScale = Vector3.one;
                connect.transform.localRotation = Quaternion.identity;
                poseConnect[i] = connect.GetComponent<UILineRenderer>();
                connect.SetActive(false);
            }

            for (int i = 0; i < 12; i++)
            {
                GameObject node = Instantiate(NodePrefab);
                node.transform.SetParent(screen.transform);
                node.transform.localScale = Vector3.one * ScreenScale;
                node.transform.localRotation = Quaternion.identity;
                poseNode[i] = node.GetComponent<RectTransform>();
                node.SetActive(false);
            }

            StartCoroutine(CoDrawSkeleton());
        }
        else
        {
            StopAllCoroutines();
            poseHead.gameObject.SetActive(false);
            for (int i = 0; i < poseNode.Length; i++)
            {
                poseNode[i].gameObject.SetActive(false);
                poseConnect[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator CoDrawSkeleton()
    {
        float rVis = 0;
        while (true)
        {
            if (Draw)
            {
                if (tracker.Landmark?.Length > 0)
                {
                    try
                    {
                        // 머리
                        if (tracker.Landmark[0].visibility > 0.5f)
                        {
                            poseHead.localPosition = tracker.Landmark[0].position;
                            poseHead.gameObject.SetActive(true);
                        }
                        else
                        {
                            poseHead.gameObject.SetActive(false);
                        }
                    }
                    catch
                    {
                        poseHead.gameObject.SetActive(false);
                    }


                    int cIdx = 0;
                    DrawLine();
                    DrawCircle();
                }
                else
                {
                    poseHead.gameObject.SetActive(false);
                    for (int i = 0; i < poseNode.Length; i++)
                    {
                        poseNode[i].gameObject.SetActive(false);
                        poseConnect[i].gameObject.SetActive(false);
                    }
                }

            }

            yield return null;
        }
    }

    void DrawLine()
    {
        int Idx = 0;
        for (int i = 0; i < POSE_CONNECTION.GetLength(0); i++)
        {
            try
            {

                if (tracker.Landmark[POSE_CONNECTION[i, 0]].visibility > 0.5f && tracker.Landmark[POSE_CONNECTION[i, 1]].visibility > 0.5f)
                {
                    poseConnect[Idx].LineThickness = lineThickness;
                    poseConnect[Idx].Points[0] = tracker.Landmark[POSE_CONNECTION[i, 0]].position;
                    poseConnect[Idx].Points[1] = tracker.Landmark[POSE_CONNECTION[i, 1]].position;
                    poseConnect[Idx].gameObject.SetActive(true);
                }
                else
                {
                    poseConnect[Idx].LineThickness = 0;
                    poseConnect[Idx].Points[0] = Vector2.zero;
                    poseConnect[Idx].Points[1] = Vector2.zero;
                    poseConnect[Idx].gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                poseConnect[Idx].LineThickness = 0;
                poseConnect[Idx].Points[0] = Vector2.zero;
                poseConnect[Idx].Points[1] = Vector2.zero;
                poseConnect[Idx].gameObject.SetActive(false);
            }
            Idx++;
        }
    }

    void DrawCircle()
    {
        int Idx = 0;
        //몸
        for (int i = 0; i < POSE_LANDMARK.Length; i++)
        {
            try
            {
                if (tracker.Landmark[POSE_LANDMARK[i]].visibility > 0.5f)
                {
                    poseNode[Idx].localPosition = tracker.Landmark[POSE_LANDMARK[i]].position;
                    poseNode[Idx].gameObject.SetActive(true);

                }
                else
                {
                    poseNode[Idx].gameObject.SetActive(false);
                }
            }
            catch
            {
                poseNode[Idx].gameObject.SetActive(false);
            }

            Idx++;
        }

    }
}
