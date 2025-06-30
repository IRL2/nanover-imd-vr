Shader "NanoverXR/Translucent/Billboard Circle"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Scale ("Scale", Float) = 1
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="AlphaTest" 
            "PreviewType"="Plane" 
            "LightMode"="ForwardBase"
        }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        
        Cull Back

        Pass {
            ZWrite On
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
           
            #pragma instancing_options procedural:setup
            
            #define POSITION_ARRAY
            #pragma multi_compile __ SCALE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            
            #include "../Base/BillboardCircle.cginc"
            
            ENDCG
        }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
           
            #pragma instancing_options procedural:setup
            
            #define POSITION_ARRAY
            #pragma multi_compile __ SCALE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            
            #include "../Base/BillboardCircle.cginc"
            
            ENDCG
        }
    }
}