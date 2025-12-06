using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System;

public class LevelLogic : MonoBehaviour
{
    [Header("Timer Settings")]
    public float maxBonusTime = 100f;
    public int targetWordCount = 10;

    [Header("UI")]
    public Text timerText;
    public Text bonusText;
    public Text wordsCountText;
    private float startTime;
    private bool timerRunning = false;
    private bool levelFinished = false;
    public int wordsFound = 0;
    public int BonusInt;
    public float elapsed = 0f;

    public BonusManager bonusManager;
    public MenuManager menuManager;

    void Start()
    {
        bonusManager = FindObjectOfType<BonusManager>();
        menuManager = FindObjectOfType<MenuManager>();
    }


    void Update()
    {
        if (!timerRunning) return;

        elapsed += Time.deltaTime;

        float remaining = Mathf.Max(0, maxBonusTime - elapsed);
        BonusInt = Mathf.RoundToInt(remaining);

        if (timerText != null)
            timerText.text = $"{Mathf.FloorToInt(elapsed)}s";

        if (bonusText != null)
            bonusText.text = $"Bonus: +{BonusInt}";

        if (levelFinished)
            timerRunning = false;
    }



    public void StartTimer()
    {
        startTime = Time.time;
        timerRunning = true;
        levelFinished = false;
        wordsFound = 0;

        if (bonusText != null)
            bonusText.text = "";
    }

    public void OnWordFound()
    {
        if (!timerRunning) return;

        wordsFound++;

        if (wordsFound >= targetWordCount)
        {
            StartCoroutine(LevelFinished());
        }
    }

    public void FinishLevel()
    {
        levelFinished = true;
        float elapsed = Time.time - startTime;
        float remaining = Mathf.Max(0, maxBonusTime - elapsed);
        BonusInt = Mathf.RoundToInt(remaining);

        if (bonusText != null)
            bonusText.text = $"Bonus: +{BonusInt}";

        Debug.Log($"🏁 Level finished in {elapsed:F1}s → Bonus: +{BonusInt}");

    }
    public IEnumerator LevelFinished()
    {
        yield return new WaitForSecondsRealtime(3f);
        FinishLevel();
    }

    public void SendBonusToPlayFab(int bonus, int levelId)
    {
        if (bonusManager == null)
        {
            Debug.LogError("❌ BonusManager not found!");
            return;
        }

        int totalBonus = bonusManager.BonusInt + bonus;

        var stats = new List<StatisticUpdate>
    {
        new StatisticUpdate { StatisticName = $"Bonus_Level_{levelId}", Value = bonus },
        new StatisticUpdate { StatisticName = "Bonus", Value = totalBonus }
    };

        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest { Statistics = stats },
            result =>
            {
                Debug.Log($"✅ Sent Bonus for Level {levelId}: +{bonus}, Total: {totalBonus}");
                bonusManager.BonusInt = totalBonus;
            },
            error => Debug.LogError($"❌ Failed to update bonus: {error.GenerateErrorReport()}")
        );
    }

}
