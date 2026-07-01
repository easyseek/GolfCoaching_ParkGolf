using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WebcamTrackerController : MonoBehaviour
{
    [SerializeField] WebcamTracker webcamTrackerFront;
    [SerializeField] WebcamTracker webcamTrackerSide;

    [SerializeField] bool StartOnAwake = false;
    [SerializeField] bool isFrontOn = false;
    [SerializeField] bool isSideOn = false;

    public bool IsSideOn
    {
        get { return isSideOn; }
        set { isSideOn = value; }
    }
    //[SerializeField] Toggle tglTest;

    public enum PROCMODE //정,측면 카메라 포즈추적 모드
    {
        Sequential, //순차 측정
        Parallel    //동시 측정
    }
    [SerializeField] PROCMODE ProcMode = PROCMODE.Sequential;


    IEnumerator Start()
    {
        StartCoroutine(CoTrackerProcess());

        if (StartOnAwake)
        {
            if(isFrontOn)
                yield return new WaitUntil(() => webcamTrackerFront.isInit == true);
            if (isSideOn)
                yield return new WaitUntil(() => webcamTrackerSide.isInit == true);

            SetTracker(isFrontOn, isSideOn);
        }
    }

    public void SetTracker(bool FrontOn, bool SideOn)
    {
        if (FrontOn)
        {
            webcamTrackerFront.SetTrackPose();
            isFrontOn = true;
        }
        else// if (isFrontOn == true && FrontOn == false)
        {
            webcamTrackerFront.ResetTrackPose();
            isFrontOn = false;
        }

        if (SideOn)
        {
            webcamTrackerSide.SetTrackPose();
            isSideOn = true;
        }
        else// if (isSideOn == true && SideOn == false)
        {
            webcamTrackerSide.ResetTrackPose();
            isSideOn = false;
        }
    }

    IEnumerator CoTrackerProcess()
    {
        while (true)
        {
            if (ProcMode == PROCMODE.Sequential)
            {
                if (isFrontOn && webcamTrackerFront.isTrackReady)
                {
                    webcamTrackerFront.GetTrackPose();
                    yield return null;
                }

                if (isSideOn && webcamTrackerSide.isTrackReady)
                {
                    webcamTrackerSide.GetTrackPose();
                    //yield return null;
                }
                yield return null;
            }
            else
            {
                if (isFrontOn && webcamTrackerFront.isTrackReady)
                {
                    webcamTrackerFront.GetTrackPose();
                }
                if (isSideOn && webcamTrackerSide.isTrackReady)
                {
                    webcamTrackerSide.GetTrackPose();
                }
                yield return null;
            }

            if (isFrontOn == false && isSideOn == false)
                yield return null;
        }
    }
}