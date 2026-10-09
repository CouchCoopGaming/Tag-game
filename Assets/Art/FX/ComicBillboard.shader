Shader "Tag/ComicBillboard"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Tilt ("Tilt", Float) = 0
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Tilt;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 center = TransformObjectToWorld(float3(0, 0, 0));
                float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                float3x3 objectToWorld = (float3x3)GetObjectToWorldMatrix();
                float sx = length(objectToWorld._m00_m10_m20);
                float sy = length(objectToWorld._m01_m11_m21);
                float s = sin(_Tilt);
                float c = cos(_Tilt);
                float2 p = input.positionOS.xy;
                float2 spun = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                float3 world = center + right * spun.x * sx + up * spun.y * sy;
                output.positionCS = TransformWorldToHClip(world);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return tex * _Color;
            }
            ENDHLSL
        }
    }
}
