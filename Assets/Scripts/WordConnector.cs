using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class LevelData
{
    public int levelId;
    public string letters;
}
public class WordConnector : MonoBehaviour
{
    [Header("Letter Setup")]
    public RectTransform[] spawnPoints;
    public GameObject[] letterPrefab;
    public string letters = "";

    [Header("Line Setup")]
    public GameObject linePrefab;
    public RectTransform lineParent;

    [Header("Word Validation")]
    public Text foundWordsText;
    private HashSet<string> validWords = new HashSet<string>();
    private HashSet<string> foundWords = new HashSet<string>();

    [Header("Found Words UI")]
    public Transform foundWordsContent;
    public GameObject foundWordPrefab;

    [Header("Score")]
    public Text scoreText;
    private int totalScore = 0;
    public GameObject NextLevelButton;
    public GameObject CantChoose;

    private List<RectTransform> selectedLetters = new List<RectTransform>();
    private List<GameObject> lineSegments = new List<GameObject>();
    private string currentWord = "";

    public List<LevelData> allLevels = new List<LevelData>();
    public int currentLevelId = 1;

    public GameObject LevelGm;

    public LevelLogic levelLogic;
    public WordAnimator wordAnimator;

    public MenuManager menuManager;
    public BonusManager bonusManager;

    private readonly Dictionary<char, int> letterValues = new Dictionary<char, int>()
    {
        { 'A', 1 }, { 'B', 4 }, { 'C', 4 }, { 'D', 2 }, { 'E', 1 },
        { 'F', 4 }, { 'G', 3 }, { 'H', 3 }, { 'I', 1 }, { 'J', 10 },
        { 'K', 5 }, { 'L', 2 }, { 'M', 4 }, { 'N', 2 }, { 'O', 1 },
        { 'P', 4 }, { 'Q', 10 }, { 'R', 1 }, { 'S', 1 }, { 'T', 1 },
        { 'U', 2 }, { 'V', 5 }, { 'W', 4 }, { 'X', 8 }, { 'Y', 3 },
        { 'Z', 10 }
    };

    void Start()
    {
        menuManager = FindAnyObjectByType<MenuManager>();
        levelLogic = FindAnyObjectByType<LevelLogic>();
        wordAnimator = FindAnyObjectByType<WordAnimator>();
        bonusManager = FindAnyObjectByType<BonusManager>();
        LoadValidWords();
        UpdateScoreUI();
    }
    public void StartLevel(int levelID)
    {
        currentLevelId = levelID;
        CantChoose.gameObject.SetActive(false);
        NextLevelButton.gameObject.SetActive(false);
        LevelGm.gameObject.SetActive(true);
        ClearLevelData();
        LoadLevelLetters();
        SpawnLetters();
        levelLogic.StartTimer(); 
    }
    void LoadLevelLetters()
    {
        LevelData level = allLevels.FirstOrDefault(l => l.levelId == currentLevelId);
        if (level != null)
        {
            letters = level.letters;
            Debug.Log($"🔠 Loaded letters for level {currentLevelId}: {letters}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No level found for ID {currentLevelId}, using default letters.");
        }
    }
    void LoadValidWords()
    {
        TextAsset wordFile = Resources.Load<TextAsset>("ValidWords");
        if (wordFile != null)
        {
            string[] lines = wordFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string w = line.Trim().ToUpper();
                if (!string.IsNullOrEmpty(w))
                    validWords.Add(w);
            }
            Debug.Log($"✅ Loaded {validWords.Count} valid words");
        }
        else
        {
            Debug.LogWarning("⚠️ No ValidWords.txt found in Resources!");
        }
    }

    void ClearLevelData()
    {
        levelLogic.StartTimer();
        levelLogic.elapsed = 0;
        foreach (RectTransform spawn in spawnPoints)
        {
            for (int i = spawn.childCount - 1; i >= 0; i--)
            {
                Destroy(spawn.GetChild(i).gameObject);
            }
        }
        foundWords.Clear();

        if (foundWordsContent != null)
        {
            for (int i = foundWordsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(foundWordsContent.GetChild(i).gameObject);
            }
        }
        currentWord = "";
        selectedLetters.Clear();

        if (foundWordsText != null)
            foundWordsText.text = "";

        if (levelLogic != null && levelLogic.wordsCountText != null)
        {
            levelLogic.wordsFound = 0;
            levelLogic.wordsCountText.text = "0";
        }
    }


    void SpawnLetters()
    {
        string[] splitLetters = letters.Split(' ');
        int count = Mathf.Min(splitLetters.Length, spawnPoints.Length, letterPrefab.Length);

        for (int i = 0; i < count; i++)
        {
            RectTransform spawn = spawnPoints[i];
            string letterStr = splitLetters[i].Trim().ToUpper();

            GameObject prefabToSpawn = letterPrefab[i];
            GameObject letterObj = Instantiate(prefabToSpawn, spawn);

            RectTransform rect = letterObj.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            LetterTile tile = letterObj.GetComponent<LetterTile>();
            if (tile != null)
            {
                tile.letter = letterStr[0].ToString();
                if (tile.textDisplay != null)
                    tile.textDisplay.text = tile.letter;

                char c = tile.letter[0];
                if (tile.valueDisplay != null && letterValues.ContainsKey(c))
                    tile.valueDisplay.text = letterValues[c].ToString();
            }
            else
            {
                Text txt = letterObj.GetComponentInChildren<Text>();
                if (txt != null && letterStr.Length > 0)
                    txt.text = letterStr;
            }

            letterObj.name = "Letter_" + letterStr + "_" + i;
        }
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0)) StartDrag();
        if (Input.GetMouseButton(0)) ContinueDrag();
        if (Input.GetMouseButtonUp(0)) EndDrag();
    }

    void StartDrag()
    {
        selectedLetters.Clear();
        currentWord = "";
        foreach (var seg in lineSegments) Destroy(seg);
        lineSegments.Clear();
    }

    RectTransform lastHitLetter;

    void ContinueDrag()
    {
        Vector2 mousePos = Input.mousePosition;
        RectTransform hitLetter = GetLetterUnderMouse(mousePos);

        if (hitLetter != null && hitLetter != lastHitLetter && !selectedLetters.Contains(hitLetter))
        {
            lastHitLetter = hitLetter;
            selectedLetters.Add(hitLetter);

            Text legacyText = hitLetter.GetComponentInChildren<Text>();
            string letterStr = legacyText != null ? legacyText.text : "";

            currentWord += letterStr;

            if (selectedLetters.Count > 1)
                CreateLine(
    selectedLetters[selectedLetters.Count - 2],
    hitLetter
);

        }

        if (selectedLetters.Count > 0)
        {
            if (lineSegments.Count > selectedLetters.Count - 1)
            {
                Destroy(lineSegments[lineSegments.Count - 1]);
                lineSegments.RemoveAt(lineSegments.Count - 1);
            }

            GameObject tempLine = Instantiate(linePrefab, lineParent);
            LineBetween(
    tempLine.GetComponent<RectTransform>(),
    selectedLetters[selectedLetters.Count - 1],
    mousePos
);

            lineSegments.Add(tempLine);
        }

        if (hitLetter == null)
            lastHitLetter = null;
    }


    void EndDrag()
    {
        currentWord = currentWord.ToUpper().Trim();
        Debug.Log("Word formed: " + currentWord);

        if (!string.IsNullOrEmpty(currentWord) && validWords.Contains(currentWord))
        {
            if (!foundWords.Contains(currentWord))
            {
                foundWords.Add(currentWord);

                int points = CalculateWordScore(currentWord);
                totalScore += points;
                UpdateScoreUI();

                UpdateFoundWordsUI(currentWord, points);
                if (wordAnimator != null) wordAnimator.PlayWord(currentWord);
                Debug.Log($"✅ Found valid word: {currentWord} (+{points})");
            }
            else
            {
                Debug.Log($"⚠️ Word '{currentWord}' already found!");
            }
        }
        else
        {
            Debug.Log($"❌ Invalid word: {currentWord}");
        }

        selectedLetters.Clear();
        currentWord = "";
        foreach (var seg in lineSegments) Destroy(seg);
        lineSegments.Clear();
    }

    private int CalculateWordScore(string word)
    {
        int total = 0;
        foreach (char c in word.ToUpper())
        {
            if (letterValues.TryGetValue(c, out int value))
                total += value;
        }
        return total;
    }

    void UpdateFoundWordsUI(string newWord, int points)
    {
        if (foundWordsText != null)
            foundWordsText.text = newWord.ToUpper(); // show only the last found word

        if (foundWordsContent == null || foundWordPrefab == null)
            return;

        if (levelLogic.wordsFound == levelLogic.targetWordCount)
        {
            levelLogic.FinishLevel();
            int bonus = levelLogic.BonusInt;
        }



        GameObject entry = Instantiate(foundWordPrefab, foundWordsContent);
        Text[] texts = entry.GetComponentsInChildren<Text>();

        int index = foundWords.Count; // total count after adding

        if (texts.Length >= 4)
        {
            texts[0].text = index.ToString() + ".";      // number
            texts[1].text = newWord.ToUpper();           // word
            texts[2].text = points.ToString();           // points
            if (levelLogic.wordsFound == levelLogic.targetWordCount)
            {
                int bonus = levelLogic.BonusInt;
                texts[3].text = bonus.ToString();
                NextLevelButton.gameObject.SetActive(true);
                CantChoose.gameObject.SetActive(true);
                //Time.timeScale = 0;
                StartCoroutine(GoToLevels());
            }
            else
            {
                texts[3].text = "0";
            }


        }
        else if (texts.Length == 2)
        {
            texts[0].text = index.ToString() + ".";
            texts[1].text = newWord.ToUpper() + " — " + points;
        }
        else if (texts.Length == 1)
        {
            texts[0].text = index.ToString() + ". " + newWord.ToUpper() + " — " + points;
        }

        // update level logic
        if (levelLogic != null)
        {
            levelLogic.wordsFound = foundWords.Count;
            if (levelLogic.wordsCountText != null)
                levelLogic.wordsCountText.text = levelLogic.wordsFound.ToString();
        }
    }

    IEnumerator GoToLevels()
    {
        yield return new WaitForSecondsRealtime(2.5f);

        int finishedLevel = currentLevelId;
        int previousBest = bonusManager.GetBonusForLevel(currentLevelId);
        bool sendBonus = false;

        // First-time highest level completion
        if (finishedLevel == menuManager.Levelindex)
        {
            menuManager.Levelindex++;
            menuManager.SetLevelIndex();
            sendBonus = true;
            Debug.Log("Unlocked next level!");
        }
        else
        {
            if (levelLogic.BonusInt > previousBest)
            {
                sendBonus = true;
                Debug.Log($"Replayed level → new bonus {levelLogic.BonusInt} is better than previous {previousBest}, sending bonus.");
            }
            else
            {
                sendBonus = false;
                Debug.Log($"Replayed level → new bonus {levelLogic.BonusInt} is not better than previous {previousBest}, skipping send.");
            }
        }

        if (sendBonus)
            if (sendBonus)
                levelLogic.SendBonusToPlayFab(levelLogic.BonusInt, currentLevelId);


        menuManager.FetchLevels();
        Time.timeScale = 1;
        StartCoroutine(FetchData());
        menuManager.Panels[6].gameObject.SetActive(false);
    }



    IEnumerator FetchData()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        bonusManager.LoadBonusFromPlayFab();
        Debug.Log("Done");
    }


    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + totalScore.ToString();
    }

    RectTransform GetLetterUnderMouse(Vector2 mousePos)
    {
        foreach (var spawn in spawnPoints)
        {
            foreach (Transform letter in spawn) // loop through actual letters inside spawn
            {
                if (!letter.gameObject.activeInHierarchy) continue;

                RectTransform rect = letter.GetComponent<RectTransform>();
                if (rect == null) continue;

                // Use RectangleContainsScreenPoint (works well for ScreenSpace-Overlay)
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
                {
                    return rect; // return this specific letter instance
                }
            }
        }
        return null;
    }



    void CreateLine(RectTransform from, RectTransform to)
    {
        GameObject lineObj = Instantiate(linePrefab, lineParent);
        lineSegments.Add(lineObj);

        Vector2 toScreenPos = RectTransformUtility.WorldToScreenPoint(null, to.position);
        LineBetween(lineObj.GetComponent<RectTransform>(), from, toScreenPos);
    }

    Vector2 ToLocalPosFromWorld(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineParent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out Vector2 localPos
        );
        return localPos;
    }

    Vector2 ToLocalPosFromScreen(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineParent,
            screenPos,
            null,
            out Vector2 localPos
        );
        return localPos;
    }

    void LineBetween(RectTransform line, RectTransform from, Vector2 toScreenPos)
    {
        // Convert both endpoints into lineParent local coordinates (anchored space)
        Vector2 startLocal = ToLocalPosFromWorld(from.position);
        Vector2 endLocal = ToLocalPosFromScreen(toScreenPos);

        Vector2 dir = endLocal - startLocal;
        float length = dir.magnitude;

        // Ensure pivot is at left so anchoredPosition is the start point
        line.pivot = new Vector2(0f, 0.5f);

        // Set line size & position
        line.sizeDelta = new Vector2(length, Mathf.Max(1f, line.sizeDelta.y));
        line.anchoredPosition = startLocal;

        // Set rotation
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

}
