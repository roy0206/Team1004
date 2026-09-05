Shader "Team1004/WaterDepth"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShallowColor ("Shallow Color", Color) = (0.36, 0.72, 0.92, 1)
        _DeepColor ("Deep Color", Color) = (0.03, 0.14, 0.32, 1)
        _Depth ("Depth", Float) = 6.5
        _Falloff ("Falloff", Range(0.2, 4)) = 1.3
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex DepthVertex
            #pragma fragment DepthFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float localY : TEXCOORD4;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ShallowColor;
                half4 _DeepColor;
                float _Depth;
                float _Falloff;
            CBUFFER_END

            Varyings DepthVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                o.localY = input.positionOS.y;
                return o;
            }

            half4 DepthFragment(Varyings input) : SV_Target
            {
                float t = saturate(-input.localY / max(_Depth, 0.001));
                t = pow(t, _Falloff);
                half4 depthColor = lerp(_ShallowColor, _DeepColor, t);
                return CommonUnlitFragment(input, input.color * depthColor);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
