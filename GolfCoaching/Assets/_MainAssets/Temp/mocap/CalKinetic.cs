using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class CalKinetic : MonoBehaviour
{
    [SerializeField] Transform orgPos;
    [SerializeField] Transform tarPos;
    [SerializeField] Transform[] orgTr;
    public Transform[] tarTr;
    [SerializeField] int ChestIndex = 0;
    [SerializeField] int PelvisIndex = 0;
    [SerializeField] int SpineIndex = 1;
    public Transform PelvisObject;
    public float ShoulderValue = 0;
    public float PelvisValue = 0;
    public float SpineValue = 0;

    //[SerializeField] mocapFront mocapFront;

    public bool useUserAngle = false;

    [Serializable]
    public enum POSITIONTYPE
    { FRONT, SIDE }
    [SerializeField] POSITIONTYPE positionType;

    [SerializeField] TextMeshProUGUI txtDebug;

    // Start is called before the first frame update
    void Start()
    {

    }

    //[SerializeField] Transform sd;
    //[SerializeField] Transform pd;
    //[SerializeField] Transform md;

    // Update is called once per frame
    void Update()
    {

        Vector3 pos = orgPos.position;
        tarPos.position = pos;
        tarPos.rotation = orgPos.rotation;


        for (int i = 0; i < orgTr.Length; i++)
        {
            Quaternion qt = orgTr[i].localRotation;

            tarTr[i].localRotation = qt;
        }

        //tarTr[SpineIndex].localRotation *= Quaternion.Euler(0f, SpineValue, 0f);

    }

}
