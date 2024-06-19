Shader "Unlit/CellDensity" {
    Properties {
        _Color ("Color", Color) = (0,1,0,1)
        _ColorOverflow ("Color Overflow", Color) = (1,0,0,0)
        _Scale ("Scale", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZTest LEqual
        ZWrite On

        Pass {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/GPUUniformGrid/ShaderLIbrary/UniformGrid.hlsl"

            static const float3 box_vertices[] = {
                float3(0, 0, 0), 
                float3(1, 0, 0), 
                float3(0, 1, 0), 
                float3(1, 1, 0), 
                float3(0, 0, 1), 
                float3(1, 0, 1), 
                float3(0, 1, 1), 
                float3(1, 1, 1)
            };
            static const uint box_indices[] = {
                0, 1,
                1, 3,
                3, 2,
                2, 0,

                4, 5,
                5, 7,
                7, 6,
                6, 4,

                0, 4,
                1, 5,
                3, 7,
                2, 6
			};

            struct v2g {
                float4 vertex : SV_POSITION;
                uint4 cell : TEXCOORD0;
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

            v2g vert(uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID) {
                uint cellCount = UniformGrid_cellCount;
                uint3 cellIndex = float3(instanceID, instanceID, instanceID) 
                    / float3(1, cellCount, cellCount * cellCount);
                cellIndex %= cellCount;

                uint cellID = MortonCode_Encode3(cellIndex);
                uint elementID = UniformGrid_GetHeadElementID(cellID);
                uint elementCount = 0;
                for (uint i = 0; i < 8; i++) {
                    if (elementID == UniformGrid_InitValue) break;
                    elementID = UniformGrid_GetNextElementID(elementID);
                    elementCount++;
                }

                float3 pos = UniformGrid_cellOffset + UniformGrid_cellSize * cellIndex; 

                v2g o;
                o.vertex = float4(pos, 1);
                o.cell = uint4(cellIndex, elementCount);
                return o;
            }
            [maxvertexcount(24)]
            void geom(point v2g i[1], inout LineStream<g2f> stream) {
                v2g p = i[0];
                uint3 cellIndex = p.cell.xyz;
                uint elementCount = p.cell.w;
                if (elementCount <= 0) return;

                for (uint i = 0; i < 24; i++) {
                    float3 pos = UniformGrid_cellOffset + UniformGrid_cellSize 
                        * (cellIndex + box_vertices[box_indices[i]]);

				    g2f o;
				    o.vertex = mul(UNITY_MATRIX_VP, float4(pos, 1));
				    o.color = (elementCount > 4) ? _ColorOverflow : _Color;
				    stream.Append(o);
                    if (i % 2 == 1) stream.RestartStrip();
                }
			}

            fixed4 frag (g2f i) : SV_Target {
                return i.color;
            }
            ENDCG
        }
    }
}
