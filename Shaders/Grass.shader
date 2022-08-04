//Custom Grass Geometry Shader by Traegis

Shader "Custom/Grass"
{
	Properties
	{
		_BaseColour("Base Colour", Color) = (0.1, 0.3, 0, 1)
		_TipColour("Tip Colour", Color) = (0.7, 0.9, 0.4, 1)
		_ColourRandom("Colour Randomness", Range(0, 1)) = 0.1
		_TintAmount("Tint Amount", Range(0, 1)) = 0.1
		_TintColour("Tint Colour", Color) = (0.33, 0.16, 0.1, 1)
			[Space(15)]
		_BladeWidth("Blade Width", Range(0, 0.2)) = 0.05
		_BladeWidthRandom("Blade Width Randomness", Range(0, 1)) = 0.5
			[Space(15)]
		_BladeHeight("Blade Height", Range(0, 2)) = 0.6
		_BladeHeightRandom("Blade Height Randomness", Range(0, 1)) = 0.5
			[Space(15)]
		_BladeBendDistance("Bend Distance", Range(0, 1)) = 0.38
		_BladeBendCurve("Bend Curvature", Range(1, 4)) = 2
		_BladeBendAmount("Bend Movement Amount", Range(0, 1)) = 0.15
		_BendDelta("Bend Variation", Range(0, 1)) = 0.2
			[Space(15)]
		_TessDensity("Tessellation Density", Range(0, 1000)) = 100
		_MaxDistance("Max Draw Distance", Float) = 20
		_FadeDistance("Fade Distance", Float) = 3
			[Space(15)]
		_AmbientColour("Ambient Lighting", Color) = (0.25, 0.25, 0.25, 1)
			[Space(15)]


		_WindTintScale("Wind Tint Scale", Float) = 1
		_WindDir("Wind Direction (XZ)", Vector) = (1, 0, 0.5, 0)

		_WindMap("Wind Offset Map", 2D) = "bump" {}
		_WindFrequency("Wind Pulse Frequency", Range(0, 1)) = 0.01
		
		[Toggle] _DisplacementEnabled("Enable Player Displacement", Int) = 0
		_DisplacementRadius("Displacement Radius", Float) = 0.5
		_DisplacementStrength("Displacement Strength", Float) = 1
		_DisplacementTintScale("Displacement Tint Scale", Float) = 1

		_GrassThreshold("Grass Visibility Threshold", Range(-0.1, 1)) = 0.5
		_GrassFalloff("Grass Visibility Fade-In Falloff", Range(0, 0.5)) = 0.05
		_GlobalEffectRT("Grass Visibility Map", 2D) = "white" {}

	}
	
	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"RenderPipeline" = "UniversalPipeline"
		}
		LOD 100
		Cull Off

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


			// "Hash without Sine": https://www.shadertoy.com/view/4djSRW
			float rand(float3 p3)
			{
				p3  = frac(p3 * .1031);
				p3 += dot(p3, p3.zyx + 31.32);
				return frac((p3.x + p3.y) * p3.z);
			}

			// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
			float3x3 angleAxis3x3(float angle, float3 axis)
			{
				float c, s;
				sincos(angle, s, c);

				float t = 1 - c;
				float x = axis.x;
				float y = axis.y;
				float z = axis.z;

				return float3x3
				(
					t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
					t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
					t * x * z - s * y,  t * y * z + s * x,  t * z * z + c
				);
			}


			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

			#pragma require tessellation tessHW


			#define BLADE_SEGMENTS 5
			#define UNITY_PI 3.14159265359f
			#define UNITY_TWO_PI 6.28318530718f

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseColour;
				float4 _TipColour;
				float _ColourRandom;
				float _TintAmount;
				float4 _TintColour;

				float _BladeWidth;
				float _BladeWidthRandom;

				float _BladeHeight;
				float _BladeHeightRandom;

				float _BladeBendDistance;
				float _BladeBendCurve;
				float _BladeBendAmount;
				float _BendDelta;

				float _TessDensity;
				float _MaxDistance;
				float _FadeDistance;

				float4 _AmbientColour;

				float4 _WindDir;
				float _WindTintScale;
				float  _GrassThreshold;
				float  _GrassFalloff;


				sampler2D _WindMap;
				float4 _WindMap_ST;
				float  _WindFrequency;

				int _DisplacementEnabled;
				float _DisplacementRadius;
				float _DisplacementStrength;
				float _DisplacementTintScale;


			CBUFFER_END

			uniform float3 _PlayerPosition;
			uniform sampler2D _GlobalEffectRT;
			uniform float _OrthographicCamSize;
			uniform float3 _CameraPosition;


			// original appdata input
			struct AppVertex
			{
				float4 vertex 	: POSITION;
				float3 normal 	: NORMAL;
				float4 tangent 	: TANGENT;
				float2 uv 		: TEXCOORD0;
				float4 color 	: COLOR;
			};

			// vert -> tess -> geom output
			struct VertexData
			{
				float4 vertex 	: SV_POSITION;
				float3 normal 	: NORMAL;
				float4 tangent 	: TANGENT;
				float2 uv 		: TEXCOORD0;
				float4 color 	: TEXCOORD1;
			};

			// geom -> frag output
			struct GrassData
			{
				float4 pos 		: SV_POSITION;
				float2 uv 		: TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float3 normal 	: TEXCOORD2;
				float3 colOff 	: TEXCOORD3;
				float  tintMul 	: TEXCOORD4;
			};

			// vertex shader
			// store vertex data in world space
			VertexData vert(AppVertex v)
			{
				VertexData o;
				o.vertex = float4(TransformObjectToWorld(v.vertex), 1.0f);
				o.normal = TransformObjectToWorldNormal(v.normal);
				o.tangent = v.tangent;
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			// create vertex and transform to clip space
			GrassData CreateVert(float3 pos, float3 offset, float3x3 transform, float2 uv, float3 normal, float3 colOff, float tintMul)
			{
				GrassData o;

				o.pos = TransformObjectToHClip(pos + mul(transform, offset));
				o.uv = uv;
				o.worldPos = TransformObjectToWorld(pos + mul(transform, offset));
				o.normal = normal;
				o.colOff = colOff;
				o.tintMul = tintMul;

				return o;
			}

			// geometry shader
			[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
			void geom(point VertexData input[1], inout TriangleStream<GrassData> triStream)
			{
				// scale comes from red vertex colour
				float grassScale = input[0].color.r;

				// smoothly fade near the max distance cutoff
				float3 worldPos = TransformObjectToWorld(input[0].vertex.xyz);
				float dist = distance(worldPos, _PlayerPosition);
				float distScale = 1.0 - smoothstep(_MaxDistance - _FadeDistance, _MaxDistance, dist);
				grassScale *= distScale;

				// cull nonvisible grass (masked out or too far away)
				if (grassScale >= 0)
				{
					// allow mesh to distort grass
					float3 pos = input[0].vertex.xyz;
					float3 worldPosition = mul(unity_ObjectToWorld, input[0].vertex).xyz;
					float2 uv = worldPosition.xz - _CameraPosition.xz;
					uv /= (_OrthographicCamSize*2);
					uv += 0.5;
					float grassVisibility = tex2Dlod(_GlobalEffectRT, float4(uv, 0, 0)).r;

					float3 normal = input[0].normal;
					float4 tangent = input[0].tangent;
					float3 bitangent = cross(normal, tangent.xyz) * tangent.w;

					float3x3 tangentToLocal = float3x3
						(
							tangent.x, bitangent.x, normal.x,
							tangent.y, bitangent.y, normal.y,
							tangent.z, bitangent.z, normal.z
							);

					// Rotate around the y-axis a random amount.
					float3x3 randRotMatrix = angleAxis3x3(rand(pos) * UNITY_TWO_PI, float3(0, 0, 1.0f));

					// Rotate around the bottom of the blade a random amount.
					float3x3 randBendMatrix = angleAxis3x3(rand(pos.zzx) * _BendDelta * UNITY_PI * 0.5f, float3(-1.0f, 0, 0));

					float tintMul = 1 - input[0].color.g;

					float2 windUV = pos.xz * _WindMap_ST.xy + _WindMap_ST.zw + normalize(_WindDir.xzy) * _WindFrequency * _Time.y;
					float2 windSample = (tex2Dlod(_WindMap, float4(windUV, 0, 0)).xy * 2 - 1) * length(_WindDir);

					float3 displacement = 0;
					if (_DisplacementEnabled == 1)
					{
						float3 playerDist = distance(_PlayerPosition, worldPos);
						float3 displacementFallof = 1 - saturate(playerDist - _DisplacementRadius);
						displacement = (worldPos - _PlayerPosition) * displacementFallof * _DisplacementStrength;
						displacement *= 1 - tintMul * (1 - _DisplacementTintScale);

						windSample *= 1 - displacementFallof;
					}

					float3 windAxis = normalize(float3(windSample.x, windSample.y, 0));
					float3x3 windMatrix = angleAxis3x3(UNITY_PI * windSample, windAxis);

					// Transform the grass blades to the correct tangent space.
					float3x3 baseTransformationMatrix = mul(tangentToLocal, randRotMatrix);
					float3x3 tipTransformationMatrix = mul(mul(mul(tangentToLocal, windMatrix), randBendMatrix), randRotMatrix);

					float falloff = smoothstep(_GrassThreshold, _GrassThreshold + _GrassFalloff, grassVisibility);

					float widthLimit = _BladeWidth * _BladeWidthRandom;
					float heightLimit = _BladeHeight * _BladeHeightRandom;
					float width = lerp(_BladeWidth - widthLimit, _BladeWidth + widthLimit, rand(pos.xzy) * falloff);
					float height = lerp(_BladeHeight - heightLimit, _BladeHeight + heightLimit, rand(pos.zyx) * falloff);
					float forward = rand(pos.yyz) * _BladeBendDistance;


					float3 colOff = (float3(rand(pos.xyz), rand(pos.yzx), rand(pos.zxy)) * 2 - 1) * _ColourRandom;



					// Create blade segments by adding two vertices at once.
					for (int i = 0; i < BLADE_SEGMENTS; ++i)
					{
						float t = i / (float)BLADE_SEGMENTS;
						//float3 offset = float3(width * (1 - t), pow(t, _BladeBendCurve) * forward, height * t);
						float3 offset = float3(width * (1 - t), pow(t, _BladeBendCurve) * forward - 0.1, height*t);

						float3x3 transformationMatrix = (i == 0) ? baseTransformationMatrix : tipTransformationMatrix;
						float3 newPos = (i == 0) ? pos : pos + (displacement) * t;

						triStream.Append(CreateVert(newPos, float3(offset.x, offset.y, offset.z), transformationMatrix, float2(0, t), normal, colOff, tintMul));
						triStream.Append(CreateVert(newPos, float3(-offset.x, offset.y, offset.z), transformationMatrix, float2(1, t), normal, colOff, tintMul));
					}

					// Add the final vertex at the tip of the grass blade.
					float3 tipPos = pos + displacement;
					triStream.Append(CreateVert(tipPos, float3(0, forward, height), tipTransformationMatrix, float2(0.5, 1), normal, colOff, tintMul));

					triStream.RestartStrip();
				}
			}

			// begin tessellation
			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside  : SV_InsideTessFactor;
			};

			[domain("tri")]
			[outputcontrolpoints(3)]
			[outputtopology("triangle_cw")]
			[partitioning("integer")] // fractional_odd, fractional_even, integer, pow2
			[patchconstantfunc("patchConstantFunc")]
			VertexData hull(InputPatch<VertexData, 3> patch, uint id : Sv_OutputControlPointID)
			{
				return patch[id];
			}

			// determine tessellation amount
			float EdgeFactor(VertexData vert0, VertexData vert1)
			{
				float3 v0 = vert0.vertex.xyz;

				// cull by max distance to camera
				float dist = distance(v0, _PlayerPosition);
				if (dist > _MaxDistance + _FadeDistance)
					return 0;

				// density independent of mesh scale
				float3 v1 = vert1.vertex.xyz;
				float edgeLength = distance(v0, v1);
				return edgeLength / (10 / _TessDensity);
			}

			TessellationFactors patchConstantFunc(InputPatch<VertexData, 3> patch)
			{
				TessellationFactors f;

				f.edge[0] = EdgeFactor(patch[1], patch[2]);
				f.edge[1] = EdgeFactor(patch[2], patch[0]);
				f.edge[2] = EdgeFactor(patch[0], patch[1]);
				f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0f;

				return f;
			}

			// interpolate tessellated vertices
			[domain("tri")]
			VertexData domain(TessellationFactors factors, OutputPatch<VertexData, 3> patch, float3 barycentricCoords : SV_DomainLocation)
			{
				VertexData i;

				#define INTERPOLATE(fieldName) i.fieldName = \
					patch[0].fieldName * barycentricCoords.x + \
					patch[1].fieldName * barycentricCoords.y + \
					patch[2].fieldName * barycentricCoords.z;

				// all VertexData attributes
				INTERPOLATE(vertex);
				INTERPOLATE(normal);
				INTERPOLATE(tangent);
				INTERPOLATE(uv);
				INTERPOLATE(color);

				return i;
			}
		ENDHLSL

		// render pass
		Pass
		{
			Name "RenderGrass"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
				#pragma require geometry
				#pragma geometry geom
				#pragma hull hull
				#pragma domain domain
				#pragma vertex vert
				#pragma fragment frag

				float4 frag(GrassData i) : SV_Target
				{
					float3 lighting = 0;

					// main lighting
					#if defined(SHADOWS_SCREEN)
						half4 clipPos = TransformWorldToHClip(i.worldPos);
						half4 shadowCoord = ComputeScreenPos(clipPos);
					#else
						half4 shadowCoord = TransformWorldToShadowCoord(i.worldPos);
					#endif

					Light light = GetMainLight(shadowCoord);
					lighting += light.color * saturate(dot(i.normal, light.direction)) * light.distanceAttenuation * light.shadowAttenuation;


					// additional lighting
					int pixelLightCount = GetAdditionalLightsCount();
					for (int li = 0; li < pixelLightCount; li++) {
						#if defined(SHADOWS_SCREEN)
							half4 clipPos = TransformWorldToHClip(i.worldPos);
							half4 shadowCoord = ComputeScreenPos(clipPos);
						#else
							half4 shadowCoord = TransformWorldToShadowCoord(i.worldPos);
						#endif

						Light light = GetAdditionalLight(li, i.worldPos, shadowCoord);
						lighting += light.color * saturate(dot(i.normal, light.direction)) * light.distanceAttenuation * light.shadowAttenuation;
					}


					// ambient
					lighting = max(lighting, _AmbientColour.rgb);


					// colour & tinting
					float3 colour = lerp(_BaseColour, _TipColour, i.uv.y);
					//colour += i.colOff;
					colour *= lerp(1, _TintColour, i.colOff);
					colour *= lerp(1, _TintColour, _TintAmount);
					
					return float4(colour * lighting, 1.0f);
				}
			ENDHLSL
		}

		// shadow pass
		Pass
		{
			Name "GrassShadows"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM

			#define SHADERPASS_SHADOWCASTER

			#pragma require geometry
			#pragma geometry geom
			#pragma hull hull
			#pragma domain domain
			#pragma vertex vert
			#pragma fragment frag

			half4 frag(VertexData input) : SV_TARGET {
				return 1;
			}

			ENDHLSL
		}
	}
}
