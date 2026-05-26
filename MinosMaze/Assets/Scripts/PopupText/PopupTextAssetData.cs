using UnityEngine;

namespace UI.PopupText
{
    [CreateAssetMenu(fileName = "New PopupTextData", menuName = "PopupText/PopupTextData")]
    public class PopupTextAssetData : ScriptableObject
    {
        public Color fontColor; // �����л�
        [Range(4, 7)] public float fontSize; // ���� 4 ���ʲ��и���

        /// <summary>
        /// ������С: 8֡, 0.13s
        /// �˶�����: 40֡, 0.66s
        /// ����: �˶����28֡, ��0.33s
        /// </summary>
        public AnimationCurve scaleCurve; // �����л�
        public AnimationCurve verticalCurve; // �����л�
        public AnimationCurve horizontalCurve; // �����л�
        public AnimationCurve alphaCurve; // �����л�
        public Sprite icon; // �����л�

        public float EndTime
        {
            get
            {
                float scaleEnd = scaleCurve != null && scaleCurve.keys.Length > 0 ? scaleCurve.keys[^1].time : 0f;
                float horizEnd = horizontalCurve != null && horizontalCurve.keys.Length > 0 ? horizontalCurve.keys[^1].time : 0f;
                return scaleEnd + horizEnd;
            }
        }

        public float EvaluateScale(float time)
        {
            if (scaleCurve == null)
                return 1;
            return scaleCurve.Evaluate(time);
        }

        public float EvaluateVertical(float time)
        {
            if (verticalCurve == null || verticalCurve.keys.Length == 0)
                return 0;

            if (scaleCurve == null || scaleCurve.keys.Length == 0)
                return verticalCurve.Evaluate(time);

            float scaleEnd = scaleCurve.keys[^1].time;
            if (time < scaleEnd)
                return 0;
            return verticalCurve.Evaluate(time - scaleEnd);
        }

        public float EvaluateHorizontal(float time)
        {
            if (horizontalCurve == null || horizontalCurve.keys.Length == 0)
                return 0;

            if (scaleCurve == null || scaleCurve.keys.Length == 0)
                return horizontalCurve.Evaluate(time);

            float scaleEnd = scaleCurve.keys[^1].time;
            if (time < scaleEnd)
                return 0;
            return horizontalCurve.Evaluate(time - scaleEnd);
        }

        /// <summary>
        /// ������ʱ����verticalCurveһ��ʼ
        /// </summary>
        public float EvaluateAlpha(float time)
        {
            if (alphaCurve == null || alphaCurve.keys.Length == 0)
                return 1;

            if (scaleCurve == null || scaleCurve.keys.Length == 0)
                return Mathf.Clamp(alphaCurve.Evaluate(time), 0, 1);

            float scaleEnd = scaleCurve.keys[^1].time;
            if (time < scaleEnd)
                return 1;
            return Mathf.Clamp(alphaCurve.Evaluate(time - scaleEnd), 0, 1);
        }
    }
}