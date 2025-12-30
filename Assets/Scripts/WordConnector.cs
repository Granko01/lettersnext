using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public GameObject foundWordPrefabLetter;

    [Header("Score")]
    public Text scoreText;
    private int totalScore = 0;
    public Text Bonus;
    public GameObject NextLevelButton;
    public GameObject CantChoose;

    private List<RectTransform> selectedLetters = new List<RectTransform>();
    private List<GameObject> lineSegments = new List<GameObject>();
    private List<GameObject> currentDragLetters = new List<GameObject>();
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
        CantChoose.SetActive(false);
        NextLevelButton.SetActive(false);
        LevelGm.SetActive(true);
        ClearLevelData();
        LoadLevelLetters();
        SpawnLetters();
        levelLogic.StartTimer(menuManager.Levelindex);
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
            string[] lines = wordFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
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
        levelLogic.elapsed = 0;
        foreach (RectTransform spawn in spawnPoints)
            for (int i = spawn.childCount - 1; i >= 0; i--)
                Destroy(spawn.GetChild(i).gameObject);

        foundWords.Clear();

        if (foundWordsContent != null)
            for (int i = foundWordsContent.childCount - 1; i >= 0; i--)
                Destroy(foundWordsContent.GetChild(i).gameObject);

        currentWord = "";
        selectedLetters.Clear();
        currentDragLetters.Clear();

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
        foreach (var seg in lineSegments)
            Destroy(seg);
        lineSegments.Clear();
        lastHitLetter = null;
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

            LetterTile tile = hitLetter.GetComponent<LetterTile>();
            string letterStr = tile != null ? tile.letter : "";

            currentWord += letterStr;

            if (foundWordPrefabLetter != null)
            {
                GameObject letterClone = Instantiate(foundWordPrefabLetter, lineParent);

                RectTransform cloneRect = letterClone.GetComponent<RectTransform>();
                RectTransform hitRect = hitLetter.GetComponent<RectTransform>();

                cloneRect.position = hitRect.position; 
                cloneRect.localScale = Vector3.zero;

                Text letterText = letterClone.GetComponent<Text>();
                if (letterText != null)
                {
                    letterText.text = letterStr;
                    letterText.color = Color.black;
                }

                StartCoroutine(DropLetter(cloneRect, 120f, 0.25f));

                currentDragLetters.Add(letterClone);
            }

            if (selectedLetters.Count > 1)
                CreateLine(selectedLetters[selectedLetters.Count - 2], hitLetter);
        }

        if (selectedLetters.Count > 0)
        {
            if (lineSegments.Count > selectedLetters.Count - 1)
            {
                Destroy(lineSegments[lineSegments.Count - 1]);
                lineSegments.RemoveAt(lineSegments.Count - 1);
            }

            GameObject tempLine = Instantiate(linePrefab, lineParent);
            LineBetween(tempLine.GetComponent<RectTransform>(), selectedLetters[selectedLetters.Count - 1], mousePos);
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

                if (foundWordsContent != null && foundWordPrefab != null)
                {
                    GameObject entry = Instantiate(foundWordPrefab, foundWordsContent);

                    Text numberText = entry.transform.Find("NumberText")?.GetComponent<Text>();
                    if (numberText != null)
                        numberText.text = foundWords.Count + ".";

                    Transform lettersContainer = entry.transform.Find("LettersContainer");

                    if (lettersContainer != null)
                    {
                        for (int i = 0; i < currentDragLetters.Count && i < lettersContainer.childCount; i++)
                        {
                            GameObject letterObj = currentDragLetters[i];
                            Transform targetSlot = lettersContainer.GetChild(i);

                            StartCoroutine(MoveLetterToSlot(letterObj.transform, targetSlot));
                        }

                    }

                    currentDragLetters.Clear();

                    Text pointsText = entry.transform.Find("PointsText")?.GetComponent<Text>();
                    if (pointsText != null)
                        pointsText.text = points.ToString();

                    if (levelLogic != null && levelLogic.wordsFound + 1 == levelLogic.targetWordCount)
                    {
                        Text bonusText = entry.transform.Find("BonusText")?.GetComponent<Text>();
                        if (bonusText != null)
                            bonusText.text = levelLogic.BonusInt.ToString();

                        Bonus.text = levelLogic.BonusInt.ToString();
                        NextLevelButton.SetActive(true);
                        CantChoose.SetActive(true);
                        StartCoroutine(GoToLevels());
                    }
                }

                Debug.Log($"✅ Found valid word: {currentWord} (+{points})");
                levelLogic.wordsFound++;
                levelLogic.wordsCountText.text =$"{levelLogic.wordsFound} / {levelLogic.targetWordCount}";
            }
        }
        else
        {
            Debug.Log($"❌ Invalid word: {currentWord}");
            foreach (var letterObj in currentDragLetters)
                Destroy(letterObj);
            currentDragLetters.Clear();
        }

        selectedLetters.Clear();
        currentWord = "";
        foreach (var seg in lineSegments)
            Destroy(seg);
        lineSegments.Clear();
    }

    IEnumerator MoveLetterToSlot(Transform letter, Transform targetSlot)
    {
        RectTransform letterRect = letter as RectTransform;

        letterRect.SetParent(targetSlot, false);

        Vector3 startPos = letterRect.localPosition;
        Vector3 endPos = Vector3.zero;

        float duration = 0.75f;
        float time = 0f;

        while (time < duration)
        {
            letterRect.localPosition = Vector3.Lerp(startPos, endPos, time / duration);
            letterRect.localScale = Vector3.Lerp(letterRect.localScale, Vector3.one, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        letterRect.localPosition = Vector3.zero;
        letterRect.localScale = Vector3.one;
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

    IEnumerator DropLetter(Transform letter, float dropHeight = 100f, float duration = 0.3f)
    {
        RectTransform rect = letter as RectTransform;

        Vector3 endPos = rect.localPosition;
        Vector3 startPos = endPos + new Vector3(0, dropHeight, 0);

        rect.localPosition = startPos;
        rect.localScale = Vector3.zero;

        float time = 0f;
        while (time < duration)
        {
            rect.localPosition = Vector3.Lerp(startPos, endPos, time / duration);
            rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        rect.localPosition = endPos;
        rect.localScale = Vector3.one;
    }


    IEnumerator MoveLetterToParent(Transform letter, Transform target)
    {
        Vector3 startPos = letter.position;
        Vector3 endPos = target.position;
        float duration = 0.5f;
        float time = 0f;

        while (time < duration)
        {
            letter.position = Vector3.Lerp(startPos, endPos, time / duration);
            letter.localScale = Vector3.Lerp(Vector3.one, Vector3.one, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        letter.SetParent(target, worldPositionStays: false);
        letter.localPosition = Vector3.zero;
        letter.localScale = Vector3.one;
    }

    RectTransform GetLetterUnderMouse(Vector2 mousePos)
    {
        foreach (var spawn in spawnPoints)
        {
            foreach (Transform letter in spawn)
            {
                if (!letter.gameObject.activeInHierarchy) continue;

                RectTransform rect = letter.GetComponent<RectTransform>();
                if (rect == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
                    return rect;
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineParent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos), null, out Vector2 localPos);
        return localPos;
    }

    Vector2 ToLocalPosFromScreen(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineParent, screenPos, null, out Vector2 localPos);
        return localPos;
    }

    void LineBetween(RectTransform line, RectTransform from, Vector2 toScreenPos)
    {
        Vector2 startLocal = ToLocalPosFromWorld(from.position);
        Vector2 endLocal = ToLocalPosFromScreen(toScreenPos);
        Vector2 dir = endLocal - startLocal;
        float length = dir.magnitude;

        line.pivot = new Vector2(0f, 0.5f);
        line.sizeDelta = new Vector2(length, Mathf.Max(1f, line.sizeDelta.y));
        line.anchoredPosition = startLocal;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + totalScore.ToString();
    }

    IEnumerator GoToLevels()
    {
        yield return new WaitForSecondsRealtime(2.5f);

        int finishedLevel = currentLevelId;
        int previousBest = bonusManager.GetBonusForLevel(currentLevelId);
        bool sendBonus = false;

        if (finishedLevel == menuManager.Levelindex)
        {
            menuManager.Levelindex++;
            menuManager.SetLevelIndex();
            sendBonus = true;
            Debug.Log("Unlocked next level!");
        }
        else if (levelLogic.BonusInt > previousBest)
        {
            sendBonus = true;
            Debug.Log($"Replayed level → new bonus {levelLogic.BonusInt} is better than previous {previousBest}, sending bonus.");
        }
        else
        {
            sendBonus = false;
            Debug.Log($"Replayed level → new bonus {levelLogic.BonusInt} is not better than previous {previousBest}, skipping send.");
        }

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
}
