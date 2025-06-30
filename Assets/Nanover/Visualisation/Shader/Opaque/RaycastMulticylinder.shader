Shader "NanoverIMD/Opaque/Raycast Multiple Cylinders"
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
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
        }
        LOD 100
        
        Cull Front

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
            #pragma multi_compile __ FILTER_ARRAY
            
            #include "../Base/RaycastMulticylinder.cginc"
            
            ENDCG
        }
    }
}