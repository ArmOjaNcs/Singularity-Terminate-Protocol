using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using UpgradeConfigs;

public class UpgradeItem : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color _newLevelTextColor;
    [SerializeField] private Color _defaultLevelTextColor = Color.white;

    [Header("UIElements")]
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private TextMeshProUGUI _levelText;

    public void Setup(UpgradeState item)
    {
        UpgradeConfig config = item.Config;
        bool isNew = item.CurrentLevel == 0;
        int newLevel = item.CurrentLevel += 1;

        _image.sprite = config.Icon;
        _name.text = config.Title;
        _description.text = config.Description;

        _levelText.color = isNew ? _newLevelTextColor : _defaultLevelTextColor;
        _levelText.text = isNew ? "Новое!" : $"Уровень{newLevel}";
    }
}
