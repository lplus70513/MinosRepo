Shader "Custom/ProceduralSmokeBackground"
{
    Properties
    {
        _Color ("颜色", Color) = (0, 0, 0, 1)
        _Opacity ("整体不透明度", Range(0, 1)) = 0.6
        _NoiseScale ("噪声缩放", Range(1, 20)) = 5
        _FlowSpeed ("流动速度", Range(0, 1)) = 0.15
        _FlowAngle ("流动角度", Range(0, 6.28318)) = 0.785
        _Distortion ("扭曲强度", Range(0, 0.5)) = 0.1
        _EdgeSoftness ("边缘柔和度", Range(0, 1)) = 0.4
        _VignettePower ("暗角强度", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Opacity;
            float _NoiseScale;
            float _FlowSpeed;
            float _FlowAngle;
            float _Distortion;
            float _EdgeSoftness;
            float _VignettePower;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float value = 0;
                float amp = 0.5;
                float freq = 1.0;

                value += amp * noise(p * freq);
                freq *= 2.0;
                amp *= 0.5;

                value += amp * noise(p * freq + float2(1.7, 9.2));
                freq *= 2.0;
                amp *= 0.5;

                value += amp * noise(p * freq + float2(8.3, 2.8));

                return value;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float2 flowDir = float2(cos(_FlowAngle), sin(_FlowAngle));
                float time = _Time.y * _FlowSpeed;

                float2 distortSampleUV = uv * _NoiseScale * 0.4 + flowDir * time * 0.6;
                float d1 = noise(distortSampleUV);
                float d2 = noise(distortSampleUV + float2(5.2, 1.3));
                float distort = (d1 * 0.6 + d2 * 0.4 - 0.5) * _Distortion;

                float2 mainUV = (uv + distort) * _NoiseScale + flowDir * time * 0.3;
                float n = fbm(mainUV);

                float2 edgeDist = abs(uv - 0.5) * 2.0;
                float vignette = 1.0 - smoothstep(0.25, 0.85, max(edgeDist.x, edgeDist.y)) * _VignettePower;

                float alpha = smoothstep(0, _EdgeSoftness, n) * _Opacity * vignette;

                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
