using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

[System.Serializable]
public class LevelStars
{
    public Button[] stars; // 3 stars per level
}

public class BonusManager : MonoBehaviour
{
    public int BonusInt;
    public Text[] bonusText;
    public LevelStars[] levelCircles;
    public MenuManager menuManager;
    public PlayFabLogin playFabLogin;

    private Dictionary<int, int> levelBonuses = new Dictionary<int, int>();

    void Start()
    {
        menuManager = FindAnyObjectByType<MenuManager>();
        playFabLogin = FindAnyObjectByType<PlayFabLogin>();
    }

    public void LoadBonusFromPlayFab()
    {
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            OnStatisticsReceived,
            OnUserDataError
        );
    }

    public int GetBonusForLevel(int level)
    {
        if (levelBonuses == null) return 0;
        if (levelBonuses.TryGetValue(level, out int previousBonus))
            return previousBonus;
        return 0;
    }

    private void OnStatisticsReceived(GetPlayerStatisticsResult result)
    {
        levelBonuses.Clear();

        if (result.Statistics == null || result.Statistics.Count == 0)
        {
            BonusInt = 0;
            return;
        }

        foreach (var stat in result.Statistics)
        {
            if (stat.StatisticName.StartsWith("Bonus_Level_"))
            {
                string[] parts = stat.StatisticName.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int levelNumber))
                {
                    levelBonuses[levelNumber] = stat.Value;

                    if (levelNumber - 1 < bonusText.Length)
                        bonusText[levelNumber - 1].text = $"Bonus: +{stat.Value}";

                    string statName = $"Bonus_Level_{levelNumber}";

                    FetchLeaderboardRank(statName, levelNumber);
                }
            }
        }
    }

    private void FetchLeaderboardRank(string statName, int levelIndex)
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = statName,
            MaxResultsCount = 1
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, result =>
        {
            if (result.Leaderboard != null && result.Leaderboard.Count > 0)
            {
                int rank = result.Leaderboard[0].Position + 1;

                HighlightLevelUI(levelIndex, rank);
                return;
            }

            HighlightLevelUI(levelIndex, 999);
        },
        error =>
        {
            Debug.LogError($"❌ Failed to fetch leaderboard rank for {statName}: {error.GenerateErrorReport()}");
        });
    }

    private void HighlightLevelUI(int levelIndex, int rank)
    {
        int index = levelIndex - 1;

        if (levelCircles == null || index < 0 || index >= levelCircles.Length)
        {
            Debug.LogWarning($"⚠️ levelIndex {levelIndex} out of range or no levelCircles assigned.");
            return;
        }

        Button[] stars = levelCircles[index].stars;

        if (stars == null || stars.Length == 0)
        {
            Debug.LogWarning($"⚠️ No stars assigned at index {index}");
            return;
        }

        foreach (var star in stars)
        {
            star.interactable = false;
        }

        int starsToEnable = 0;
        if (rank == 1 || rank == 3) starsToEnable = 3;
        else if (rank == 2) starsToEnable = 2;
        else starsToEnable = 0;

        Color starColor = Color.gray;
        switch (rank)
        {
        }

        for (int i = 0; i < starsToEnable && i < stars.Length; i++)
        {
            stars[i].interactable = true;
        }

        Debug.Log($"🎨 Level {levelIndex} → Rank {rank} → {starsToEnable} stars");
    }

    private void OnUserDataError(PlayFabError error)
    {
        Debug.LogError($"❌ Failed to load bonus: {error.GenerateErrorReport()}");
    }
}
