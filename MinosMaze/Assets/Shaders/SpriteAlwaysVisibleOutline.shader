Shader "Custom/SpriteAlwaysVisibleOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Float) = 2
        [PerRendererData] _EnableOutline ("Enable Outline", Float) = 0
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
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _EnableOutline;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

                if (_EnableOutline > 0.5 && c.a < 0.1)
                {
                    float2 size = _MainTex_TexelSize.xy * _OutlineWidth;
                    float a = 0;

                    a = max(a, tex2D(_MainTex, i.uv + float2( size.x,  0      )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(-size.x,  0      )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2( 0,       size.y )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2( 0,      -size.y )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2( size.x,  size.y )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(-size.x,  size.y )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2( size.x, -size.y )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(-size.x, -size.y )).a);

                    float half_size_x = size.x * 0.5;
                    float half_size_y = size.y * 0.5;
                    a = max(a, tex2D(_MainTex, i.uv + float2( half_size_x,  size.y     )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(-half_size_x,  size.y     )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2( half_size_x, -size.y     )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(-half_size_x, -size.y     )).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2( size.x,       half_size_y)).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(-size.x,       half_size_y)).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2( size.x,      -half_size_y)).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(-size.x,      -half_size_y)).a);

                    if (a > 0.1)
                    {
                        c = _OutlineColor;
                    }
                }

                return c;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 texcoord : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vertShadow(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 c = tex2D(_MainTex, i.texcoord);
                clip(c.a - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
