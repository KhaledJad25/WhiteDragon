// Dark screen-space outlines for URP's Full Screen Pass renderer feature (WhiteDragon_URP_Renderer > Outline).
// Edges come from depth and normal discontinuities. Tune on WhiteDragon_Outline.mat; turn off by disabling
// the renderer feature (Low effects quality does this at runtime).
Shader "WhiteDragon/Outline"
{
    Properties
    {
        _OutlineColor ("Outline color", Color) = (0, 0, 0, 1)
        _Thickness ("Thickness (pixels)", Range(0.5, 3)) = 1
        _DepthThreshold ("Depth edge threshold (relative)", Range(0.01, 1)) = 0.12
        _NormalThreshold ("Normal edge threshold", Range(0.01, 1)) = 0.4
        _FadeDistance ("Fade out by distance (m)", Float) = 40
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _Thickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float _FadeDistance;

            float EyeDepth(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 px = _Thickness * _ScreenSize.zw;
                float2 uvR = uv + float2(px.x, 0), uvL = uv - float2(px.x, 0);
                float2 uvU = uv + float2(0, px.y), uvD = uv - float2(0, px.y);

                // Depth: relative difference, so the threshold works near and far.
                float d = EyeDepth(uv);
                float depthDiff = abs(EyeDepth(uvR) - d) + abs(EyeDepth(uvL) - d)
                                + abs(EyeDepth(uvU) - d) + abs(EyeDepth(uvD) - d);
                float depthEdge = step(_DepthThreshold, depthDiff / max(d, 0.001));

                // Normals: largest angle change to a neighbour.
                float3 n = SampleSceneNormals(uv);
                float minDot = min(min(dot(n, SampleSceneNormals(uvR)), dot(n, SampleSceneNormals(uvL))),
                                   min(dot(n, SampleSceneNormals(uvU)), dot(n, SampleSceneNormals(uvD))));
                float normalEdge = step(_NormalThreshold, 1 - minDot);

                float edge = max(depthEdge, normalEdge) * saturate(1 - d / _FadeDistance) * _OutlineColor.a;
                return half4(lerp(color.rgb, _OutlineColor.rgb, edge), color.a);
            }
            ENDHLSL
        }
    }
}
