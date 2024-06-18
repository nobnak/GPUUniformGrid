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

            float4 _Color;
            float _Scale;

            v2f vert (uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID) {
                uint cellCount = UniformGrid_cellCount;
                uint3 cellID = float3(instanceID, instanceID, instanceID) 
                    / float3(1, cellCount, cellCount * cellCount);
                cellID %= cellCount;

                uint elementID = UniformGrid_GetHeadElementID(cellID);
                uint elementCount = 0;
                for (uint i = 0; i < 4; i++) {
                    if ((int)elementID < 0) break;
                    elementID = UniformGrid_GetNextElementID(elementID);
                    elementCount++;
                }

                //float3 pos = _Scale * float3(vertexID, instanceID, 0);

                float3 pos = UniformGrid_cellOffset + UniformGrid_cellSize * (cellID + 0.01);
                pos.x += vertexID * (elementCount / 5.0) * UniformGrid_cellSize.x;

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
