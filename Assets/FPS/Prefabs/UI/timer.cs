using System;
using TMPro;
using Unity.FPS.Game;
using UnityEngine;

public class timer : MonoBehaviour
{
    private TextMeshProUGUI m_TextMeshPro;
    private float m_Time;
    public float bestScore;
    private bool win = false;

    void Start()
    {
        m_TextMeshPro = GetComponentInChildren<TextMeshProUGUI>();
        EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
        if (bestScore < 0)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
        else
        {
            bestScore = PlayerPrefs.GetFloat("score");
        }
        if (bestScore == 0) bestScore = 10000000000;
    }

    void Update()
    {
        if (!win)
        {
            m_Time += Time.deltaTime;
            m_TextMeshPro.text = GenTimeSpanFromSeconds(m_Time);
        }
        else if(m_Time < bestScore)
        {
            m_TextMeshPro.text = "Highscore: "+GenTimeSpanFromSeconds(m_Time) + "\n" + "Previous best: " + GenTimeSpanFromSeconds(bestScore);
            Debug.Log("<color=green>"+m_TextMeshPro.text+"</color>");
            PlayerPrefs.SetFloat("score", m_Time);
            PlayerPrefs.Save();
            bestScore = m_Time;
        }
    }

    static String GenTimeSpanFromSeconds(float seconds)
    {
        string timeInterval = TimeSpan.FromSeconds(seconds).ToString();
        return timeInterval.Substring(0, timeInterval.Length - 4);
    }
    void OnAllObjectivesCompleted(AllObjectivesCompletedEvent evt)
    {
        win = true;
    }
}
