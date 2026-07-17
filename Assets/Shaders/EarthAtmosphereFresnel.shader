Shader "Custom/EarthAtmosphereFresnel"
{
    Properties
    {
        [HDR] _AtmosphereColor("Atmosphere Color", Color) = (0.1, 0.45, 1.0, 2.0)
        _FresnelPower("Fresnel Power", Float) = 3.5
        _FresnelIntensity("Fresnel Intensity", Float) = 1.5
        
        // 方向性渐变控制
        _GradientSharpness("Gradient Sharpness", Range(0.1, 8.0)) = 1.5
        _GradientIntensity("Gradient Intensity", Range(0.0, 5.0)) = 2.0
        _GradientAngle("Gradient Angle", Range(0, 360)) = 135
        
        // 圆环泛光控制
        _RingEnabled("Enable Ring Glow", Float) = 1.0
        _RingInnerRadius("Ring Inner Radius", Range(0.0, 1.0)) = 0.35
        _RingOuterRadius("Ring Outer Radius", Range(0.0, 1.0)) = 0.65
        _RingSoftEdge("Ring Soft Edge", Range(0.01, 0.3)) = 0.08
        _RingIntensity("Ring Intensity", Float) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "ForwardOnly" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            float4 _AtmosphereColor;
            float _FresnelPower;
            float _FresnelIntensity;
            
            // 渐变参数
            float _GradientSharpness;
            float _GradientIntensity;
            float _GradientAngle;
            
            // 圆环参数
            float _RingEnabled;
            float _RingInnerRadius;
            float _RingOuterRadius;
            float _RingSoftEdge;
            float _RingIntensity;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                output.positionOS = input.positionOS;
                return output;
            }

            // 计算方向性渐变：基于物体局部坐标，左上亮 → 右下暗
            float CalculateDirectionalGradient(float3 positionOS)
            {
                // 将物体空间 XY 归一化到 [0, 1]（假设球体半径 0.5）
                float2 localUV = (positionOS.xy + 0.5); // 左下(0,0) → 右上(1,1)
                
                // 将角度转换为方向向量 (默认135° = 左上→右下)
                float angleRad = radians(_GradientAngle);
                float2 gradientDir = float2(cos(angleRad), sin(angleRad));
                
                // 计算当前像素在梯度方向上的投影
                float projection = dot(localUV - 0.5, gradientDir);
                
                // 归一化到 [0, 1]，反转：投影大的地方暗，小的地方亮
                float gradient = 1.0 - saturate((projection + 0.707) / 1.414);
                
                // 应用锐化控制
                gradient = pow(gradient, _GradientSharpness);
                
                // 应用强度控制：0 = 无渐变，1 = 完全渐变，>1 = 更强渐变
                gradient = lerp(1.0, gradient, _GradientIntensity);
                
                return gradient;
            }

            // 计算圆环形 mask
            float CalculateRingMask(float3 positionOS)
            {
                // 计算到中心的归一化距离（在 XY 平面）
                float distFromCenter = length(positionOS.xy);
                
                // 假设球体半径为 0.5（Unity 标准球），归一化到 [0, 1]
                float normalizedDist = distFromCenter / 0.5;
                normalizedDist = saturate(normalizedDist);
                
                // 创建环形 mask：内圆和外圆之间为 1
                float innerEdge = smoothstep(_RingInnerRadius - _RingSoftEdge, _RingInnerRadius + _RingSoftEdge, normalizedDist);
                float outerEdge = 1.0 - smoothstep(_RingOuterRadius - _RingSoftEdge, _RingOuterRadius + _RingSoftEdge, normalizedDist);
                
                return innerEdge * outerEdge;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                
                // 1. 基础 Fresnel 边缘光
                float fresnel = 1.0 - saturate(abs(dot(N, V)));
                fresnel = pow(fresnel, _FresnelPower);
                
                // 2. 方向性亮度渐变（基于物体空间坐标）
                float directionalGradient = CalculateDirectionalGradient(input.positionOS);
                
                // 3. 圆环泛光 mask
                float ringMask = 0.0;
                if (_RingEnabled > 0.5)
                {
                    ringMask = CalculateRingMask(input.positionOS);
                }
                
                // 4. 合成最终颜色
                float baseIntensity = fresnel * directionalGradient;
                float totalIntensity = baseIntensity + (ringMask * _RingIntensity * fresnel);
                
                float3 color = _AtmosphereColor.rgb * _AtmosphereColor.a * _FresnelIntensity * totalIntensity;
                
                return float4(color, totalIntensity);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Shader Graph/FallbackError"
}
