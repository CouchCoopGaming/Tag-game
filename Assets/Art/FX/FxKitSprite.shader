Shader "Tag/FxKitSprite"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Shape ("Shape", Float) = 0
        _Billboard ("Billboard", Float) = 1
        _Guard ("Guard", Float) = 0
        _Edge ("Edge", Float) = -1
        _Owner ("Owner", Vector) = (0, 0, 0, 0)
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
                float4 _Owner;
                float _Shape;
                float _Billboard;
                float _Guard;
                float _Edge;
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
                float3 center = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float sx = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float sy = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));
                float3 world;
                if (_Billboard > 0.5)
                {
                    float3 right = normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(1, 0, 0)));
                    float3 up = normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(0, 1, 0)));
                    world = center + right * v.positionOS.x * sx + up * v.positionOS.y * sy;
                }
                else
                {
                    world = mul(unity_ObjectToWorld, float4(v.positionOS, 1)).xyz;
                }
                o.positionCS = TransformWorldToHClip(world);
                o.uv = v.uv;
                return o;
            }

            float StarMask(float2 p)
            {
                float r = length(p);
                float ang = atan2(p.y, p.x);
                float sector = 1.25663706;
                float u = abs(fmod(ang + 3.14159265, sector) - sector * 0.5);
                float rad = lerp(0.20, 0.98, saturate(1.0 - u / (sector * 0.5)));
                return saturate(1.0 - smoothstep(rad - 0.07, rad, r));
            }

            half4 frag(Varyings i) : SV_Target
            {
                if (_Guard > 0.5)
                {
                    if (distance(_WorldSpaceCameraPos, _Owner.xyz) > 0.35)
                        discard;
                }
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                float mask = 1.0;
                if (_Shape < 0.5)
                    mask = saturate(1.0 - smoothstep(0.05, 1.0, r));
                else if (_Shape < 1.5)
                {
                    float beam = saturate(1.0 - smoothstep(0.06, 0.22, abs(p.x)));
                    beam = max(beam, saturate(1.0 - smoothstep(0.06, 0.22, abs(p.y))));
                    float core = saturate(1.0 - smoothstep(0.0, 0.32, r));
                    mask = saturate(max(beam, core));
                }
                else if (_Shape < 2.5)
                    mask = StarMask(p);

                float edge = 1.0;
                if (_Edge >= 0.0 && _Edge < 0.5) edge = 1.0 - i.uv.x;
                else if (_Edge < 1.5) edge = i.uv.x;
                else if (_Edge < 2.5) edge = 1.0 - i.uv.y;
                else if (_Edge < 3.5) edge = i.uv.y;
                if (_Edge >= 0.0)
                    edge = smoothstep(0.0, 1.0, edge);

                float a = _Color.a * mask * edge;
                if (a < 0.01) discard;
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
