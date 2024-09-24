Shader "TLab/Outline"
{
    Properties
    {
        /*
        _MainTex("Base (RGB)", 2D) = "white" { }
        _MainColor("Main Color", Color) = (.5,.5,.5,1)
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        */
        _OutlineColor("Outline Color", Color) = (0,1,1,1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = .025
        _ZOffset("Z Offset", Range(-0.5, 0.5)) = .2
    }

    SubShader
    {
        /*
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        // MainTex
        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        // MainColor
        uniform float4 _MainColor;

        // Smoothness
        half _Glossiness;

        // Metallic
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _MainColor;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        ENDCG
        */

        Pass
        {
            Name "OUTLINE"

            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha

            LOD 100

            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Outline
            uniform float _ZOffset;
	        uniform float _OutlineWidth;
	        uniform float4 _OutlineColor;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
				float3 normal : NORMAL;
                float4 color  : COLOR;
			};

			struct v2f 
			{
				float4 pos : SV_POSITION;
				float2 uv  : TEXCOORD0;
			};

            // https://3dcg-school.pro/unity-outline-shader/

            v2f vert(appdata v)
            {
                v2f o;

                float3 positionWS = mul(unity_ObjectToWorld, v.vertex);
                float3 zOffset = normalize(positionWS - _WorldSpaceCameraPos) * _ZOffset;
				o.pos = UnityWorldToClipPos(positionWS + zOffset);

				float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.color.rgb));
				float3 offset = TransformViewToProjection(norm);

				//o.pos.xyz += offset.xyz * UNITY_Z_0_FAR_FROM_CLIPSPACE(o.pos.z) * _OutlineWidth;
				o.pos.xyz += offset.xyz * _OutlineWidth;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}