// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Brushes/Cell Brush"
{
Properties {
	_Depth ("Depth", Float) = 0.5
	_Power("Envelope Power", Float) = 1.25
	_Frequency("Frequency", Float) = 2
	_Speed("Speed", Float) = 1
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
	Blend One One
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile A B C D E F G H

			#include "UnityCG.cginc"

			float _Depth;
			float _Power;
			float _Frequency;
			float _Phase;
			float _Speed;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				o.texcoord1 = mul(unity_ObjectToWorld, v.vertex).xz;
				return o;
			}
			
            // Distance metric. A slightly rounded triangle is being used (C here), but the usual distance
            // metrics all work.
            float s(float2 p){
                
                p = frac(p) - .5;  
                 
                #if A
                return max(abs(p.x)*.866 + p.y*.5, -p.y);
                #endif
                
                #if B
                return (length(p)*1.5 + .25)*max(abs(p.x)*.866 + p.y*.5, -p.y);
                #endif
                
                #if C
                return (dot(p, p)*2. + .5)*max(abs(p.x)*.866 + p.y*.5, -p.y);
                #endif
                
                #if D
                return dot(p, p)*2.;
                #endif
                
                #if E
                return length(p);
                #endif
                
                #if F
                return max(abs(p.x), abs(p.y)); // Etc.
                #endif
                
                #if G
                return abs(p.x)+abs(p.y);
                #endif
                
                #if H
                return max(max(abs(p.x)*.866 + p.y*.5, -p.y), -max(abs(p.x)*.866 - p.y*.5, p.y) + .2);
                #endif
                
                return 0;
            }
            
            // Very cheap wrappable cellular tiles. This one produces a block pattern on account of the
            // metric used, but other metrics will produce the usual patterns.
            //
            // Construction is pretty simple: Plot two points in a wrappable cell and record their distance. 
            // Rotate by a third of a circle then repeat ad infinitum. Unbelievably, just one rotation 
            // is needed for a random looking pattern. Amazing... to me anyway. :)
            //
            // Note that there are no random points at all, no loops, and virtually no setup, yet the 
            // pattern appears random anyway.
            float m(float2 p){    
                
                // Offset - used for animation. Put in as an afterthought, so probably needs more
                // tweaking, but it works well enough for the purpose of this demonstration.
                float2 o = sin(float2(1.93, 0) + _Time.y*_Speed)*.166;
                
                // The distance to two wrappable, moving points.
                float a = s(p + float2(o.x, 0)), b = s(p + float2(0, .5 + o.y));
                
                // Rotate the layer (coordinates) by 120 degrees. 
                p = mul((p + .5),-float2x2(.5, -.866, .866, .5));
                // The distance to another two wrappable, moving points.
                float c = s(p + float2(o.x, 0)), d = s(p + float2(0, .5 + o.y)); 
            
                // Rotate the layer (coordinates) by 120 degrees.
                p = mul((p + .5),-float2x2(.5, -.866, .866, .5));
                // The distance to yet another two wrappable, moving points.
                float e = s(p + float2(o.x, 0)), f = s(p + float2(0, .5 + o.y)); 
            
                // Return the minimum distance among the six points.
                return min(min(min(a, b), min(c, d)),  min(e, f))*2.;
            }

			float powerPulse( float x, float power )
			{
				x = saturate(abs(x));
				return pow((x + 1.0f) * (1.0f - x), power);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float dist = length(i.texcoord - float2(.5,.5));
				return _Depth * powerPulse(dist * 2,_Power) * m(i.texcoord1.xy*_Frequency);
			}
			ENDCG 
		}
	}	
}
CustomEditor "CellBrushEditor"
}
