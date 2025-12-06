using UnityEngine;
using UnityEngine.UI;

public class LetterTile : MonoBehaviour
{
    public string letter = "";
    public Text textDisplay;
    public Text valueDisplay;

    private Image background;
    private Color originalColor;

    void Awake()
    {
        background = GetComponent<Image>();
        if (background != null)
            originalColor = background.color;
    }

    void Start()
    {
        if (textDisplay != null)
            textDisplay.text = letter.ToString().ToUpper();
    }

    public void Highlight(bool state)
    {
        if (background != null)
            background.color = state ? Color.yellow : originalColor;
    }
}
