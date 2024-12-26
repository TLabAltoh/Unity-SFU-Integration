// https://zenn.dev/kento_o/articles/9657c594695954

Shader "TLab/VRProjct/Skybox/DefaultSky" {
    Properties{
        _TopColor("Top Color", Color) = (1,1,1,1)
        _ButtomColor("Buttom Color", Color) = (1,1,1,1)
        _TopColorPos("Top Color Pos", Range(0, 1)) = 1
        _TopColorAmount("Top Color Amount", Range(0, 1)) = 0.5

        [Toggle] _Star("Star", Int) = 0
        _StarsAmount("Stars Amount", Int) = 10
    }
    SubShader{
        Tags {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass{
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            int _Star;
            int _StarsAmount;
            fixed4 _TopColor;
            fixed4 _ButtomColor;
            fixed _TopColorPos;
            fixed _TopColorAmount;

            struct appdata {
                half4 vertex    : POSITION;
                half2 uv        : TEXCOORD0;
            };
            struct v2f {
                half4 vertex    : POSITION;
                half3 pos       : WORLD_POS;
                half2 uv        : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.pos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            float2 random2(float2 st)
            {
                st = float2(dot(st, float2(127.1, 311.7)), dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }

            fixed4 frag(v2f i) : COLOR{
                fixed amount = clamp(abs(_TopColorPos - i.uv.y) + (0.5 - _TopColorAmount), 0, 1);
                fixed4 gradation = lerp(_TopColor, _ButtomColor, amount);

                i.color = gradation;

                if (_Star) {
                    float3 dir = normalize(i.pos);
                    float2 rad = float2(atan2(dir.x, dir.z), asin(dir.y));
                    float2 uv = rad / float2(UNITY_PI / 2, UNITY_PI / 2);

                    uv *= _StarsAmount;

                    float2 ist = floor(uv);
                    float2 fst = frac(uv);

                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            float2 neighbor = float2(x, y);
                            float2 p = random2(ist);
                            float2 diff = neighbor + p - fst;

                            float r = rand(p + 1);
                            float g = rand(p + 2);
                            float b = rand(p + 3);
                            float4 randColor = float4(r, g, b, 1);

                            float interpolation = 1 - step(0.01, length(diff));

                            i.color += lerp(0, randColor, interpolation);
                        }
                    }
                }

                return i.color;
            }
            ENDCG
        }
    }
}