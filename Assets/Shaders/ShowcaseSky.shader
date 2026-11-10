Shader "PolyCar/ShowcaseSky"
{
    Properties
    {
        _Zenith("Zenith", Color)=(0.1,0.38,0.62,1)
        _Horizon("Horizon", Color)=(0.65,0.78,0.8,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Zenith, _Horizon;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
            half4 Frag(Varyings i):SV_Target
            {
                float3 d=normalize(i.direction);float height=saturate(d.y);
                half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(height,0.45));
                float2 p=d.xz/max(0.15,d.y+0.12)*2.8;
                float cloud=Noise(p)*0.6+Noise(p*2.1)*0.28+Noise(p*4.2)*0.12;
                cloud=smoothstep(0.58,0.77,cloud)*smoothstep(0.05,0.25,height)*0.48;
                color=lerp(color,half3(0.94,0.94,0.90),cloud);
                float sun=pow(saturate(dot(d,normalize(float3(0.40,0.65,-0.64)))),600);
                color+=sun*half3(0.65,0.52,0.30);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
