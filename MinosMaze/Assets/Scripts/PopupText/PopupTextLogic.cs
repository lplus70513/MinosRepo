using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace UI.PopupText
{
    public class PopupTextLogic : MonoBehaviour
    {
        [SerializeField] private TMP_Text textDisplay;

        private Action<PopupTextLogic> onComplete;
        private PopupTextAssetData assetData;
        private Tween tween;
        private Vector3 startWorldPos;
        private int toRight;

        private static Camera camera3D;

        void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer("Combatant");

            if (textDisplay == null)
                textDisplay = GetComponent<TMP_Text>();

            if (textDisplay != null)
            {
                Shader overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
                if (overlayShader != null)
                    textDisplay.fontMaterial.shader = overlayShader;
            }

            if (camera3D == null)
            {
                var camObj = GameObject.FindGameObjectWithTag("3D Camera");
                if (camObj != null) camera3D = camObj.GetComponent<Camera>();
            }

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 32767;
                renderer.material.renderQueue = 5000;
            }
        }

        public void Show(PopupTextData data, Action<PopupTextLogic> callback)
        {
            onComplete = callback;
            assetData = data.AssetData;
            startWorldPos = data.Position;
            toRight = data.ToRight;

            gameObject.SetActive(true);
            textDisplay.text = data.Text;
            textDisplay.color = assetData.fontColor;
            textDisplay.fontSize = assetData.fontSize;

            PlayAnimation();
        }

        private void PlayAnimation()
        {
            tween?.Kill();

            float endTime = assetData.EndTime;
            float elapsed = 0;

            tween = DOTween.To(() => elapsed, t =>
            {
                elapsed = t;

                float scale = assetData.EvaluateScale(t);
                float vert = assetData.EvaluateVertical(t);
                float horiz = assetData.EvaluateHorizontal(t) * toRight;
                float alpha = assetData.EvaluateAlpha(t);

                transform.localScale = Vector3.one * scale;
                transform.position = startWorldPos + new Vector3(horiz, vert, 0);

                Color c = textDisplay.color;
                c.a = alpha;
                textDisplay.color = c;
            }, endTime, endTime).SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                onComplete?.Invoke(this);
            });
        }

        void LateUpdate()
        {
            if (camera3D != null)
                transform.rotation = camera3D.transform.rotation;
        }

        void OnDestroy()
        {
            tween?.Kill();
        }
    }
}
