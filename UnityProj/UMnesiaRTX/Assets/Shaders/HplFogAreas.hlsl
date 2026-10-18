// cFogArea (FogArea.cpp, Renderer.cpp, deferred_fog_frag.glsl): the fog areas in view of the current camera, set by
// RTXMnesia.World.HplFogAreaPass before the camera draws (AfterOpaqueColor), sorted back to front. A box is the unit
// cube (-0.5..0.5) of the area matrix. Used by Hidden/RTXMnesia/HplFogArea (the areas over the opaque world) and by
// the translucent shaders (iRenderer::GetFogAreaVisibilityForObject: their alpha × visibility).
#ifndef HPL_FOG_AREAS_INCLUDED
#define HPL_FOG_AREAS_INCLUDED

#define HPL_MAX_FOG_AREAS 16

int _HplFogAreaCount;
float4x4 _HplFogAreaWorldToBox[HPL_MAX_FOG_AREAS]; // absolute world → box space
float4 _HplFogAreaColor[HPL_MAX_FOG_AREAS];        // HPL2 (gamma) color, a = opacity at the end
float4 _HplFogAreaParams[HPL_MAX_FOG_AREAS];       // start, end - start, falloff exponent, flags
float4 _HplFogAreaRadiance[HPL_MAX_FOG_AREAS];     // rgb: the color as HDRP linear radiance (HplLighting.Radiance)

// Flags: the near plane of the camera touches the box (mbInsideNearFrustum), the backside of the box limits the fog
// (ShowBacksideWhenInside / WhenOutside for that state).
#define HPL_FOG_AREA_INSIDE 1
#define HPL_FOG_AREA_BACKSIDE 2

bool HplFogAreaFlag(float4 params, int flag)
{
    return ((int)(params.w + 0.5) & flag) != 0;
}

// iRenderer::CheckFogAreaRayIntersection: the sides of the cube hit by the ray at t >= 0 (t in world units of the
// direction), entry = the nearest, exit = the farthest. From inside only the exit is hit: entry = exit.
bool HplFogAreaRay(float4x4 worldToBox, float3 boxRayStart, float3 rayDirWS, out float entry, out float exitDist)
{
    float3 dir = mul((float3x3)worldToBox, rayDirWS);
    bool found = false;
    entry = 0.0;
    exitDist = 0.0;
    [unroll] for (int i = 0; i < 6; i++)
    {
        float3 normal = 0.0;
        normal[i >> 1] = (i & 1) ? 1.0 : -1.0;
        float mulDir = dot(normal, dir);
        if (abs(mulDir) < 0.0001) continue;
        float t = -(dot(normal, boxRayStart) + 0.5) / mulDir;
        if (t < 0.0) continue;
        if (any(abs(dir * t + boxRayStart) > 0.5001)) continue;
        if (!found)
        {
            entry = t;
            exitDist = t;
            found = true;
        }
        else
        {
            entry = min(entry, t);
            exitDist = max(exitDist, t);
        }
    }
    return found && exitDist >= 0.0;
}

// iRenderer::GetFogAreaVisibilityForObject for all areas (the product): the radial distance the ray from the camera
// to the object's center travels in each area.
float HplFogAreaVisibility(float3 objectCenterAbsWS, float3 cameraAbsWS)
{
    float visibility = 1.0;
    float3 ray = objectCenterAbsWS - cameraAbsWS;
    float cameraDistance = length(ray);
    if (cameraDistance <= 0.0) return 1.0;
    ray /= cameraDistance;
    [loop] for (int i = 0; i < min(_HplFogAreaCount, HPL_MAX_FOG_AREAS); i++)
    {
        float4 params = _HplFogAreaParams[i];
        float4x4 worldToBox = _HplFogAreaWorldToBox[i];
        float entry, exitDist;
        if (!HplFogAreaRay(worldToBox, mul(worldToBox, float4(cameraAbsWS, 1.0)).xyz, ray, entry, exitDist)) continue;
        bool inside = HplFogAreaFlag(params, HPL_FOG_AREA_INSIDE);
        bool backside = HplFogAreaFlag(params, HPL_FOG_AREA_BACKSIDE);
        if (!inside && cameraDistance < entry) continue;

        float fogDist = inside
            ? (backside ? min(exitDist, cameraDistance) : cameraDistance)
            : (backside ? min(exitDist - entry, cameraDistance - entry) : cameraDistance - entry);
        if (fogDist <= 0.0) continue;
        float fogStart = params.x, fogEnd = params.x + params.y;
        float fogAlpha = 1.0 - _HplFogAreaColor[i].a;
        if (fogDist < fogStart) continue;
        if (fogDist > fogEnd || params.y <= 0.0)
        {
            visibility *= fogAlpha;
            continue;
        }
        float alpha = (fogDist - fogStart) / (fogEnd - fogStart);
        if (params.z != 1.0) alpha = pow(alpha, params.z);
        visibility *= (1.0 - alpha) + fogAlpha * alpha;
    }
    return visibility;
}

// cRendererDeferred::RenderFog for one pixel: what the boxes of all areas drew there, composited (alpha blend, back
// to front) as color + transmittance × destination. ray: the world ray through the pixel with view depth 1 per unit,
// sceneDepth: the view depth of the opaque surface (0 for nothing drawn: sky).
void HplFogAreasAlongRay(float3 cameraAbsWS, float3 ray, float sceneDepth, bool sky, float exposure, out float3 color, out float transmittance)
{
    color = 0.0;
    transmittance = 1.0;
    [loop] for (int i = 0; i < min(_HplFogAreaCount, HPL_MAX_FOG_AREAS); i++)
    {
        float4 params = _HplFogAreaParams[i];
        float4x4 worldToBox = _HplFogAreaWorldToBox[i];
        float3 origin = mul(worldToBox, float4(cameraAbsWS, 1.0)).xyz;
        float3 dir = mul((float3x3)worldToBox, ray);
        float3 invDir = 1.0 / (abs(dir) > 1e-12 ? dir : (dir >= 0.0 ? 1e-12 : -1e-12));
        float3 t0 = (-0.5 - origin) * invDir, t1 = (0.5 - origin) * invDir;
        float3 tNear = min(t0, t1), tFar = max(t0, t1);
        float front = max(max(tNear.x, tNear.y), tNear.z);
        float back = min(min(tFar.x, tFar.y), tFar.z);
        if (back < front) continue;
        bool backside = HplFogAreaFlag(params, HPL_FOG_AREA_BACKSIDE);

        float depth;
        if (HplFogAreaFlag(params, HPL_FOG_AREA_INSIDE))
        {
            if (back <= 0.0) continue;
            depth = backside ? min(back, sceneDepth) : sceneDepth;
        }
        else
        {
            // The front face in front of the camera and of the scene (the depth test).
            if (front <= 0.0 || (!sky && front >= sceneDepth)) continue;
            depth = sceneDepth - front;
            if (backside) depth = min(back - front, depth);
        }
        if (params.y <= 0.0) continue;
        depth = min(depth - params.x, params.y);
        float amount = max(depth / params.y, 0.0);
        float alpha = amount > 0.0 ? pow(amount, params.z) * _HplFogAreaColor[i].a : 0.0;
        color = color * (1.0 - alpha) + _HplFogAreaRadiance[i].rgb * exposure * alpha;
        transmittance *= 1.0 - alpha;
    }
}

#endif
