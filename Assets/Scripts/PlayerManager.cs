using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public GameObject usernamePanel;
    public InputField usernameInput;
    public Text Playername;

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
            Playername.text = "Welcome: " + savedName.ToString();
        }
        else
        {
            // Check PlayFab
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
            {
                if (!string.IsNullOrEmpty(result.AccountInfo.TitleInfo.DisplayName))
                {
                    string pfName = result.AccountInfo.TitleInfo.DisplayName;
                    PlayerPrefs.SetString("Username", pfName);
                    PlayerPrefs.Save();
                    usernamePanel.SetActive(false);
                    Debug.Log("Loaded PlayFab username: " + pfName);
                    Playername.text = "Welcome: " + pfName.ToString();
                }
                else
                {
                    // Show input panel if no name
                    usernamePanel.SetActive(true);
                }
            }, error =>
            {
                Debug.LogError("Failed to get account info: " + error.GenerateErrorReport());
                usernamePanel.SetActive(true);
            });
            
        }
        
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
        }, error =>
        {
            Debug.LogError("❌ Failed to set username: " + error.GenerateErrorReport());
        });
    }
}
