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
    public Image crownIcon;
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

    private static readonly Color VIPColor = new Color(0.6f, 0f, 1f);

    private void FetchVIPStatuses(List<PlayerLeaderboardEntry> entries, System.Action<HashSet<string>> onDone)
    {
        var vipIds = new HashSet<string>();
        int pending = entries.Count;

        if (pending == 0) { onDone(vipIds); return; }

        foreach (var entry in entries)
        {
            string playFabId = entry.PlayFabId;
            PlayFabClientAPI.GetUserData(
                new GetUserDataRequest { PlayFabId = playFabId },
                result =>
                {
                    if (result.Data != null && result.Data.ContainsKey("IsVIP") && result.Data["IsVIP"].Value == "1")
                        vipIds.Add(playFabId);
                    if (--pending == 0) onDone(vipIds);
                },
                error =>
                {
                    if (--pending == 0) onDone(vipIds);
                }
            );
        }
    }

    private string FormatLeaderboardName(PlayerLeaderboardEntry entry, HashSet<string> vipIds, string suffix = "")
    {
        return (entry.DisplayName ?? "Player") + suffix;
    }

    private void ApplyCrown(Image crownIcon, bool isVip)
    {
        if (crownIcon != null) crownIcon.gameObject.SetActive(isVip);
    }

    private void UpdateLeaderboardUI(List<PlayerLeaderboardEntry> entries)
    {
        FetchVIPStatuses(entries, vipIds =>
        {
            for (int i = 0; i < leaderboardUI.Length; i++)
            {
                if (i < entries.Count)
                {
                    bool isVip = vipIds.Contains(entries[i].PlayFabId);
                    leaderboardUI[i].rankText.text = (i + 1).ToString();
                    leaderboardUI[i].playerNameText.text = FormatLeaderboardName(entries[i], vipIds);
                    leaderboardUI[i].playerNameText.color = isVip ? VIPColor : Color.white;
                    leaderboardUI[i].timeText.text = entries[i].StatValue + " pts";
                    ApplyCrown(leaderboardUI[i].crownIcon, isVip);
                }
                else
                {
                    leaderboardUI[i].rankText.text = "";
                    leaderboardUI[i].playerNameText.text = "";
                    leaderboardUI[i].timeText.text = "";
                    ApplyCrown(leaderboardUI[i].crownIcon, false);
                }
            }
        });
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
        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest { StatisticName = statName, StartPosition = 0, MaxResultsCount = 1 },
            topResult =>
            {
                string topName = "-";
                string topPlayFabId = null;
                int topScore = 0;

                if (topResult.Leaderboard != null && topResult.Leaderboard.Count > 0)
                {
                    topName = topResult.Leaderboard[0].DisplayName ?? "Player";
                    topScore = topResult.Leaderboard[0].StatValue;
                    topPlayFabId = topResult.Leaderboard[0].PlayFabId;
                }

                PlayFabClientAPI.GetLeaderboardAroundPlayer(
                    new GetLeaderboardAroundPlayerRequest { StatisticName = statName, MaxResultsCount = 1 },
                    aroundResult =>
                    {
                        int rank = 999;
                        int myScore = 0;

                        if (aroundResult.Leaderboard != null && aroundResult.Leaderboard.Count > 0)
                        {
                            rank = aroundResult.Leaderboard[0].Position + 1;
                            myScore = aroundResult.Leaderboard[0].StatValue;
                        }

                        if (topPlayFabId != null)
                        {
                            PlayFabClientAPI.GetUserData(
                                new GetUserDataRequest { PlayFabId = topPlayFabId },
                                dataResult =>
                                {
                                    bool topIsVip = dataResult.Data != null &&
                                                    dataResult.Data.ContainsKey("IsVIP") &&
                                                    dataResult.Data["IsVIP"].Value == "1";
                                    HighlightLevelUI(levelIndex, rank, myScore, topScore, topName, topIsVip);
                                },
                                _ => HighlightLevelUI(levelIndex, rank, myScore, topScore, topName, false)
                            );
                        }
                        else
                        {
                            HighlightLevelUI(levelIndex, rank, myScore, topScore, topName, false);
                        }
                    },
                    error => Debug.LogError($"❌ AroundPlayer error ({statName}): {error.GenerateErrorReport()}")
                );
            },
            error => Debug.LogError($"❌ Top fetch error ({statName}): {error.GenerateErrorReport()}")
        );
    }

    private void HighlightLevelUI(int levelIndex, int rank, int myScore, int topScore, string topName, bool topIsVip)
    {
        int index = levelIndex - 1;

        if (levelCircles == null || index < 0 || index >= levelCircles.Length)
            return;

        LevelStars levelStar = levelCircles[index];
        if (levelStar == null)
        {
            Debug.LogWarning($"⚠️ LevelStars not assigned for level {levelIndex}");
            return;
        }

        if (levelStar.Position != null)
        {
            bool isTop3 = rank >= 1 && rank <= 3;
            levelStar.Position.gameObject.SetActive(isTop3);
            if (isTop3) levelStar.Position.text = "Your postion: " + rank.ToString();
            if (levelStar.backgroundImg != null &&
                levelStar.rankSprites != null &&
                levelStar.rankSprites.Length > 0)
            {
                int spriteIndex = Mathf.Clamp(rank - 1, 0, levelStar.rankSprites.Length - 1);
                levelStar.backgroundImg.sprite = levelStar.rankSprites[spriteIndex];
            }
        }

        if (levelStar.LevelIndex != null)
            levelStar.LevelIndex.text = $"Level : {levelIndex}";

        if (levelStar.YourScore != null)
            levelStar.YourScore.text = myScore > 0 ? $"Your score: {myScore} pts" : "-";

        if (levelStar.TopPlayerScore != null)
            levelStar.TopPlayerScore.text = topScore > 0 ? $"Top score: {topScore} pts" : "-";

        if (levelStar.TopPlayerName != null)
        {
            string displayName = topIsVip ? $"First place: <color=#9900FF>{topName}</color>" : "First place: " + topName;
            levelStar.TopPlayerName.text = displayName;
            levelStar.TopPlayerName.color = Color.black;
        }

        if (levelStar.topPlayerCrown != null)
            levelStar.topPlayerCrown.gameObject.SetActive(topIsVip);
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

    private void UpdateEndGameUI(List<PlayerLeaderboardEntry> entries, int myRank)
    {
        FetchVIPStatuses(entries, vipIds =>
        {
            for (int i = 0; i < endGameLeaderboardUI.Length; i++)
            {
                if (i < entries.Count)
                {
                    var entry = entries[i];
                    bool isMe = entry.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
                    bool isVip = vipIds.Contains(entry.PlayFabId);

                    endGameLeaderboardUI[i].rankText.text = (entry.Position + 1).ToString();
                    endGameLeaderboardUI[i].playerNameText.text = FormatLeaderboardName(entry, vipIds, isMe ? " (YOU)" : "");
                    endGameLeaderboardUI[i].playerNameText.color = isVip ? VIPColor : (isMe ? Color.black : Color.white);
                    endGameLeaderboardUI[i].timeText.text = entry.StatValue + " pts";
                    ApplyCrown(endGameLeaderboardUI[i].crownIcon, isVip);
                }
                else
                {
                    endGameLeaderboardUI[i].rankText.text = "";
                    endGameLeaderboardUI[i].playerNameText.text = "";
                    endGameLeaderboardUI[i].timeText.text = "";
                    ApplyCrown(endGameLeaderboardUI[i].crownIcon, false);
                }
            }

            if (endGameYourPosition != null)
                endGameYourPosition.text = "Your Rank: " + myRank;
        });
    }

    private void UpdatePreGameLeaderboardUI(List<PlayerLeaderboardEntry> entries)
    {
        FetchVIPStatuses(entries, vipIds =>
        {
            for (int i = 0; i < preGameLeaderboardUI.Length; i++)
            {
                if (i < entries.Count)
                {
                    var entry = entries[i];
                    bool isMe = entry.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
                    bool isVip = vipIds.Contains(entry.PlayFabId);

                    preGameLeaderboardUI[i].rankText.text = (entry.Position + 1).ToString();
                    preGameLeaderboardUI[i].playerNameText.text = FormatLeaderboardName(entry, vipIds, isMe ? " (YOU)" : "");
                    preGameLeaderboardUI[i].playerNameText.color = isVip ? VIPColor : (isMe ? Color.black : Color.white);
                    preGameLeaderboardUI[i].timeText.text = entry.StatValue + " pts";
                    ApplyCrown(preGameLeaderboardUI[i].crownIcon, isVip);
                }
                else
                {
                    preGameLeaderboardUI[i].rankText.text = "";
                    preGameLeaderboardUI[i].playerNameText.text = "";
                    preGameLeaderboardUI[i].timeText.text = "";
                    ApplyCrown(preGameLeaderboardUI[i].crownIcon, false);
                }
            }
        });
    }
    #endregion

    #region BADGES


    #endregion

    private void OnUserDataError(PlayFabError error)
    {
        Debug.LogError($"❌ Failed to load bonus: {error.GenerateErrorReport()}");
    }
}