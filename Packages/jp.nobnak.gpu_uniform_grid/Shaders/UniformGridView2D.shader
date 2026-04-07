Shader "Unlit/UniformGridView2D" {
    Properties {
        _Color ("Color", Color) = (0,1,0.5,1)
        _ColorOverflow ("Color Overflow", Color) = (1,0,0,0)
        _Scale ("Cell outline scale", Float) = 0.95
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZTest LEqual

        Pass {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/UniformGrid2D.hlsl"

            static const float2 rect_v[] = {
                float2(0, 0), float2(1, 0), float2(1, 1), float2(0, 1)
            };
            static const uint rect_e[] = { 0, 1, 1, 2, 2, 3, 3, 0 };

            struct v2g {
                uint3 cell : TEXCOORD0;
            };
            struct g2f {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _ColorOverflow;
            float _Scale;
            CBUFFER_END

            v2g vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID) {
                uint n = UniformGrid2D_cellCount;
                uint2 cellIndex = uint2(instanceID % n, instanceID / n);

                uint cellID = MortonCode_Encode2(cellIndex);
                uint elementID = UniformGrid2D_GetHeadElementID(cellID);
                uint elementCount = 0;
                for (uint i = 0; i < 8; i++) {
                    if (elementID == UniformGrid2D_InitValue) break;
                    elementID = UniformGrid2D_GetNextElementID(elementID);
                    elementCount++;
                }

                v2g o;
                o.cell = uint3(cellIndex, elementCount);
                return o;
            }

            [maxvertexcount(8)]
            void geom(point v2g i[1], inout LineStream<g2f> stream) {
                uint2 cellIndex = i[0].cell.xy;
                uint elementCount = i[0].cell.z;
                if (elementCount <= 0) return;

                float2 cs = UniformGrid2D_cellSize.xy;
                float3 basePos = float3(UniformGrid2D_cellOffset.xy, UniformGrid2D_cellOffset.z);

                for (uint e = 0; e < 8; e++) {
                    float2 u = rect_v[rect_e[e]];
                    float2 corner = (cellIndex + 0.5 + _Scale * (u - 0.5)) * cs;
                    float3 pos = basePos + float3(corner.x, corner.y, 0);

                    g2f o;
                    o.vertex = mul(UNITY_MATRIX_VP, float4(pos, 1));
                    o.color = (elementCount > 4) ? _ColorOverflow : _Color;
                    stream.Append(o);
                    if (e % 2 == 1) stream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target {
                return i.color;
            }
            ENDCG
        }
    }
}
