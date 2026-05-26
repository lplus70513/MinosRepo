using UnityEngine;

namespace UI.PopupText
{
    [CreateAssetMenu(fileName = "PopupTextSetting", menuName = "PopupText/PopupTextSetting")]
    public class PopupTextSetting : ScriptableObject
    {
        public PopupTextAssetData damageTextAsset; // DamageText.asset
        public PopupTextAssetData healTextAsset; // HealText.asset
        public PopupTextAssetData criticalDamageTextAsset; // CriticalDamageText.asset
        public PopupTextAssetData commonTextAsset; // CommonText.asset
    }
}