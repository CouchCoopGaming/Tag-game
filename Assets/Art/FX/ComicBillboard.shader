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
        _Life ("Life", Float) = 1
        _WordPunch ("Word Punch", Float) = 1
        _WideOverTall ("Wide Over Tall", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
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
            ZTest [_ZTest]
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
                float _Life;
                float _WordPunch;
                float _WideOverTall;
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

            // Clip y spans 2, so a half of 0.30 is 30% of the pane.
            // The spikes fill 344/512 of the cell. Screen size follows Scale,
            // and the 1.15 overshoot is locked at 30% of the pane.
            #define INK_FRAC 0.671875
            #define PANE_PEAK 0.30
            #define LIFE_PEAK 1.15

            float ScreenHalf(float life)
            {
                float visible = PANE_PEAK * (life / LIFE_PEAK);
                if (visible < 0.0) visible = 0.0;
                if (visible > PANE_PEAK) visible = PANE_PEAK;
                return visible / INK_FRAC;
            }

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
                float3 center = origin + shift;
                float life = _Life;
                if (life < 0.0) life = 0.0;
                // World size at this life, grown only when that would miss the screen target.
                float boost = 1.0;
                if (_RestHalf > 0.001 && life > 0.0001)
                {
                    float4 restClip = TransformWorldToHClip(center + up * _RestHalf);
                    float4 restCenter = TransformWorldToHClip(center);
                    if (restClip.w > 0.0001 && restCenter.w > 0.0001)
                    {
                        float halfNdc = abs(restClip.y / restClip.w - restCenter.y / restCenter.w);
                        float worldNdc = halfNdc * life;
                        float desired = ScreenHalf(life);
                        if (worldNdc > 0.0001 && worldNdc < desired)
                            boost = desired / worldNdc;
                    }
                }
                float punch = _WordPunch;
                if (punch < 1.0) punch = 1.0;
                float aspect = _WideOverTall;
                if (aspect < 0.2) aspect = 1.0;
                float s = sin(_Tilt);
                float c = cos(_Tilt);
                // Pad from the drawn word, including tilt, so the nudge matches every quad.
                float hy = _RestHalf * life * boost * punch;
                float hx = hy * aspect;
                float padX = abs(c) * hx + abs(s) * hy + abs(_Skew) * hy;
                float padY = abs(s) * hx + abs(c) * hy + abs(_Arc) * hy;
                float2 nudge = float2(0.0, 0.0);
                float4 centerClip = TransformWorldToHClip(center);
                if (_RestHalf > 0.001 && centerClip.w > 0.0001 && hy > 0.001)
                {
                    float3 corner = center + right * padX + up * padY;
                    float4 cornerClip = TransformWorldToHClip(corner);
                    float2 cNdc = centerClip.xy / centerClip.w;
                    float2 eNdc = cornerClip.xy / max(cornerClip.w, 0.0001);
                    float2 pad = abs(eNdc - cNdc) + 0.03;
                    float2 limit = max(float2(1.0, 1.0) - pad, float2(0.0, 0.0));
                    nudge = clamp(cNdc, -limit, limit) - cNdc;
                }
                if (_Tail > 0.5)
                {
                    float along = input.positionOS.y;
                    float across = input.positionOS.x;
                    float jag = input.positionOS.z;
                    float3 pos = origin + shift * along + right * (across * _TailWidth + jag);
                    float4 tailClip = TransformWorldToHClip(pos);
                    // The hit stays put. The tip follows the burst inward.
                    tailClip.xy += nudge * along * tailClip.w;
                    tailClip.z -= _Front * tailClip.w;
                    output.positionCS = tailClip;
                    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                    return output;
                }
                float3x3 objectToWorld = (float3x3)GetObjectToWorldMatrix();
                float sx = length(objectToWorld._m00_m10_m20);
                float sy = length(objectToWorld._m01_m11_m21);
                float2 p = input.positionOS.xy;
                // Italic lean, then a small arc so the line is not a flat stamp.
                p.x += p.y * _Skew;
                p.y += p.x * p.x * _Arc;
                float2 spun = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                float3 world = center + right * spun.x * sx * boost + up * spun.y * sy * boost;
                float4 clip = TransformWorldToHClip(world);
                if (clip.w > 0.0001)
                    clip.xy += nudge * clip.w;
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
