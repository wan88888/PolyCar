Shader "PolyCar/ThemeSky"
{
    Properties
    {
        _Zenith("Zenith", Color)=(0.1,0.38,0.62,1)
        _Horizon("Horizon", Color)=(0.65,0.78,0.8,1)
        _CloudColor("Cloud color", Color)=(0.94,0.94,0.9,1)
        _CloudStrength("Cloud strength", Float)=0.35
        _SunDirection("Sun direction", Vector)=(0.4,0.65,-0.64,0)
        _SunColor("Sun / moon", Color)=(1,0.82,0.55,1)
        _Stars("Stars", Float)=0
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
            half4 _Zenith,_Horizon,_CloudColor,_SunDirection,_SunColor;
            float _CloudStrength,_Stars;
            CBUFFER_END
            struct A { float4 p:POSITION; };
            struct V { float4 p:SV_POSITION; float3 direction:TEXCOORD0; };
            V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.direction=i.p.xyz;return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
            half4 Frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);float h=saturate(d.y);
                half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,0.45));
                float2 p=d.xz/max(0.15,d.y+0.12)*2.8;
                float cloud=smoothstep(0.55,0.8,Noise(p)*0.65+Noise(p*2.1)*0.35)*smoothstep(0.03,0.24,h)*_CloudStrength;
                color=lerp(color,_CloudColor.rgb,cloud);
                float dotSun=saturate(dot(d,normalize(_SunDirection.xyz)));
                color+=pow(dotSun,650)*_SunColor.rgb+pow(dotSun,20)*_SunColor.rgb*0.08;
                float2 starUV=d.xz/max(0.08,d.y)*105;
                float star=step(0.9975,Hash(floor(starUV)))*pow(saturate(1-length(frac(starUV)-0.5)*2),5)*smoothstep(0.1,0.5,h);
                color+=star*_Stars;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
