float pack(float a, float b)
{
    uint aScaled = f32tof16(a);
    uint bScaled = f32tof16(b);
    uint abPacked = (aScaled << 16) | (bScaled & 0xFFFF);
    return asfloat(abPacked);
}

void unpack(float x, out float a, out float b)
{
    float inputFloat = x;
    uint uintInput = asuint(inputFloat);
    a = f16tof32(uintInput >> 16);
    b = f16tof32(uintInput & 0xFFFF);
}