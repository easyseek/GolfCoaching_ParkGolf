using UnityEngine;
using System;
using System.Collections;
using TMPro;

public class BottomStatusBar : MonoBehaviour
{

    public TextMeshProUGUI txtSlotNum;
    public TextMeshProUGUI txtModeTitle;
    public TextMeshProUGUI txtClock;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        StartCoroutine(CoClock());
    }


    IEnumerator CoClock()
    {
        while (true)
        {
            txtClock.text = DateTime.Now.ToString("hh:mm");
            yield return new WaitForSeconds(0.1f);
        }
    }
}
