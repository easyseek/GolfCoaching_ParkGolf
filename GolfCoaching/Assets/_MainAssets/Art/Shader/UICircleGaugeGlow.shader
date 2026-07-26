Shader "UI/CircleGaugeGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowSpread ("Glow Spread", Range(0.001, 0.08)) = 0.012
        _GlowPower ("Glow Power", Range(0, 4)) = 0.15
        _UVScale ("UV Scale", Range(1, 2)) = 1.45
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0
        _FillOrigin ("Fill Origin", Range(0, 3)) = 2.0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _GlowSpread;
            float _GlowPower;
            float _UVScale;
            float _FillAmount;
            float _FillOrigin;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed SampleAlpha(float2 uv)
            {
                float inside = step(0.0, uv.x) * step(uv.x, 1.0)
                    * step(0.0, uv.y) * step(uv.y, 1.0);
                float2 direction = uv - 0.5;
                float angle = atan2(direction.x, direction.y);
                float topProgress = frac(angle / 6.28318530718 + 1.0);
                float originProgress = frac((2.0 - _FillOrigin) * 0.25 + 1.0);
                float progress = frac(topProgress - originProgress + 1.0);
                float fillMask = step(progress, _FillAmount);
                return tex2D(_MainTex, saturate(uv)).a * inside * fillMask;
            }

            float GaussianWeight(int offset)
            {
                int distance = abs(offset);

                if (distance == 0)
                {
                    return 100.0;
                }

                if (distance == 1)
                {
                    return 97.0;
                }

                if (distance == 2)
                {
                    return 88.0;
                }

                if (distance == 3)
                {
                    return 75.0;
                }

                if (distance == 4)
                {
                    return 61.0;
                }

                if (distance == 5)
                {
                    return 46.0;
                }

                if (distance == 6)
                {
                    return 32.0;
                }

                if (distance == 7)
                {
                    return 22.0;
                }

                return 14.0;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 sourceUv = (input.texcoord - 0.5) * _UVScale + 0.5;
                fixed center = SampleAlpha(sourceUv);
                float blurredAlpha = 0.0;

                for (int y = -8; y <= 8; y++)
                {
                    float weightY = GaussianWeight(y);

                    for (int x = -8; x <= 8; x++)
                    {
                        float weight = GaussianWeight(x) * weightY;
                        float2 offset = float2(x, y) * _GlowSpread;
                        blurredAlpha += SampleAlpha(sourceUv + offset) * weight;
                    }
                }

                blurredAlpha /= 940900.0;

                fixed alpha = saturate(blurredAlpha - center * 0.55) * _GlowPower;
                return fixed4(input.color.rgb, alpha * input.color.a);
            }
            ENDCG
        }
    }
}
