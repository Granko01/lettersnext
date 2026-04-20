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
    public Transform LastEntriesPos;
    public GameObject foundWordPrefab;
    public GameObject LastEntries;
    public GameObject LastEntriesSecond;
    public GameObject foundWordPrefabLetter;
    public GameObject PreGamePanel;
    [Header("Score")]
    public Text scoreText;
    private int totalScore = 0;
    public int LengthTextCount;
    public GameObject NextLevelButton;
    public GameObject CantChoose;
    public GameObject DisableCircle;
    public int totalLetterPoints = 0;
    public int totalLengthPoints = 0;
    public int totalTimeBonus = 0;


    private List<RectTransform> selectedLetters = new List<RectTransform>();
    private List<GameObject> lineSegments = new List<GameObject>();
    private List<GameObject> currentDragLetters = new List<GameObject>();
    private string currentWord = "";

    public List<LevelData> allLevels = new List<LevelData>();
    public int currentLevelId = 1;
    public GameObject LevelGm;

    bool showingValues = false;
    Coroutine toggleCoroutine;


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
    private readonly Dictionary<int, int> CountWordsValues = new Dictionary<int, int>()
{
    { 3, 1 },
    { 4, 3 },
    { 5, 5 },
    { 6, 10 },
    { 7, 20 },
    { 8, 30 },
    { 9, 50 },
    { 10, 100 }
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
    public void ClosePreGamePanel()
    {
        PreGamePanel.gameObject.SetActive(false);
        menuManager.UseEnergy();
        levelLogic.StartTimer(menuManager.Levelindex);
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
    }

   void LoadLevelLetters()
{
    LevelData level = allLevels.FirstOrDefault(l => l.levelId == currentLevelId);
    if (level != null)
    {
        // Split letters by space
        List<string> letterList = level.letters.Split(' ').ToList();

            // Shuffle (Fisher-Yates)
            // for (int i = 0; i < letterList.Count; i++)
            // {
            //     int randomIndex = UnityEngine.Random.Range(i, letterList.Count);
            //     string temp = letterList[i];
            //     letterList[i] = letterList[randomIndex];
            //     letterList[randomIndex] = temp;
            // }

            letters = string.Join(" ", letterList);

        Debug.Log($"🔀 Shuffled letters for level {currentLevelId}: {letters}");
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
        PointsInt = 0;
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

    private RectTransform previewLine;
  void ContinueDrag()
{
    Vector2 mousePos = Input.mousePosition;
    RectTransform hitLetter = GetLetterUnderMouse(mousePos);

    if (hitLetter != null)
    {
        // 🟢 UNDO (go back 1 step)
        if (selectedLetters.Count > 1 && hitLetter == selectedLetters[selectedLetters.Count - 2])
        {
            UndoLastLetter();
            lastHitLetter = hitLetter;
            return;
        }

        // 🔴 CLEAR ALL (back to first letter)
        if (selectedLetters.Count > 1 && hitLetter == selectedLetters[0])
        {
            ClearCurrentSelection();
            lastHitLetter = hitLetter;
            return;
        }

        // 🟡 NORMAL ADD (your existing logic, slightly wrapped)
        if (hitLetter != lastHitLetter && !selectedLetters.Contains(hitLetter))
        {
            lastHitLetter = hitLetter;
            selectedLetters.Add(hitLetter);

            LetterTile tile = hitLetter.GetComponent<LetterTile>();
            string letterStr = tile != null ? tile.letter : "";

            currentWord += letterStr;

            if (foundWordsText != null)
            {
                foundWordsText.text = currentWord;
                foundWordsText.transform.localScale = Vector3.one * 1.2f;
            }

            // Spawn dragged letter UI clone
            if (foundWordPrefabLetter != null)
            {
                GameObject letterClone = Instantiate(foundWordPrefabLetter, lineParent);

                RectTransform cloneRect = letterClone.GetComponent<RectTransform>();
                RectTransform hitRect = hitLetter.GetComponent<RectTransform>();

                LetterData data = letterClone.GetComponent<LetterData>();
                if (data == null)
                    data = letterClone.AddComponent<LetterData>();

                data.SetLetter(letterStr);

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

                // Create line
                if (selectedLetters.Count > 1)
                {
                    CreateLine(
                        selectedLetters[selectedLetters.Count - 2],
                        hitLetter
                    );
                }
            }
        }
    }

    // ---------- Live Preview Line ----------
    if (selectedLetters.Count > 0)
    {
        if (previewLine == null)
        {
            GameObject lineObj = Instantiate(linePrefab, lineParent);
            previewLine = lineObj.GetComponent<RectTransform>();
            lineSegments.Add(lineObj);
        }

        LineBetween(
            previewLine,
            selectedLetters[selectedLetters.Count - 1],
            Input.mousePosition
        );
    }

    if (hitLetter == null)
        lastHitLetter = null;
}
void UndoLastLetter()
{
    if (selectedLetters.Count == 0) return;

    int lastIndex = selectedLetters.Count - 1;

    // Animate letter removal (snap back)
    if (currentDragLetters.Count > lastIndex)
    {
        StartCoroutine(AnimateRemoveLetter(
            currentDragLetters[lastIndex],
            selectedLetters[lastIndex]
        ));
        currentDragLetters.RemoveAt(lastIndex);
    }

    // Animate line removal
    if (lineSegments.Count > 0)
    {
        StartCoroutine(FadeOutLine(lineSegments[lineSegments.Count - 1]));
        lineSegments.RemoveAt(lineSegments.Count - 1);
    }

    // Remove from data
    selectedLetters.RemoveAt(lastIndex);

    if (currentWord.Length > 0)
        currentWord = currentWord.Substring(0, currentWord.Length - 1);

    if (foundWordsText != null)
        foundWordsText.text = currentWord;
}
IEnumerator AnimateRemoveLetter(GameObject letterObj, RectTransform targetTile)
{
    RectTransform rect = letterObj.GetComponent<RectTransform>();

    Vector3 startPos = rect.position;
    Vector3 endPos = targetTile != null ? targetTile.position : startPos + new Vector3(0, 80f, 0);

    float duration = 0.25f;
    float time = 0f;

    while (time < duration)
    {
        float t = Mathf.SmoothStep(0, 1, time / duration);

        rect.position = Vector3.Lerp(startPos, endPos, t);
        rect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

        time += Time.deltaTime;
        yield return null;
    }

    Destroy(letterObj);
}
IEnumerator FadeOutLine(GameObject lineObj)
{
    Image img = lineObj.GetComponent<Image>();
    RectTransform rect = lineObj.GetComponent<RectTransform>();

    float duration = 0.2f;
    float time = 0f;

    Color startColor = img.color;
    Vector3 startScale = rect.localScale;

    while (time < duration)
    {
        float t = Mathf.SmoothStep(0, 1, time / duration);

        img.color = new Color(startColor.r, startColor.g, startColor.b, 1 - t);
        rect.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

        time += Time.deltaTime;
        yield return null;
    }

    Destroy(lineObj);
}
void AddLetter(RectTransform hitLetter)
{
    selectedLetters.Add(hitLetter);

    LetterTile tile = hitLetter.GetComponent<LetterTile>();
    string letterStr = tile != null ? tile.letter : "";

    currentWord += letterStr;

    if (foundWordsText != null)
    {
        foundWordsText.text = currentWord;
        foundWordsText.transform.localScale = Vector3.one * 1.2f;
    }

    if (foundWordPrefabLetter != null)
    {
        GameObject letterClone = Instantiate(foundWordPrefabLetter, lineParent);

        RectTransform cloneRect = letterClone.GetComponent<RectTransform>();
        RectTransform hitRect = hitLetter.GetComponent<RectTransform>();

        LetterData data = letterClone.GetComponent<LetterData>() ?? letterClone.AddComponent<LetterData>();
        data.SetLetter(letterStr);

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

        if (selectedLetters.Count > 1)
        {
            CreateLine(
                selectedLetters[selectedLetters.Count - 2],
                hitLetter
            );
        }
    }
}
void ClearCurrentSelection()
{
    foreach (var obj in currentDragLetters)
        StartCoroutine(AnimateRemoveLetter(obj, null));

    foreach (var line in lineSegments)
        StartCoroutine(FadeOutLine(line));

    selectedLetters.Clear();
    currentDragLetters.Clear();
    lineSegments.Clear();

    currentWord = "";

    if (foundWordsText != null)
        foundWordsText.text = "";
}
    public int PointsInt;
    public int TotalTimeint;
    public int LetterPointsint;
    public int LetterLengthint;
    public int TimeBonusint;
    public int GrandTotalInt;
    void EndDrag()
    {
        currentWord = currentWord.ToUpper().Trim();
        Debug.Log("Word formed: " + currentWord);

        // Case 1: Valid word AND not already found
        if (!string.IsNullOrEmpty(currentWord) && validWords.Contains(currentWord) && !foundWords.Contains(currentWord))
        {
            foundWords.Add(currentWord);

            int letterScore = CalculateWordScore(currentWord);
            int lengthScore = GetLengthScore(currentWord.Length);

            totalLetterPoints += letterScore;
            totalLengthPoints += lengthScore;

            int points = letterScore;
            totalScore = points;

            Debug.Log($"Letters Total: {totalLetterPoints}, Length Total: {totalLengthPoints}, Overall Total: {totalScore}");

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
                    // Clear any leftover letters in this entry
                    foreach (Transform placeholder in lettersContainer)
                        foreach (Transform child in placeholder)
                            Destroy(child.gameObject);

                    int slotCount = lettersContainer.childCount;
                    int letterCount = currentDragLetters.Count;
                    int startIndex = Mathf.Max(0, slotCount - letterCount);

                    for (int i = 0; i < letterCount && (startIndex + i) < slotCount; i++)
                    {
                        GameObject letterObj = currentDragLetters[i];
                        Transform targetSlot = lettersContainer.GetChild(startIndex + i);

                        StartCoroutine(MoveLetterToSlot(letterObj.transform, targetSlot));
                    }
                }

                currentDragLetters.Clear();

                Text pointsText = entry.transform.Find("PointsText")?.GetComponentInChildren<Text>();
                if (pointsText != null)
                    pointsText.text = points.ToString();

                Text letterText = entry.transform.Find("Letterlength")?.GetComponentInChildren<Text>();
                if (letterText != null)
                    letterText.text = lengthScore.ToString();

                // Check if level is complete
                if (levelLogic != null && levelLogic.wordsFound + 1 == levelLogic.targetWordCount)
                {
                    levelLogic.FinishLevel();
                   

                    GameObject lastEntry = Instantiate(LastEntries, foundWordsContent);
                    GameObject LastEntrySecond = Instantiate(LastEntriesSecond, LastEntriesPos.position, LastEntriesPos.rotation, LastEntriesPos);
                    Text[] texts = lastEntry.GetComponentsInChildren<Text>(true);
                    Text[] texts1 = LastEntrySecond.GetComponentsInChildren<Text>(true);

                    foreach (Text t in texts)
                    {
                        switch (t.name)
                        {
                            case "LetterLength":
                                LetterPointsint = totalLetterPoints;
                                t.text = totalLetterPoints.ToString();
                                break;
                            case "WordsLength":
                                LetterLengthint = totalLengthPoints;
                                t.text = totalLengthPoints.ToString();
                                break;
                        }
                    }
                    foreach (Text t1 in texts1)
                    {
                        switch (t1.name)
                        {
                            case "TotalTime":
                                TotalTimeint = Mathf.RoundToInt(levelLogic.TotalTime);
                                t1.text = TotalTimeint.ToString();
                                break;

                            case "LetterLength":
                                LetterPointsint = totalLetterPoints;
                                t1.text = LetterPointsint.ToString();
                                break;

                            case "TimeBonus":
                                TimeBonusint = Mathf.RoundToInt(levelLogic.GetTimersBonus);
                                t1.text = TimeBonusint.ToString();
                                break;

                            case "WordsLength":
                                LetterLengthint = totalLengthPoints;
                                t1.text = totalLengthPoints.ToString();
                                break;

                            case "GrandTotal":
                                GrandTotalInt =
                                    LetterPointsint +
                                    TimeBonusint +
                                    LetterLengthint;

                                levelLogic.BonusInt = GrandTotalInt;
                                t1.text = GrandTotalInt.ToString();
                                break;
                        }
                    }


                    NextLevelButton.SetActive(true);
                    DisableCircle.gameObject.SetActive(false);
                    StartCoroutine(ShowWinPanel());
                    StartCoroutine(CovertWordsWait());
                    StartCoroutine(SendDataDelay());
                }
            }

            Debug.Log($"✅ Found valid word: {currentWord} (+{points})");
            levelLogic.wordsFound++;
            levelLogic.wordsCountText.text = $"{levelLogic.wordsFound} / {levelLogic.targetWordCount}";
        }
        else
        {
            Debug.Log($"❌ Invalid or duplicate word: {currentWord}");

            foreach (var letterObj in currentDragLetters)
                Destroy(letterObj);
            currentDragLetters.Clear();

            foreach (var seg in lineSegments)
                Destroy(seg);
            lineSegments.Clear();

            selectedLetters.Clear();
            currentWord = "";
            lastHitLetter = null;

            if (foundWordsText != null)
                foundWordsText.text = "";
        }

        selectedLetters.Clear();
        currentWord = "";
        foreach (var seg in lineSegments)
            Destroy(seg);
        lineSegments.Clear();
        lastHitLetter = null;

        if (foundWordsText != null)
            foundWordsText.text = "";
    }

    public IEnumerator SendDataDelay()
    {
        yield return new WaitForSecondsRealtime(2f);
         SendData();
         
    }
    int GetLengthScore(int length)
    {
        if (length >= 10) return CountWordsValues[10];
        if (CountWordsValues.TryGetValue(length, out int score)) return score;
        return 0;
    }

    // IEnumerator AnimateAddWordToText(string word, float letterDelay = 0.1f)
    // {
    //     if (foundWordsText == null)
    //         yield break;

    //     // Clear previous word
    //     foundWordsText.text = "";

    //     foreach (char c in word)
    //     {
    //         foundWordsText.text += c;
    //         yield return new WaitForSeconds(letterDelay);
    //     }
    // }


    IEnumerator CovertWordsWait()
    {
        yield return new WaitForSecondsRealtime(2f);

        // first flip to values (your existing method)
        ConvertFoundWordsToPoints();

        // start looping toggle
        if (toggleCoroutine != null)
            StopCoroutine(toggleCoroutine);

        toggleCoroutine = StartCoroutine(ToggleLettersAndValues());
    }

    public void ConvertFoundWordsToPoints()
    {
        if (foundWordsContent == null) return;

        foreach (Transform wordEntry in foundWordsContent)
        {
            Transform lettersContainer = wordEntry.Find("LettersContainer");
            if (lettersContainer == null) continue;

            for (int i = 0; i < lettersContainer.childCount; i++)
            {
                Transform placeholder = lettersContainer.GetChild(i);
                if (placeholder.childCount == 0) continue;

                Transform letterTransform = placeholder.GetChild(0);
                Text letterText = letterTransform.GetComponentInChildren<Text>();
                LetterData data = letterTransform.GetComponent<LetterData>();

                if (letterText != null && data != null)
                {
                    StartCoroutine(AnimateLetterToValue(letterText, data.value));
                }
            }
        }
    }

    IEnumerator AnimateLetterToValue(Text letterText, int value, float duration = 0.3f)
    {
        Vector3 originalScale = letterText.transform.localScale;

        // Shrink the letter to 0 (like a flip)
        float time = 0f;
        while (time < duration / 2f)
        {
            letterText.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, time / (duration / 2f));
            time += Time.deltaTime;
            yield return null;
        }

        // Swap text to value
        letterText.text = value.ToString();

        // Expand back to original size
        time = 0f;
        while (time < duration / 2f)
        {
            letterText.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, time / (duration / 2f));
            time += Time.deltaTime;
            yield return null;
        }

        letterText.transform.localScale = originalScale;
    }
    IEnumerator AnimateValueToLetter(Text letterText, string letter, float duration = 0.3f)
    {
        Vector3 originalScale = letterText.transform.localScale;

        float time = 0f;
        while (time < duration / 2f)
        {
            letterText.transform.localScale =
                Vector3.Lerp(originalScale, Vector3.zero, time / (duration / 2f));
            time += Time.deltaTime;
            yield return null;
        }

        letterText.text = letter;

        time = 0f;
        while (time < duration / 2f)
        {
            letterText.transform.localScale =
                Vector3.Lerp(Vector3.zero, originalScale, time / (duration / 2f));
            time += Time.deltaTime;
            yield return null;
        }

        letterText.transform.localScale = originalScale;
    }
    IEnumerator ToggleLettersAndValues()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(3f);

            showingValues = !showingValues;

            foreach (Transform wordEntry in foundWordsContent)
            {
                Transform lettersContainer = wordEntry.Find("LettersContainer");
                if (lettersContainer == null) continue;

                for (int i = 0; i < lettersContainer.childCount; i++)
                {
                    Transform placeholder = lettersContainer.GetChild(i);
                    if (placeholder.childCount == 0) continue;

                    Transform letterTransform = placeholder.GetChild(0);
                    Text letterText = letterTransform.GetComponentInChildren<Text>();
                    LetterData data = letterTransform.GetComponent<LetterData>();

                    if (letterText == null || data == null) continue;

                    if (showingValues)
                        StartCoroutine(AnimateLetterToValue(letterText, data.value));
                    else
                        StartCoroutine(AnimateValueToLetter(letterText, data.letter));
                }
            }
        }
    }


    IEnumerator ShowWinPanel()
    {
        yield return new WaitForSecondsRealtime(1f);
        CantChoose.gameObject.SetActive(true);
     
    }

    IEnumerator MoveLetterToSlot(Transform letter, Transform targetSlot)
    {
        RectTransform letterRect = letter as RectTransform;
        letterRect.SetParent(targetSlot, false);

        Vector3 startPos = letterRect.localPosition;
        Vector3 endPos = Vector3.zero;

        float duration = 0.75f;
        float time = 0f;

        // Make sure the value text is visible
        Text valueText = letterRect.GetComponentInChildren<Text>(); // or valueDisplay reference
        if (valueText != null)
            valueText.enabled = true;

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
            {
                total += value;
                Debug.Log($"Letter: {c}, Value: {value}, Running Total: {total}");
            }
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

    LineBetween(
        lineObj.GetComponent<RectTransform>(),
        from,
        to
    );
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

    void LineBetween(RectTransform line, RectTransform from, RectTransform to)
{
    Vector2 startLocal;
    Vector2 endLocal;

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        lineParent,
        RectTransformUtility.WorldToScreenPoint(null, from.position),
        null,
        out startLocal
    );

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        lineParent,
        RectTransformUtility.WorldToScreenPoint(null, to.position),
        null,
        out endLocal
    );

    DrawLine(line, startLocal, endLocal);
}
void LineBetween(RectTransform line, RectTransform from, Vector2 mouseScreenPos)
{
    Vector2 startLocal;
    Vector2 endLocal;

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        lineParent,
        RectTransformUtility.WorldToScreenPoint(null, from.position),
        null,
        out startLocal
    );

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        lineParent,
        mouseScreenPos,
        null,
        out endLocal
    );

    DrawLine(line, startLocal, endLocal);
}
void DrawLine(RectTransform line, Vector2 startLocal, Vector2 endLocal)
{
    Vector2 dir = endLocal - startLocal;
    float length = dir.magnitude;

    line.pivot = new Vector2(0f, 0.5f);
    line.sizeDelta = new Vector2(length, line.sizeDelta.y);
    line.anchoredPosition = startLocal;

    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    line.localRotation = Quaternion.Euler(0, 0, angle);
}
    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + totalScore.ToString();
    }

    public void SendData()
    {

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
       

    }
    public void GoToLevels()
    {
         menuManager.Panels[6].gameObject.SetActive(false);
        DisableCircle.gameObject.SetActive(true);
        TotalTimeint = 0;
        LetterPointsint = 0;
        LetterLengthint = 0;
        TimeBonusint = 0;
        GrandTotalInt = 0;
        totalLetterPoints = 0;
        totalLengthPoints = 0;
        totalTimeBonus = 0;
    }
    public GameObject NoEnergies;
    public void Retry()
    {
        if (menuManager.Energies > 0)
        {
             int finishedLevel = currentLevelId;
        int previousBest = bonusManager.GetBonusForLevel(currentLevelId);
        bool sendBonus = false;
         TotalTimeint = 0;
        LetterPointsint = 0;
        LetterLengthint = 0;
        TimeBonusint = 0;
        GrandTotalInt = 0;
        totalLetterPoints = 0;
        totalLengthPoints = 0;
        totalTimeBonus = 0;
        levelLogic.StartTimer(menuManager.Levelindex);

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

        if (sendBonus)
            levelLogic.SendBonusToPlayFab(levelLogic.BonusInt, currentLevelId);

        menuManager.FetchLevels();
        Time.timeScale = 1;
        NextLevelButton.SetActive(false);
        DisableCircle.gameObject.SetActive(true);
        
        ClearLevelData();
        LoadLevelLetters();
        SpawnLetters();
        menuManager.UseEnergy();
        CantChoose.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("No energies");
            StartCoroutine(NoEnergiesMeth());
        }
    }
     private bool isShowingNoEnergy = false;

    public IEnumerator NoEnergiesMeth()
    {
        if (isShowingNoEnergy)
            yield break;

        isShowingNoEnergy = true;

        NoEnergies.gameObject.SetActive(true);

        Vector3 startPos = NoEnergies.transform.position;
        Vector3 endPos = startPos + new Vector3(0, 50f, 0);

        var graphic = NoEnergies.GetComponent<UnityEngine.UI.Graphic>();
        Color c = graphic.color;
        c.a = 1f;
        graphic.color = c;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            NoEnergies.transform.position = Vector3.Lerp(startPos, endPos, t);

            c.a = Mathf.Lerp(1f, 0f, t);
            graphic.color = c;

            yield return null;
        }

        // reset
        NoEnergies.transform.position = startPos;
        c.a = 1f;
        graphic.color = c;

        NoEnergies.gameObject.SetActive(false);

        isShowingNoEnergy = false;
    }

    IEnumerator FetchData()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        bonusManager.LoadBonusFromPlayFab();
        bonusManager.ShowEndGameResults(currentLevelId);
        Debug.Log("Done");
    }
}
