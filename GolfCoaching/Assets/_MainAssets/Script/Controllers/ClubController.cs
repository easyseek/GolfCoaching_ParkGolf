using Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static RootMotion.FinalIK.AimPoser;

public class ClubController : MonoBehaviour
{
    [SerializeField] private GameObject m_ClubPanel;
    [SerializeField] private GameObject m_IronToggleObj;
    [SerializeField] private GameObject[] m_TGIronClub;

    [SerializeField] private ToggleGroup m_TGClub;
    [SerializeField] private ToggleGroup m_TGIron;
    
    [SerializeField] Toggle[] m_ClubToggles;
    [SerializeField] Toggle[] m_IronToggles;

    [SerializeField] private EClub club = EClub.None;

    [SerializeField] private int ironNumber = 0;

    private bool bInit = false;

    private void OnEnable()
    {
        if (bInit)
        {
            StartCoroutine(OnEnableReset());
        }
    }

    private IEnumerator OnEnableReset()
    {
        yield return null;

        SetClub();
    }

    private void Start()
    {
        for (int i = 0; i < m_ClubToggles.Length; i++)
        {
            m_ClubToggles[i].onValueChanged.AddListener(OnValueChanged_Club);
        }

        for (int i = 0; i < m_IronToggles.Length; i++)
        {
            m_IronToggles[i].onValueChanged.AddListener(OnValueChanged_Iron);
        }

        SetClub();

        bInit = true;
    }

    private void SetClub()
    {
        Toggle clubToggle = m_ClubToggles[(int)GameManager.Instance.Club];

        clubToggle.isOn = true;
        clubToggle.GetComponentInChildren<ToggleUIAddon>().m_ExternalObj.SetActive(true);

        if(GameManager.Instance.Club >= EClub.ShortIron && GameManager.Instance.Club <= EClub.LongIron)
        {
            for (int i = 0; i < m_IronToggles.Length; i++)
            {
                if (GameManager.Instance.IronNumber == m_IronToggles[i].GetComponentInChildren<UIValueObject>().intValue)
                {
                    m_IronToggles[i].isOn = true;
                    m_IronToggles[i].GetComponentInChildren<ToggleUIAddon>().m_ExternalObj.SetActive(true);
                }

            }
        }
    }

    public void OnValueChanged_Club(bool isOn)
    {
        if (!isOn)
            return;

        Toggle activeToggle = m_TGClub.ActiveToggles().FirstOrDefault();
        int value = activeToggle != null ? activeToggle.GetComponent<UIValueObject>().intValue : 3;
        EClub c = (EClub)value;

        if (c == club)
            return;

        //Debug.Log($"OnValueChanged_Club {isOn} {c}");

        club = c;

        switch(c)
        {
            case EClub.ShortIron:
            case EClub.MiddleIron:
            case EClub.LongIron:
                m_IronToggleObj.SetActive(true);

                for (int i = 0; i < m_IronToggles.Length; i++)
                {
                    m_IronToggles[i].isOn = false;
                }

                if (c == EClub.ShortIron)
                {
                    for (int i = 0; i < m_IronToggles.Length; i++)
                    {
                        if (9 == m_IronToggles[i].GetComponentInChildren<UIValueObject>().intValue)
                        {
                            m_IronToggles[i].isOn = true;
                            ironNumber = 9;
                        }
                    }
                }
                else if(c == EClub.MiddleIron)
                {
                    for (int i = 0; i < m_IronToggles.Length; i++)
                    {
                        if (7 == m_IronToggles[i].GetComponentInChildren<UIValueObject>().intValue)
                        {
                            m_IronToggles[i].isOn = true;
                            ironNumber = 7;
                        }
                    }
                }
                else if (c == EClub.LongIron)
                {
                    for (int i = 0; i < m_IronToggles.Length; i++)
                    {
                        if (4 == m_IronToggles[i].GetComponentInChildren<UIValueObject>().intValue)
                        {
                            m_IronToggles[i].isOn = true;
                            ironNumber = 4;
                        }
                    }
                }

                for (int i = 0; i < m_TGIronClub.Length; i++)
                {
                    m_TGIronClub[i].SetActive(i == value - 2 ? true : false);
                }

                break;

            case EClub.Driver:
            case EClub.Wood:
            case EClub.Approach:
            case EClub.Putter:
                m_IronToggleObj.SetActive(false);

                for (int i = 0; i < m_TGIronClub.Length; i++)
                    m_TGIronClub[i].SetActive(false);

                ironNumber = -1;

                break;
        }
    }

    public void OnValueChanged_Iron(bool isOn)
    {
        if (!isOn)
            return;

        Toggle activeToggle = m_TGIron.ActiveToggles().FirstOrDefault();
        int value = activeToggle != null ? activeToggle.GetComponent<UIValueObject>().intValue : 7;
        //Debug.Log($"[OnValueChanged_Club] {value}");

        ironNumber = value;
    }

    public void OnClick_ClubPanel()
    {
        m_ClubPanel.SetActive(!m_ClubPanel.activeInHierarchy);
    }

    public void OnClick_Apply()
    {
        if(GameManager.Instance.Club == club && GameManager.Instance.IronNumber == ironNumber)
        {
            m_ClubPanel.SetActive(!m_ClubPanel.activeInHierarchy);
        }
        else
        {
            GameManager.Instance.Club = club;
            GameManager.Instance.IronNumber = ironNumber;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
