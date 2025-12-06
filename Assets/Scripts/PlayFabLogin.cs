using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabLogin : MonoBehaviour
{
    BonusManager bonusManager;
    PlayerManager playerManager;
    public string playFabId;
    void Awake()
    {
        
    }
    void Start()
    {
        LoginToPlayFab();
        playerManager = FindObjectOfType<PlayerManager>();
        bonusManager = FindObjectOfType<BonusManager>();
    }

    void LoginToPlayFab()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
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
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("❌ PlayFab login failed: " + error.GenerateErrorReport());
    }
}
