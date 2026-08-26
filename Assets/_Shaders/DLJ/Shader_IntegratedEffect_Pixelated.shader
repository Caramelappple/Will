Shader "Will/Particle/IntegradedEffectPixelated"
{
		Properties {
			[Header(Main)]
				[Space]
					[HDR]_TintColor("Color",Color) = (1,1,1,1)
					[Toggle(IS_USE_SECOND_COLOR)]_SecondColor("Is use second color",int) = 0
					[HDR]_TintColor2("Color2",Color) = (1,1,1,1)
					_MainTex ("Main Tex", 2D) = "white" {}
					[Toggle] _UsePixelation ("Use Pixelation", Float) = 1
					_PixelCount ("Pixel Count (Atlas Grid)", Vector) = (160, 160, 0, 0)
					_ColorFactor("Color Factor", float) = 1
					[Toggle(IS_TEXTURE_ANIMATE)]_TextureAnimate("Is Texture Animate",int) = 0
						_TextureAnimateSpeed("Texture Animate Speed",float) = 1.0
						_TextureAnimateStyle("Texture Animate Style", Range(0,2)) = 1
					[Toggle(IS_TEXTURE_ANIMATE_ADVANCED)]_TextureAnimateAdvanced("Is Texture Animate Advanced",int) = 0 //Need to check 'Is Texture Animate'
						_MaxIndex("Texture Mix Count",int) = 2
					[Toggle(IS_TEXTURE_BLEND)]_TextureBlend("Is Texture Blend",int) = 0
					[Toggle(IS_UNITY_PARTICLE_INSTANCING_ENABLED)]_ParticleInstancing("Is Unity Paticle Instancing Enable",int) = 0
					[Toggle(IS_ALL_TEXTURE_STRAIGHT_MOVE)]_MixedMove("Is All Texture Straight Move",int) = 0
						_TexPosMove("xPosMove", Range(-1,1)) = 0
					[Toggle(IS_USE_ROTATE_UV)]_IsRotateAngle("Is Rotate Angle", int) = 0
						_RotateAngle("RotateAngle", Range(-1,1)) = 0
			[Header(Outline)]
				[Toggle] _UseOutline ("Use Outline", Float) = 1
				[HDR] _OutlineColor ("Outline Color", Color) = (0.5, 0, 1, 1)
				_OutlineWidth ("Outline Width", Range(0, 4)) = 1
				_SheetTiles ("Sheet Tiles", Vector) = (10, 10, 0, 0)
			[Header(Dithering)]
				[Toggle] _UseDithering ("Use Dithering", Float) = 0
				_DitherStrength ("Color Dither Strength", Range(0, 1)) = 1
				_DitherScale ("Screen Pixel Scale", Range(1, 8)) = 2
				[Enum(Texture Brightness, 0, Radial, 1, Vertical, 2)] _DitherSource ("Color Layer Source", Float) = 0
				[Toggle] _DitherInvert ("Invert Color Layers", Float) = 0
				_DitherLayerAngle ("Color Layer Angle", Range(-180, 180)) = 0
				_DitherValueScale ("Color Layer Scale", Range(0.1, 4)) = 1
				_DitherValueOffset ("Color Layer Offset", Range(-1, 1)) = 0
				[HideInInspector] _DitherColorCount ("Dither Color Count", Float) = 3
				[HideInInspector][HDR] _DitherColor0 ("Dither Color 0", Color) = (0.02, 0.04, 0.025, 1)
				[HideInInspector][HDR] _DitherColor1 ("Dither Color 1", Color) = (0.12, 0.30, 0.14, 1)
				[HideInInspector][HDR] _DitherColor2 ("Dither Color 2", Color) = (0.48, 0.72, 0.36, 1)
				[HideInInspector][HDR] _DitherColor3 ("Dither Color 3", Color) = (0.75, 0.90, 0.62, 1)
				[HideInInspector][HDR] _DitherColor4 ("Dither Color 4", Color) = (1, 1, 1, 1)
				[HideInInspector][HDR] _DitherColor5 ("Dither Color 5", Color) = (1, 1, 1, 1)
				[HideInInspector][HDR] _DitherColor6 ("Dither Color 6", Color) = (1, 1, 1, 1)
				[HideInInspector][HDR] _DitherColor7 ("Dither Color 7", Color) = (1, 1, 1, 1)
			[Header(Soft Particle)]
				[Space]
					[Toggle(IS_SOFT_PARTICLES)]_SoftParticle("Is Soft Particles",int) = 1
						_InvFade("Soft Particle Factor",float) = 1
			[Header(Normal)]
				[Space]
					[Toggle(IS_NORMAL_DISTORTION)]_NormalDistortion("Is Normal Distortion",int) = 0
						_NormalTex("Normal Tex",2D) = "white"{}
						_NormalDistortionFactor("Normal Distortion Factor",float) = 1.0
					[Toggle(IS_NORMAL_ANIMATE)]_NormalAnimate("Is Normal Animate",int) = 0
						_NormalAnimateSpeed("Normal Animate Speed",float) = 1.0
					[Toggle(IS_TEXTURE_NOISE)]_TextureNoise("Is Texture Noise", int)= 0
						_NoiseNormal("Noise Normal",2D) = "white"{}
						_NoiseNormalFactor("NoiseNormalFactor",float) = 1.0
			[Header(Mask Fade)]
				[Space]
					[Toggle(IS_MASK_FADE)]_MaskFade("Is Mask Fade",int) = 0
					[Toggle(IS_USE_TEXANIMATION)]_UseTexAnimation("Is UseTexAnimation",int) = 0
						_FixedMaskTex("Fixed Mask Tex",2D) = "white"{}
						_MaskTex("Mask Tex",2D) = "white"{}
						_MaskOffsetFactor("Mask Offset Factor",float) = 1.0
						_MaskDistortion("Mask Distortion Tex",2D) = "white"{}
						_MaskAnimatedSpeed("_MaskAnimatedSpeed",float) = 1.0
						_MaskCutOut("Mask CutOut",Range(0,1)) = 1
			[Header(Render)]
				[Space]
					[Toggle]_ZWrite("ZWrite On/Off", int) = 0
					[Enum(Culling Off,0, Culling Front, 1, Culling Back, 2)]_Culling("Culling",float) = 2
					[Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc("BlendSrc", float) = 1
					[Enum(UnityEngine.Rendering.BlendMode)]_BlendDst("BlendDst", float) = 1
					_ZTest2("_ZTest2", int) = 2
			[Header(VertexAnimation)]
				[Space]
					[Toggle(IS_VERTEXANIMATION)]_VertexAnimation("Is Vertex Animation", int) = 0
						_NoiseTex("Vertex Animation Noise Map",2D) = "black"{}
						_NoiseValue("Noise Value", Vector) = (1,1,1,0)
						_NoiseScale("Noise Scale", float) = 1
			[Header(RimLight)]
				[Space]
					[Toggle(IS_RIMLIGHT)]_RimLight("Is Rim Light",int) = 0
						[HDR]_RimColor("RimColor",Color) = (1,1,1,1)
						_RimScale("Rim Light Power",float) = 1
						_RimStrength("Rim Light Strength",float) = 1
			[Header(Impact)]
				[Space]
					[Toggle(IS_IMPACT)]_Impact("Is Impact",int) = 0
						_ImpactSize("Impact Size",float) = 0.5
						_ImpactFactor("Impact Factor",float) = 1
			[Header(Texcoord)]
					[Space]
						[Toggle(IS_TEXCOORD_MOVE)]_TexcoordMove("Is Texcoord Move",int) = 0
							_xTexcoordMove("xTexcoordMove", Range(-1,1)) = 0
							_yTexcoordMove("yTexcoordMove", Range(-1,1)) = 0
						_TexcoordMoveStrength("TexcoordMoveStrength",float) = 0
						[Toggle(IS_TEXCOORD_MOVE_USING_CUSTOM)]_TexcoordMoveUsingCustom("Is Texcoord Move Using Custom",int) = 0
			[Header(LinePass)]
				[Space]
					[Toggle(IS_LINEPASS)]_LinePass("Is LinePass", int) = 0
						_TexLength("TexLength",Range(0,1)) = 1.0
					_LinePassTex("LinePassTex", 2D) = "white" {}


		}
			Category{
					Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
					Blend[_BlendSrc][_BlendDst]
					Cull [_Culling]
					ZWrite[_ZWrite]
					Lighting Off
					ZTest[_ZTest2]

					SubShader {
						Pass{

							CGPROGRAM
							#pragma vertex vert
							#pragma fragment frag
							#pragma multi_compile_particles
							#pragma multi_compile_fog
							#pragma multi_compile_instancing

							#pragma shader_feature IS_SOFT_PARTICLES
							#pragma shader_feature IS_USE_SECOND_COLOR
							#pragma shader_feature IS_NORMAL_DISTORTION
							#pragma shader_feature IS_TEXCOORD_MOVE_USING_CUSTOM
							#pragma shader_feature IS_TEXTURE_NOISE
							#pragma shader_feature IS_MASK_FADE
							#pragma shader_feature IS_TEXTURE_BLEND
							#pragma shader_feature IS_TEXTURE_ANIMATE
							#pragma shader_feature IS_TEXTURE_ANIMATE_ADVANCED
							#pragma shader_feature IS_ALL_TEXTURE_STRAIGHT_MOVE
							#pragma shader_feature IS_TEXCOORD_MOVE
							#pragma shader_feature IS_NORMAL_ANIMATE
							#pragma shader_feature IS_VERTEXANIMATION
							#pragma shader_feature IS_RIMLIGHT
							#pragma shader_feature IS_IMPACT
							#pragma shader_feature UNITY_PARTICLE_INSTANCING_ENABLED
							#pragma shader_feature IS_LINEPASS
							#pragma shader_feature IS_USE_TEXANIMATION
							#pragma shader_feature IS_USE_ROTATE_UV
							
							#include "UnityCG.cginc"	
							#include "UnityStandardParticleInstancing.cginc"

							sampler2D _MainTex;
							half4 _MainTex_ST;
							
							float _UseOutline;
							half4 _OutlineColor;
							float _OutlineWidth;
							float4 _SheetTiles;

							float _UseDithering;
							float _DitherStrength;
							float _DitherScale;
							float _DitherSource;
							float _DitherInvert;
							float _DitherLayerAngle;
							float _DitherValueScale;
							float _DitherValueOffset;
							float _DitherColorCount;
							half4 _DitherColor0;
							half4 _DitherColor1;
							half4 _DitherColor2;
							half4 _DitherColor3;
							half4 _DitherColor4;
							half4 _DitherColor5;
							half4 _DitherColor6;
							half4 _DitherColor7;
							
							float _UsePixelation;
							float4 _PixelCount;
							
							float2 PixelateUV(float2 uv)
							{
							    float2 count = max(_PixelCount.xy, float2(1.0, 1.0));
							    float2 snappedUV = (floor(uv * count) + 0.5) / count;

							    return lerp(uv, snappedUV, step(0.5, _UsePixelation));
							}
							
							half SampleOutlineAlpha(float2 centerUV)
							{
							    float2 pixelCount = max(_PixelCount.xy, float2(1.0, 1.0));
							    float2 tiles = max(_SheetTiles.xy, float2(1.0, 1.0));

							    // 현재 애니메이션 프레임의 UV 영역
							    float2 tileSize = 1.0 / tiles;
							    float2 tileIndex = floor(min(centerUV, 0.99999) * tiles);
							    float2 tileMin = tileIndex * tileSize;
							    float2 tileMax = tileMin + tileSize;

							    float2 padding = 0.5 / pixelCount;
							    float2 offset = _OutlineWidth / pixelCount;

							    tileMin += padding;
							    tileMax -= padding;

							    half result = 0;

							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV + float2( offset.x, 0), tileMin, tileMax))
							    ).a);

							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV + float2(-offset.x, 0), tileMin, tileMax))
							    ).a);

							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV + float2(0,  offset.y), tileMin, tileMax))
							    ).a);

							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV + float2(0, -offset.y), tileMin, tileMax))
							    ).a);

							    // 대각선까지 읽으면 모서리가 조금 더 고르게 나와
							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV + offset, tileMin, tileMax))
							    ).a);

							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV - offset, tileMin, tileMax))
							    ).a);

							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV + float2(offset.x, -offset.y), tileMin, tileMax))
							    ).a);

							    result = max(result, tex2D(
							        _MainTex,
							        PixelateUV(clamp(centerUV + float2(-offset.x, offset.y), tileMin, tileMax))
							    ).a);

							    return result;
							}

							float Bayer2x2(float2 pixelPosition)
							{
								float2 p = fmod(floor(pixelPosition), 2.0);
								return 3.0 * p.y + 2.0 * p.x - 4.0 * p.x * p.y;
							}

							float Bayer4x4(float2 pixelPosition)
							{
								float2 p = fmod(floor(pixelPosition), 4.0);
								float lowBits = Bayer2x2(fmod(p, 2.0));
								float highBits = Bayer2x2(floor(p / 2.0));

								return (4.0 * lowBits + highBits + 0.5) / 16.0;
							}

							half3 GetDitherPaletteColor(int index)
							{
								if (index <= 0) return _DitherColor0.rgb;
								if (index == 1) return _DitherColor1.rgb;
								if (index == 2) return _DitherColor2.rgb;
								if (index == 3) return _DitherColor3.rgb;
								if (index == 4) return _DitherColor4.rgb;
								if (index == 5) return _DitherColor5.rgb;
								if (index == 6) return _DitherColor6.rgb;
								return _DitherColor7.rgb;
							}
							
							sampler2D _CameraDepthTexture;

							#ifdef IS_TEXTURE_ANIMATE_ADVANCED
										int _MaxIndex;
							#endif

							#ifdef IS_NORMAL_DISTORTION
										sampler2D _NormalTex;
										half4 _NormalTex_ST;
										half _NormalDistortionFactor;
							#endif

							#ifdef IS_TEXTURE_NOISE
										sampler2D _NoiseNormal;
										half4 _NoiseNormal_ST;
										half _NoiseNormalFactor;
							#endif

							#ifdef IS_MASK_FADE
										sampler2D _FixedMaskTex;
										half4 _FixedMaskTex_ST;
										sampler2D _MaskTex;
										half4 _MaskTex_ST;
										half _MaskOffsetFactor;
										sampler2D _MaskDistortion;
										half4 _MaskDistortion_ST;
							#endif
							half _MaskCutOut;

							#ifdef IS_ALL_TEXTURE_STRAIGHT_MOVE
										half _TexPosMove;
							#endif

							#ifdef IS_VERTEXANIMATION
										sampler2D _NoiseTex;
										half4 _NoiseTex_ST;
										half _NoiseScale;
							#endif

							#ifdef IS_RIMLIGHT
										half _RimScale;
										half _RimStrength;
							#endif

							#ifdef IS_IMPACT
										half _ImpactSize;
										half _ImpactFactor;
										int _PointSize;
										fixed4 _Points[30];
							#endif

							#ifdef IS_TEXCOORD_MOVE
										half _xTexcoordMove;
										half _yTexcoordMove;
										half _TexcoordMoveStrength;
							#endif
							
							#ifdef IS_LINEPASS
										sampler2D _LinePassTex;
										half4 _LinePassTex_ST;
										half _TexLength;
										half _TexLength2;
							#endif
								
							#ifdef IS_USE_ROTATE_UV
										half _RotateAngle;
							#endif
							

							UNITY_INSTANCING_BUFFER_START(data)
								UNITY_DEFINE_INSTANCED_PROP(half4, _TintColor)
							#define _TintColor_arr data
							#ifdef IS_USE_SECOND_COLOR
									UNITY_DEFINE_INSTANCED_PROP(half4, _TintColor2)
								#define _TintColor2_arr data
							#endif
									UNITY_DEFINE_INSTANCED_PROP(half, _ColorFactor)
								#define _ColorFactor_arr data
							#ifdef IS_RIMLIGHT
									UNITY_DEFINE_INSTANCED_PROP(half4,_RimColor)
								#define _RimColor_arr data
							#endif
							#ifdef IS_VERTEXANIMATION
									UNITY_DEFINE_INSTANCED_PROP(half4,_NoiseValue)
								#define _NoiseValue_arr data
							#endif
							#ifdef IS_TEXTURE_ANIMATE
									UNITY_DEFINE_INSTANCED_PROP(half, _TextureAnimateSpeed)
								#define _TextureAnimateSpeed_arr data
									UNITY_DEFINE_INSTANCED_PROP(int, _TextureAnimateStyle)
								#define _TextureAnimateStyle_arr data
							#endif
							#ifdef IS_NORMAL_ANIMATE
									UNITY_DEFINE_INSTANCED_PROP(half, _NormalAnimateSpeed)
								#define _NormalAnimateSpeed_arr data
							#endif
							#ifdef IS_MASK_FADE
									UNITY_DEFINE_INSTANCED_PROP(half,_MaskAnimatedSpeed)
								#define _MaskAnimatedSpeed_arr data
							#endif
							UNITY_INSTANCING_BUFFER_END(data)

							half _InvFade;
							
							struct appdata_t {
								float4 vertex : POSITION;
								float3 normal : NORMAL;
								half4 color : COLOR;
								#ifdef IS_TEXTURE_BLEND
									half4 texcoord : TEXCOORD0;
									half texcoordBlend : TEXCOORD1;
								#else
									half4 texcoord : TEXCOORD0;
									#ifdef IS_TEXCOORD_MOVE_USING_CUSTOM
									half2 texcoord2 : TEXCOORD1;
									#endif
								#endif
								UNITY_VERTEX_INPUT_INSTANCE_ID
							};

							struct v2f {
								float4 vertex : SV_POSITION;
								half4 color : COLOR;
								half2 maintex : TEXCOORD0;
								half2 ditherUV : TEXCOORD13;
								#ifdef IS_TEXTURE_BLEND
									half3 maintexBlend : TEXCOORD1;
								#endif
								#ifdef IS_NORMAL_DISTORTION
									half2 normaltex : TEXCOORD2;
								#endif
								#ifdef IS_MASK_FADE
									half2 fixedmasktex : TEXCOORD3;
									half2 masktex : TEXCOORD4;
									half2 masknormaltex: TEXCOORD5;
								#endif
								#ifdef IS_TEXTURE_NOISE
									half2 noisetex : TEXCOORD6;
								#endif
								#ifdef SOFTPARTICLES_ON
									half4 projPos : TEXCOORD7;
								#endif
								UNITY_FOG_COORDS(8)
								#ifdef IS_RIMLIGHT
									half3 viewDir : TEXCOORD9;
									half3 normal : TEXCOORD10;
								#endif
								#ifdef IS_IMPACT
									float3 worldPos : TEXCOORD11;
								#endif
								#ifdef IS_LINEPASS
									float2 linepasscoord : TEXCOORD12;
								#endif
				
								UNITY_VERTEX_INPUT_INSTANCE_ID
								UNITY_VERTEX_OUTPUT_STEREO
							};

							v2f vert (appdata_t i)
							{
								v2f o;
								UNITY_SETUP_INSTANCE_ID(i);
								UNITY_INITIALIZE_OUTPUT(v2f, o);
								UNITY_TRANSFER_INSTANCE_ID(i, o);
								UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
								
								#ifdef IS_VERTEXANIMATION
								float4 Noise = mul(UNITY_MATRIX_M, i.vertex) * UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue) * float4(0.1f, 0.1f, 1.5f, 1);
								//Set noiseTex with normal tex using time & scale info. time is for animate vertex
								float4 NoiseTex = tex2Dlod(_NoiseTex, Noise + float4(float3(_Time.x / 2, _Time.y / 2, _Time.z * 2) * _NoiseScale * 10, 0));
								//NoiseTex *= tex2Dlod(_NoiseTex, Noise - float4(float3(_Time.x / 2, _Time.y / 2, _Time.z * 2) * _NoiseScale * 10, 0));
								i.vertex = i.vertex * UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).w +
									//Add changed noise info with normal value to original object vertex.
									(saturate(NoiseTex) - 0.5f) * (
										//Additionally trigonometric value to original object vertex.
										sin((i.vertex.x + _Time * UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).x)* UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).y) +
										cos((i.vertex.y + _Time * UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).x)* UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).y) +
										sin((i.vertex.z + _Time * UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).x)* UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).y)
										)* UNITY_ACCESS_INSTANCED_PROP(_NoiseValue_arr, _NoiseValue).z*_NoiseScale * 10;
								#endif

								o.vertex = UnityObjectToClipPos(i.vertex);

								half4 originaltex = i.texcoord;
								o.ditherUV = originaltex.xy;

								#ifdef IS_TEXCOORD_MOVE
										#ifdef IS_TEXCOORD_MOVE_USING_CUSTOM
											i.texcoord.y += i.texcoord2.y;
										#else
											i.texcoord.x += _Time * _xTexcoordMove * _TexcoordMoveStrength;
											i.texcoord.y += _Time * _yTexcoordMove * _TexcoordMoveStrength;
										#endif
								#endif

								#ifdef IS_UNITY_PARTICLE_INSTANCING_ENABLED //GPU Rendering
									#ifdef IS_TEXTURE_BLEND
										vertInstancingUVs(i.texcoord.xy, o.maintex, o.,maintexBlend);
									#else
										vertInstancingUVs(i.texcoord, o.maintex);
										o.maintex = TRANSFORM_TEX(o.texcoord, _MainTex);
									#endif
								#else
									#ifdef IS_TEXTURE_BLEND
										o.maintex = i.texcoord.xy;
										o.maintexBlend.xy = i.texcoord.zw;
										o.maintexBlend.z = i.texcoordBlend;
									#else
										o.maintex = TRANSFORM_TEX(i.texcoord, _MainTex);
									#endif
								#endif

								#ifdef IS_NORMAL_DISTORTION
									o.normaltex = TRANSFORM_TEX(i.texcoord, _NormalTex);
								#endif
								#ifdef IS_MASK_FADE
									half4 masktexcoord;
									#ifdef IS_USE_TEXANIMATION
										masktexcoord = i.texcoord;
									#else
										masktexcoord = originaltex;
									#endif
									o.fixedmasktex = TRANSFORM_TEX(masktexcoord, _FixedMaskTex);
									o.masktex = TRANSFORM_TEX(masktexcoord, _MaskTex);
									o.masknormaltex = TRANSFORM_TEX(masktexcoord, _MaskDistortion);
								#endif
								#ifdef IS_TEXTURE_NOISE
									o.noisetex = TRANSFORM_TEX(i.texcoord, _NoiseNormal);
								#endif

								#ifdef IS_SOFTPARTICLES
									#ifdef SOFTPARTICLES_ON
										o.projPos = ComputeNonStereoScreenPos(o.vertex);
										COMPUTE_EYEDEPTH(o.projPos.z);
									#endif
								#endif

								#ifdef IS_RIMLIGHT
										o.viewDir = WorldSpaceViewDir(i.vertex);
										o.normal = UnityObjectToWorldNormal(i.vertex);
								#endif

								#ifdef IS_IMPACT
										o.worldPos = i.vertex;
								#endif

								#ifdef IS_LINEPASS
										half4 originaltexcoord = i.texcoord;
										float length = i.texcoord.z;
										length = lerp(1.0f, 3.0f, length);
										i.texcoord.x *= length;

										float length2 = i.texcoord.w * _TexLength * 2;
										length2 = lerp(1, 0, length2);

										i.texcoord.x -= length2;

										i.texcoord.x = clamp(i.texcoord.x, 0, 1);
										i.texcoord.y = clamp(i.texcoord.y, 0, 1);

										o.linepasscoord = TRANSFORM_TEX(i.texcoord, _LinePassTex);
										i.texcoord = originaltexcoord;
								#endif

								#ifdef IS_USE_ROTATE_UV
										float2 center = float2(0.5, 0.5);
										float cosA = cos(_RotateAngle);
										float sinA = sin(_RotateAngle);
										float2x2 rt = float2x2(cosA, -sinA, sinA, cosA);

										//main_tex uv
										float2 uv = o.maintex - center;
										o.maintex = mul(rt, uv);
										o.maintex += uv;
								#endif

								o.color = i.color;

								UNITY_TRANSFER_FOG(o, o.vertex);
								return o;
							}

							half4 frag(v2f i): SV_Target
							{
								UNITY_SETUP_INSTANCE_ID(i);
								UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
								#ifdef IS_SOFTPARTICLES
									half sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
									half partZ = i.projPos.z;
									half fade = saturate(_InvFade * (sceneZ - partZ));
									i.color.a *= fade;
								#endif

								#ifdef IS_TEXTURE_NOISE
									half2 noiseTex = tex2D(_NoiseNormal, i.noisetex);
									half2 offset = (noiseTex * 2 - 1) * _NoiseNormalFactor;
									i.maintex.xy += offset;
								#endif

								#ifdef IS_NORMAL_DISTORTION
									#ifdef IS_NORMAL_ANIMATE
										//Mixed Distort Move
										#ifdef IS_ALL_TEXTURE_STRAIGHT_MOVE
											half2 distort = UnpackNormal(tex2D(_NormalTex, i.normaltex +(float(UNITY_ACCESS_INSTANCED_PROP(_NormalAnimateSpeed_arr, _NormalAnimateSpeed)) * _Time / 10)*_TexPosMove)).rg;
											distort *= UnpackNormal(tex2D(_NormalTex, i.normaltex +((float(UNITY_ACCESS_INSTANCED_PROP(_NormalAnimateSpeed_arr, _NormalAnimateSpeed))*_Time / 10) + float2(0.5f, 0.15f))*_TexPosMove)).rg;
											distort *= UnpackNormal(tex2D(_NormalTex, i.normaltex +((float(UNITY_ACCESS_INSTANCED_PROP(_NormalAnimateSpeed_arr, _NormalAnimateSpeed))*_Time / 10) + float2(0.15f, 0.5f))*_TexPosMove)).rg;
										#else
											half2 distort = UnpackNormal(tex2D(_NormalTex, i.normaltex - (float(UNITY_ACCESS_INSTANCED_PROP(_NormalAnimateSpeed_arr, _NormalAnimateSpeed)) * _Time / 10))).rg;
											distort *= UnpackNormal(tex2D(_NormalTex, i.normaltex + (float(UNITY_ACCESS_INSTANCED_PROP(_NormalAnimateSpeed_arr, _NormalAnimateSpeed))*_Time / 10) - float2(-0.25f, -0.15f))).rg;
										#endif
									#else
										half2 distort = UnpackNormal(tex2D(_NormalTex, i.normaltex)).rg;
									#endif
									#ifdef IS_TEXTURE_BLEND
									i.maintex.xy += distort.xy* _NormalDistortionFactor;
									i.maintexBlend.xy += distort.xy* _NormalDistortionFactor;
									#else
									i.maintex.xy += distort.xy* _NormalDistortionFactor;
									#endif
								#endif

								#ifdef IS_TEXTURE_ANIMATE
									#ifdef IS_ALL_TEXTURE_STRAIGHT_MOVE
										half4 tex = tex2D(_MainTex, i.maintex.xy + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10)*_TexPosMove);
										tex *= tex2D(_MainTex, i.maintex.xy + ((float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(0.25f, -0.25f))*_TexPosMove);
										half4 tex2 = tex2D(_MainTex, i.maintex.xy + ((float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(-0.5f, 0.5f))*_TexPosMove) * 2.5f;
										tex = (tex) / 1.5f;
									#else
										#ifdef IS_TEXTURE_ANIMATE_ADVANCED
											half4 tex = half4(1, 1, 1, 1);
											half reversefactor = -1;

											for (uint j = 1; j < uint(_MaxIndex); j++) {
												half movefactor = ( uint(j) / _MaxIndex);
												half timefactor;

												reversefactor *= -1;
												timefactor = (_Time.x + movefactor) *reversefactor;
												tex *= tex2D(_MainTex, i.maintex + movefactor + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed))) * float2(timefactor, 0) );
												tex *= tex2D(_MainTex, i.maintex + movefactor + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed))) * float2(0, timefactor));
											}
											tex = saturate(pow(tex, 1.0f/_MaxIndex));
										#else
											half4 tex = half4(0, 0, 0, 0);
											if (UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateStyle_arr, _TextureAnimateStyle) == 0)
											{
												tex = tex2D(_MainTex, i.maintex.xy - (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10));
												tex *= tex2D(_MainTex, i.maintex.xy - (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(0.25f, -0.25f));
												half4 tex2 = tex2D(_MainTex, i.maintex.xy + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10));
												tex2 *= tex2D(_MainTex, i.maintex.xy + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(0.15f, -0.15f));
												tex = (tex + tex2) / 1.5f;
											}
											else if (UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateStyle_arr, _TextureAnimateStyle) == 1)
											{
												tex = tex2D(_MainTex, i.maintex.xy - (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10));
												tex *= tex2D(_MainTex, i.maintex.xy - (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(-0.25f, -0.25f));
												half4 tex2 = tex2D(_MainTex, i.maintex.xy + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10));
												tex2 *= tex2D(_MainTex, i.maintex.xy + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(0.15f, -0.15f));
												tex *= tex2 * 3.5f;
											}
											else
											{ 
												tex = tex2D(_MainTex, i.maintex.xy - (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(0.25f, -0.25f));
												half4 tex2 = tex2D(_MainTex, i.maintex.xy + (float(UNITY_ACCESS_INSTANCED_PROP(_TextureAnimateSpeed_arr, _TextureAnimateSpeed)) * _Time / 10) + float2(0.15f, -0.15f));
												tex = (tex * tex2)*1.25f + (tex + tex2)*0.5f;
											}
										#endif
									#endif
								#else
									half4 tex = tex2D(_MainTex, PixelateUV(i.maintex.xy));
									#ifdef IS_TEXTURE_BLEND
									    half4 tex2 = tex2D(
									        _MainTex,
									        PixelateUV(i.maintexBlend.xy)
									    );

									    tex = lerp(tex, tex2, i.maintexBlend.z);
									#endif
								#endif

								half outlineMask = 0;
								#ifndef IS_TEXTURE_ANIMATE
									if (_UseOutline > 0.5 && _OutlineWidth > 0)
									{
										half expandedAlpha = SampleOutlineAlpha(i.maintex.xy);

										#ifdef IS_TEXTURE_BLEND
											half expandedAlpha2 = SampleOutlineAlpha(i.maintexBlend.xy);
											expandedAlpha = lerp(expandedAlpha, expandedAlpha2, i.maintexBlend.z);
										#endif

										outlineMask = saturate(expandedAlpha - tex.a) * i.color.a;
									}
								#endif

								#ifdef IS_MASK_FADE
										half fixed_mask = tex2D(_FixedMaskTex, i.fixedmasktex);
										#ifdef IS_ALL_TEXTURE_STRAIGHT_MOVE
											half2 mask_noise = tex2D(_MaskDistortion, i.masknormaltex.xy + ((float(UNITY_ACCESS_INSTANCED_PROP(_MaskAnimatedSpeed_arr, _MaskAnimatedSpeed)) * _Time / 5) + float2(0.25f, 0.25f))*_TexPosMove);
											mask_noise *= tex2D(_MaskDistortion, i.masknormaltex.xy + ((float(UNITY_ACCESS_INSTANCED_PROP(_MaskAnimatedSpeed_arr, _MaskAnimatedSpeed)) * _Time / 4) - float2(0.25f, 0.25f))*_TexPosMove);
											mask_noise *= tex2D(_MaskDistortion, i.masknormaltex.xy + ((float(UNITY_ACCESS_INSTANCED_PROP(_MaskAnimatedSpeed_arr, _MaskAnimatedSpeed))* _Time / 20) - float2(0.25f, 0.25f))*_TexPosMove);
											half2 mask_offset = mask_noise * _MaskOffsetFactor;
										#else
											half2 mask_noise = tex2D(_MaskDistortion, i.masknormaltex.xy - (float(UNITY_ACCESS_INSTANCED_PROP(_MaskAnimatedSpeed_arr, _MaskAnimatedSpeed)) * _Time / 10) + float2(0.25f, 0.25f));
											mask_noise *= tex2D(_MaskDistortion, i.masknormaltex.xy + (float(UNITY_ACCESS_INSTANCED_PROP(_MaskAnimatedSpeed_arr, _MaskAnimatedSpeed)) * _Time / 10) - float2(0.5f, 0.5f));
											half2 mask_offset = mask_noise * _MaskOffsetFactor;
										#endif
										i.masktex.xy += mask_offset;
										#ifdef IS_USE_SECOND_COLOR
											half mask_a = saturate(tex2D(_MaskTex, i.masktex) - (1- saturate(i.color.a*_MaskCutOut))) * fixed_mask * (float(UNITY_ACCESS_INSTANCED_PROP(_TintColor2_arr, _TintColor2).a));
										#else
											half mask_a = saturate(tex2D(_MaskTex, i.masktex) - (1 - saturate(i.color.a*_MaskCutOut))) * fixed_mask * (float(UNITY_ACCESS_INSTANCED_PROP(_TintColor_arr, _TintColor).a));
										#endif
								#else
									#ifdef IS_USE_SECOND_COLOR
										half mask_a = tex.a *_MaskCutOut * i.color.a * float(UNITY_ACCESS_INSTANCED_PROP(_TintColor2_arr, _TintColor2).a);
									#else
										half mask_a = tex.a *_MaskCutOut * i.color.a * float(UNITY_ACCESS_INSTANCED_PROP(_TintColor_arr, _TintColor).a);
									#endif
								#endif

								#ifdef IS_USE_SECOND_COLOR
									half4 res = tex * float4(i.color.rgb, 1) * float4(UNITY_ACCESS_INSTANCED_PROP(_TintColor2_arr, _TintColor2).rgb, 1) * float(UNITY_ACCESS_INSTANCED_PROP(_ColorFactor_arr, _ColorFactor));
								#else
									half4 res = tex * float4(i.color.rgb,1) * float4(UNITY_ACCESS_INSTANCED_PROP(_TintColor_arr, _TintColor).rgb,1) * float(UNITY_ACCESS_INSTANCED_PROP(_ColorFactor_arr, _ColorFactor));
								#endif
								half alpha = mask_a *  float(UNITY_ACCESS_INSTANCED_PROP(_ColorFactor_arr, _ColorFactor));
								res.a = saturate(pow(alpha, 2.0f));

								if (_UseDithering > 0.5 && _DitherStrength > 0)
								{
									// 모든 파티클이 같은 화면 격자를 사용해야 겹쳐도 패턴이 무너지지 않는다.
									float2 ditherPixel = floor(i.vertex.xy / max(_DitherScale, 1.0));
									half ditherThreshold = Bayer4x4(ditherPixel);
									half ditherStrength = saturate(_DitherStrength);
									int colorCount = clamp((int)floor(_DitherColorCount + 0.5), 2, 8);

									// 텍스처 밝기, 중심 거리, 세로 위치 중 하나로 색층을 고른다.
									half sourceValue = saturate(max(tex.r, max(tex.g, tex.b)));
									if (_DitherSource > 0.5 && _DitherSource < 1.5)
										sourceValue = saturate(length((i.ditherUV - 0.5) * 2.0));
									else if (_DitherSource >= 1.5)
									{
										float layerAngle = radians(_DitherLayerAngle);
										float2 layerDirection = float2(sin(layerAngle), cos(layerAngle));
										sourceValue = saturate(dot(i.ditherUV - 0.5, layerDirection) + 0.5);
									}

									sourceValue = saturate(sourceValue * _DitherValueScale + _DitherValueOffset);
									sourceValue = lerp(sourceValue, 1.0 - sourceValue, step(0.5, _DitherInvert));
									half palettePosition = sourceValue * (colorCount - 1);
									int lowerIndex = min((int)floor(palettePosition), colorCount - 1);
									int upperIndex = min(lowerIndex + 1, colorCount - 1);
									half blendToUpper = step(ditherThreshold, frac(palettePosition));
									half3 ditheredColor = lerp(
										GetDitherPaletteColor(lowerIndex),
										GetDitherPaletteColor(upperIndex),
										blendToUpper
									);

									res.rgb = lerp(res.rgb, ditheredColor, ditherStrength);
								}

								half outlineAmount = saturate(outlineMask * _OutlineColor.a);
								res.rgb = lerp(res.rgb, _OutlineColor.rgb, outlineAmount);
								res.a = max(res.a, outlineAmount);

								#ifdef IS_RIMLIGHT
									half rim = 1.0 - saturate(dot(normalize(i.viewDir), i.normal));
									res.rgb += float3(UNITY_ACCESS_INSTANCED_PROP(_RimColor_arr, _RimColor).rgb * pow(rim, _RimScale) * _RimStrength);
								#endif

								#ifdef IS_IMPACT
									float3 objPos =i.worldPos;
									float Impact_alpha = 0.0f;
									for (unsigned int index = 0; index < _Points.Length; ++index)
									{
										float Impact = pow(saturate(frac(1.0 - saturate((_Points[index].w*_ImpactSize) - distance(_Points[index].xyz, objPos.xyz))))*saturate(1.0 - _Points[index].w),2);
										Impact_alpha += Impact * _ImpactFactor;
										Impact_alpha = pow(Impact_alpha, 1.1f);
									}

									res.a += Impact_alpha * 5.0f;
								#endif

								#ifdef IS_LINEPASS
									half4 linepasstex = tex2D(_LinePassTex, i.linepasscoord.xy);
									return res *= linepasstex;
								#endif
								UNITY_APPLY_FOG_COLOR(i.fogCoord, res, half4(0, 0, 0, 0));
								return res;
							}
							ENDCG
					}
				}
		}

		CustomEditor "DLJ_PixelatedParticleShaderGUI"
}
