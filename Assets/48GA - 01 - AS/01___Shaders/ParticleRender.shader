Shader "Custom/ParticleRender"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 2.0
        _GlowIntensity ("Glow Intensity", Float) = 1.5
        _AttractorWeight ("Attractor Weight", Float) = 0.0
        _GlowMultiplier ("Glow Multiplier", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ParticlePass"
            Blend One One
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Particle
            {
                float2 position;
                float2 velocity;
                float4 color;
                float  life;
                float  mass;
                int    colorVariant;
                float  pad;
            };

            StructuredBuffer<Particle> _Particles;
            float  _PointSize;
            float  _GlowIntensity;
            float  _AttractorWeight;
            float  _GlowMultiplier;
            float2 _CustomScreenSize;
            float  _ChaosTemperature;

            struct Attributes { uint vertexID : SV_VertexID; };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                uint id = input.vertexID;
                Particle p = _Particles[id];

                float2 ndc = (p.position / _CustomScreenSize) * 2.0 - 1.0;
                ndc.y = -ndc.y;

                output.positionHCS = float4(ndc, 0, 1);
                output.uv = float2(0.5, 0.5);

                float speed     = length(p.velocity);
                float speedNorm = saturate(speed / 10.0);
                float4 base     = p.color;
                float  glow     = lerp(0.3, 1.2, _AttractorWeight);

                float4 chaosColor = float4(
                    base.r * (0.7 + speedNorm * 0.3),
                    base.g * (0.5 + speedNorm * 0.2),
                    base.b * (0.4 + speedNorm * 0.6),
                    1.0
                );
                float4 orderColor = float4(base.r, base.g * 0.8, base.b * 0.6, 1.0);
                float4 finalColor = lerp(chaosColor, orderColor, _AttractorWeight);
                finalColor *= glow * _GlowIntensity;

                float pulse = 0.7 + sin(p.life * 6.28318) * 0.3;
                finalColor *= pulse * _GlowMultiplier;
                finalColor.a = saturate(0.4 + _AttractorWeight * 0.6) * pulse * _GlowMultiplier;

                output.color = finalColor;
                return output;
            }

            float4 frag(Varyings input) : SV_Target { return input.color; }

            ENDHLSL
        }
    }
}
