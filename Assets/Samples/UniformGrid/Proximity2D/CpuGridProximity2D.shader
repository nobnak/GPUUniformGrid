Shader "Unlit/CpuGridProximity2D" {
    Properties {
        [HDR] _BaseColor ("Base Color", Color) = (0.15, 0.15, 0.2, 1)
        [HDR] _HighlightColor ("Highlight (within R)", Color) = (1, 0.35, 0.1, 1)
        _ProximityRadius ("Proximity R (plane, world)", Range(0.05, 8)) = 1.5
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
            #include "Assets/Samples/UniformGrid/Include/CpuProximityQuery2D.hlsl"

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

            // サンプル: グリッド論理座標 = ワールド XY。平面の埋め込みを変える場合は利用側で座標を変換すること。
            float4 frag(v2f i) : SV_Target {
                float minSq;
                GetNearestPointSqDistanceAtPosition2D(i.positionWS.xy, _ProximityRadius, minSq);
                float r2 = _ProximityRadius * _ProximityRadius;
                bool inside = minSq <= r2;
                return inside ? _HighlightColor : _BaseColor;
            }
            ENDHLSL
        }
    }
}
