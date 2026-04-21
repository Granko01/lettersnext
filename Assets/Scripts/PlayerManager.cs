using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public GameObject usernamePanel;
    public InputField usernameInput;
    public Text Playername;
    public Image crownIcon;

    private bool IsVIP => PlayerPrefs.GetInt("IsVIP", 0) == 1;

    private string FormatName(string name) => name;

    private void Start()
    {
    }

    public void CheckUsername()
    {
        if (PlayerPrefs.HasKey("Username"))
        {
            string savedName = PlayerPrefs.GetString("Username");
            Debug.Log("Loaded username: " + savedName);
            usernamePanel.SetActive(false);
            Playername.text = "Welcome: " + FormatName(savedName);
            if (crownIcon != null) crownIcon.gameObject.SetActive(IsVIP);
        }
        else
        {
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
            {
                if (!string.IsNullOrEmpty(result.AccountInfo.TitleInfo.DisplayName))
                {
                    string pfName = result.AccountInfo.TitleInfo.DisplayName;
                    PlayerPrefs.SetString("Username", pfName);
                    PlayerPrefs.Save();
                    usernamePanel.SetActive(false);
                    Debug.Log("Loaded PlayFab username: " + pfName);
                    Playername.text = "Welcome: " + FormatName(pfName);
                }
                else
                {
                    usernamePanel.SetActive(true);
                }
            }, error =>
            {
                Debug.LogError("Failed to get account info: " + error.GenerateErrorReport());
                usernamePanel.SetActive(true);
            });
        }
    }

    public void RefreshNameWithCrown()
    {
        if (PlayerPrefs.HasKey("Username"))
            Playername.text = "Welcome: " + FormatName(PlayerPrefs.GetString("Username"));
        if (crownIcon != null) crownIcon.gameObject.SetActive(IsVIP);
    }

    public void SubmitUsername()
    {
        string username = usernameInput.text;

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Username cannot be empty!");
            return;
        }

        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = username
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request, result =>
        {
            Debug.Log("✅ Username set successfully: " + result.DisplayName);
            PlayerPrefs.SetString("Username", result.DisplayName);
            PlayerPrefs.Save();
            usernamePanel.SetActive(false);
            Playername.text = "Welcome: " + FormatName(result.DisplayName);
        }, error =>
        {
            Debug.LogError("❌ Failed to set username: " + error.GenerateErrorReport());
        });
    }
}
