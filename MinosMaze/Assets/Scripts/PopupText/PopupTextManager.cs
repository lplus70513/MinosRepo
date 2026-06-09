using System.Collections.Generic;
using UnityEngine;

namespace UI.PopupText
{
    public class PopupTextManager : Singleton<PopupTextManager>
    {
        [SerializeField] private PopupTextSetting popupTextSetting;
        [SerializeField] private PopupTextLogic popupTextPrefab;
        [SerializeField] private int poolSize = 10;

        private Queue<PopupTextLogic> pool = new();
        private Transform poolRoot;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            InitPool();
        }

        private void InitPool()
        {
            if (!ValidateSetup()) return;

            poolRoot = new GameObject("PopupTextPool").transform;
            poolRoot.SetParent(transform);

            for (int i = 0; i < poolSize; i++)
            {
                CreatePooled();
            }
        }

        private bool ValidateSetup()
        {
            if (popupTextPrefab == null)
            {
                Debug.LogWarning("[PopupTextManager] popupTextPrefab 未赋值");
                return false;
            }
            if (popupTextSetting == null)
            {
                Debug.LogWarning("[PopupTextManager] popupTextSetting 未赋值");
                return false;
            }
            return true;
        }

        private PopupTextLogic CreatePooled()
        {
            PopupTextLogic instance = Instantiate(popupTextPrefab, poolRoot);
            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
            return instance;
        }

        public void ShowDamageText(Transform actorTransform, int damageValue, PopupTextType textType, Vector3 hitVelocity = default)
        {
            if (actorTransform == null)
            {
                Debug.LogError("[PopupTextManager] actorTransform 为空，无法显示跳字");
                return;
            }
            if (!ValidateSetup()) return;

            var textAsset = textType switch
            {
                PopupTextType.Damage => popupTextSetting.damageTextAsset,
                PopupTextType.CriticalDamage => popupTextSetting.criticalDamageTextAsset,
                PopupTextType.Heal => popupTextSetting.healTextAsset,
                _ => null
            };

            if (textAsset == null)
            {
                Debug.LogWarning($"[PopupTextManager] 未找到 {textType} 对应的资源");
                return;
            }

            int toRight = hitVelocity == default ?
                (Random.Range(0, 1f) > 0.5f ? 1 : -1) :
                (hitVelocity.x >= 0 ? 1 : -1);

            var textData = new PopupTextData(actorTransform.position + Vector3.up * 5f, damageValue.ToString(), textAsset, toRight);
            textData.SetDamageValue(damageValue);

            var text = pool.Count > 0 ? pool.Dequeue() : CreatePooled();
            text.Show(textData, OnTextComplete);
        }

        public void ShowCommonText(Transform actorTransform, string content)
        {
            if (actorTransform == null)
            {
                Debug.LogError("[PopupTextManager] actorTransform 为空，无法显示跳字");
                return;
            }
            if (!ValidateSetup()) return;

            var textAsset = popupTextSetting.commonTextAsset;
            if (textAsset == null)
            {
                Debug.LogWarning("[PopupTextManager] commonTextAsset 未赋值");
                return;
            }

            var textData = new PopupTextData(actorTransform.position + Vector3.up * 5f, content, textAsset, toRight: 0);
            var text = pool.Count > 0 ? pool.Dequeue() : CreatePooled();
            text.Show(textData, OnTextComplete);
        }

        private void OnTextComplete(PopupTextLogic text)
        {
            text.gameObject.SetActive(false);
            pool.Enqueue(text);
        }
    }
}
