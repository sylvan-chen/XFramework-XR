Shader "Custom/Effect/SpatialMagnifier"
{
    Properties
    {
        [Header(MagnifySetting)]
        _Zoom ("Zoom", Range(1, 2)) = 1.35
        [Enum(None,0,Quadratic,1,Sine,2,Exponential,3,Cubic,4)] _FisheyeMode("Fisheye Mode", Float) = 1
        _FisheyeStrength ("Fisheye Strength", Range(0, 1)) = 0.3
        _MagnifierRadius ("Magnifier Radius", Float) = 0.8

        [Header(PassSetting)]
        [Enum(Off,0,On,1)]_Zwrite("Zwrite",int)=1

        [Header(Stencil)]
        [Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Comparison", Int)=8
		_Stencil ("Stencil ID", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)]_StencilOp("Stencil Operation", Int)=0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

        [Header(Debug)]
        [Toggle] _DebugMode ("Debug Mode", Float) = 0
        [Toggle] _EnableMagnifierOpaqueTexture ("Enable Magnifier Opaque Texture", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            ZWrite [_Zwrite]
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON UNITY_STEREO_INSTANCING_ENABLED
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D_X(_MagnifierOpaqueTexture);
            SAMPLER(sampler_MagnifierOpaqueTexture);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                float2 uv               : TEXCOORD0;     // UV坐标
                float4 screenPos        : TEXCOORD1;     // 屏幕空间坐标
                float4 magnifierCenter  : TEXCOORD2;     // 放大镜中心坐标
                UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float _Zoom;
                float _FisheyeMode;
                float _FisheyeStrength;
                float4 _CenterL;
                float4 _CenterR;
                float _MagnifierRadius;
                float _EnableMagnifierOpaqueTexture;
                float _DebugMode;
            CBUFFER_END

            Varyings vert(Attributes v, uint instanceID: SV_InstanceID)
            {
                Varyings o = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);  // 裁剪空间坐标
                o.uv = v.uv;                                              // UV坐标
                o.screenPos = ComputeScreenPos(o.positionCS);             // 屏幕坐标

                if (unity_StereoEyeIndex == 0) // 左眼
                {
                    o.magnifierCenter = _CenterL;
                }
                else // 右眼
                {
                    o.magnifierCenter = _CenterR;
                }

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 uv = screenUV;
                float2 magnifierCenter = i.magnifierCenter.xy;

                // 单通道立体渲染时，UV需要做特殊处理
                #if defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(STEREO_MULTIVIEW_ON)
                    uv = UnityStereoTransformScreenSpaceTex(uv);
                    magnifierCenter = UnityStereoTransformScreenSpaceTex(magnifierCenter);
                #endif

                if (_EnableMagnifierOpaqueTexture < 0.5)
                {
                    // 如果不启用放大镜纹理，保持透明
                    return half4(0, 0, 0, 0.3);
                }

                if (_DebugMode > 0.5)
                {
                    // 绿色十字标记放大镜中心
                    float2 debugMagCenter = abs(uv - magnifierCenter.xy);
                    if (debugMagCenter.x < 0.005 || debugMagCenter.y < 0.005)
                        return half4(0, 1, 0, 1); 

                    // 红色圆点标记放大镜中心
                    if (distance(uv, magnifierCenter.xy) < 0.02)
                        return half4(1, 0, 0, 1); 

                    // 蓝色圆点标记屏幕中心
                    if (distance(uv, float2(0.5, 0.5)) < 0.02)
                        return half4(0, 0, 1, 1);

                    // 显示放大镜半径边界
                    float radiusInUV = _MagnifierRadius;
                    if (abs(distance(uv, magnifierCenter.xy) - radiusInUV) < 0.005)
                        return half4(1, 1, 0, 1); // 黄色边界

                    // 显示坐标网格
                    float2 gridUV = frac(uv * 10);
                    if (gridUV.x < 0.05 || gridUV.y < 0.05)
                        return half4(0.5, 0.5, 0.5, 1); // 灰色网格
                }

                float2 offset = uv - magnifierCenter;
                float dist = length(offset);

                float radiusInUV = _MagnifierRadius; // 如果半径已经是UV比例，直接使用

                if (dist < radiusInUV)
                {
                    float normalizedDist = dist / radiusInUV;
                    // 鱼眼变形因子计算
                    float fisheyeFactor = 1.0;
                    if (_FisheyeMode == 0)
                    {
                        // 无鱼眼效果
                        fisheyeFactor = 1.0;
                    }
                    else if (_FisheyeMode == 1)
                    {
                        // 二次函数：温和变形
                        fisheyeFactor = 1.0 - _FisheyeStrength * normalizedDist * normalizedDist;
                    }
                    else if (_FisheyeMode == 2)
                    {
                        // 正弦函数：自然变形
                        fisheyeFactor = 1.0 - _FisheyeStrength * sin(normalizedDist * 3.14159265);
                    }
                    else if (_FisheyeMode == 3)
                    {
                        // 指数函数：强烈变形
                        fisheyeFactor = exp(-_FisheyeStrength * normalizedDist);
                    }
                    else if (_FisheyeMode == 4)
                    {
                        // 三次函数：更强烈变形
                        fisheyeFactor = pow(abs(normalizedDist), _FisheyeStrength);
                    }

                    float scale = fisheyeFactor / _Zoom;
                    float2 magnifiedUV = magnifierCenter + offset * scale;

                    magnifiedUV = saturate(magnifiedUV);

                    half4 col = SAMPLE_TEXTURE2D_X(_MagnifierOpaqueTexture, sampler_MagnifierOpaqueTexture, magnifiedUV);
                    return half4(col.rgb, 1.0);
                }

                // 放大镜边界外用灰度透明覆盖
                return half4(1, 0, 0, 0.3);
            }
            ENDHLSL
        }

    }

}