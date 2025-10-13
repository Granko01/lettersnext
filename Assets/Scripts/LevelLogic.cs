using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelLogic : MonoBehaviour
{
    [Header("Timer Settings")]
    public float maxBonusTime = 100f;   // maximum bonus possible
    public int targetWordCount = 10;    // words needed to complete level

    [Header("UI")]
    public Text timerText;              // optional UI text
    public Text bonusText;           
    public Text wordsCountText;
    private float startTime;
    private bool timerRunning = false;
    private bool levelFinished = false;
    public int wordsFound = 0;

    void Start()
    {
        StartTimer();
    }

    void Update()
    {
        if (!timerRunning) return;

        float elapsed = Time.time - startTime;

        // update UI timer
        if (timerText != null)
            timerText.text = $"{elapsed:F1}s";

        // stop timer when finished
        if (levelFinished)
            timerRunning = false;
    }

    public void StartTimer()
    {
        startTime = Time.time;
        timerRunning = true;
        levelFinished = false;
        wordsFound = 0;

        if (bonusText != null)
            bonusText.text = "";
    }

    public void OnWordFound()
    {
        if (!timerRunning) return;

        wordsFound++;

        if (wordsFound >= targetWordCount)
        {
            FinishLevel();
        }
    }

    void FinishLevel()
    {
        levelFinished = true;
        float elapsed = Time.time - startTime;
        float remaining = Mathf.Max(0, maxBonusTime - elapsed);
        int bonus = Mathf.RoundToInt(remaining);

        if (bonusText != null)
            bonusText.text = $"Bonus: +{bonus}";

        Debug.Log($"🏁 Level finished in {elapsed:F1}s → Bonus: +{bonus}");
    }
}
