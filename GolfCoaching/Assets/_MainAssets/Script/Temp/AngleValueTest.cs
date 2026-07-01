using System.Collections;
using TMPro;
using UnityEngine;

public class AngleValueTest : MonoBehaviour
{

    public MirrorDirector mirror;
    public SensorProcess sensorProcess;

    [SerializeField] RectTransform iGetShoulderAngle;    
    [SerializeField] RectTransform iGetShoulderSideDirWorld;    
    [SerializeField] RectTransform iGetShoulderFrontDirWorld;    
    [SerializeField] RectTransform iGetShoulderDir;    

    [SerializeField] RectTransform iGetPelvisAngle;    
    [SerializeField] RectTransform iGetPelvisSideDirWorld;    
    [SerializeField] RectTransform iGetPelvisFrontDirWorld;    
    [SerializeField] RectTransform iGetPelvisDir;    

    [SerializeField] TextMeshProUGUI txtiGetShoulderAngle;
    [SerializeField] TextMeshProUGUI txtiGetShoulderSideDirWorld;
    [SerializeField] TextMeshProUGUI txtiGetShoulderFrontDirWorld;
    [SerializeField] TextMeshProUGUI txtiGetShoulderDir;

    [SerializeField] TextMeshProUGUI txtiGetPelvisAngle;
    [SerializeField] TextMeshProUGUI txtiGetPelvisSideDirWorld;
    [SerializeField] TextMeshProUGUI txtiGetPelvisFrontDirWorld;
    [SerializeField] TextMeshProUGUI txtiGetPelvisDir;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        yield return new WaitUntil(() => mirror.IsCoaching);
        StartCoroutine(DrawUserInfoFront());
    }

    IEnumerator DrawUserInfoFront()
    {
        while(true)
        {
            //2. 어께
            iGetShoulderAngle.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetShoulderAngle);            
            iGetShoulderSideDirWorld.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetShoulderSideDirWorld);
            iGetShoulderFrontDirWorld.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetShoulderFrontDirWorld);
            iGetShoulderDir.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetShoulderDir);

            txtiGetShoulderAngle.text = sensorProcess.iGetShoulderAngle.ToString();
            txtiGetShoulderSideDirWorld.text = sensorProcess.iGetShoulderSideDirWorld.ToString();
            txtiGetShoulderFrontDirWorld.text = sensorProcess.iGetShoulderFrontDirWorld.ToString();
            txtiGetShoulderDir.text = sensorProcess.iGetShoulderDir.ToString();

            //3. 골반
            iGetPelvisAngle.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetPelvisAngle);
            iGetPelvisSideDirWorld.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetPelvisSideDirWorld);
            iGetPelvisFrontDirWorld.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetPelvisFrontDirWorld);
            iGetPelvisDir.localRotation = Quaternion.Euler(0, 0, sensorProcess.iGetPelvisDir);

            txtiGetPelvisAngle.text = sensorProcess.iGetPelvisAngle.ToString();
            txtiGetPelvisSideDirWorld.text = sensorProcess.iGetPelvisSideDirWorld.ToString();
            txtiGetPelvisFrontDirWorld.text = sensorProcess.iGetPelvisFrontDirWorld.ToString();
            txtiGetPelvisDir.text = sensorProcess.iGetPelvisDir.ToString();


            yield return null;
        }
    }
}
