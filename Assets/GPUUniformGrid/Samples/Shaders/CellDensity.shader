Shader "Unlit/CellDensity" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
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
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/GPUUniformGrid/ShaderLIbrary/UniformGrid.hlsl"

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Scale;
            CBUFFER_END

            v2f vert (uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID) {
                uint cellCount = UniformGrid_cellCount;
                uint3 cellIndex = float3(instanceID, instanceID, instanceID) 
                    / float3(1, cellCount, cellCount * cellCount);
                cellIndex %= cellCount;
                uint cellID = MortonCode_Encode3(cellIndex);

                uint elementID = UniformGrid_GetHeadElementID(cellID);
                uint elementCount = 0;
                for (uint i = 0; i < 4; i++) {
                    if (elementID == UniformGrid_InitValue) break;
                    elementID = UniformGrid_GetNextElementID(elementID);
                    elementCount++;
                }
                
                float t = saturate((float)elementCount / 4.0);
                float3 pos = UniformGrid_cellOffset + UniformGrid_cellSize * (cellIndex + float3(0, 0.95, 0));
                pos.x += vertexID * t * UniformGrid_cellSize.x;

                v2f o;
                o.vertex = mul(UNITY_MATRIX_VP, float4(pos, 1));
                o.color = _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return i.color;
            }
            ENDCG
        }
    }
}
