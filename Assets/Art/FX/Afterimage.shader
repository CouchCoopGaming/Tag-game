Shader "Tag/Afterimage"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.3, 0.3, 1)
        _Fade ("Fade", Float) = 1
        _Fresnel ("Fresnel", Float) = 1.6
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
                float4 _Color;
                float _Fade;
                float _Fresnel;
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
                float3 world = center + right * input.positionOS.x * sx + up * input.positionOS.y * sy;
                output.positionCS = TransformWorldToHClip(world);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float r = length(p);
                float rim = pow(saturate(r), max(_Fresnel, 0.2));
                float body = saturate(1.0 - r);
                float mask = saturate(rim * 0.85 + body * 0.22);
                mask *= saturate(1.15 - r);
                float a = mask * _Color.a * saturate(_Fade);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
