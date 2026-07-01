using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static WebcamTracker;
using Image = UnityEngine.UI.Image;
using Unity.Mathematics;

public class DrawGuideLine : MonoBehaviour
{
    [SerializeField] WebcamTracker tracker;    
    [SerializeField] Transform screen;
    [SerializeField] RectTransform RectMaskScreen;
    RectTransform RectScreen;
    //[SerializeField] Rect ScreenRect;
    CAMPOSITION camPosition;

    //가이드라인 그리기
    
    //사용자 상시 추적
    [SerializeField] GameObject NodePrefab;
    //[SerializeField] GameObject NodePrefabDebug;
    [SerializeField] GameObject ConnectPrefab;
    [SerializeField] GameObject BallObject;
    RectTransform[] userNode;
    UILineRenderer[] userConnect;

    //어드레스 기준 고정
    [SerializeField] RectTransform poseHead;
    [SerializeField] RectTransform poseCenter;
    [SerializeField] GameObject poseNodePrefab;

    [SerializeField] GameObject SideConnectEndPrefab;
    UILineRenderer[] poseConnect;
    RectTransform[] poseNode;
    RectTransform[] poseEndNode;
    
    public bool Draw = true;
    public RectTransform ballPosition;


    //readonly int[] POSE_LANDMARK = { 16, 14, 12, 11, 13, 15, 24, 23, 26, 25, 28, 27 };
    readonly int[] POSE_LANDMARK = { 12, 11, 24, 23 };

    [SerializeField] float lineThickness = 5f;
    [SerializeField] float lineThicknessUser = 10f;
    float _lineThickness = 5f;    
    float _lineThicknessUser = 10f;
    [SerializeField] float lineOverLength = 60f;
    float _lineOverLength = 60f;
    public float ScreenScale = 1f;
    Vector3 _nodeScale = Vector3.one;

    public bool LinePause = false;


    public Vector2 filteringShoulderCenter;
    public Vector2 filteringPelvisCenter;
    public Vector2 filteringFootLeft;
    public Vector2 filteringFootRight;
    public int SwingLimitUp = 90;
    public int SwingLimitDown = 0;

    [Header("* Ref. Color")]
    [SerializeField] Color colUserPose;
    [SerializeField] Color colPose;


    [Header("* Layers")]
    [SerializeField] Transform LayerHalfBottom;

    [Header("* Debug")]
    [SerializeField] TextMeshProUGUI txtDebug;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (RectMaskScreen == null)
            RectMaskScreen = screen.GetComponent<RectTransform>();

        
        RectScreen = screen.GetComponent<RectTransform>();
        

    }

    private void Update()
    {
        if(camPosition == CAMPOSITION.SIDE && Input.GetMouseButtonDown(0))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(RectScreen, Input.mousePosition, Camera.main, out Vector2 localPosition);
            Debug.Log($"{RectMaskScreen.sizeDelta})");
            Debug.Log($"{ballPosition.anchoredPosition} -> {localPosition}({Input.mousePosition})");
            if(localPosition.x > 0//
                && localPosition.x < (RectMaskScreen.sizeDelta.y / 2f)
                && localPosition.y > 0
                && localPosition.y < (RectMaskScreen.sizeDelta.x / 2f))
                ballPosition.anchoredPosition = localPosition;

            float rX = ballPosition.anchoredPosition.x / RectMaskScreen.sizeDelta.y;
            float rY = ballPosition.anchoredPosition.y / RectMaskScreen.sizeDelta.x;
            PlayerPrefs.SetFloat("BALLPOSX", rX);
            PlayerPrefs.SetFloat("BALLPOSY", rY);
        }
    }

    public void SetDraw(bool isOn, bool isCoaching)
    {
        ScreenScale = RectMaskScreen.sizeDelta.x / 1080f;// 1920f;
        _lineThickness = Mathf.Clamp(lineThickness * ScreenScale, 1f, 20f);
        _lineThicknessUser = Mathf.Clamp(lineThicknessUser * ScreenScale, 1f, 40f);
        camPosition = tracker.camPosition;
        _lineOverLength = lineOverLength * ScreenScale;
        _nodeScale = Vector3.one * ScreenScale;

        if (camPosition == CAMPOSITION.FRONT)
            SetDrawFront();
        else
        {
            SetDrawSide();
            BallObject.SetActive(false);

            Vector2 localPosition = new Vector2();
            localPosition.x = RectMaskScreen.sizeDelta.y * PlayerPrefs.GetFloat("BALLPOSX", 0.5f);
            localPosition.y = RectMaskScreen.sizeDelta.x *PlayerPrefs.GetFloat("BALLPOSY", 0.5f);
            ballPosition.anchoredPosition = localPosition;
            ballPosition.localScale = Vector3.one * ScreenScale;
        }

        if (Draw == false)
        {
            Draw = isOn;
            if (camPosition == CAMPOSITION.FRONT)
                SetDrawFront();
            else
                SetDrawSide();
        }
        else
        {
            Draw = isOn;
            if (poseConnect != null)
            {
                for (int i = 0; i < poseConnect.Length; i++)
                    poseConnect[i].gameObject.SetActive(false);
            }

            if (userConnect != null)
            {
                for (int i = 0; i < userConnect.Length; i++)
                    userConnect[i].gameObject.SetActive(false);
            }

            if (poseNode != null)
            {
                for (int i = 0; i < poseNode.Length; i++)
                    poseNode[i].gameObject.SetActive(false);
            }

            if (userNode != null)
            {
                for (int i = 0; i < userNode.Length; i++)
                    userNode[i].gameObject.SetActive(false);
            }

            if (poseHead != null)
                poseHead.gameObject.SetActive(false);
                    
            if (poseCenter != null)
                poseCenter.gameObject.SetActive(false);
                
            if (poseEndNode != null && poseEndNode.Length > 0)
            {
                for (int i = 0; i < poseEndNode.Length; i++)
                {
                    poseEndNode[i].gameObject.SetActive(false);
                }
            }
        }

        if (camPosition == CAMPOSITION.SIDE)
            BallObject.SetActive(isCoaching ? true : Draw);
    }

    public void SetDrawFront()// bool isDraw)
    {
        //Draw = isDraw;
        if (Draw)
        {
            //노드, 연결부
            if (poseConnect == null)
            {
                //사용자 상시 추적
                userConnect = new UILineRenderer[5];
                for (int i = 0; i < 5; i++)
                {
                    GameObject connect = Instantiate(ConnectPrefab);
                    connect.transform.SetParent(screen.transform);
                    connect.GetComponent<RectTransform>().sizeDelta = RectMaskScreen.sizeDelta;
                    connect.transform.localPosition = Vector3.zero;
                    connect.transform.localScale = Vector3.one;
                    connect.transform.localRotation = Quaternion.identity;
                    userConnect[i] = connect.GetComponent<UILineRenderer>();
                    userConnect[i].color = colUserPose;
                    connect.SetActive(false);
                    connect.transform.SetParent(LayerHalfBottom);
                }

                userNode = new RectTransform[6];
                for (int i = 0; i < 6; i++)
                {
                    GameObject node = Instantiate(NodePrefab);
                    node.transform.SetParent(screen.transform);
                    node.transform.localScale = _nodeScale;
                    node.transform.localRotation = Quaternion.identity;
                    userNode[i] = node.GetComponent<RectTransform>();
                    node.SetActive(false);
                    node.transform.SetParent(LayerHalfBottom);
                }

                //어드레스 기준 고정
                poseConnect = new UILineRenderer[1];
                for (int i = 0; i < 1; i++)
                {
                    GameObject connect = Instantiate(ConnectPrefab);
                    connect.transform.SetParent(screen.transform);
                    connect.GetComponent<RectTransform>().sizeDelta = RectMaskScreen.sizeDelta;
                    connect.transform.localPosition = Vector3.zero;
                    connect.transform.localScale = Vector3.one;
                    connect.transform.localRotation = Quaternion.identity;
                    poseConnect[i] = connect.GetComponent<UILineRenderer>();
                    poseConnect[i].color = colPose;
                    connect.SetActive(false);
                }

                poseCenter.SetAsLastSibling();
                poseHead.SetAsLastSibling();

            }
            StartCoroutine(CoDrawFront());
        }
        else
        {
            StopAllCoroutines();
            if (poseConnect != null && poseConnect.Length > 0)
            {
                for (int i = 0; i < poseConnect.Length; i++)
                {
                    poseConnect[i].gameObject.SetActive(false);
                }
            }
            
            if (poseNode != null && poseNode.Length > 0)
            {
                for (int i = 0; i < poseNode.Length; i++)
                {
                    poseNode[i].gameObject.SetActive(false);
                }
            }
            if (userConnect != null && userConnect.Length > 0)
            {
                for (int i = 0; i < userConnect.Length; i++)
                {
                    userConnect[i].gameObject.SetActive(false);
                }
            }
            if (userNode != null && userNode.Length > 0)
            {
                for (int i = 0; i < userNode.Length; i++)
                {
                    userNode[i].gameObject.SetActive(false);
                }
            }
            poseHead.gameObject.SetActive(false);
            poseCenter.gameObject.SetActive(false);
        }
    }

    IEnumerator CoDrawFront()
    {
        //float rVis = 0;
        while (true)
        {
            if (Draw)
            {
                if (tracker.Landmark?.Length > 0)
                {
                    DrawLineFront();
                }
                else
                {
                    for (int i = 0; i < poseConnect.Length; i++)
                    {
                        poseConnect[i].gameObject.SetActive(false);
                    }
                }

            }

            yield return null;
        }
    }


    void DrawLineFront()
    {
        for(int i = 0; i < 6; i++)
        {
            if (tracker.Landmark == null || tracker.Landmark.Length == 0)
            {
                userNode[i].gameObject.SetActive(false);
            }
            else
            {
                userNode[i].gameObject.SetActive(tracker.Landmark[i+23].visibility > 0.5f ? true : false);
                userNode[i].localScale = _nodeScale;
                userNode[i].localPosition = tracker.KalmanPositions.GetIndexPosition(i + 23);//tracker.Landmark[i+23].position;                
            }
        }
        CheckAndDrawUser(0, 23, 24);
        CheckAndDrawUser(1, 24, 26);
        CheckAndDrawUser(2, 26, 28);
        CheckAndDrawUser(3, 23, 25);
        CheckAndDrawUser(4, 25, 27);
        
        if (LinePause == false)
        {
            filteringShoulderCenter = tracker.KalmanPositions.CenterShoulder;
            

            //센터
            if (tracker.Landmark[11].visibility > 0.5f && tracker.Landmark[12].visibility > 0.5f)
            {
                CheckAndDraw(0, tracker.KalmanPositions.CenterShoulder, tracker.KalmanPositions.CenterPelvis, true, true);

                poseCenter.gameObject.SetActive(true);
                poseCenter.localScale = _nodeScale;
                poseCenter.localPosition = tracker.KalmanPositions.CenterShoulder;//tracker.Landmark[0].position;
            }
            else
            {
                poseCenter.gameObject.SetActive(false);
                poseConnect[0].gameObject.SetActive(false);
            }

            if (tracker.Landmark[31].visibility > 0.5f && tracker.Landmark[32].visibility > 0.5f)
            {
                filteringFootLeft = tracker.KalmanPositions.LeftFoot;
                filteringFootRight = tracker.KalmanPositions.RightFoot;
            }

            //머리
            if (tracker.Landmark[0].visibility > 0.5f)
            {
                poseHead.gameObject.SetActive(true);
                poseHead.localScale = _nodeScale;
                poseHead.localPosition = tracker.KalmanPositions.Nose;//tracker.Landmark[0].position;
                Vector2 angle = tracker.KalmanPositions.Nose - tracker.KalmanPositions.CenterShoulder;
                float fangle = Mathf.Atan2(angle.x, angle.y) * Mathf.Rad2Deg;
                //Debug.Log($"fangle:{fangle}");
                poseHead.localRotation = Quaternion.Euler(new Vector3(0, 0, -fangle));
            }
            else
                poseHead.gameObject.SetActive(false);
        }
        else
        {
        }        
    }

    public void SetDrawSide()//bool isDraw)
    {
        //Draw = isDraw;
        if (Draw)
        {
            //노드, 연결부
            if (poseConnect == null)
            {
                
                userConnect = new UILineRenderer[2];
                for (int i = 0; i < 2; i++)
                {
                    GameObject connect = Instantiate(ConnectPrefab);
                    connect.transform.SetParent(screen.transform);
                    connect.GetComponent<RectTransform>().sizeDelta = RectMaskScreen.sizeDelta;
                    connect.transform.localPosition = Vector3.zero;
                    connect.transform.localScale = Vector3.one;
                    connect.transform.localRotation = Quaternion.identity;
                    userConnect[i] = connect.GetComponent<UILineRenderer>();
                    userConnect[i].color = colUserPose;
                    connect.SetActive(false);
                }

                userNode = new RectTransform[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject node = Instantiate(NodePrefab);
                    node.transform.SetParent(screen.transform);
                    node.transform.localScale = _nodeScale;
                    node.transform.localRotation = Quaternion.identity;
                    userNode[i] = node.GetComponent<RectTransform>();
                    node.SetActive(false);
                }
                
                poseConnect = new UILineRenderer[4];
                for (int i = 0; i < 4; i++)
                {
                    GameObject connect = Instantiate(ConnectPrefab);
                    connect.transform.SetParent(screen.transform);
                    connect.GetComponent<RectTransform>().sizeDelta = RectMaskScreen.sizeDelta;
                    connect.transform.localPosition = Vector3.zero;
                    connect.transform.localScale = Vector3.one;
                    connect.transform.localRotation = Quaternion.identity;
                    poseConnect[i] = connect.GetComponent<UILineRenderer>();
                    poseConnect[i].color = colPose;
                    connect.SetActive(false);
                }
                Color tpCol = colPose;
                tpCol.a = 1f;
                poseConnect[0].color = tpCol;

                poseEndNode = new RectTransform[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject node = Instantiate(SideConnectEndPrefab);
                    node.transform.SetParent(screen.transform);
                    node.transform.localRotation = Quaternion.identity;
                    node.transform.localScale = Vector3.one;
                    poseEndNode[i] = node.GetComponent<RectTransform>();
                    node.SetActive(false);
                }

                poseNode = new RectTransform[2];
                for (int i = 0; i < 2; i++)
                {
                    GameObject node = Instantiate(poseNodePrefab);
                    node.transform.SetParent(screen.transform);
                    node.transform.localScale = _nodeScale;
                    node.transform.localRotation = Quaternion.identity;
                    poseNode[i] = node.GetComponent<RectTransform>();
                    node.SetActive(false);
                }

                //poseCenter.SetAsLastSibling();
                poseHead.SetAsLastSibling();

            }
            StartCoroutine(CoDrawSide());
        }
        else
        {
            StopAllCoroutines();
            if (poseConnect != null && poseConnect.Length > 0)
            {
                for (int i = 0; i < poseConnect.Length; i++)
                {
                    poseConnect[i].gameObject.SetActive(false);
                }
            }
            
            if (poseNode != null && poseNode.Length > 0)
            {
                for (int i = 0; i < poseNode.Length; i++)
                {
                    poseNode[i].gameObject.SetActive(false);
                }
            }
            if (userConnect != null && userConnect.Length > 0)
            {
                for (int i = 0; i < userConnect.Length; i++)
                {
                    userConnect[i].gameObject.SetActive(false);
                }
            }
            if (userNode != null && userNode.Length > 0)
            {
                for (int i = 0; i < userNode.Length; i++)
                {
                    userNode[i].gameObject.SetActive(false);
                }
            }
            if (poseEndNode != null && poseEndNode.Length > 0)
            {
                for (int i = 0; i < poseEndNode.Length; i++)
                {
                    poseEndNode[i].gameObject.SetActive(false);
                }
            }
            poseHead.gameObject.SetActive(false);
            poseCenter.gameObject.SetActive(false);
            
        }
    }

    IEnumerator CoDrawSide()
    {
        while (true)
        {
            if (Draw)
            {
                if (tracker.Landmark?.Length > 0)
                {
                    DrawLineSide();
                }
                else
                {
                    for (int i = 0; i < poseConnect.Length; i++)
                    {
                        poseConnect[i].gameObject.SetActive(false);
                    }
                }

            }

            yield return null;
        }
    }

    void DrawLineSide()
    {
        Vector2 cShoulder = Vector2.zero;
        Vector2 cPelvis = Vector2.zero;
        bool ChkShoulder = true;
        bool ChkPelvis = true;

        userNode[0].gameObject.SetActive(tracker.Landmark[24].visibility > 0.5f ? true : false);
        userNode[0].localScale = _nodeScale;
        userNode[0].localPosition = tracker.KalmanPositions.GetIndexPosition(24);// tracker.Landmark[24].position;        
        userNode[1].gameObject.SetActive(tracker.Landmark[26].visibility > 0.5f ? true : false);
        userNode[1].localScale = _nodeScale;
        userNode[1].localPosition = tracker.KalmanPositions.GetIndexPosition(26);//tracker.Landmark[26].position;
        userNode[2].gameObject.SetActive(tracker.Landmark[28].visibility > 0.5f ? true : false);
        userNode[2].localScale = _nodeScale;
        userNode[2].localPosition = tracker.KalmanPositions.GetIndexPosition(28);//tracker.Landmark[28].position;
        
        CheckAndDrawUser(0, 24, 26);
        CheckAndDrawUser(1, 26, 28);

        //어께 중심
        if (tracker.Landmark[12].visibility > 0.5f)
        {
            /*if (tracker.Landmark[11].visibility > 0.5f)
            {
                cShoulder = tracker.KalmanPositions.CenterShoulder;
            }
            else
            {
                cShoulder = tracker.KalmanPositions.RightShoulder;
            }*/
            cShoulder = tracker.KalmanPositions.RightShoulder;
        }
        else
        {
            ChkShoulder = false;
        }

        //골반 중심
        if (tracker.Landmark[24].visibility > 0.5f)
        {
            /*if (tracker.Landmark[23].visibility > 0.5f)
            {
                cPelvis = tracker.KalmanPositions.CenterPelvis;
            }
            else
            {
                cPelvis = tracker.KalmanPositions.RightPelvis;
            }*/
            cPelvis = tracker.KalmanPositions.RightPelvis;
        }
        else
        {
            ChkPelvis = false;
        }
        

        if (ChkShoulder && ChkPelvis)
        {

            //CheckAndDrawUser(2, cShoulder, cPelvis);

            if (LinePause == false)
            {

                Vector2 rightFoot = filteringFootRight = tracker.KalmanPositions.RightFoot;// filteringFootRight;

                //척추
                CheckAndDraw(0, cShoulder, cPelvis, true, true);
                
                poseNode[0].gameObject.SetActive(true);
                poseNode[0].localScale = _nodeScale;
                poseNode[0].localPosition = cShoulder;
                poseNode[1].gameObject.SetActive(true);
                poseNode[1].localScale = _nodeScale;
                poseNode[1].localPosition = cPelvis;

                //스윙각도
                if (ballPosition != null)
                {
                    CheckAndDraw(1, ballPosition.anchoredPosition, cShoulder, endNoOver: true, addOver: 200f);
                    Vector2 dir = (cShoulder - ballPosition.anchoredPosition).normalized;
                    SwingLimitUp = (int)(Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg) + 180;
                    CheckAndDraw(2, ballPosition.anchoredPosition, cPelvis, endNoOver: true, addOver: 200f);
                    dir = (cPelvis - ballPosition.anchoredPosition).normalized;
                    SwingLimitDown = (int)(Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg) + 180;
                    CheckAndDraw(3, ballPosition.anchoredPosition, (cShoulder + cPelvis) / 2, endNoOver: true, addOver: 200f);

                    poseEndNode[0].localScale = _nodeScale;
                    poseEndNode[0].localPosition = poseConnect[1].Points[0];
                    poseEndNode[1].localScale = _nodeScale;
                    poseEndNode[1].localPosition = poseConnect[2].Points[0];
                    poseEndNode[2].localScale = _nodeScale;
                    poseEndNode[2].localPosition = poseConnect[3].Points[0];
                    poseEndNode[0].gameObject.SetActive(true);
                    poseEndNode[1].gameObject.SetActive(true);
                    poseEndNode[2].gameObject.SetActive(true);  
                }
                else
                {
                    poseConnect[1].gameObject.SetActive(false);
                    poseConnect[2].gameObject.SetActive(false);
                    poseConnect[3].gameObject.SetActive(false);
                    poseEndNode[0].gameObject.SetActive(false);
                    poseEndNode[1].gameObject.SetActive(false);
                    poseEndNode[2].gameObject.SetActive(false);                   
                }


                //힙  
                /*Vector2 rightHip = tracker.KalmanPositions.RightPelvis;
                rightHip.y -= (130f * ScreenScale);

                poseConnect[4].LineThickness = _lineThickness;
                poseConnect[4].Points[0] = rightHip + Vector2.right * (_lineOverLength * 5); ;
                poseConnect[4].Points[1] = rightHip + Vector2.left * (_lineOverLength * 7);
                poseConnect[4].gameObject.SetActive(true);
                */

                //머리
                if (tracker.Landmark[0].visibility > 0.5f)
                {
                    poseHead.gameObject.SetActive(true);
                    poseHead.localScale = _nodeScale;
                    poseHead.localPosition = tracker.KalmanPositions.Nose;//tracker.Landmark[0].position;
                    Vector2 angle = tracker.KalmanPositions.Nose - tracker.KalmanPositions.RightShoulder;//.CenterShoulder;
                    float fangle = Mathf.Atan2(angle.x, angle.y) * Mathf.Rad2Deg;
                    //Debug.Log($"fangle:{fangle}");
                    poseHead.localRotation = Quaternion.Euler(new Vector3(0, 0, -fangle));
                }
                else
                    poseHead.gameObject.SetActive(false);
            }


        }
        else
        {
            poseConnect[0].gameObject.SetActive(false);
            poseConnect[1].gameObject.SetActive(false);
            poseConnect[2].gameObject.SetActive(false);
            poseConnect[3].gameObject.SetActive(false);
            //poseConnect[4].gameObject.SetActive(true);
            //poseCenter.gameObject.SetActive(false);
            for (int i = 0; i < poseNode.Length; i++)
            {
                poseNode[i].gameObject.SetActive(false);
            }

        }
    }

    bool CheckAndDraw(in int lineIdx, int start, int end, bool startNoOver = false, bool endNoOver = false, float addOver = 0)
    {
        if (tracker.Landmark[start].visibility > 0.5f && tracker.Landmark[end].visibility > 0.5f)
        {
            //CheckAndDraw(lineIdx, tracker.Landmark[start].position, tracker.Landmark[end].position, startNoOver, endNoOver, addOver);
            CheckAndDraw(lineIdx, tracker.KalmanPositions.GetIndexPosition(start), tracker.KalmanPositions.GetIndexPosition(end), startNoOver, endNoOver, addOver);
            return true;
        }
        else
            return false;
    }

    void CheckAndDraw(in int lineIdx, in Vector2 startVec, in Vector2 endVec, bool startNoOver = false, bool endNoOver = false, float addOver = 0)
    {
        Vector2 dir = (startVec - endVec);
        dir.Normalize();
        Vector2 start = endVec - dir * (startNoOver ? 1f : _lineOverLength + (addOver * ScreenScale));
        Vector2 end = startVec + dir * (endNoOver ? 1f : _lineOverLength + (addOver * ScreenScale));

        poseConnect[lineIdx].LineThickness = _lineThickness;
        poseConnect[lineIdx].Points[0] = start;
        poseConnect[lineIdx].Points[1] = end;
        poseConnect[lineIdx].gameObject.SetActive(true);
    }

    bool CheckAndDrawUser(in int lineIdx, int start, int end)
    {
        if (tracker.Landmark[start].visibility > 0.5f && tracker.Landmark[end].visibility > 0.5f)
        {
            //CheckAndDrawUser(lineIdx, tracker.Landmark[start].position, tracker.Landmark[end].position);
            CheckAndDrawUser(lineIdx, tracker.KalmanPositions.GetIndexPosition(start), tracker.KalmanPositions.GetIndexPosition(end));
            return true;
        }
        else
            return false;
    }

    void CheckAndDrawUser(in int lineIdx, in Vector2 startVec, in Vector2 endVec)//, bool startNoOver = false, bool endNoOver = false)
    {
        userConnect[lineIdx].LineThickness = _lineThicknessUser;
        userConnect[lineIdx].Points[0] = startVec;
        userConnect[lineIdx].Points[1] = endVec;
        userConnect[lineIdx].gameObject.SetActive(true);
    }

}
