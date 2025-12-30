using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WordAnimator : MonoBehaviour
{
    public RectTransform letterContainer;  
    public GameObject letterPrefab;     
    public float dropDelay = 0.05f;   
    public float dropDistance = 50f;    

    public void PlayWord(string word)
    {
        StartCoroutine(AnimateWord(word));
    }

  private IEnumerator AnimateWord(string word)
{
    var layout = letterContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();
    if (layout != null) layout.enabled = false;

    // Clear previous letters
    foreach (Transform child in letterContainer)
        Destroy(child.gameObject);

    float spacing = 0f;
    if (letterContainer.childCount > 0)
        spacing = 60f; 
    else
        spacing = 80f;

    float startX = -((word.Length - 1) * spacing) / 2f;

    for (int i = 0; i < word.Length; i++)
    {
        char c = word[i];

        GameObject letterObj = Instantiate(letterPrefab, letterContainer);
        Text text = letterObj.GetComponent<Text>();
        if (text != null)
            text.text = c.ToString().ToUpper();

        RectTransform rect = letterObj.GetComponent<RectTransform>();

        Vector2 finalPos = new Vector2(startX + i * spacing, 0f);
        rect.anchoredPosition = finalPos + Vector2.up * dropDistance; 

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

    if (layout != null) layout.enabled = true;
}



}
