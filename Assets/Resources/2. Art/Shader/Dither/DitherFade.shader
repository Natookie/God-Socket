Shader "Custom/DitherFade"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _Fade("Fade", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }

        Pass
        {
            Name "Forward"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Fade;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = pos.positionCS;
                OUT.screenPos = ComputeScreenPos(pos.positionCS);
                OUT.uv = IN.uv;

                return OUT;
            }

            float Dither4x4(int2 pixel)
            {
                static const float bayer[16] =
                {
                    0.0/16.0, 8.0/16.0, 2.0/16.0,10.0/16.0,
                   12.0/16.0, 4.0/16.0,14.0/16.0, 6.0/16.0,
                    3.0/16.0,11.0/16.0, 1.0/16.0, 9.0/16.0,
                   15.0/16.0, 7.0/16.0,13.0/16.0, 5.0/16.0
                };

                pixel &= 3;

                return bayer[pixel.x + pixel.y * 4];
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                int2 pixel = int2(screenUV * _ScreenParams.xy);

                float threshold = Dither4x4(pixel);

                clip(_Fade - threshold);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                return col * _BaseColor;
            }

            ENDHLSL
        }
    }
}