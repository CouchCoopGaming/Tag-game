Shader "Tag/ComicBillboard"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Tilt ("Tilt", Float) = 0
        _ClampExtent ("Clamp", Float) = 0
        _Front ("Front", Float) = 0
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
                float _ClampExtent;
                float _Front;
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
                float4 clip = TransformWorldToHClip(world);
                // Shift the whole quad in this camera so the word stays inside the pane.
                float4 centerClip = TransformWorldToHClip(center);
                if (_ClampExtent > 0.001 && clip.w > 0.0001 && centerClip.w > 0.0001)
                {
                    float3 corner = center + right * _ClampExtent + up * _ClampExtent;
                    float4 cornerClip = TransformWorldToHClip(corner);
                    float2 cNdc = centerClip.xy / centerClip.w;
                    float2 eNdc = cornerClip.xy / max(cornerClip.w, 0.0001);
                    float2 pad = abs(eNdc - cNdc);
                    // 0.03 keeps a sliver of the pane around the quad.
                    float2 limit = max(float2(1, 1) - pad - 0.03, float2(0, 0));
                    float2 shifted = clamp(cNdc, -limit, limit);
                    float2 ndc = clip.xy / clip.w + (shifted - cNdc);
                    clip.xy = ndc * clip.w;
                }
                clip.z -= _Front * clip.w;
                output.positionCS = clip;
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
