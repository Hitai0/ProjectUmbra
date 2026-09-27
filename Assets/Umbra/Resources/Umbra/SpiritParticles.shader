Shader "Umbra/SpiritParticles"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.color=v.color;o.uv=v.uv;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=abs(i.uv*2-1);
                // A bright diamond core and feathered halo without a texture allocation.
                half halo=saturate(1-dot(p,p));
                half core=saturate(1-(p.x+p.y)*1.6);
                return half4(i.color.rgb*(.65+core),i.color.a*(halo*halo*.4+core*.6));
            }
            ENDHLSL
        }
    }
}
