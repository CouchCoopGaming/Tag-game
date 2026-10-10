Shader "Tag/FxKitRim"
{
    Properties
    {
        _Color ("Color", Color) = (0.95, 0.28, 0.32, 0.7)
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
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

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posOS = v.positionOS + v.normalOS * 0.012;
                o.positionWS = TransformObjectToWorld(posOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 view = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fres = pow(saturate(1.0 - dot(n, view)), 2.2);
                float edge = smoothstep(0.55, 1.0, fres);
                float ink = saturate(_Ink);
                float3 rgb = lerp(_Color.rgb, float3(0.08, 0.07, 0.06), edge * ink);
                float a = _Color.a * fres;
                if (ink > 0.5)
                    a = max(a, edge * 0.90 * saturate(_Color.a));
                if (a < 0.02) discard;
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
