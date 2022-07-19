#pragma once
#include "Assets/Shaders/Volumetric.cginc"

float _ExtinctionCoefficient;

struct RaymarchStatus {
	float3 intensity;
	float depth;
	float depthweightsum;
	float intTransmittance;
};

void InitRaymarchStatus(inout RaymarchStatus result){
	result.intTransmittance = 1.0f;
	result.intensity = 0.0f;
	result.depthweightsum = 0.00001f;
	result.depth = 0.0f;
}

void IntegrateRaymarch(float3 startPos, float3 rayPos, float fade, float stepsize, inout RaymarchStatus result){
	float4 c = VolumeSampleColor(rayPos);
	float density = c.a;
	if (density <= 0.0f)
		return;
	float extinction = _ExtinctionCoefficient * density / (1-fade);

	float clampedExtinction = max(extinction, 1e-7);
	float transmittance = exp(-extinction * stepsize);
	
	float3 luminance = c.rgb;
	float3 integScatt = (luminance - luminance * transmittance) / clampedExtinction;
	float depthWeight = result.intTransmittance * (1-transmittance);		//Is it a better idead to use (1-transmittance) * intTransmittance as depth weight?

	result.intensity += result.intTransmittance * integScatt;
	result.depth += depthWeight * length(rayPos - startPos);
	result.depthweightsum += depthWeight;
	result.intTransmittance *= transmittance;
}
 
float GetDensity(float3 startPos, float3 dir, float maxSampleDistance, float raymarchOffset, out float3 intensity,out float depth) {
	float raymarchDistance = 0;

	RaymarchStatus result;
	InitRaymarchStatus(result);

	[loop]
	for (int j = 0; j < SAMPLE_COUNT; j++) {
		float prevRayDist = raymarchDistance;
		raymarchDistance = pow((j+raymarchOffset)/SAMPLE_COUNT,2) * _ProjectionParams.z;
		if(raymarchDistance > maxSampleDistance) break;
		float step = raymarchDistance - prevRayDist;
		float3 rayPos = startPos + dir * raymarchDistance;
		float fade = smoothstep(_ProjectionParams.z*.9,_ProjectionParams.z, raymarchDistance);
		IntegrateRaymarch(startPos, rayPos, fade, step, result);
		if (result.intTransmittance < 0.01f) {
			result.intTransmittance = 0;
			break;
		}
	}

	depth = result.depth / result.depthweightsum / _ProjectionParams.z;
	if (depth == 0.0f) {
		depth = maxSampleDistance;
	}
	intensity = result.intensity;
	return (1.0f - result.intTransmittance);	
}
