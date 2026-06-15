Shader "Custom/UIOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("描边颜色", Color) = (1,1,1,1)
        _OutlineWidth ("描边宽度", Float) = 2
        _EnableOutline ("启用描边", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _EnableOutline;

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                if (_EnableOutline > 0.5 && color.a < 0.1)
                {
                    float2 size = _MainTex_TexelSize.xy * _OutlineWidth;
                    float a = 0;

                    a = max(a, tex2D(_MainTex, i.texcoord + float2( size.x,  0      )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-size.x,  0      )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( 0,       size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( 0,      -size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( size.x,  size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-size.x,  size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( size.x, -size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-size.x, -size.y )).a);

                    float hx = size.x * 0.5;
                    float hy = size.y * 0.5;
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( hx,      size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-hx,      size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( hx,     -size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-hx,     -size.y )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( size.x,  hy     )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-size.x,  hy     )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( size.x, -hy     )).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-size.x, -hy     )).a);

                    if (a > 0.1)
                    {
                        color = _OutlineColor;
                    }
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
