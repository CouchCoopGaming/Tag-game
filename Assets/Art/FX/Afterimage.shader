Shader "Tag/Afterimage"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.3, 0.3, 1)
        _Fade ("Fade", Float) = 1
        _Fresnel ("Fresnel", Float) = 1.6
        _Ink ("Ink", Float) = 0
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
                float _Ink;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = world;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(GetCameraPositionWS() - input.positionWS);
                float ndv = abs(dot(n, v));
                float fres = pow(saturate(1.0 - ndv), max(_Fresnel, 0.2));
                float body = 0.28 + 0.12 * ndv;
                float mask = saturate(body + fres * 0.9);
                float a = mask * _Color.a * saturate(_Fade);
                float3 rgb = _Color.rgb * (0.72 + 0.45 * fres);
                float edge = smoothstep(0.55, 1.0, fres);
                float ink = saturate(_Ink);
                rgb = lerp(rgb, float3(0.08, 0.07, 0.06), edge * ink);
                if (ink > 0.5)
                    a = lerp(a, 0.90 * saturate(_Fade), edge);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
