Shader "PolyCar/HarborWater"
{
    Properties { _BaseColor("Water", Color)=(0.045,0.24,0.31,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A { float4 p:POSITION; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; };
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target
            {
                float2 p=i.world.xz;float time=_Time.y*0.35;
                float a=sin(p.x*0.55+p.y*0.31+time);
                float b=sin(p.y*1.3-p.x*0.17-time*0.6);
                float glint=smoothstep(0.94,1,a)*smoothstep(0.25,0.95,b);
                half3 color=_BaseColor.rgb*(0.93+0.07*a)+glint*half3(0.10,0.16,0.16);
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
