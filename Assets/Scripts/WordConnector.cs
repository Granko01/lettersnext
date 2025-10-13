using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WordConnector : MonoBehaviour
{
    [Header("Letter Setup")]
    public RectTransform[] spawnPoints;   // UI spawn points
    public GameObject letterPrefab;       // Prefab with Image + legacy Text
    public string letters = "T A E R S N O L I C";

    [Header("Line Setup")]
    public GameObject linePrefab;         // UI Image prefab for line segments
    public RectTransform lineParent;      // Usually your Canvas

    [Header("Word Validation")]
    public Text foundWordsText;           // Shows last found word
    private HashSet<string> validWords = new HashSet<string>();
    private HashSet<string> foundWords = new HashSet<string>();

    [Header("Found Words UI")]
    public Transform foundWordsContent;   // ScrollView Content
    public GameObject foundWordPrefab;    // Prefab for each found word (Number + Word+Score)

    [Header("Score")]
    public Text scoreText;
    private int totalScore = 0;

    private List<RectTransform> selectedLetters = new List<RectTransform>();
    private List<GameObject> lineSegments = new List<GameObject>();
    private string currentWord = "";

    LevelLogic levelLogic;
    public WordAnimator wordAnimator;

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
        levelLogic = FindObjectOfType<LevelLogic>();
        wordAnimator = FindObjectOfType<WordAnimator>();
        LoadValidWords();
        SpawnLetters();
        UpdateScoreUI();
    }

    // Load valid words from Resources/ValidWords.txt
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

    void SpawnLetters()
    {
        string[] splitLetters = letters.Split(' ');
        int count = Mathf.Min(splitLetters.Length, spawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            RectTransform spawn = spawnPoints[i];

            // instantiate as child of spawn and align
            GameObject letterObj = Instantiate(letterPrefab, spawn);
            RectTransform rect = letterObj.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            // Set letter (if prefab has LetterTile), otherwise set text directly
            LetterTile tile = letterObj.GetComponent<LetterTile>();
            string letterStr = splitLetters[i].Trim().ToUpper();
            if (tile != null)
            {
                tile.letter = letterStr[0];
                if (tile.textDisplay != null)
                    tile.textDisplay.text = tile.letter.ToString();
            }
            else
            {
                Text txt = letterObj.GetComponentInChildren<Text>();
                if (txt != null && letterStr.Length > 0)
                    txt.text = letterStr;
            }

            letterObj.name = "Letter_" + letterStr;
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

        if (hitLetter != null && hitLetter != lastHitLetter)
        {
            lastHitLetter = hitLetter; // mark this as last added
            selectedLetters.Add(hitLetter);

            // Get letter text
            Text legacyText = hitLetter.GetComponentInChildren<Text>();
            string letterStr = legacyText != null ? legacyText.text : "";

            currentWord += letterStr; // add only once per hover

            if (selectedLetters.Count > 1)
                CreateLine(selectedLetters[selectedLetters.Count - 2], hitLetter);
        }

        // Optional: line to mouse
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
            lastHitLetter = null; // reset when not over any letter
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

                // Update UI and animation
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

        // add new item to scroll content (keep old ones)
        GameObject entry = Instantiate(foundWordPrefab, foundWordsContent);
        Text[] texts = entry.GetComponentsInChildren<Text>();

        int index = foundWords.Count; // total count after adding

        if (texts.Length >= 3)
        {
            texts[0].text = index.ToString() + ".";      // number
            texts[1].text = newWord.ToUpper();           // word
            texts[2].text = points.ToString();           // points
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

                Vector2 letterPos = rect.position;
                float size = 50f; // adjust to your letter size
                if (Vector2.Distance(mousePos, letterPos) < size)
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
        LineBetween(lineObj.GetComponent<RectTransform>(), from, to.position);
    }

    void LineBetween(RectTransform line, RectTransform from, Vector2 toPos)
    {
        Vector2 start = from.position;
        Vector2 dir = toPos - start;
        float length = dir.magnitude;

        line.sizeDelta = new Vector2(length, 5f);
        line.pivot = new Vector2(0, 0.5f);
        line.position = start;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.rotation = Quaternion.Euler(0, 0, angle);
    }
}
