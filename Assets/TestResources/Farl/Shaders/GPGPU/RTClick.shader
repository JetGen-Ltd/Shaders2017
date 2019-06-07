﻿// http://matthias-mueller-fischer.ch/talks/GDC2008.pdf

Shader "Hidden/Farl/RTClick"
{
	Properties
	{
		_Click ("Click", Vector) = (0, 0, 0, 0)
		_WaveParam ("Wave (Density, Damping Force, Speed, Damping", Vector) = (0, 0, 0, 0)
        _PositionBuffer ("-", 2D) = "gray"
        _VelocityBuffer ("-", 2D) = "gray"
	}

	CGINCLUDE
			
		#include "UnityCG.cginc"

		float4 _Click;
		float4 _WaveParam;
		sampler2D _PositionBuffer;
		uniform float4 _PositionBuffer_TexelSize;
		sampler2D _VelocityBuffer;

		float4 updatePosition(v2f_img i) : SV_Target
		{
   			float4 p = tex2D(_PositionBuffer, i.uv);
   			float4 v = tex2D(_VelocityBuffer, i.uv) * (2 * 10) - 10;
			float dt = 1 / 60.0;

			p.xyz = p.xyz + v.xyz * dt;

   			if (_Click.w >= 1)
   			{
				float f = saturate(0.1 - length(_Click.xy - i.uv));
				if (f > 0)
					p.y = min(p.y, f);
   			}

   			p = clamp(p, 0, 1);

			return p;
		}

		float4 updateVelocity(v2f_img i) : SV_Target
		{
   			float4 v = tex2D(_VelocityBuffer, i.uv) * (2 * 10) - 10;
			float dt = 1 / 60.0;


   			int r = 1;

   			float h = _WaveParam.x + 1e-9;
   			float c = _WaveParam.z * (h / dt);

   			float k = _WaveParam.y;

   			float4 p = tex2D(_PositionBuffer, i.uv);
			float4 f = tex2D(_PositionBuffer, saturate(i.uv + float2(1, 0) * _PositionBuffer_TexelSize.xy));
			f += tex2D(_PositionBuffer, saturate(i.uv + float2(-1, 0) * _PositionBuffer_TexelSize.xy));
			f += tex2D(_PositionBuffer, saturate(i.uv + float2(0, 1) * _PositionBuffer_TexelSize.xy));
			f += tex2D(_PositionBuffer, saturate(i.uv + float2(0, -1) * _PositionBuffer_TexelSize.xy));
			f -= 4 * p;

   			float3 acc = ((0.5 - p) * k) + (pow(c, 2) * f / pow(h, 2));

   			float drag = _WaveParam.w;
   			v *= drag;

   			v.xyz = v.xyz + acc * dt;


   			v = clamp(v, -10, 10);
   			v = (v + 10) / (2 * 10);

			return v;
		}

	ENDCG

	SubShader
	{
		Pass
		{
            CGPROGRAM
            #pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment updatePosition
			ENDCG
		}
		Pass
		{
            CGPROGRAM
            #pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment updateVelocity
			ENDCG
		}
	}
}