﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScorecardLoader : MonoBehaviour
{
    public static ScorecardLoader inst;

    public int firstoutplayer = -1;
    public Color32 defaultColor;
    public Color32 WinnerColor;

    [Header("Waiting")]
    public GameObject waitingPopup;
    public TextMeshProUGUI waitingScore;

    [Header("Scorecard")]
    public GameObject scoreCard;
    public TextMeshProUGUI[] nameTexts;
    public ScorecardTexts[] scorecardTexts;
    public TextMeshProUGUI[] totalScores;

    public GameObject showScoreBtn;
    public GameObject closeScoreBtn;

    [Header("Ready Checking")]
    public GameObject readycheck;
    public TextMeshProUGUI readyText;
    public bool ready;

    int[][] loadedScores;

    private void Awake()
    {
        inst = this;
        gameObject.SetActive(false);
    }

    public void EnableWait(int score)
    {
        waitingPopup.SetActive(true);
        waitingScore.text = score.ToString();
    }

    public void DisableWait()
    {
        waitingPopup.SetActive(false);
        scoreCard.SetActive(true);
    }

    public void LoadNames(string[] playerNames)
    {
        for(int i = 0; i < playerNames.Length; i++)
        {
            nameTexts[i].text = playerNames[i];
        }
    }

    public void LoadScores(int[][] scores)
    {
        for(int i = 0; i < scores.Length; i++)
        {
            if (scores[i] == null) break;

            for(int j = 0; j < scores[i].Length; j++)
            {
                if(scores[i][j] == 0)
                {
                    Debug.Log("who was first out? " + firstoutplayer);
                    if (firstoutplayer == j) scorecardTexts[i].roundRow[j].text = "X";
                    else scorecardTexts[i].roundRow[j].text = "-";
                }
                else scorecardTexts[i].roundRow[j].text = scores[i][j].ToString();
            }
        }

        CalculateTotals(scores);
        gameObject.GetComponent<UnityEngine.UI.Image>().enabled = false;
        StartCoroutine(DelayReadyButton());
    }

    public void CalculateTotals(int[][] scores)
    {
        foreach (TextMeshProUGUI t in totalScores) t.color = defaultColor;

        Debug.Log("calculating totals...");

        int[] totals = new int[scores[0].Length];
        for (int i = 0; i < totals.Length; i++) totals[i] = 0;

        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i] == null) break;

            for (int j = 0; j < scores[0].Length; j++)
            {
                totals[j] += scores[i][j];
            }
        }

        List<int> winning = new List<int>();
        for (int i = 0; i < totals.Length; i++)
        {
            totalScores[i].text = totals[i].ToString();

            if (winning.Count == 0) winning.Add(i);
            else if (totals[winning[0]] == totals[i]) winning.Add(i);
            else if(totals[winning[0]] > totals[i])
            {
                winning.Clear();
                winning.Add(i);
            }
        }

        foreach (int i in winning)
            totalScores[i].color = WinnerColor;
    }

    public void ReadyCheck()
    {
        if (!ready)
        {
            ready = true;
            nh_network.server.setReady();
            readyText.text = "Waiting for other players...";
            gameObject.GetComponent<UnityEngine.UI.Image>().enabled = true;
        }
    }

    public void reset()
    {
        ready = false;
        readycheck.SetActive(false);
        scoreCard.SetActive(false);
        gameObject.SetActive(false);

        if (!showScoreBtn.activeInHierarchy) showScoreBtn.SetActive(true);
    }

    IEnumerator DelayReadyButton()
    {
        yield return new WaitForSeconds(5F);

        readycheck.SetActive(true);
        readyText.text = "Tap anywhere to start next round!";
    }

    public void OpenScoreCard()
    {
        scoreCard.SetActive(true);
        closeScoreBtn.SetActive(true);
    }

    public void CloseScoreCard()
    {
        closeScoreBtn.SetActive(false);
        scoreCard.SetActive(false);
    }
}

[System.Serializable]
public struct ScorecardTexts
{
    public TextMeshProUGUI[] roundRow;
}