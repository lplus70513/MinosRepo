Shader "Custom/CardBurn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // ── 消融参数 ──
        _BurnAmount ("Burn Amount", Range(0, 1)) = 0
        _LineWidth ("Burn Line Width", Range(0, 0.2)) = 0.03
        _BurnFirstColor ("Burn Edge (Outer)", Color) = (1, 0.85, 0, 1)   // 亮黄/白
        _BurnSecondColor ("Burn Edge (Inner)", Color) = (1, 0.3, 0, 1)   // 橙红

        // ── 程序化噪点参数 ──
        _NoiseScale ("Noise Scale", Range(1, 20)) = 8
        _NoiseOctaves ("Noise Octaves", Range(1, 4)) = 3
        _NoiseSeed ("Noise Seed", Float) = 0

        // ── Sprite 兼容 ──
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BurnFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _BurnAmount;
            float _LineWidth;
            fixed4 _BurnFirstColor;
            fixed4 _BurnSecondColor;
            float _NoiseScale;
            int _NoiseOctaves;
            float _NoiseSeed;

            // ── 程序化噪点函数（无需纹理贴图）──

            // 二维哈希：输入整数格点坐标，输出 0~1 伪随机值
            float hash2D(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h) * 43758.5453123);
            }

            // 值噪声：在格点间 smoothstep 插值，产生连续平滑噪声
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // Hermite 平滑：f = 3t² - 2t³
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(hash2D(i + float2(0, 0)), hash2D(i + float2(1, 0)), u.x),
                    lerp(hash2D(i + float2(0, 1)), hash2D(i + float2(1, 1)), u.x),
                    u.y);
            }

            // 分形布朗运动（FBM）：叠加多倍频噪点，产生自然的消融边缘
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                float2 shift = float2(_NoiseSeed * 12.9898, _NoiseSeed * 78.233);

                for (int i = 0; i < 4; i++)
                {
                    if (i >= octaves) break;
                    value += amplitude * valueNoise(p * frequency + shift * i);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            fixed4 BurnFrag(v2f IN) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;

                // 程序化生成噪点值，无需 _BurnMap 纹理
                float burn = fbm(IN.texcoord * _NoiseScale, _NoiseOctaves);

                // ── 像素裁切：噪点值低于消融阈值则丢弃 ──
                clip(burn - _BurnAmount);

                // ── 燃烧边缘发光 ──
                // edge: 1.0 在裁切边界处, 0.0 在 LineWidth 距离之外
                float edge = 1.0 - smoothstep(_BurnAmount, _BurnAmount + _LineWidth, burn);

                if (edge > 0.001)
                {
                    // inner: 在边缘内侧产生双色渐变（外层亮黄 -> 内层橙红）
                    float inner = 1.0 - smoothstep(_BurnAmount + _LineWidth * 0.3, _BurnAmount + _LineWidth, burn);
                    fixed4 burnColor = lerp(_BurnFirstColor, _BurnSecondColor, inner);

                    // 烧蚀区域叠加自发光，模拟火焰照亮纸牌边缘
                    col.rgb = lerp(col.rgb, burnColor.rgb, edge);
                    // 燃烧边缘也增加亮度
                    col.rgb += burnColor.rgb * edge * 0.5;
                }

                return col;
            }
            ENDCG
        }
    }
}
