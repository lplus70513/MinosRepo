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
            textDisplay.color = Color.white;
            textDisplay.fontSize = 12;

            FaceCamera();
            PlayAnimation();
        }

        private void FaceCamera()
        {
            if (camera3D == null)
            {
                var camObj = GameObject.FindGameObjectWithTag("3D Camera");
                if (camObj != null) camera3D = camObj.GetComponent<Camera>();
            }
            if (camera3D != null)
                transform.rotation = camera3D.transform.rotation;
        }

        private void PlayAnimation()
        {
            tween?.Kill();

            float duration = assetData.motionDuration;
            float elapsed = 0;

            tween = DOTween.To(() => elapsed, t =>
            {
                elapsed = t;

                float nt = duration > 0 ? t / duration : 1f;

                float vert = assetData.parabolaHeight * nt * (1f - nt) * 4f;
                float horiz = assetData.parabolaHorizontalDistance * nt * toRight;

                transform.localScale = Vector3.one;
                transform.position = startWorldPos + new Vector3(horiz, vert, 0);

                Color c = textDisplay.color;
                c.a = 1f - nt;
                textDisplay.color = c;
            }, duration, duration).SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                onComplete?.Invoke(this);
            });
        }

        void LateUpdate()
        {
            FaceCamera();
        }

        void OnDestroy()
        {
            tween?.Kill();
        }
    }
}
