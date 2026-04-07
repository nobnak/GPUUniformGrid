Shader "Unlit/CpuGridProximity" {
    Properties {
        [HDR] _BaseColor ("Base Color", Color) = (0.15, 0.15, 0.2, 1)
        [HDR] _HighlightColor ("Highlight (within R)", Color) = (1, 0.35, 0.1, 1)
        _ProximityRadius ("Proximity R (world)", Range(0.05, 8)) = 1.5
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline" = "UniversalPipeline" }
        ZWrite On
        Cull Off

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Samples/ShaderGraphLibrary/CpuProximityQuery.hlsl"

            struct appdata {
                float4 positionOS : POSITION;
            };

            struct v2f {
                float3 positionWS : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _HighlightColor;
            float _ProximityRadius;
            CBUFFER_END

            v2f vert(appdata v) {
                v2f o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                float minSq;
                GetNearestPointSqDistanceAtPosition(i.positionWS, _ProximityRadius, minSq);
                float r2 = _ProximityRadius * _ProximityRadius;
                bool inside = minSq <= r2;
                return inside ? _HighlightColor : _BaseColor;
            }
            ENDHLSL
        }
    }
}
