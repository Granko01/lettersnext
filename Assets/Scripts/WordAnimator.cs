using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WordAnimator : MonoBehaviour
{
    public RectTransform letterContainer;   // where letters are spawned
    public GameObject letterPrefab;         // Text prefab for a single letter
    public float dropDelay = 0.05f;         // delay between letters
    public float dropDistance = 50f;        // start above

    public void PlayWord(string word)
    {
        StartCoroutine(AnimateWord(word));
    }

  private IEnumerator AnimateWord(string word)
{
    // Disable Layout Group if any
    var layout = letterContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();
    if (layout != null) layout.enabled = false;

    // Clear previous letters
    foreach (Transform child in letterContainer)
        Destroy(child.gameObject);

    // Calculate horizontal spacing
    float spacing = 0f;
    if (letterContainer.childCount > 0)
        spacing = 30f; // optional spacing between letters
    else
        spacing = 40f; // default spacing

    float startX = -((word.Length - 1) * spacing) / 2f; // center word in container

    for (int i = 0; i < word.Length; i++)
    {
        char c = word[i];

        GameObject letterObj = Instantiate(letterPrefab, letterContainer);
        Text text = letterObj.GetComponent<Text>();
        if (text != null)
            text.text = c.ToString().ToUpper();

        RectTransform rect = letterObj.GetComponent<RectTransform>();

        // Set horizontal position
        Vector2 finalPos = new Vector2(startX + i * spacing, 0f);
        rect.anchoredPosition = finalPos + Vector2.up * dropDistance; // start above

        // Drop animation
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            rect.anchoredPosition = Vector2.Lerp(finalPos + Vector2.up * dropDistance, finalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rect.anchoredPosition = finalPos;

        yield return new WaitForSeconds(dropDelay);
    }

    // Re-enable Layout Group if it exists
    if (layout != null) layout.enabled = true;
}



}
