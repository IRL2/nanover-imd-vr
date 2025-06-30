Shader "NanoverIMD/Translucent/Billboard Edge"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _EdgeScale ("Edge Scale", Float) = 1
         [Range(0, 1)]
        _GradientWidth ("Gradient Width", Float) = 1
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
        }
        LOD 100
        
        Cull Back
        
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
            #pragma multi_compile __ COLOR_ARRAY
            
            #include "../Base/BillboardEdge.cginc"
            
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
            #pragma multi_compile __ COLOR_ARRAY
            
            #include "../Base/BillboardEdge.cginc"
            
            ENDCG
        }
    }
}