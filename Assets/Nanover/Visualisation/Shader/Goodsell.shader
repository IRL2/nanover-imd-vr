Shader "NanoverIMD/Goodsell"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _DepthTex;
            float4 _DepthTex_ST;
            
            sampler2D_float _ResidueTex;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            struct fout {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };
            
            float SampleDepth(float2 uv) {
                return DecodeFloatRG(tex2D(_DepthTex, uv).rg);
            }
            
            static const float3x3 sobel_y = float3x3( 
                 1.0, 0.0, -1.0, 
                 2.0, 0.0, -2.0, 
                 1.0, 0.0, -1.0 
            );
            
            static const float3x3 sobel_x = float3x3( 
                 1.0, 2.0, 1.0, 
                 0.0, 0.0, 0.0, 
                -1.0, -2.0, -1.0 
            );
            
            #define NUM_OFFSETS 21
            
            static const float2 offsets[NUM_OFFSETS] = {
                float2(-2, -1),
                float2(-2, 0),
                float2(-2, 1),
                float2(-1, -2),
                float2(-1, -1),
                float2(-1, 0),
                float2(-1, 1),
                float2(-1, 2),
                float2(0, -2),
                float2(0, -1),
                float2(0, 0),
                float2(0, 1),
                float2(0, 2),
                float2(1, -2),
                float2(1, -1),
                float2(1, 0),
                float2(1, 1),
                float2(1, 2),
                float2(2, -1),
                float2(2, 0),
                float2(2, 1)
            };
            
            fout frag (v2f i)
            {
                fout o;
                
                float4 col = tex2D(_MainTex, i.uv);
                
                float centerdepth = SampleDepth(i.uv);
                
                float unit = 0;
                
                float d = tex2D(_ResidueTex, i.uv);
                
                for (int x = 0; x < NUM_OFFSETS; x++) {
                    float2 uv = i.uv + 0.001 * offsets[x];
                    float c = tex2D(_ResidueTex, uv);
                    if(abs(c-d) > 4.5)
                        unit += 1;
                }
            
                if(centerdepth < 0.0001)
                    discard;
                
                o.color = col * (1 - clamp((unit-3)/5.0, 0, 1));
                o.depth = centerdepth;
                
                return o;
            }
            ENDCG
        }
    }
}