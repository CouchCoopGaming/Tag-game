Shader "Tag/FxMark"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Mode ("Mode", Float) = 0
        _Rim ("Rim", Float) = 0.82
        _Front ("Front", Float) = 0
        _Vtx ("Vertex color", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Mode;
                float _Rim;
                float _Front;
                float _Vtx;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 pos;
                if (_Mode < 0.5)
                {
                    pos = TransformObjectToWorld(input.positionOS);
                }
                else
                {
                    float3 center = TransformObjectToWorld(float3(0, 0, 0));
                    float3x3 m = (float3x3)GetObjectToWorldMatrix();
                    float sx = length(m._m00_m10_m20);
                    float sy = length(m._m01_m11_m21);
                    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                    float3 toCam = _WorldSpaceCameraPos - center;
                    float dist = length(toCam);
                    if (dist > 0.001)
                        center += (toCam / dist) * _Front;
                    pos = center + right * input.positionOS.x * sx + up * input.positionOS.y * sy;
                }
                output.positionCS = TransformWorldToHClip(pos);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_Mode > 1.5)
                {
                    float2 q = abs(input.uv - 0.5) * 2.0;
                    float box = max(q.x, q.y);
                    clip(0.98 - box);
                    half rim = box > _Rim ? 1.0 : 0.0;
                    half3 ink = rim > 0.5 ? half3(0.82, 0.86, 0.90) : half3(0.02, 0.02, 0.02);
                    return half4(ink, _BaseColor.a);
                }
                if (_Vtx > 0.5)
                {
                    if (input.color.a < 0.01) discard;
                    return input.color;
                }
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
