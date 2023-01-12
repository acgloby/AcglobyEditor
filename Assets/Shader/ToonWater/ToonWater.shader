Shader "URPDemo/ToonWater"
{
    Properties
    {
        _ShallowColor("浅部颜色", Color) = (0.6, 0.8, 0.95, 1.0)
        _DeepColor("深部颜色", Color) = (0, 0, 1.0, 1.0)
        _DepthMaxDistance("深度范围", Range(0,1)) = 0.5
        _FoamColor("泡沫颜色", Color) = (1.0, 1.0, 1.0, 1.0)
        _SurfaceNoise("水波纹噪声贴图", 2D) = "white" {}
        _SurfaceNoiseCutoff("水波纹阈值", Range(0, 1)) = 0.777
        _FoamMaxDistance("泡沫最大距离", Range(0, 1)) = 0.4
        _FoamMinDistance("泡沫最小距离", Range(0, 1)) = 0.04
        _SurfaceNoiseScroll("流动方向", Vector) = (0.03, 0.03, 0, 0)
        _SurfaceDistortion("失真纹理贴图", 2D) = "white" {}	
        _SurfaceDistortionAmount("扰动强度", Range(0, 1)) = 0.27
        
    }
    SubShader
    {
        Tags 
        {   
            "RenderType"="Transparent" 
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define SMOOTHSTEP_AA 0.01
            #define COMPUTE_VIEW_NORMAL normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal))

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 screenPosition : TEXCOORD1;
                float2 noiseUV : TEXCOORD2;
                float2 distortUV : TEXCOORD3;
                float3 viewNormal : NORMAL; 
                float3 normalWS : NORMAL1;
            };


            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D(_SurfaceNoise);
            SAMPLER(sampler_SurfaceNoise);
            float4 _SurfaceNoise_ST;

            TEXTURE2D(_SurfaceDistortion);
            SAMPLER(sampler_SurfaceDistortion);
            float4 _SurfaceDistortion_ST;

            TEXTURE2D(_CameraNormalsTexture);       
            SAMPLER(sampler_CameraNormalsTexture);
            
            float _DepthMaxDistance;
            float4 _ShallowColor;
            float4 _DeepColor;
            float _SurfaceNoiseCutoff;
            float _FoamMaxDistance;
            float _FoamMinDistance;
            float2 _SurfaceNoiseScroll;
            float _SurfaceDistortionAmount;
            float4 _FoamColor;
            

            // 自定义Alpha混合
            float4 alphaBlend(float4 top, float4 bottom)
            {
                float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
                float alpha = top.a + bottom.a * (1 - top.a);

                return float4(color, alpha);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.screenPosition = ComputeScreenPos(o.pos);
                o.uv = v.uv;
                o.noiseUV = TRANSFORM_TEX(v.uv, _SurfaceNoise);
                o.distortUV = TRANSFORM_TEX(v.uv, _SurfaceDistortion);
                o.viewNormal = COMPUTE_VIEW_NORMAL;
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 screenPos = i.screenPosition.xy / i.screenPosition.w;
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos).r;
                float depthLinear = LinearEyeDepth(depth, _ZBufferParams);
                float waterDepth = i.screenPosition.w;
                float depthDifference = depthLinear - waterDepth;
                float waterDepthDifference = saturate(depthDifference / _DepthMaxDistance);
                float4 waterColor = lerp(_ShallowColor, _DeepColor, waterDepthDifference);

                float2 distortSample = (SAMPLE_TEXTURE2D(_SurfaceDistortion, sampler_SurfaceDistortion, i.distortUV).xy * 2 + 1) * _SurfaceDistortionAmount;
                float2 noiseUV = float2(i.noiseUV.x + _Time.y * _SurfaceNoiseScroll.x + distortSample.x, i.noiseUV.y + _Time.y * _SurfaceNoiseScroll.y + distortSample.y);
                float surfaceNoiseSample = SAMPLE_TEXTURE2D(_SurfaceNoise, sampler_SurfaceNoise, noiseUV).r;

                //比较渲染的法线与view space的法线
                float3 existingNormal = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, screenPos);
                float3 normalDot = saturate(dot(existingNormal, i.viewNormal));

                //泡沫深度
                float foamDistance = lerp(_FoamMaxDistance, _FoamMinDistance, normalDot);
                float foamDepthDifference = saturate(depthDifference / foamDistance);
                //将泡沫深度成阈值，得到边缘泡沫
                float surfaceNoiseCutoff = foamDepthDifference * _SurfaceNoiseCutoff;
                float surfaceNoise = smoothstep(surfaceNoiseCutoff - SMOOTHSTEP_AA, surfaceNoiseCutoff + SMOOTHSTEP_AA, surfaceNoiseSample);
                float4 surfaceNoiseColor = _FoamColor;
                surfaceNoiseColor.a *= surfaceNoise;

                Light light = GetMainLight();
                float3 ambient = SampleSH(i.normalWS);
                waterColor.rgb = waterColor * light.color + ambient;
                surfaceNoiseColor.rgb = surfaceNoiseColor * light.color + ambient;

                return alphaBlend(surfaceNoiseColor, waterColor);
            }
            ENDHLSL
        }
    }
}
