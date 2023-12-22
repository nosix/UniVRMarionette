Shader "Universal Render Pipeline/Alpha Hand Outline (SBB)"
{
    Properties
    {
        _ColorPrimary ("Color Primary", Color) = (0.396078, 0.725490, 1)
        _ColorTop ("Color Top", Color) = (0.031896, 0.0343398, 0.0368894)
        _ColorBottom ("Color Bottom", Color) = (0.0137021, 0.0144438, 0.0152085)
        _RimFactor ("Rim Factor", Range(0.01, 1.0)) = 0.65
        _FresnelPower ("Fresnel Power", Range(0.01,1.0)) = 0.16

        _HandAlpha ("Hand Alpha", Range(0, 1)) = 1.0
        _MinVisibleAlpha ("Minimum Visible Alpha", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        LOD 100

        // Write depth values so that you see topmost layer.
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
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
                float3 viewDirectionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float3 _ColorPrimary;
            float3 _ColorTop;
            float3 _ColorBottom;
            float _RimFactor;
            float _FresnelPower;
            float _HandAlpha;
            float _MinVisibleAlpha;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirectionWS = _WorldSpaceCameraPos.xyz - positionWS.xyz;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 viewDirection = normalize(input.viewDirectionWS);
                float viewDotNormal = saturate(dot(viewDirection, input.normalWS));
                float rim = pow(1.0 - viewDotNormal, 0.5) * (1.0 - _RimFactor) + _RimFactor;
                rim = saturate(rim);

                float3 emission = lerp(float3(0, 0, 0), _ColorPrimary, rim);
                emission += rim * 0.5;
                emission *= 0.95; // EmissionFactor

                float fresnel = saturate(pow(1.0 - viewDotNormal, _FresnelPower));
                float3 color = lerp(_ColorTop, _ColorBottom, fresnel);

                float alphaValue = step(_MinVisibleAlpha, _HandAlpha) * _HandAlpha;

                return float4(color * emission, alphaValue);
            }
            ENDHLSL
        }
    }
}