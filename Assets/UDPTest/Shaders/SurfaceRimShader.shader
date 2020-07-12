Shader "Surface/RimShader"
{
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	_BumpMap("Bumpmap", 2D) = "bump" {}
	[HDR] _RimColor("Rim Color", Color) = (0.26,0.19,0.16,0.0)
		_RimPower("Rim Power", Range(0.5,8.0)) = 3.0
	}
		SubShader{
		Tags{ "RenderType" = "Transparent" "Queue"="Transparent"}

		CGPROGRAM
		//Lighting OFF

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

#pragma surface surf Lambert alpha:fade
		struct Input {
		float2 uv_MainTex;
		float2 uv_BumpMap;
		float3 viewDir;
	};
	sampler2D _MainTex;
	sampler2D _BumpMap;
	float4 _RimColor;
	float _RimPower;
	void surf(Input IN, inout SurfaceOutput o) {
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
		float3 rimCol = _RimColor.rgb * pow(rim, _RimPower);
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Emission = rimCol;
		o.Alpha = rim*_RimColor.a;
	}
	ENDCG
	}
	Fallback "Diffuse"
}
