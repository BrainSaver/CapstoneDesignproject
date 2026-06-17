Shader "ExorcistStriker/FistEnchant"
{
    Properties
    {
        _BaseMap    ("Base Texture", 2D)      = "white" {}
        _BaseColor  ("Base Color",   Color)   = (1,1,1,1)
        _EnchantType("Enchant Type", Float)   = 0   // 0=None 1=Salt 2=HolyWater 3=SilverMelt

        // 소금 (흰 파티클 느낌)
        _SaltColor      ("Salt Color",       Color) = (0.9, 0.95, 1.0, 1)
        _SaltIntensity  ("Salt Intensity",   Range(0,2)) = 1.2

        // 성수 (푸른 물결)
        _WaterColor     ("Water Color",      Color) = (0.2, 0.6, 1.0, 1)
        _WaterIntensity ("Water Intensity",  Range(0,2)) = 1.0
        _WaterSpeed     ("Water Speed",      Float) = 1.5

        // 은도금 쇳물 (황금 림라이트)
        _SilverColor    ("Silver Color",     Color) = (1.0, 0.85, 0.2, 1)
        _SilverRimPow   ("Silver Rim Power", Range(1,8)) = 3.0
        _SilverIntensity("Silver Intensity", Range(0,3)) = 1.8

        // 공통
        _EmissionStrength("Emission Strength", Range(0,5)) = 1.0
        _PulseSpeed      ("Pulse Speed",       Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _EnchantType;
                float4 _SaltColor;       float  _SaltIntensity;
                float4 _WaterColor;      float  _WaterIntensity;  float _WaterSpeed;
                float4 _SilverColor;     float  _SilverRimPow;    float _SilverIntensity;
                float  _EmissionStrength;
                float  _PulseSpeed;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float3 normOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normWS : TEXCOORD1; float3 viewWS : TEXCOORD2; float fogFactor : TEXCOORD3; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posCS   = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv      = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normWS  = TransformObjectToWorldNormal(IN.normOS);
                OUT.viewWS  = normalize(GetWorldSpaceViewDir(TransformObjectToWorld(IN.posOS.xyz)));
                OUT.fogFactor = ComputeFogFactor(OUT.posCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseTex  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 albedo   = baseTex * _BaseColor;
                half3 emission = half3(0,0,0);

                float pulse = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5); // 0~1 펄스
                int   type  = (int)round(_EnchantType);

                // ── 소금 ──────────────────────────────────────
                if (type == 1)
                {
                    float sparkle = frac(sin(dot(IN.uv * 10.0, float2(127.1, 311.7))) * 43758.5) > 0.92 ? 1.0 : 0.0;
                    emission = _SaltColor.rgb * (_SaltIntensity * (0.7 + pulse * 0.3)) * sparkle * _EmissionStrength;
                }
                // ── 성수 ──────────────────────────────────────
                else if (type == 2)
                {
                    float wave = sin(IN.uv.y * 12.0 + _Time.y * _WaterSpeed) * 0.5 + 0.5;
                    emission = _WaterColor.rgb * wave * _WaterIntensity * _EmissionStrength;
                }
                // ── 은도금 쇳물 ───────────────────────────────
                else if (type == 3)
                {
                    float rim = 1.0 - saturate(dot(IN.normWS, IN.viewWS));
                    rim = pow(rim, _SilverRimPow);
                    emission = _SilverColor.rgb * rim * _SilverIntensity * (0.6 + pulse * 0.4) * _EmissionStrength;
                }

                half3 finalColor = albedo.rgb + emission;
                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
