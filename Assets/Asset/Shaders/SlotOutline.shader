Shader "Custom/SlotOutline"
{
    Properties
    {
        _MainTex      ("Sprite Texture", 2D)      = "white" {}
        _OutlineColor ("Outline Color", Color)    = (0.2, 1, 0.2, 0.85)
        _OutlineWidth ("Outline Width (px)", Range(1, 12)) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // auto-set by Unity: (1/w, 1/h, w, h)

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv   = IN.uv;
                float  self = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;

                // Interior of the sprite: render fully transparent (no fill)
                if (self > 0.1)
                    return half4(0, 0, 0, 0);

                // Sample 4 cardinal neighbors; if any are inside the sprite we are on the edge
                float2 step = _MainTex_TexelSize.xy * _OutlineWidth;
                float edge = 0;
                edge += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( step.x,  0     )).a;
                edge += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-step.x,  0     )).a;
                edge += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0,        step.y)).a;
                edge += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0,       -step.y)).a;

                if (edge > 0)
                    return half4(_OutlineColor.rgb, _OutlineColor.a);

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
