using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerksUI : MonoBehaviour
{
    [SerializeField] private PerkUI perkUIPrefab; // 建议检查这里是否引用的是 PerkUI 而不是 PerksUI

    private readonly List<PerkUI> perkUIs = new();

    // 修改点1：参数类型改为 Perk
    public void AddPerkUI(Perk perk)
    {
        PerkUI perkUI = Instantiate(perkUIPrefab, transform);
        perkUI.Setup(perk);
        perkUIs.Add(perkUI);
    }

    public void RemovePerkUI(Perk perk)
    {
        // 修改点2：声明类型改为 PerkUI
        PerkUI perkUI = perkUIs.Where(pui => pui.Perk == perk).FirstOrDefault();

        if (perkUI != null)
        {
            perkUIs.Remove(perkUI);
            // 修改点3：修正拼写 Destroy
            Destroy(perkUI.gameObject);
        }
    }
}