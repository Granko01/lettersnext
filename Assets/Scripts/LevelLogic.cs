using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using Unity.VisualScripting;
using System;

public class LevelLogic : MonoBehaviour
{
    [Header("Timer Settings")]
    public float maxBonusTime = 100f;
    public int targetWordCount = 3;

    [Header("UI")]
    public Text timerText;
    public int TotalTime;
    public Text wordsCountText;

    private bool timerRunning = false;
    private bool levelFinished = false;

    public int wordsFound = 0;
    public int BonusInt;
    public float elapsed = 0f;

    public BonusManager bonusManager;
    public MenuManager menuManager;
    public WordConnector wordConnector;

    public int currentLevelId;

    // 🔥 NEW: real world timer
    private DateTime startDateTime;

    void Start()
    {
        bonusManager = FindAnyObjectByType<BonusManager>();
        menuManager = FindAnyObjectByType<MenuManager>();
        wordConnector = FindAnyObjectByType<WordConnector>();

        // 🔥 Restore timer if exists (anti-cheat)
        if (PlayerPrefs.HasKey("LevelStartTime"))
        {
            startDateTime = DateTime.Parse(PlayerPrefs.GetString("LevelStartTime"));
            timerRunning = true;
        }
    }

    void Update()
    {
        if (!timerRunning || levelFinished)
            return;

        // 🔥 Real time calculation
        elapsed = (float)(DateTime.UtcNow - startDateTime).TotalSeconds;

        float remaining = Mathf.Max(0, maxBonusTime - elapsed);
        BonusInt = Mathf.RoundToInt(remaining);

        if (timerText != null)
            timerText.text = Mathf.RoundToInt(elapsed).ToString();
    }

    public void StartTimer(int levelId)
    {
        startDateTime = DateTime.UtcNow;
        PlayerPrefs.SetString("LevelStartTime", startDateTime.ToString());

        elapsed = 0f;
        timerRunning = true;
        levelFinished = false;
        wordsFound = 0;
        currentLevelId = MenuManager.CurrentLevel;
        //currentLevelId = levelId;

        if (wordsCountText != null)
            wordsCountText.text = $"0 / {targetWordCount}";
    }

    public void OnWordFound()
    {
        if (!timerRunning || levelFinished)
            return;

        wordsFound++;

        if (wordsCountText != null)
            wordsCountText.text = $"{wordsFound} / {targetWordCount}";

        if (wordsFound >= targetWordCount)
        {
            FinishLevel();
        }
    }

    public IEnumerator LevelFinished()
    {
        yield return new WaitForSecondsRealtime(1f);
        FinishLevel();
    }

    public int GetTimersBonus;

    public void FinishLevel()
    {
        levelFinished = true;
        timerRunning = false;

        // 🔥 Clear saved time
        PlayerPrefs.DeleteKey("LevelStartTime");

        if (!int.TryParse(timerText.text, out TotalTime))
            TotalTime = Mathf.RoundToInt(elapsed);

        float finalTime = elapsed;

        int timerBonus = Mathf.Max(0, Mathf.RoundToInt(maxBonusTime - finalTime));
        GetTimersBonus = timerBonus;

        SendBonusToPlayFab(BonusInt, currentLevelId);
        SendTimeToPlayFab(currentLevelId, finalTime);
    }

    public void SendBonusToPlayFab(int bonus, int levelId)
    {
        if (bonusManager == null)
        {
            Debug.LogError("❌ BonusManager not found!");
            return;
        }

        int totalBonus = bonus;

        var stats = new List<StatisticUpdate>
        {
            new StatisticUpdate
            {
                StatisticName = $"Bonus_Level_{levelId}",
                Value = bonus
            },
            new StatisticUpdate
            {
                StatisticName = "Bonus",
                Value = totalBonus
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest { Statistics = stats },
            result =>
            {
                Debug.Log($"✅ Bonus sent → Level {levelId}: +{bonus}");
                bonusManager.BonusInt = totalBonus;
            },
            error =>
            {
                Debug.LogError($"❌ Failed to send bonus: {error.GenerateErrorReport()}");
            }
        );
    }

    public void SendTimeToPlayFab(int levelId, float seconds)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = $"Time_Level_{levelId}",
                    Value = -(int)(seconds * 1000)
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result =>
            {
                Debug.Log($"⏱ Time sent → Level {levelId}: {seconds:F2}s");

                Invoke(nameof(LoadLeaderboardDelayed), 1f);
            },
            error =>
            {
                Debug.LogError($"❌ Failed to send time: {error.GenerateErrorReport()}");
            }
        );
    }

    private void LoadLeaderboardDelayed()
    {
        if (bonusManager != null)
        {
            bonusManager.LoadLevelLeaderboard(currentLevelId);
        }
    }
}