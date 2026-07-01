using UnityEngine;

public class SensorFront : MonoBehaviour
{
    [SerializeField] webcamclient client;

    // front
    KalmanFilter _iGetHandDir = new KalmanFilter();
    public int iGetHandDir; //각도 0~360

    KalmanFilter _iGetHandDistance = new KalmanFilter();
    public int iGetHandDistance;

    KalmanFilter _iGetShoulderDistance = new KalmanFilter();
    public int iGetShoulderDistance;    

    KalmanFilter _iGetSpineDir = new KalmanFilter();
    public int iGetSpineDir;

    KalmanFilter _iGetShoulderAngle = new KalmanFilter();
    public int iGetShoulderAngle;

    KalmanFilter _iGetWeight = new KalmanFilter();
    public int iGetWeight;

    KalmanFilter _iGetFootDisRate = new KalmanFilter();
    public int iGetFootDisRate;

    KalmanFilter _iGetForearmAngle = new KalmanFilter();
    public int iGetForearmAngle; //각도

    public int _iGetShoulderDir_Other; // >> 사이드에서 체크?
    public int _iGetPelvisDir_Other;   // >> 사이드에서 체크?

    Vector2 handVector;

    
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            client.StopPipeClient();
            Application.Quit();
        }

        GetHandDistance();
        GetShoulderDistance();

        GetHandPosition();

        GetHandDir();

        GetSpineDir();

        GetShoulderAngle();

        GetWeight();

        GetFootDisRate();

        GetForearmAngle();
    }


    //양 손목 사이 거리 (카메라와 거리에 따라 상대적)
    void GetHandDistance()
    {
        try
        {
            iGetHandDistance = (int)(_iGetHandDistance.Update(Vector2.Distance(client.poseData1["landmark_15"].Position,
                client.poseData1["landmark_16"].Position)));
        }
        catch { iGetHandDistance = -1; }
    }

    //양 어깨 사이 거리 (카메라와 거리에 따라 상대적)
    void GetShoulderDistance()
    {
        try
        {
            iGetShoulderDistance = (int)(_iGetShoulderDistance.Update(Vector2.Distance(client.poseData1["landmark_11"].Position,
                client.poseData1["landmark_12"].Position)));
        }
        catch { iGetShoulderDistance = -1; }
    }

    //스윙각도 계산을 위한 기준 손 좌표
    void GetHandPosition()
    {
        try
        {
            handVector = Vector2.Lerp(client.poseData1["landmark_15"].Position, client.poseData1["landmark_16"].Position
                , client.poseData1["landmark_16"].visibility);
        }
        catch { handVector = Vector2.zero;  }
    }

    //스윙각도 0~360도, 백스윙쪽이 각도가 줄어들고 팔로우쪽이 증가한다. 어드레서 약 180도
    void GetHandDir()
    {
        try
        {
            // 어꺠중심과 손중심을 기준
            Vector2 shoulderVector = (client.poseData1["landmark_11"].Position + client.poseData1["landmark_12"].Position) / 2;
            Vector2 dir = handVector - shoulderVector;

            float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            angle += 180f;
            iGetHandDir = (int)_iGetHandDir.Update(angle);
        }
        catch { iGetHandDir = -1; }
    }

    //허리 정면 각도. 왼쪽0도~오른쪽180도 허리가 수직일때 90도
    void GetSpineDir()
    {
        try
        {
            // 어꺠중심과 골반중심
            Vector2 pelvisVector = (client.poseData1["landmark_23"].Position + client.poseData1["landmark_24"].Position) / 2;
            Vector2 shoulderVector = (client.poseData1["landmark_11"].Position + client.poseData1["landmark_12"].Position) / 2;
            Vector2 dir = shoulderVector - pelvisVector;

            float angle = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            //angle += 180f;
            iGetSpineDir = (int)_iGetSpineDir.Update(angle);
        }
        catch { iGetSpineDir = -1; }
    }

    //왼쪽기준 오른쪽 어깨 각도 아래쪽 최대 0도~ 위쪽 최대 180도. 어깨 일직선 90도
    void GetShoulderAngle()
    {
        try
        {
            // 왼쪽 어꺠에서 오른쪽어께까지 각도
            Vector2 dir = client.poseData1["landmark_12"].Position - client.poseData1["landmark_11"].Position;

            float angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            //angle += 180f;
            iGetShoulderAngle = (int)_iGetShoulderAngle.Update(angle);
        }
        catch { iGetShoulderAngle = -1; }
    }

    //골만의 치우침 정도 약 -30 ~ 30도
    void GetWeight()
    {
        try
        {
            Vector2 footCenter = (client.poseData1["landmark_27"].Position + client.poseData1["landmark_28"].Position) / 2;
            Vector2 pelvisCenter = (client.poseData1["landmark_23"].Position + client.poseData1["landmark_24"].Position) / 2;

            Vector2 dir = footCenter - pelvisCenter;

            dir.Normalize();

            iGetWeight = (int)(_iGetWeight.Update( dir.x * 100f));
        }
        catch { iGetWeight = -1; }
    }


    //어깨 넓이 대비 다리 간격 백분율
    void GetFootDisRate()
    {
        try
        {
            float footDis = Vector2.Distance(client.poseData1["landmark_27"].Position, client.poseData1["landmark_28"].Position);

            iGetFootDisRate = (int)_iGetFootDisRate.Update((footDis / iGetShoulderDistance) * 100f);
        }
        catch { iGetFootDisRate = -1; }
    }

    //오른 어깨기준 오른팔꿈치 각도
    void GetForearmAngle()
    {
        try { 
            Vector2 dir = client.poseData1["landmark_14"].Position - client.poseData1["landmark_12"].Position;
            float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            angle += 180f;
            iGetForearmAngle = (int)_iGetForearmAngle.Update(angle);
        }
        catch { iGetForearmAngle = -1; }
    }
}
