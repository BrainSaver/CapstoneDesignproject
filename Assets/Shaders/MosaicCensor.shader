Shader "ExorcistStriker/MosaicCensor"
{
    Properties
    {
        _MainTex      ("Sprite Texture", 2D)    = "white" {}
        _MosaicSize   ("Mosaic Block Size",  Range(2, 64)) = 16
        _Progress     ("Censor Progress",    Range(0, 1))  = 0
        _CensorColor  ("Censor Tint",        Color)        = (0.08, 0.08, 0.08, 1)
        _StampTex     ("CENSORED Stamp",     2D)           = "black" {}
        _StampOpacity ("Stamp Opacity",      Range(0, 1))  = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MosaicPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_StampTex); SAMPLER(sampler_StampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float  _MosaicSize;
                float  _Progress;
                float4 _CensorColor;
                float4 _StampTex_ST;
                float  _StampOpacity;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // _Progress 0=정상, 1=완전 모자이크
                float doMosaic = step(0.01, _Progress);

                // 모자이크 UV: 블록 크기로 스냅
                float2 blockUV = IN.uv;
                if (doMosaic > 0.5)
                {
                    float2 texSize  = float2(1.0, 1.0) / _MainTex_TexelSize.xy;
                    float  blockPx  = max(2.0, _MosaicSize * _Progress);
                    float2 snapped  = floor(IN.uv * texSize / blockPx) * blockPx / texSize;
                    blockUV = lerp(IN.uv, snapped, _Progress);
                }

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, blockUV) * IN.color;

                // 검열 색 혼합
                col.rgb = lerp(col.rgb, _CensorColor.rgb, _Progress * 0.6);

                // CENSORED 스탬프 오버레이
                half4 stamp = SAMPLE_TEXTURE2D(_StampTex, sampler_StampTex, IN.uv);
                col.rgb = lerp(col.rgb, stamp.rgb, stamp.a * _StampOpacity);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Sprites/Lit"
}
