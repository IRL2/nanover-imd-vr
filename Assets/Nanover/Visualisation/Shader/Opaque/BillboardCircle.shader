Shader "NanoverIMD/Opaque/Billboard Circle"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Scale ("Scale", Float) = 1
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
            #pragma multi_compile __ SCALE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            #pragma multi_compile __ FILTER_ARRAY
             
            #include "../Base/BillboardCircle.cginc"
            
            ENDCG
        }
    }
}