Shader "UI/DigitalGlitchUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _TrashTex ("Trash Texture", 2D) = "white" {}
        _Intensity ("Glitch Intensity", Range(0, 1)) = 0.5
        _GlitchOpacity ("Glitch Opacity", Range(0, 1)) = 1.0
        _GlitchColor ("Glitch Color", Color) = (0,1,0.8,1)
        _GlitchColor2 ("Glitch Color 2", Color) = (1,0,1,1)
        _GlitchColor3 ("Glitch Color 3", Color) = (0.1,0.2,0.5,1)
        _AlwaysVisible ("Always Visible", Float) = 0.0
        _ColorBalance ("Color Balance", Range(0, 1)) = 0.5
        

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            sampler2D _NoiseTex;
            sampler2D _TrashTex;
            float _Intensity;
            float _GlitchOpacity;
            float4 _GlitchColor;
            float4 _GlitchColor2;
            float4 _GlitchColor3;
            float _AlwaysVisible;
            float _ColorBalance;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                half4 source = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                float4 glitch = tex2D(_NoiseTex, IN.texcoord);
         
                float thresh = 1.001 - _Intensity * 1.001;
                float w_d = step(thresh, pow(abs(glitch.z), 2.5)); 
                float w_f = step(thresh, pow(abs(glitch.w), 2.5)); 
                float w_c = step(thresh, pow(abs(glitch.z), 3.5)); 
                
                float2 uv = frac(IN.texcoord + glitch.xy * w_d);
                
                float4 trash = tex2D(_TrashTex, uv);
                
                float visibilityFactor = max(max(w_d, w_f), w_c);
                
                float showEffect = _AlwaysVisible > 0.5 ? 1.0 : visibilityFactor;
                
                float3 finalGlitchColor;
                
                // Apply color balance through lerping between colors
                if (trash.r > 0.8) {
                    float3 color1 = _GlitchColor.rgb;
                    float3 color2 = _GlitchColor2.rgb;
                    finalGlitchColor = lerp(color1, color2, _ColorBalance);
                } 
                else if (trash.g > 0.6) {
                    float3 color1 = _GlitchColor2.rgb;
                    float3 color2 = _GlitchColor3.rgb;
                    finalGlitchColor = lerp(color1, color2, _ColorBalance);
                }
                else {
                    float3 color1 = _GlitchColor3.rgb;
                    float3 color2 = _GlitchColor.rgb;
                    finalGlitchColor = lerp(color1, color2, _ColorBalance);
                }
                
                if (w_c > 0) {
                    finalGlitchColor = lerp(finalGlitchColor, finalGlitchColor.gbr, glitch.x * 0.5);
                }
                
                if (frac(glitch.y * 10) > 0.9 && w_d > 0) {
                    float3 color1 = _GlitchColor2.rgb;
                    float3 color2 = _GlitchColor3.rgb;
                    finalGlitchColor = lerp(color1, color2, _ColorBalance);
                }
                
                half4 finalColor = half4(finalGlitchColor, trash.a * _GlitchOpacity * showEffect);
                
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif
                
                return finalColor;
            }
            ENDCG
        }
    }
}