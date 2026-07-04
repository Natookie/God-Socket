Shader "Custom/LaserLineGradient"
{
    Properties
    {
        _StartColor ("Start Color", Color) = (1, 0, 0, 1)
        _EndColor ("End Color", Color) = (1, 0.4, 0, 1)
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)

        _Intensity ("Intensity", Range(0, 10)) = 3
        _CoreSize ("Core Size", Range(0, 1)) = 0.25
        _EdgePower ("Edge Fade Power", Range(0.5, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            fixed4 _StartColor;
            fixed4 _EndColor;
            fixed4 _CoreColor;

            float _Intensity;
            float _CoreSize;
            float _EdgePower;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Gradient from start to end of the line
                fixed4 gradientColor = lerp(_StartColor, _EndColor, i.uv.x);

                // Fade from center to edge
                float centerDistance = abs(i.uv.y - 0.5) * 2.0;
                float edgeFade = pow(saturate(1.0 - centerDistance), _EdgePower);

                // Bright core in the middle
                float core = 1.0 - smoothstep(0.0, _CoreSize, centerDistance);

                fixed4 finalColor = gradientColor;
                finalColor.rgb += _CoreColor.rgb * core;
                finalColor.rgb *= _Intensity;
                finalColor.a *= edgeFade;

                // Support Line Renderer color gradient too
                finalColor *= i.color;

                return finalColor;
            }
            ENDCG
        }
    }
}