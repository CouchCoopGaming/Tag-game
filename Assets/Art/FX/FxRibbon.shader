Shader "Tag/FxRibbon"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.3, 0.3, 1)
        _Fade ("Fade", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Fade;
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
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float band = abs(input.uv.y - 0.5);
                float fade = saturate(_Fade);
                float limit = lerp(0.5, 0.0, fade);
                clip(limit - band);
                // Seat ink in the middle. A dark edge keeps it readable on brick.
                float core = limit * 0.62;
                half3 ink = band > core ? half3(0.08, 0.05, 0.04) : _Color.rgb;
                return half4(ink, 1);
            }
            ENDHLSL
        }
    }
}
