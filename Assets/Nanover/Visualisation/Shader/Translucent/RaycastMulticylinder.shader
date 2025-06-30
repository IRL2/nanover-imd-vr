Shader "NanoverIMD/Translucent/Raycast Multiple Cylinders"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _EdgeScale ("Edge Scale", Float) = 1
        _Diffuse ("Diffuse", Range(0, 1)) = 0.5
        _Shrink ("Shrink Factor", Range(0, 1)) = 0.8
        _Spacing ("Spacing", Float) = 1
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="AlphaTest" 
            "LightMode"="ForwardBase"
        }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        
        Cull Front
        
        Pass
        {
            ZWrite On
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
           
            #pragma instancing_options procedural:setup
            
            #define POSITION_ARRAY
            #define EDGE_ARRAY
            #define EDGE_COUNT_ARRAY
            #pragma multi_compile __ SCALE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            
            #include "../Base/RaycastMulticylinder.cginc"
            
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
            #define EDGE_ARRAY
            #define EDGE_COUNT_ARRAY
            #pragma multi_compile __ SCALE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            
            #include "../Base/RaycastMulticylinder.cginc"
            
            ENDCG
        }
    }
}