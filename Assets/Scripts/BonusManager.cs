using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

[System.Serializable]
public class LeaderboardEntryUI
{
    public Text rankText;
    public Text playerNameText;
    public Text timeText;
}

public class BonusManager : MonoBehaviour
{
    [Header("Bonus")]
    public int BonusInt;
    public Text[] bonusText;
    public LevelStars[] levelCircles;

    [Header("Managers")]
    public MenuManager menuManager;
    public PlayFabLogin playFabLogin;

    [Header("Leaderboard UI")]
    public LeaderboardEntryUI[] leaderboardUI;
    public Dropdown levelDropdown;

    [Header("End Game Leaderboard UI")]
    public LeaderboardEntryUI[] endGameLeaderboardUI;
    public LeaderboardEntryUI[] preGameLeaderboardUI;
    public Text endGameYourPosition;

    [Header("Badges")]
    public Text Yourposition;

    private Dictionary<int, int> levelBonuses = new Dictionary<int, int>();

    void Start()
    {
        menuManager = FindAnyObjectByType<MenuManager>();
        playFabLogin = FindAnyObjectByType<PlayFabLogin>();

        SetupLevelDropdown();
        levelDropdown.onValueChanged.AddListener(OnLevelDropdownChanged);
    }

    #region DROPDOWN

    private void SetupLevelDropdown()
    {
        levelDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 1; i <= levelCircles.Length; i++)
        {
            options.Add($"Level {i}");
        }

        levelDropdown.AddOptions(options);
        levelDropdown.value = 0;
        levelDropdown.RefreshShownValue();
    }

    private void OnLevelDropdownChanged(int dropdownIndex)
    {
        ClearLeaderboardUI();
        LoadLevelLeaderboard(dropdownIndex + 1);
    }

    #endregion

    #region BONUS

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
        return levelBonuses.TryGetValue(level, out int bonus) ? bonus : 0;
    }

    private void OnStatisticsReceived(GetPlayerStatisticsResult result)
    {
        levelBonuses.Clear();

        if (result.Statistics == null)
            return;

        foreach (var stat in result.Statistics)
        {
            if (!stat.StatisticName.StartsWith("Bonus_Level_"))
                continue;

            string[] parts = stat.StatisticName.Split('_');
            if (parts.Length < 3) continue;

            if (int.TryParse(parts[2], out int levelNumber))
            {
                levelBonuses[levelNumber] = stat.Value;

                if (levelNumber - 1 < bonusText.Length)
                    bonusText[levelNumber - 1].text = $"Bonus: +{stat.Value}";

                FetchLeaderboardRank(stat.StatisticName, levelNumber);
            }
        }
    }

    #endregion

    #region LEADERBOARD

    public void LoadLevelLeaderboard(int levelNumber)
    {
        string statName = $"Bonus_Level_{levelNumber}";
        List<PlayerLeaderboardEntry> finalEntries = new List<PlayerLeaderboardEntry>();

        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest
            {
                StatisticName = statName,
                StartPosition = 0,
                MaxResultsCount = 3
            },
            topResult =>
            {
                if (topResult.Leaderboard != null)
                    finalEntries.AddRange(topResult.Leaderboard);

                PlayFabClientAPI.GetLeaderboardAroundPlayer(
                    new GetLeaderboardAroundPlayerRequest
                    {
                        StatisticName = statName,
                        MaxResultsCount = 3
                    },
                    aroundResult =>
                    {
                        if (aroundResult.Leaderboard != null)
                        {
                            foreach (var entry in aroundResult.Leaderboard)
                            {
                                if (!finalEntries.Exists(e => e.PlayFabId == entry.PlayFabId))
                                    finalEntries.Add(entry);
                            }
                        }

                        finalEntries.Sort((a, b) => a.Position.CompareTo(b.Position));
                        UpdateLeaderboardUI(finalEntries);
                    },
                    error =>
                    {
                        Debug.LogError("❌ AroundPlayer error: " + error.GenerateErrorReport());
                    }
                );
            },
            error =>
            {
                Debug.LogError("❌ Top leaderboard error: " + error.GenerateErrorReport());
            }
        );
    }

    private void UpdateLeaderboardUI(List<PlayerLeaderboardEntry> entries)
    {
        for (int i = 0; i < leaderboardUI.Length; i++)
        {
            if (i < entries.Count)
            {
                leaderboardUI[i].rankText.text = (i + 1).ToString();
                leaderboardUI[i].playerNameText.text = entries[i].DisplayName ?? "Player";
                leaderboardUI[i].timeText.text = entries[i].StatValue + " pts";
            }
            else
            {
                leaderboardUI[i].rankText.text = "";
                leaderboardUI[i].playerNameText.text = "";
                leaderboardUI[i].timeText.text = "";
            }
        }
    }

    private void ClearLeaderboardUI()
    {
        foreach (var ui in leaderboardUI)
        {
            ui.rankText.text = "";
            ui.playerNameText.text = "";
            ui.timeText.text = "";
        }
    }

    private void FetchLeaderboardRank(string statName, int levelIndex)
    {
        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = statName,
                MaxResultsCount = 1
            },
            result =>
            {
                int rank = 999;
                if (result.Leaderboard != null && result.Leaderboard.Count > 0)
                    rank = result.Leaderboard[0].Position + 1;

                HighlightLevelUI(levelIndex, rank);
            },
            error =>
            {
                Debug.LogError($"❌ Rank fetch failed ({statName}): {error.GenerateErrorReport()}");
            }
        );
    }

    #endregion

    #region END_GAME_LEADERBOARD

    public void ShowEndGameResults(int levelNumber)
    {
        string statName = $"Bonus_Level_{levelNumber}";

        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest
            {
                StatisticName = statName,
                StartPosition = 0,
                MaxResultsCount = 3
            },
            topResult =>
            {
                PlayFabClientAPI.GetLeaderboardAroundPlayer(
                    new GetLeaderboardAroundPlayerRequest
                    {
                        StatisticName = statName,
                        MaxResultsCount = 3
                    },
                    aroundResult =>
                    {
                        BuildEndGameLeaderboard(
                            topResult.Leaderboard,
                            aroundResult.Leaderboard
                        );
                    },
                    error =>
                    {
                        Debug.LogError("❌ EndGame Around error: " + error.GenerateErrorReport());
                    }
                );
            },
            error =>
            {
                Debug.LogError("❌ EndGame Top error: " + error.GenerateErrorReport());
            }
        );
        
    }
   public void ShowPreGameLeaderboard(int levelNumber)
{
    ClearPreGameLeaderboardUI();

    string statName = $"Bonus_Level_{levelNumber}";
    List<PlayerLeaderboardEntry> finalEntries = new List<PlayerLeaderboardEntry>();

    PlayFabClientAPI.GetLeaderboard(
        new GetLeaderboardRequest
        {
            StatisticName = statName,
            StartPosition = 0,
            MaxResultsCount = 3
        },
        topResult =>
        {
            if (topResult.Leaderboard != null)
                finalEntries.AddRange(topResult.Leaderboard);

            PlayFabClientAPI.GetLeaderboardAroundPlayer(
                new GetLeaderboardAroundPlayerRequest
                {
                    StatisticName = statName,
                    MaxResultsCount = 3
                },
                aroundResult =>
                {
                    if (aroundResult.Leaderboard != null)
                    {
                        foreach (var entry in aroundResult.Leaderboard)
                        {
                            if (!finalEntries.Exists(e => e.PlayFabId == entry.PlayFabId))
                                finalEntries.Add(entry);
                        }
                    }

                    finalEntries.Sort((a, b) => a.Position.CompareTo(b.Position));

                    // ✅ USE PREGAME UI HERE
                    UpdatePreGameLeaderboardUI(finalEntries);
                },
                error =>
                {
                    Debug.LogError("❌ PreGame Around error: " + error.GenerateErrorReport());
                }
            );
        },
        error =>
        {
            Debug.LogError("❌ PreGame Top error: " + error.GenerateErrorReport());
        }
    );
}

    private void BuildEndGameLeaderboard(
        List<PlayerLeaderboardEntry> topThree,
        List<PlayerLeaderboardEntry> aroundPlayer)
    {
        List<PlayerLeaderboardEntry> finalList = new List<PlayerLeaderboardEntry>();

        if (topThree != null)
            finalList.AddRange(topThree);

        if (aroundPlayer == null || aroundPlayer.Count == 0)
            return;

        int myRank = -1;

foreach (var entry in aroundPlayer)
{
    if (entry.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId)
    {
        myRank = entry.Position + 1;
        break;
    }
}

        if (myRank > 3)
        {
            foreach (var entry in aroundPlayer)
            {
                if (!finalList.Exists(e => e.PlayFabId == entry.PlayFabId))
                    finalList.Add(entry);
            }
        }

        finalList.Sort((a, b) => a.Position.CompareTo(b.Position));
    
        UpdateEndGameUI(finalList, myRank);
    }
    private void ClearPreGameLeaderboardUI()
{
    foreach (var ui in preGameLeaderboardUI)
    {
        ui.rankText.text = "";
        ui.playerNameText.text = "";
        ui.timeText.text = "";
    }
}

    private void UpdateEndGameUI(
        List<PlayerLeaderboardEntry> entries,
        int myRank)
    {
        for (int i = 0; i < endGameLeaderboardUI.Length; i++)
        {
            if (i < entries.Count)
            {
                var entry = entries[i];

                endGameLeaderboardUI[i].rankText.text = (entry.Position + 1).ToString();

                if (entry.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId)
                {
                    endGameLeaderboardUI[i].playerNameText.text = (entry.DisplayName ?? "Player") + " (YOU)";
                    endGameLeaderboardUI[i].playerNameText.color = Color.black;
                }
                else
                {
                    endGameLeaderboardUI[i].playerNameText.text = entry.DisplayName ?? "Player";
                    endGameLeaderboardUI[i].playerNameText.color = Color.white;
                }

                endGameLeaderboardUI[i].timeText.text = entry.StatValue + " pts";
            }
            else
            {
                endGameLeaderboardUI[i].rankText.text = "";
                endGameLeaderboardUI[i].playerNameText.text = "";
                endGameLeaderboardUI[i].timeText.text = "";
            }
        }
        

        if (endGameYourPosition != null)
            endGameYourPosition.text = "Your Rank: " + myRank;
    }

    private void UpdatePreGameLeaderboardUI(List<PlayerLeaderboardEntry> entries)
{
    for (int i = 0; i < preGameLeaderboardUI.Length; i++)
    {
        if (i < entries.Count)
        {
            var entry = entries[i];

            preGameLeaderboardUI[i].rankText.text = (entry.Position + 1).ToString();

            if (entry.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId)
            {
                preGameLeaderboardUI[i].playerNameText.text =
                    (entry.DisplayName ?? "Player") + " (YOU)";
                preGameLeaderboardUI[i].playerNameText.color = Color.black;
            }
            else
            {
                preGameLeaderboardUI[i].playerNameText.text =
                    entry.DisplayName ?? "Player";
                preGameLeaderboardUI[i].playerNameText.color = Color.white;
            }

            preGameLeaderboardUI[i].timeText.text = entry.StatValue + " pts";
        }
        else
        {
            preGameLeaderboardUI[i].rankText.text = "";
            preGameLeaderboardUI[i].playerNameText.text = "";
            preGameLeaderboardUI[i].timeText.text = "";
        }
    }
}
    #endregion

    #region BADGES

    private void HighlightLevelUI(int levelIndex, int rank)
    {
        int index = levelIndex - 1;

        if (levelCircles == null || index < 0 || index >= levelCircles.Length)
            return;

        LevelStars levelStar = levelCircles[index];
        if (levelStar == null || levelStar.badgeImage == null)
        {
            Debug.LogWarning($"⚠️ Text not assigned for level {levelIndex}");
            return;
        }

        levelStar.badgeImage.gameObject.SetActive(true);

        switch (rank)
        {
            case 1:
                levelStar.badgeImage.text = "1";
                break;
            case 2:
                levelStar.badgeImage.text = "2";
                break;
            case 3:
                levelStar.badgeImage.text = "3";
                break;
            default:
                levelStar.badgeImage.gameObject.SetActive(false);
                break;
        }
    }

    #endregion

    private void OnUserDataError(PlayFabError error)
    {
        Debug.LogError($"❌ Failed to load bonus: {error.GenerateErrorReport()}");
    }
}