Shader "NanoverIMD/Translucent/Raycast Sphere"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Scale ("Scale", Float) = 1
        _Diffuse ("Diffuse", Range(0, 1)) = 0.5
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

        // extra pass that renders to depth buffer only
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
            
            #include "../Base/RaycastSphere.cginc"
            
            ENDCG
        }
    
        Pass
        {
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
           
            #pragma instancing_options procedural:setup
            
            #define POSITION_ARRAY
            #pragma multi_compile __ SCALE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            
            #include "../Base/RaycastSphere.cginc"
            
            ENDCG
        }
    }
}