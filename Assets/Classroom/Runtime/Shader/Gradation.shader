Shader "Gradation/Gradation" {
    Properties{
        _TopColor("Top Color", Color) = (1,1,1,1)
        _ButtomColor("Buttom Color", Color) = (1,1,1,1)
        _TopColorPos("Top Color Pos", Range(0, 1)) = 1
        _TopColorAmount("Top Color Amount", Range(0, 1)) = 0.5
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

            fixed4 _TopColor;
            fixed4 _ButtomColor;
            fixed _TopColorPos;
            fixed _TopColorAmount;

            struct appdata {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
            };
            struct v2f {
                half4 vertex : POSITION;
                fixed4 color : COLOR;
                half2 uv : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }
            fixed4 frag(v2f i) : COLOR{
                fixed amount = clamp(abs(_TopColorPos - i.uv.y) + (0.5 - _TopColorAmount), 0, 1);
                i.color = lerp(_TopColor, _ButtomColor, amount);

                return i.color;
            }
            ENDCG
        }
    }
}