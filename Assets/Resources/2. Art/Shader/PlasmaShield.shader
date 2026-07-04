Shader "Custom/PlasmaShield"
{
    Properties
    {
        _Color("Shield Color", Color) = (0.2,0.7,1,1)

        _Opacity("Opacity", Range(0,2)) = 0.8
        _Emission("Emission", Range(0,10)) = 4

        _RimPower("Rim Power", Range(1,10)) = 5

        _LargeScale("Large Noise Scale", Float) = 2
        _MediumScale("Medium Noise Scale", Float) = 8
        _DetailScale("Detail Noise Scale", Float) = 25

        _WarpStrength("Warp Strength", Float) = 0.2

        _LargeSpeed("Large Speed", Float) = 0.2
        _MediumSpeed("Medium Speed", Float) = 1
        _DetailSpeed("Detail Speed", Float) = 2

        _Threshold("Threshold", Range(0,1)) = 0.45
        _Contrast("Contrast", Float) = 6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            float4 _Color;

            float _Opacity;
            float _Emission;
            float _RimPower;

            float _LargeScale;
            float _MediumScale;
            float _DetailScale;

            float _WarpStrength;

            float _LargeSpeed;
            float _MediumSpeed;
            float _DetailSpeed;

            float _Threshold;
            float _Contrast;

            //--------------------------------------------

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34,456.21));
                p += dot(p,p+45.32);
                return frac(p.x*p.y);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = hash21(i);
                float b = hash21(i+float2(1,0));
                float c = hash21(i+float2(0,1));
                float d = hash21(i+float2(1,1));

                float2 u = f*f*(3-2*f);

                return lerp(a,b,u.x)
                    +(c-a)*u.y*(1-u.x)
                    +(d-b)*u.x*u.y;
            }

            //--------------------------------------------

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS = pos.positionCS;
                OUT.worldPos = pos.positionWS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                return OUT;
            }

            //--------------------------------------------

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - IN.worldPos);

                float rim = pow(1 - saturate(dot(N,V)), _RimPower);

                float2 uv = IN.worldPos.xz;

                float t = _Time.y;

                //----------------------------------------
                // UV Warp
                //----------------------------------------

                float2 warp;

                warp.x = noise(uv*_MediumScale + float2(t*_MediumSpeed,0));
                warp.y = noise(uv*_MediumScale + float2(100,-t*_MediumSpeed));

                uv += (warp-0.5)*_WarpStrength;

                //----------------------------------------
                // Large blobs
                //----------------------------------------

                float large =
                    noise(
                        uv*_LargeScale +
                        float2(t*_LargeSpeed,
                               t*_LargeSpeed*0.4));

                //----------------------------------------
                // Medium turbulence
                //----------------------------------------

                float medium =
                    noise(
                        uv*_MediumScale +
                        float2(-t*_MediumSpeed,
                                t*_MediumSpeed));

                //----------------------------------------
                // Fine detail
                //----------------------------------------

                float detail =
                    noise(
                        uv*_DetailScale +
                        float2(t*_DetailSpeed,
                              -t*_DetailSpeed));

                //----------------------------------------
                // Plasma
                //----------------------------------------

                float plasma = large;

                plasma *= lerp(0.5,1.5,medium);

                plasma += detail*0.15;

                plasma = saturate((plasma-_Threshold)*_Contrast);

                plasma *= rim;

                //----------------------------------------

                float3 color =
                    _Color.rgb *
                    plasma *
                    _Emission;

                return float4(
                    color,
                    plasma*_Opacity
                );
            }

            ENDHLSL
        }
    }
}