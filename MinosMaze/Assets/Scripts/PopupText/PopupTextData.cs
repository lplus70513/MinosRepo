using UnityEngine;

namespace UI.PopupText
{
    public class PopupTextData
    {
        public string Text { get; }
        public PopupTextAssetData AssetData { get; }
        public int DamageValue { get; private set; }
        public int ToRight { get; }
        public Vector3 Position { get; }

        public PopupTextData(Vector3 position, string text, PopupTextAssetData assetData, int toRight)
        {
            Position = position;
            Text = text;
            AssetData = assetData;
            ToRight = toRight;
        }

        public void SetDamageValue(int value)
        {
            DamageValue = value;
        }
    }
}
