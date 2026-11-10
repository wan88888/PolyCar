Shader "PolyCar/TireMarks"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv; o.color = v.color;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                // Trail UV.y spans the tire width; soften both edges and suggest tread grooves.
                half edge = smoothstep(0.0,0.22,i.uv.y) * (1.0-smoothstep(0.78,1.0,i.uv.y));
                half tread = 0.78 + 0.22 * sin(i.uv.y * 37.7) * sin(i.uv.y * 37.7);
                return half4(i.color.rgb,i.color.a * edge * tread);
            }
            ENDHLSL
        }
    }
}
