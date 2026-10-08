Shader "Tag/FxKitEdge"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Edge ("Edge", Float) = 0
        _Guard ("Guard", Float) = 1
        _Owner ("Owner", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Owner;
                float _Edge;
                float _Guard;
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

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 world = mul(unity_ObjectToWorld, float4(v.positionOS, 1)).xyz;
                o.positionCS = TransformWorldToHClip(world);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                if (_Guard > 0.5)
                {
                    if (distance(_WorldSpaceCameraPos, _Owner.xyz) > 0.35)
                        discard;
                }
                float edge = 1.0;
                if (_Edge < 0.5) edge = 1.0 - i.uv.x;
                else if (_Edge < 1.5) edge = i.uv.x;
                else if (_Edge < 2.5) edge = 1.0 - i.uv.y;
                else edge = i.uv.y;
                edge = smoothstep(0.0, 1.0, edge);
                float a = _Color.a * edge;
                if (a < 0.01) discard;
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
