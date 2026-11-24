Shader "MoreMountains/MMStandardEmission" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("MainTex", 2D) = "white" {}
		_MetallicGlossMap ("MetallicGlossMap", 2D) = "gray" {}
		_Metallic ("Metallic", Range(0, 1)) = 1
		_Glossiness ("Glossiness", Range(0, 1)) = 0.5
		_BumpMap ("BumpMap", 2D) = "bump" {}
		_BumpScale ("BumpScale", Float) = 1
		_OcclusionMap ("OcclusionMap", 2D) = "white" {}
		_OcclussionStrength ("OcclussionStrength", Range(0, 1)) = 1
		[Enum(Off,0,On,1)] [Header(Depth Blend)] _ZWrite ("ZWrite", Range(0, 1)) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Range(0, 255)) = 167.5171
		[Enum(UnityEngine.Rendering.BlendMode)] [Header(Blend Modes)] _BlendSrc ("BlendSrc", Range(0, 255)) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("BlendDst", Range(0, 255)) = 10
		[Enum(UnityEngine.Rendering.CullMode)] [Header(Cull)] _CullMode ("CullMode", Range(0, 255)) = 0
		[Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask ("ColorMask", Range(0, 255)) = 255
		[IntRange] [Header(Stencil)] _Stencil ("Stencil", Range(0, 255)) = 0
		[IntRange] _StencilReadMask ("StencilReadMask", Range(0, 255)) = 15
		[IntRange] _StencilWriteMask ("StencilWriteMask", Range(0, 255)) = 15
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("StencilComp", Range(0, 255)) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOpPassFront ("StencilOpPassFront", Range(0, 255)) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOpFailFront ("StencilOpFailFront", Range(0, 255)) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOpZFailFront ("StencilOpZFailFront", Range(0, 255)) = 0
		_EmissionMap ("EmissionMap", 2D) = "white" {}
		_EmissionColor ("EmissionColor", Vector) = (0,0,0,0)
		_EmissionIntensity ("EmissionIntensity", Float) = 1
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
}