using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;

public class PlayFabLogin : MonoBehaviour
{
    BonusManager bonusManager;
    PlayerManager playerManager;
    public string playFabId;

    private string customId;

    void Start()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        bonusManager = FindAnyObjectByType<BonusManager>();

        LoginToPlayFab();
    }

    void LoginToPlayFab()
    {
        // 🔥 Get saved ID or use device ID
        if (PlayerPrefs.HasKey("CustomID"))
        {
            customId = PlayerPrefs.GetString("CustomID");
        }
        else
        {
            customId = SystemInfo.deviceUniqueIdentifier;
            PlayerPrefs.SetString("CustomID", customId);
        }

        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("✅ Logged in to PlayFab successfully!");
        playFabId = result.PlayFabId;

        bonusManager.LoadBonusFromPlayFab();
        playerManager.CheckUsername();
        bonusManager.LoadLevelLeaderboard(1);
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("❌ Login failed: " + error.GenerateErrorReport());

        // 🔥 If account is deleted → generate NEW ID
        if (error.ErrorMessage.Contains("being deleted"))
        {
            Debug.Log("⚠️ Account was deleted, generating new ID...");

            customId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("CustomID", customId);

            // Retry login with new ID
            var request = new LoginWithCustomIDRequest
            {
                CustomId = customId,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
        }
    }
}