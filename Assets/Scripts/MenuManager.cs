using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject[] Panels;

    [Header("Energy Settings")]
    public int Energies;
    public int MaxEnergy = 5;
    public int Coins;
    public Text[] CoinsText;
    public Text[] EnergyText;
    public float EnergyRegenHours = 1f; 
    public Text TimerText; 
    public GameObject[] BuyButton;
    [Header("Level settings")]
    public int Levelindex = 1;
    public Button[] LevelButtons;
    private const string EnergyKey = "Energy";
    private const string LastEnergyTimeKey = "LastEnergyTime";
    private const string CoinsKey = "CoinsKey";
    public const string LevelKey = "LevelKey";

    private DateTime lastEnergyTime;
    private float secondsPerEnergy;
    public WordConnector wordConnector;

    void Start()
    {
        wordConnector = FindObjectOfType<WordConnector>();
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

                // If still not full, reset timer
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

        LevelButtons[i].interactable = (level <= Levelindex);
        LevelButtons[i].onClick.RemoveAllListeners();

        if (level < Levelindex)
        {
            LevelButtons[i].onClick.AddListener(() =>
            {
                if (Energies > 0)
                {
                    UseEnergy();
                    wordConnector.StartLevel(level);
                }
                else
                {
                    Debug.Log("Not enough energy to replay this level!");
                }
            });
        }
        else if (level == Levelindex)
        {
            LevelButtons[i].onClick.AddListener(() =>
            {
                wordConnector.StartLevel(level);
            });
        }
        else
        {
            LevelButtons[i].interactable = false;
        }
    }
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
        {
            t.text = Energies.ToString();
        }
        foreach (var c in CoinsText)
        {
            c.text = Coins.ToString();
        }
    }

    private void UpdateTimerUI()
    {
        TimeSpan timePassed = DateTime.Now - lastEnergyTime;
        double secondsLeft = secondsPerEnergy - timePassed.TotalSeconds;
        if (secondsLeft < 0) secondsLeft = 0;

        TimeSpan t = TimeSpan.FromSeconds(secondsLeft);
        TimerText.text = "New energy in: " + string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
    }

    public void OpenPanels(string tag)
    {
        for (int i = 0; i < Panels.Length; i++)
            Panels[i].SetActive(false);

        switch (tag)
        {
            case "Play": Panels[0].SetActive(true); break;
            case "Leaderboard": Panels[1].SetActive(true); break;
            case "About": Panels[2].SetActive(true); break;
            case "Howto": Panels[3].SetActive(true); break;
            case "Close": Panels[4].SetActive(true); break;
            case "Shop": Panels[5].SetActive(true); break;
        }
    }
}
