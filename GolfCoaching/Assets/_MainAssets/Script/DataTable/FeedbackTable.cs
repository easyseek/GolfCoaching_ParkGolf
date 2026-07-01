using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class FeedbackData
{
    public string swingStep;
    public string name;
    public int priority;
    public string feedback;
}

public class FeedbackTable
{
    static FeedbackTable Instance = null;
    static readonly object padlock = new object();

    public static FeedbackTable GetInstance {
        get {
            lock (padlock)
            {
                if (null == Instance)
                    Instance = new FeedbackTable();
                return Instance;
            }
        }
    }

    private Dictionary<string, FeedbackData> m_pList = null;

    public bool LoadTable()
    {
        if (m_pList == null)
            m_pList = new Dictionary<string, FeedbackData>();
        else
            m_pList.Clear();

        var _list = CSVReader.ReadCSV("TableFeedback");

        if (_list == null || _list.Count == 0)
        {
            return false;
        }

        foreach ( var item in _list )
        {
            FeedbackData pFeedbackData = new FeedbackData {
                swingStep = item["sSwingStep"].ToString(),
                name = item["sName"].ToString(),
                priority = int.Parse(item["iPriority"].ToString()),
                feedback = item["sFeedback"].ToString()
            };

            //Debug.Log($"sSwingStep : {pFeedbackData.swingStep}, name : {pFeedbackData.name}, priority : {pFeedbackData.priority}, feedback : {pFeedbackData.feedback}");

            m_pList.Add(pFeedbackData.swingStep, pFeedbackData);
        }

        return true;
    }

    public FeedbackData GetFeedbackData(string swingStep)
    {
        FeedbackData temp;
        this.m_pList.TryGetValue(swingStep, out temp);
        return temp;
    }

    public bool ContainsKey(string swingStep)
    {
        return m_pList.ContainsKey(swingStep);
    }

    public Dictionary<string, FeedbackData> Getlist()
    {
        return m_pList;
    }
}
