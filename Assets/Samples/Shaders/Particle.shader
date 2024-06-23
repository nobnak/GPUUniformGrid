Shader "Unlit/Particle" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        [HDR] _ColorDense ("Color Dense", Color) = (1,1,1,1)
        _Distance ("Distance", Range(0, 10)) = 1
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Samples/ShaderGraphLibrary/ParticleDensity.hlsl"

            struct appdata {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 positionHCS : SV_POSITION;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ColorDense;
            float _Distance;
            CBUFFER_END

            v2f vert (appdata v) {
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                int count = 0;
                GetParticleDensity(positionWS, _Distance, count);
                float t = smoothstep(1, 200, count);
                float4 c = v.color * lerp(_Color, _ColorDense, smoothstep(0.3, 0.7, t));

                v2f o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = c;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return col * i.color;
            }
            ENDHLSL
        }
    }
}
