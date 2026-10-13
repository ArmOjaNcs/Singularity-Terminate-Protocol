using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade Config", menuName = "Game/Experience/Upgrade Config")]
public class UpgradeConfig : ScriptableObject
{
    public string UpgradeId;
    public string Title;
    [TextArea(3, 5)]
    public string Description;
    public Sprite Icon;
    public int MaxLevel = 5;

    public List<UpgradeConfig> RequiredUpgradeIds;
}
