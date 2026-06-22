Shader "Custom/ProceduralParticle"
{
    Properties
    {
        _Color ("颜色", Color) = (0.7, 0.7, 0.8, 1)
        _Opacity ("整体不透明度", Range(0, 1)) = 0.8
        _ParticleSize ("粒子大小", Range(0.001, 0.04)) = 0.006
        _CellDensity ("粒子密度", Range(3, 15)) = 7
        _FlowSpeed ("流动速度", Range(0, 0.5)) = 0.08
        _FlowAngle ("流动角度", Range(0, 6.28318)) = 2.356
        _FlowDistance ("流动距离", Range(0.1, 1)) = 0.5
        _LifetimeMin ("生命周期下限", Range(3, 10)) = 5
        _LifetimeMax ("生命周期上限", Range(5, 15)) = 10
        _PerpSpread ("横向扩散", Range(0, 0.3)) = 0.08
        _EdgeSoftness ("边缘柔和度", Range(0.1, 1.5)) = 0.4
        _VignettePower ("暗角强度", Range(0, 1)) = 0.3
        _AspectRatio ("宽高比", Float) = 1.777
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
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
            float _ParticleSize;
            float _CellDensity;
            float _FlowSpeed;
            float _FlowAngle;
            float _FlowDistance;
            float _LifetimeMin;
            float _LifetimeMax;
            float _PerpSpread;
            float _EdgeSoftness;
            float _VignettePower;
            float _AspectRatio;

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

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float2 hash22(float2 p)
            {
                float n = hash21(p);
                return float2(n, hash21(p + float2(0.587, 0.913)));
            }

            float particle(float2 uv, float2 cellIdx, float time, float density)
            {
                float2 rand = hash22(cellIdx);
                float2 cellCenter = (cellIdx + 0.5) / density;
                float2 localOffset = (rand - 0.5) / density;
                float2 basePos = cellCenter + localOffset;

                float lifetimeRand = hash21(cellIdx + 17.3);
                float lifetime = lerp(_LifetimeMin, _LifetimeMax, lifetimeRand);

                float phaseOffset = hash21(cellIdx + 42.1);
                float phase = frac(time / lifetime + phaseOffset);

                float2 flowDir = float2(cos(_FlowAngle), sin(_FlowAngle));
                float2 perpDir = float2(-flowDir.y, flowDir.x);
                float perpBias = perpDir * (rand.y - 0.5) * _PerpSpread * 2.0;

                float2 startOffset = -flowDir * _FlowDistance * 0.5;
                float2 pos = basePos + startOffset + (flowDir + perpBias) * phase * _FlowDistance;

                float fadeIn = smoothstep(0, 0.12, phase);
                float fadeOut = 1.0 - smoothstep(0.88, 1.0, phase);
                float fade = fadeIn * fadeOut;

                float2 d = float2((uv.x - pos.x) * _AspectRatio, uv.y - pos.y);
                float dist = length(d);
                float sizeVar = lerp(0.6, 1.5, hash21(cellIdx + 99.9));
                float radius = _ParticleSize * sizeVar;
                float innerRadius = radius * _EdgeSoftness * 0.5;

                return (1.0 - smoothstep(innerRadius, radius, dist)) * fade;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _FlowSpeed;
                float density = _CellDensity;

                float2 cellUV = uv * density;
                float2 baseCell = floor(cellUV);

                float alpha = 0;

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 cellIdx = baseCell + float2(x, y);
                        float p = particle(uv, cellIdx, time, density);
                        alpha = max(alpha, p);
                    }
                }

                float2 edgeDist = abs(uv - 0.5) * 2.0;
                float vignette = 1.0 - smoothstep(0.2, 0.9, max(edgeDist.x, edgeDist.y)) * _VignettePower;

                alpha = saturate(alpha) * _Opacity * vignette;

                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
