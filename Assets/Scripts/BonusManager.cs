using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class BonusManager : MonoBehaviour
{
    public int BonusInt;
    public Text[] bonusText;
    public Image[] levelCircles;
    public MenuManager menuManager;
    public PlayFabLogin playFabLogin;

    void Start()
    {
        menuManager = FindObjectOfType<MenuManager>();
        playFabLogin = FindObjectOfType<PlayFabLogin>();
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

private Dictionary<int, int> levelBonuses = new Dictionary<int, int>();

   private void OnStatisticsReceived(GetPlayerStatisticsResult result)
{
    levelBonuses.Clear(); // clear previous data

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
                levelBonuses[levelNumber] = stat.Value; // assign to class-level
        }
    }

    var orderedLevels = new List<int>(levelBonuses.Keys);
    orderedLevels.Sort();

    for (int i = 0; i < orderedLevels.Count && i < bonusText.Length; i++)
    {
        int level = orderedLevels[i];
        int bonusValue = levelBonuses[level];
        bonusText[i].text = $"Bonus: +{bonusValue}";

        string statName = $"Bonus_Level_{level}";
        FetchLeaderboardRank(statName, i);
    }
}

    

    private void FetchLeaderboardRank(string statName, int levelIndex)
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = statName,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, result =>
        {
            foreach (var entry in result.Leaderboard)
            {

                if (entry.PlayFabId == playFabLogin.playFabId)
                {
                    int rank = entry.Position + 1;
                    HighlightLevelUI(levelIndex, rank);
                    return;
                }
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
        if (levelCircles == null || levelIndex >= levelCircles.Length)
        {
            Debug.LogWarning($"⚠️ levelIndex {levelIndex} out of range or no levelCircles assigned.");
            return;
        }

        Image highlightCircle = levelCircles[levelIndex];

        if (highlightCircle == null)
        {
            Debug.LogWarning($"⚠️ No Image assigned at index {levelIndex}");
            return;
        }

        switch (rank)
        {
            case 1:
                highlightCircle.color = Color.green;
                break;
            case 2:
                highlightCircle.color = new Color(1f, 0.64f, 0f); // orange
                break;
            case 3:
                highlightCircle.color = Color.yellow; // optional
                break;
            default:
                highlightCircle.color = Color.white;
                break;
        }
        Debug.Log($"🎨 Level {levelIndex + 1} → Rank {rank} → Color applied to {highlightCircle.name}");
    }

    private void OnUserDataError(PlayFabError error)
    {
        Debug.LogError($"❌ Failed to load bonus: {error.GenerateErrorReport()}");
    }
}
