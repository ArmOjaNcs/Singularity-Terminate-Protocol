using Core.Services;
using ECS.PlayerComponents;
using Scellecs.Morpeh;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using UpgradeConfigs;
using Zenject;

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
    [SerializeField] private Button _button;

    private UpgradeConfig _config;
    private World _world;
    private UIService _uiService;

    [Inject]
    public void Construct(World world, UIService uiService)
    {
        _world = world;
        _uiService = uiService;
    }

    public void Setup(UpgradeState item)
    {
        UpgradeConfig config = item.Config;
        bool isNew = item.CurrentLevel == 0;
        int newLevel = item.CurrentLevel += 1;

        _config = config;

        _image.sprite = config.Icon;
        _name.text = config.Title;
        _description.text = config.Description;

        _levelText.color = isNew ? _newLevelTextColor : _defaultLevelTextColor;
        _levelText.text = isNew ? "Новое!" : $"Уровень{newLevel}";

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        Entity eventEntity = _world.CreateEntity();
        var upgradeStash = _world.GetStash<UpgradeSelectedEvent>();
        ref var selectEvent = ref upgradeStash.Add(eventEntity);

        selectEvent.SelectedUpgrade = _config;

        _uiService.HideLevelUpScreen();
    }
}
