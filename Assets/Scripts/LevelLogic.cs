using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class LevelLogic : MonoBehaviour
{
    [Header("Timer Settings")]
    public float maxBonusTime = 100f;
    public int targetWordCount = 10;

    [Header("UI")]
    public Text timerText;
    public Text bonusText;
    public Text wordsCountText;

    private bool timerRunning = false;
    private bool levelFinished = false;

    public int wordsFound = 0;
    public int BonusInt;
    public float elapsed = 0f;

    public BonusManager bonusManager;
    public MenuManager menuManager;

    private int currentLevelId;

    void Start()
    {
        bonusManager = FindAnyObjectByType<BonusManager>();
        menuManager = FindAnyObjectByType<MenuManager>();
    }

    void Update()
    {
        if (!timerRunning || levelFinished)
            return;

        elapsed += Time.deltaTime;

        float remaining = Mathf.Max(0, maxBonusTime - elapsed);
        BonusInt = Mathf.RoundToInt(remaining);

        if (timerText != null)
            timerText.text = $"{Mathf.FloorToInt(elapsed)}s";

        if (bonusText != null)
            bonusText.text = $"Bonus: +{BonusInt}";
    }

    public void StartTimer(int levelId)
    {
        elapsed = 0f;
        timerRunning = true;
        levelFinished = false;
        wordsFound = 0;
        currentLevelId = levelId;

        if (bonusText != null)
            bonusText.text = "";

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
            StartCoroutine(LevelFinished());
        }
    }

    public IEnumerator LevelFinished()
    {
        yield return new WaitForSecondsRealtime(1f);
        FinishLevel();
    }

    public void FinishLevel()
    {
        levelFinished = true;
        timerRunning = false;

        float finalTime = elapsed;

        float remaining = Mathf.Max(0, maxBonusTime - finalTime);
        BonusInt = Mathf.RoundToInt(remaining);

        if (bonusText != null)
            bonusText.text = $"Bonus: +{BonusInt}";

        Debug.Log($"🏁 Level {currentLevelId} finished in {finalTime:F2}s → Bonus: +{BonusInt}");

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

        int totalBonus = bonusManager.BonusInt + bonus;

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
