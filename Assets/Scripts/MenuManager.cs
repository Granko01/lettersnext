using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject[] Panels;

    [Header("VIP")]
    public GameObject vipBadgeText;

    [Header("Energy Settings")]
    public int Energies;
    public int MaxEnergy = 5;
    public int Coins;
    public Text[] CoinsText;
    public Text[] EnergyText;
    public Text AfterLevelEnergyText;
    public float EnergyRegenHours = 1f;
    public Text TimerText;
    public GameObject[] BuyButton;

    [Header("Subscription Panels")]
    public GameObject SubscriptionPanel099;
    public GameObject SubscriptionPanel9999;
    public GameObject VIPBenefitsPanel;
    [Range(0, 100)] public int VIPShowChance = 30;
    [Header("Level settings")]
    public int Levelindex = 1;
    public static int CurrentLevel;
    public Button[] LevelButtons;
    public ScrollRect levelScrollRect;
    public GameObject NoEnergy;
    private const string EnergyKey = "Energy";
    private const string LastEnergyTimeKey = "LastEnergyTime";
    private const string CoinsKey = "CoinsKey";
    public const string LevelKey = "LevelKey";
    private const string VIPKey = "IsVIP";

    public bool IsVIP => PlayerPrefs.GetInt(VIPKey, 0) == 1;

    public void SetVIP(bool value)
    {
        PlayerPrefs.SetInt(VIPKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SaveVIPToPlayFab()
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { VIPKey, "1" } },
            Permission = UserDataPermission.Public
        },
        result => Debug.Log("✅ VIP saved to PlayFab!"),
        error => Debug.LogError("❌ Failed to save VIP: " + error.GenerateErrorReport()));
    }

    public void LoadVIPFromPlayFab(System.Action onDone = null)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result =>
        {
            if (result.Data != null && result.Data.ContainsKey(VIPKey) && result.Data[VIPKey].Value == "1")
            {
                PlayerPrefs.SetInt(VIPKey, 1);
                PlayerPrefs.Save();
                Debug.Log("✅ VIP status loaded from PlayFab.");
            }
            onDone?.Invoke();
        },
        error =>
        {
            Debug.LogError("❌ Failed to load VIP: " + error.GenerateErrorReport());
            onDone?.Invoke();
        });
    }

    private DateTime lastEnergyTime;
    private float secondsPerEnergy;
    public WordConnector wordConnector;
    public BonusManager bonusManager;

    void Start()
    {
        wordConnector = FindAnyObjectByType<WordConnector>();
        bonusManager = FindAnyObjectByType<BonusManager>();
        secondsPerEnergy = EnergyRegenHours * 3600f;
        GetLevelIndex();
        FetchLevels();
        RestoreEnergy();
        GetCoins();
        UpdateUI();
    }
    public void SetActiveButtonBuy(string tag)
    {
        if (tag == "+1energy")
        {
            BuyButton[0].gameObject.SetActive(true);
        }
        if (tag == "+1slot")
        {
            BuyButton[1].gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (IsVIP)
        {
            TimerText.text = "Unlimited ♾";
            return;
        }

        if (Energies < MaxEnergy)
        {
            TimeSpan timePassed = DateTime.Now - lastEnergyTime;
            double secondsPassed = timePassed.TotalSeconds;

            if (secondsPassed >= secondsPerEnergy)
            {
                int energyToAdd = Mathf.FloorToInt((float)(secondsPassed / secondsPerEnergy));
                Energies = Mathf.Min(Energies + energyToAdd, MaxEnergy);
                UpdateUI();
                SetEnergy();

                if (Energies < MaxEnergy)
                {
                    double leftoverSeconds = secondsPassed % secondsPerEnergy;
                    lastEnergyTime = DateTime.Now.AddSeconds(-leftoverSeconds);
                    PlayerPrefs.SetString(LastEnergyTimeKey, lastEnergyTime.ToBinary().ToString());
                }
            }

            UpdateTimerUI();
        }
        else
        {
            TimerText.text = "Full Energy";
        }
    }

    public void FetchLevels()
    {
        for (int i = 0; i < LevelButtons.Length; i++)
        {
            int level = i + 1;
            GameObject stars = null;

            foreach (Transform child in LevelButtons[i].transform)
            {
                if (child.CompareTag("stars"))
                {
                    stars = child.gameObject;
                    break;
                }
            }

            LevelButtons[i].interactable = (level <= Levelindex);
            LevelButtons[i].onClick.RemoveAllListeners();

            if (level < Levelindex)
            {
                LevelButtons[i].onClick.AddListener(() =>
                {
                    if (IsVIP || Energies > 0)
                    {
                        wordConnector.PreGamePanel.gameObject.SetActive(true);
                        CurrentLevel = level;
                        wordConnector.StartLevel(level);
                        bonusManager.ShowPreGameLeaderboard(level);
                    }
                    else
                    {
                        if (!isShowingNoEnergy)
                            StartCoroutine(NoEnergies());

                        Debug.Log("Not enough energy to replay this level!");
                    }
                });
            }
            else if (level == Levelindex)
            {
                LevelButtons[i].onClick.AddListener(() =>
                {
                    wordConnector.PreGamePanel.gameObject.SetActive(true);
                    CurrentLevel = level;
                    wordConnector.StartLevel(level);
                    bonusManager.ShowPreGameLeaderboard(level);

                });
                if (stars != null)
                {
                    stars.SetActive(true);

                }
            }
            else
            {
                LevelButtons[i].interactable = false;
                if (stars != null)
                {
                    stars.SetActive(false);
                }
            }
             //StartCoroutine(ScrollToCurrentLevelCoroutine());
        }
    }

    private bool isShowingNoEnergy = false;

    private IEnumerator ScrollToCurrentLevelCoroutine()
    {
        if (levelScrollRect == null)
        {
            Debug.LogError("[Scroll] levelScrollRect is not assigned!");
            yield break;
        }

        yield return new WaitForEndOfFrame();

        LayoutRebuilder.ForceRebuildLayoutImmediate(levelScrollRect.content);
        Canvas.ForceUpdateCanvases();

        int index = Mathf.Clamp(Levelindex - 1, 0, LevelButtons.Length - 1);
        RectTransform target = LevelButtons[index].GetComponent<RectTransform>();
        RectTransform content = levelScrollRect.content;
        RectTransform viewport = levelScrollRect.viewport;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        if (contentHeight <= viewportHeight) yield break;

        // Use world-space position so we don't depend on anchoredPosition being set by layout
        Vector3 targetWorldPos = target.TransformPoint(target.rect.center);
        Vector2 targetInViewport = viewport.InverseTransformPoint(targetWorldPos);
        float viewportCenterY = viewport.rect.center.y;

        float offset = targetInViewport.y - viewportCenterY;
        float newY = Mathf.Clamp(content.anchoredPosition.y - offset, 0f, contentHeight - viewportHeight);

        Vector2 startPos = content.anchoredPosition;
        Vector2 endPos = new Vector2(content.anchoredPosition.x, newY);
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            content.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        content.anchoredPosition = endPos;
    }

    public IEnumerator NoEnergies()
    {
        if (isShowingNoEnergy)
            yield break;

        isShowingNoEnergy = true;

        NoEnergy.gameObject.SetActive(true);

        Vector3 startPos = NoEnergy.transform.position;
        Vector3 endPos = startPos + new Vector3(0, 50f, 0);

        var graphic = NoEnergy.GetComponent<UnityEngine.UI.Graphic>();
        Color c = graphic.color;
        c.a = 1f;
        graphic.color = c;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            NoEnergy.transform.position = Vector3.Lerp(startPos, endPos, t);

            c.a = Mathf.Lerp(1f, 0f, t);
            graphic.color = c;

            yield return null;
        }

        // reset
        NoEnergy.transform.position = startPos;
        c.a = 1f;
        graphic.color = c;

        NoEnergy.gameObject.SetActive(false);

        isShowingNoEnergy = false;
    }
    public void GetLevelIndex()
    {
        Levelindex = PlayerPrefs.GetInt(LevelKey, Levelindex);
    }
    public void SetLevelIndex()
    {
        PlayerPrefs.SetInt(LevelKey, Levelindex);
        PlayerPrefs.Save();
    }

    public void UseEnergy()
    {
        if (IsVIP) return;

        if (Energies > 0)
        {
            Energies--;
            SetEnergy();
            UpdateUI();

            if (Energies < MaxEnergy)
            {
                lastEnergyTime = DateTime.Now;
                PlayerPrefs.SetString(LastEnergyTimeKey, lastEnergyTime.ToBinary().ToString());
            }
        }
    }

    private void RestoreEnergy()
    {
        secondsPerEnergy = EnergyRegenHours * 3600f;
        Energies = PlayerPrefs.GetInt(EnergyKey, MaxEnergy);
        string lastTimeString = PlayerPrefs.GetString(LastEnergyTimeKey, string.Empty);

        if (!string.IsNullOrEmpty(lastTimeString))
        {
            long binaryTime = Convert.ToInt64(lastTimeString);
            lastEnergyTime = DateTime.FromBinary(binaryTime);
            TimeSpan timePassed = DateTime.Now - lastEnergyTime;
            int energyToAdd = Mathf.FloorToInt((float)(timePassed.TotalSeconds / secondsPerEnergy));

            if (energyToAdd > 0)
            {
                Energies = Mathf.Min(Energies + energyToAdd, MaxEnergy);
                if (Energies < MaxEnergy)
                {
                    double leftoverSeconds = timePassed.TotalSeconds % secondsPerEnergy;
                    lastEnergyTime = DateTime.Now.AddSeconds(-leftoverSeconds);
                }
            }
        }
        else
        {
            lastEnergyTime = DateTime.Now;
        }

        SetEnergy();
    }

    public void SetEnergy()
    {
        PlayerPrefs.SetInt(EnergyKey, Energies);
        PlayerPrefs.Save();
    }
    public void GetCoins()
    {
        Coins = PlayerPrefs.GetInt(CoinsKey, Coins);
    }
    public void SetCoins()
    {
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
    }

    public void UpdateUI()
    {
        foreach (var t in EnergyText)
            t.text = IsVIP ? "Energy: ∞" : "Energy: " + Energies.ToString();

        foreach (var c in CoinsText)
            c.text = "Coins: " + Coins.ToString();

        if (vipBadgeText != null)
            vipBadgeText.SetActive(IsVIP);
    }

    private void UpdateTimerUI()
    {
        TimeSpan timePassed = DateTime.Now - lastEnergyTime;
        double secondsLeft = secondsPerEnergy - timePassed.TotalSeconds;
        if (secondsLeft < 0) secondsLeft = 0;

        TimeSpan t = TimeSpan.FromSeconds(secondsLeft);
        TimerText.text = "New energy in: " + string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
    }

    public void ShowEnergyAfterLevel()
    {
        if (AfterLevelEnergyText != null)
            AfterLevelEnergyText.text = IsVIP ? "Energy: ∞" : "Energy: " + Energies + " / " + MaxEnergy;
        UpdateUI();
    }

    public void CheckSubscriptionsForLevelsPanel()
    {
        if (IsVIP) return;

        if (SubscriptionPanel099 != null) SubscriptionPanel099.SetActive(false);
        if (SubscriptionPanel9999 != null) SubscriptionPanel9999.SetActive(false);

        if (Energies <= 0)
        {
            if (SubscriptionPanel099 != null) SubscriptionPanel099.SetActive(true);
        }
        else if (UnityEngine.Random.Range(0, 100) < VIPShowChance)
        {
            if (SubscriptionPanel9999 != null) SubscriptionPanel9999.SetActive(true);
        }
    }

    public void ShowVIPBenefitsPanel()
    {
        if (IsVIP || VIPBenefitsPanel == null) return;
        VIPBenefitsPanel.SetActive(true);
    }

    public void CloseVIPBenefitsPanel()
    {
        if (VIPBenefitsPanel != null) VIPBenefitsPanel.SetActive(false);
    }

    public void OpenPanels(string tag)
    {
        for (int i = 0; i < Panels.Length; i++)
            Panels[i].SetActive(false);

        switch (tag)
        {
            case "Play":
            Panels[0].SetActive(true);
            if (Levelindex >= 4)
                StartCoroutine(ScrollToCurrentLevelCoroutine());
            CheckSubscriptionsForLevelsPanel();
             break;
            case "Leaderboard": Panels[1].SetActive(true); break;
            case "About": Panels[2].SetActive(true); break;
            case "Howto": Panels[3].SetActive(true); break;
            case "Close": Panels[4].SetActive(true); break;
            case "Shop": Panels[5].SetActive(true); break;
            case "Settings": Panels[8].SetActive(true); break;
            case "Levels":
                Panels[9].SetActive(true);
                break;
        }
        if (tag == "Home")
        {
            for (int i = 0; i < Panels.Length; i++)
            {
                Panels[i].gameObject.SetActive(false);
            }
            Panels[7].gameObject.SetActive(true);
        }

    }
}
