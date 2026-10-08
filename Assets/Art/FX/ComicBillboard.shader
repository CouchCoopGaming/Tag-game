Shader "Tag/ComicBillboard"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Tilt ("Tilt", Float) = 0
        _Skew ("Skew", Float) = 0
        _Arc ("Arc", Float) = 0
        _ClampExtent ("Clamp", Float) = 0
        _Front ("Front", Float) = 0
        _RestHalf ("Rest Half", Float) = 0
        _UpDist ("Up", Float) = 0
        _SideDist ("Side", Float) = 0
        _Tail ("Tail", Float) = 0
        _TailWidth ("Tail Width", Float) = 0.04
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
                float _Skew;
                float _Arc;
                float _ClampExtent;
                float _Front;
                float _RestHalf;
                float _UpDist;
                float _SideDist;
                float _Tail;
                float _TailWidth;
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

            // 22% of the pane. Clip y spans 2, so the full quad is 0.44 and the half is 0.22.
            #define PANE_HALF_MIN 0.22

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                float side = 1.0;
                float4 originClip = TransformWorldToHClip(origin);
                if (originClip.w > 0.0001)
                {
                    // The side with more empty pane gets the burst.
                    side = (originClip.x / originClip.w) > 0.0 ? -1.0 : 1.0;
                }
                float3 shift = up * _UpDist + right * (side * _SideDist);
                if (_Tail > 0.5)
                {
                    float along = input.positionOS.y;
                    float across = input.positionOS.x;
                    float jag = input.positionOS.z;
                    float3 pos = origin + shift * along + right * (across * _TailWidth + jag);
                    float4 tailClip = TransformWorldToHClip(pos);
                    tailClip.z -= _Front * tailClip.w;
                    output.positionCS = tailClip;
                    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                    return output;
                }
                float3 center = origin + shift;
                float3x3 objectToWorld = (float3x3)GetObjectToWorldMatrix();
                float sx = length(objectToWorld._m00_m10_m20);
                float sy = length(objectToWorld._m01_m11_m21);
                // World size at rest, grown only when that would be under 22% of this pane.
                float boost = 1.0;
                if (_RestHalf > 0.001)
                {
                    float4 restClip = TransformWorldToHClip(center + up * _RestHalf);
                    float4 centerClip = TransformWorldToHClip(center);
                    if (restClip.w > 0.0001 && centerClip.w > 0.0001)
                    {
                        float halfNdc = abs(restClip.y / restClip.w - centerClip.y / centerClip.w);
                        if (halfNdc > 0.0001 && halfNdc < PANE_HALF_MIN)
                            boost = PANE_HALF_MIN / halfNdc;
                    }
                }
                float s = sin(_Tilt);
                float c = cos(_Tilt);
                float2 p = input.positionOS.xy;
                // Italic lean, then a small arc so the line is not a flat stamp.
                p.x += p.y * _Skew;
                p.y += p.x * p.x * _Arc;
                float2 spun = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                float3 world = center + right * spun.x * sx * boost + up * spun.y * sy * boost;
                float4 clip = TransformWorldToHClip(world);
                // Shift the whole quad in this camera so the word stays inside the pane.
                float4 centerClip = TransformWorldToHClip(center);
                if (_ClampExtent > 0.001 && clip.w > 0.0001 && centerClip.w > 0.0001)
                {
                    float3 corner = center + right * _ClampExtent * boost + up * _ClampExtent * boost;
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
