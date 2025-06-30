Shader "NanoverIMD/Opaque/Billboard Edge"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _EdgeScale ("Edge Scale", Float) = 1
        _GradientWidth ("Gradient Width", Range(0, 1)) = 1
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
           
            #pragma instancing_options procedural:setup
            
            #define POSITION_ARRAY
            #define EDGE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            #pragma multi_compile __ FILTER_ARRAY
            
            #include "../Base/BillboardEdge.cginc"
            
            ENDCG
        }
    }
}