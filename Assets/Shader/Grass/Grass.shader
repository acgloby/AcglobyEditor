Shader "URPDemo/Grass"
{
    Properties
    {
        _BottomColor("Bottom Color", color) = (1,1,1,1)
        _TopColor("Top Color", color) = (1,1,1,1)

        _BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2
        
        _BladeWidth("Blade Width",float) = 0.05
        _BladeWidthRandom("Blade Width Random",float) = 0.02
        _BladeHeight("Blade Height",float) = 0.5
        _BladeHeightRandom("Blade Height Random",float) = 0.3

        //风贴图RG通道
        _WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
        _WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)

        //风的强度
        _WindStrength("Wind Strength", float) = 0.1

        _BladeForward("Blade Forward Amount", Float) = 0.38
        _BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2

        //细分
        _TessellationUniform("Tessellation Uniform", Range(1, 64)) = 1
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    #include "Assets/Shader/Grass/CustomTessellation.hlsl"
        
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS //主光源阴影
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE //主光源层级阴影是否开启
    #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS //额外光源阴影
    #pragma multi_compile _ _SHADOWS_SOFT //软阴影

    #define UNITY_PI 3.14159265359f   //圆周率
    #define UNITY_TWO_PI 6.28318530718f //2倍圆周率
    #define BLADE_SEGMENTS 3 //草的面数

    float4 _BottomColor;
    float4 _TopColor;
    float _BendRotationRandom;
    float _BladeWidth;
    float _BladeWidthRandom;
    float _BladeHeight;
    float _BladeHeightRandom;

    TEXTURE2D(_WindDistortionMap);
    SAMPLER(sampler_WindDistortionMap);
    float4 _WindDistortionMap_ST;
    float2 _WindFrequency;
    float _WindStrength;

    float _BladeForward;
    float _BladeCurve;

    float3 _LightDirection;

    //几何着色器数据
    struct geometryOutput
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
        float4 tangent : TANGENT;
        float3 positionWS : TEXCOORD1;
        float4 shadowCoord : TEXCOORD2;
    };

    //生成草的几何数据
    geometryOutput VertexOutput(float3 pos, float2 uv, float3 normal)
    {
        geometryOutput o = (geometryOutput)0;
        o.pos = TransformObjectToHClip(pos);
        o.uv = uv;
        o.normal = TransformObjectToWorldNormal(normal);
        o.positionWS = TransformObjectToWorld(pos);
        o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
        #if UNITY_PASS_SHADOWCASTER
            o.positionWS = ApplyShadowBias(o.positionWS, o.normal, _LightDirection);
        #endif
        return o;
    }

    //生成草的顶点
    geometryOutput GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv, float3x3 transformMatrix)
    {
        float3 tangentPoint = float3(width, forward, height);
        float3 trangentNormal = normalize(float3(0, -1, forward));
        float3 localNormal = mul(transformMatrix , trangentNormal);
        float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
        return VertexOutput(localPosition, uv, localNormal);
    }


    //随机数生成
    float rand(float3 co)
    {
        return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
    }

    //随机方向
    float3x3 AngleAxis3x3(float angle, float3 axis)
    {
        float c, s;
        sincos(angle, s, c);
        float t = 1 - c;
        float x = axis.x;
        float y = axis.y;
        float z = axis.z;
        return float3x3(
            t * x * x + c, t * x - s * z, t * x * z + s * y,
            t * x * y + s * z, t * y * y + c, t * y * z - s * x,
            t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
    }

    //几何着色器
    [maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
    void geo(triangle vertexOutput IN[3] : SV_POSITION, inout TriangleStream<geometryOutput> triStream)
    {
        geometryOutput o;
        float3 pos =  IN[0].vertex;
        float3 vNormal = IN[0].normal;
        float4 vTangent = IN[0].tangent;
        float3 vBinormal = cross(vNormal,vTangent) * vTangent.w;
        
        //TBN
        float3x3 tangentToLocal = float3x3(
            vTangent.x,vBinormal.x,vNormal.x,
            vTangent.y,vBinormal.y,vNormal.y,
            vTangent.z,vBinormal.z,vNormal.z
        );

        float2 uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
        float2 windSample = (SAMPLE_TEXTURE2D_LOD(_WindDistortionMap, sampler_WindDistortionMap, float4(uv, 0, 0), 0).xy * 2 - 1) * _WindStrength;
        //Wind Vector
        float3 wind = normalize(float3(windSample.x, windSample.y, 0));
        float3x3 windRotation = AngleAxis3x3(UNITY_PI * windSample, wind);

        //随机草的方向
        float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos.zzx) * UNITY_TWO_PI, float3(0,0,1));
        float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * UNITY_PI * 0.5, float3(-1, 0, 0));
        float3x3 tranformationMatrix = mul(mul(mul(tangentToLocal, facingRotationMatrix), bendRotationMatrix), windRotation);
        //底部顶点矩阵不包含旋转
        float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix);

        //随机草的大小
        float height = (rand(pos.zzx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
        float width = (rand(pos.zzx) * 2 - 1) * _BladeWidthRandom + _BladeWidth;

        float forward = rand(pos.yyz) * _BladeForward;

        //细分草叶顶点
        for(int i = 0; i < BLADE_SEGMENTS; i++)
        {
            float t = i / (float)BLADE_SEGMENTS;
            float segmentHeight = height * t;
            float segmentWidth = width * (1 - t);
            
            //计算顶点forward偏移
            float segmentForward = pow(t, _BladeCurve) * forward;

            float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : tranformationMatrix;
            triStream.Append(GenerateGrassVertex(pos, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix));
            triStream.Append(GenerateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix));
        }

        triStream.Append(GenerateGrassVertex(pos, 0, height, forward, float2(0.5, 1), tranformationMatrix));
    }
    
    vertexOutput vert (vertexInput v)
    {
        vertexOutput o;
        o.uv = v.uv;
        o.vertex = v.vertex;
        o.normal = v.normal;
        o.tangent = v.tangent;
        return o;
    }

    ENDHLSL

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
        }

        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo
            #pragma hull hull
            #pragma domain domain
            #pragma target 4.6

            float4 frag (geometryOutput i, float facing : VFACE) : SV_Target
            {
                Light light = GetMainLight(i.shadowCoord);
                float3 lightDir = normalize(light.direction);
                float3 normal = normalize(facing > 0 ? i.normal : - i.normal);
                float NdotL = saturate(dot(normal, lightDir) + 0.5 * 0.5);
                float3 ambient = SampleSH(normal);
                float3 lightColor = light.color;
                float3 lightIntensity = NdotL * light.color + ambient * (light.shadowAttenuation * 0.5 + 0.5);
                float3 col = lerp(_BottomColor, _TopColor, i.uv.y) * lightIntensity;
                return float4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geo
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma target 4.6
            #pragma multi_compile_shadowcaster

            float4 frag(geometryOutput i) : SV_Target
            {
                return 0;
            }

            ENDHLSL

        }
    }
}
