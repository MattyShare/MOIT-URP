Shader "Hidden/MOITComposite"
{
    Properties
    {
        [Toggle] _CATCHBIASERRORS ("Catch Bias Errors", Float) = 0 // set this on if you see bloom fireflies and are not able to fix it with the other controls / are scared to see it again in build
    }

    HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #ifdef _MOMENT_SINGLE_PRECISION
            TEXTURE2D_FLOAT(_B0);
        #else
            TEXTURE2D_HALF(_B0);
        #endif
        //TEXTURE2D(_MOIT); // _MOIT is _BlitTexture since we don't need to sample screen tex (alpha blended)
        
        // sampler_PointClamp sampler_LinearClamp
        #define singleSampler sampler_LinearClamp

        half4 MOITComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            //float4 moit = SAMPLE_TEXTURE2D(_BlitTexture, singleSampler, input.texcoord);
            half4 moit = SAMPLE_TEXTURE2D(_BlitTexture, singleSampler, input.texcoord);
            #ifdef _MOMENT_SINGLE_PRECISION
            float b0 = SAMPLE_TEXTURE2D(_B0, singleSampler, input.texcoord).r;
            #else
            half b0 = SAMPLE_TEXTURE2D(_B0, singleSampler, input.texcoord).r;
            #endif
            moit.rgb /= moit.a;

            #if _CATCHBIASERRORS_ON
            // catch negative color values when alpha is very close to 1 (>0.99)
            // TODO: explore why the non shader graph tmpro shader writes negatives values in moit texture (in game view only)
            return half4(max(moit.rgb, 0.0), exp(-b0));
            #else
            return half4(moit.rgb, exp(-b0));
            #endif
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "Composite"

            ZTest Always
            Blend OneMinusSrcAlpha SrcAlpha

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment MOITComposite

            #pragma shader_feature _CATCHBIASERRORS_ON

            ENDHLSL
        }

        Pass
        {
            Name "CompositeFrameBuffer"

            ZTest Always
            Blend OneMinusSrcAlpha SrcAlpha

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment MOITCompositeFB

            #pragma shader_feature _CATCHBIASERRORS_ON

            FRAMEBUFFER_INPUT_X_FLOAT(0);

            half4 MOITCompositeFB(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                //float4 moit = SAMPLE_TEXTURE2D(_BlitTexture, singleSampler, input.texcoord);
                //float4 moit = LOAD_FRAMEBUFFER_X_INPUT(0, input.positionCS.xy);
                half4 moit = LOAD_FRAMEBUFFER_X_INPUT(0, input.positionCS.xy);
                #ifdef _MOMENT_SINGLE_PRECISION
                float b0 = SAMPLE_TEXTURE2D(_B0, singleSampler, input.texcoord).r;
                #else
                half b0 = SAMPLE_TEXTURE2D(_B0, singleSampler, input.texcoord).r;
                #endif
                moit.rgb /= moit.a;

                #if _CATCHBIASERRORS_ON
                // catch negative color values when alpha is very close to 1 (>0.99)
                // TODO: explore why the non shader graph tmpro shader writes negatives values in moit texture (in game view only)
                return half4(max(moit.rgb, 0.0), exp(-b0));
                #else
                return half4(moit.rgb, exp(-b0));
                #endif
            }

            ENDHLSL
        }
    }
}
