using UnityEngine;
using UnityEngine.UI;

public static class MenuTheme
{
    public static readonly Color Backdrop = new Color(.025f, .045f, .052f, .76f);
    public static readonly Color Surface = new Color(.067f, .102f, .113f, .98f);
    public static readonly Color Text = new Color(.95f, .96f, .93f);
    public static readonly Color Muted = new Color(.70f, .79f, .79f);
    public static readonly Color Accent = new Color(.67f, .86f, .73f);
    public static readonly Color Danger = new Color(.92f, .73f, .66f);

    public const string NavigationHint = "↑ ↓ or Tab to select   ·   Enter to confirm";
    public static readonly Vector2 Center = new Vector2(.5f, .5f);

    public static void Style(Button button, bool primary = false)
    {
        var colors = button.colors;
        colors.normalColor = primary ? new Color(.23f, .43f, .36f) : new Color(.13f, .21f, .23f);
        colors.highlightedColor = new Color(.25f, .43f, .37f);
        colors.selectedColor = new Color(.30f, .51f, .42f);
        colors.pressedColor = new Color(.19f, .35f, .30f);
        colors.fadeDuration = .12f;
        button.colors = colors;
        var label = button.GetComponentInChildren<Text>();
        if (label) label.color = Text;
    }

    public static GameObject Card(Transform parent, string name, Vector2 center, Vector2 size, Color color)
    {
        var card = SeghaniTakSceneBuilder.MakePanel(parent, name, color);
        Place(card.GetComponent<RectTransform>(), center, size);
        return card;
    }

    public static void Place(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Center;
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
    }

    public static Text Label(Transform parent, Font font, string value, int size, TextAnchor align,
        Vector2 center, Vector2 dimensions, Color color, bool bold = false)
    {
        var text = SeghaniTakSceneBuilder.MakeText(parent, "Label", font, size, align, Center, center, dimensions);
        text.text = value;
        text.color = color;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.raycastTarget = false;
        return text;
    }

    public static Button Button(Transform parent, Font font, string label, Vector2 center, Vector2 size,
        UnityEngine.Events.UnityAction action, bool primary = false)
    {
        var button = SeghaniTakSceneBuilder.MakeButton(parent, label, font, center);
        Place(button.GetComponent<RectTransform>(), center, size);
        var text = button.GetComponentInChildren<Text>();
        text.rectTransform.sizeDelta = size - new Vector2(32f, 0f);
        text.fontSize = 29;
        text.fontStyle = FontStyle.Bold;
        Style(button, primary);
        button.onClick.AddListener(action);
        return button;
    }
}
