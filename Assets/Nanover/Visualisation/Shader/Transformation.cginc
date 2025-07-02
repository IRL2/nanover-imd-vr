/// Contains methods for setting up world-to-object/object-to-world matrices for various cases

#ifndef TRANSFORMATION_CGINC_INCLUDED

    #define TRANSFORMATION_CGINC_INCLUDED

    uniform float4x4 ObjectToWorld = float4x4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);
    uniform float4x4 WorldToObject = float4x4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);
    uniform float4x4 ObjectToWorldInverseTranspose = float4x4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);

    #if defined(UNITY_CG_INCLUDED)
        #define InstanceSpaceViewDirection(v) mul(WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz - v
    #else
        #define InstanceSpaceViewDirection(v) float3(1, 0, 0)
    #endif
    
     /// Setup the transformation matrices for local and world transformations for 
    /// a world transformation with orthonormal basis vectors ex, ey, ez with 
    /// scales s, centered at a position p
    void setup_transformation(float3 ex, float3 ey, float3 ez, float3 s, float3 p) {
    
        unity_ObjectToWorld._11_21_31_41 = s.x * float4(ex, 0);
        unity_ObjectToWorld._12_22_32_42 = s.y * float4(ey, 0);
        unity_ObjectToWorld._13_23_33_43 = s.z * float4(ez, 0);
        unity_ObjectToWorld._14_24_34_44 = float4(p, 1);

        unity_WorldToObject._11_12_13_14 = float4(ex, -dot(ex, p)) / s.x;
        unity_WorldToObject._21_22_23_24 = float4(ey, -dot(ey, p)) / s.y;
        unity_WorldToObject._31_32_33_34 = float4(ez, -dot(ez, p)) / s.z;
        unity_WorldToObject._41_42_43_44 = float4(0,0,0,1);
    
        unity_ObjectToWorld = mul(ObjectToWorld, unity_ObjectToWorld);
        unity_WorldToObject = mul(unity_WorldToObject, WorldToObject);
    }


    /// Set the transformation matrices for an object with a certain position and scale, but with no/irrelevant
    /// rotation (isotropic). Used for position spheres.
    void setup_isotropic_transformation(float3 position, float scale) {
    
        unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
        unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
        unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
        unity_ObjectToWorld._14_24_34_44 = float4(position, 1);

        unity_WorldToObject._11_21_31_41 = float4(1/scale, 0, 0, 0);
        unity_WorldToObject._12_22_32_42 = float4(0, 1/scale, 0, 0);
        unity_WorldToObject._13_23_33_43 = float4(0, 0, 1/scale, 0);
        unity_WorldToObject._14_24_34_44 = float4(-position.x/scale, -position.y/scale, -position.z/scale, 1);

        unity_ObjectToWorld = mul(ObjectToWorld, unity_ObjectToWorld);
        unity_WorldToObject = mul(unity_WorldToObject, WorldToObject);
    }
    
    // https://stackoverflow.com/a/43454629
    float3 arbitrary_orthogonal(float3 vec)
    {
        bool b0 = (abs(vec.x) <  abs(vec.y)) && (abs(vec.x) <  abs(vec.z));
        bool b1 = (abs(vec.y) <= abs(vec.x)) && (abs(vec.y) <  abs(vec.z));
        bool b2 = (abs(vec.z) <= abs(vec.x)) && (abs(vec.z) <= abs(vec.y));
    
        return cross(vec, float3(b0, b1, b2));
    }

    /// Set the transformation matrices for an object between two positions, but with no specific rotation relative
    /// to the axis between the points. Used for position cylinders.
    void setup_isotropic_edge_transformation(float3 startPosition, float3 endPosition, float scale) {
        // translation should be the midpoint
        float4 p = float4(0.5 * (startPosition + endPosition), 1);
    
        // z-axis is the unit vector pointing along the edge
        float4 d = float4(normalize(endPosition - startPosition), 0);

        // z-scale is the length of the edge
        float3 s = float3(scale, scale, length(endPosition - startPosition));
        
        // generate two axes orthogonal to d
        float4 k1 = float4(normalize(arbitrary_orthogonal(d.xyz)), 0);
        float4 k2 = float4(normalize(cross(d.xyz, k1.xyz)), 0);

        // transform matrix is the three axis vectors scaled, plus transformation
        unity_ObjectToWorld._11_21_31_41 = s.x * k1;
        unity_ObjectToWorld._12_22_32_42 = s.y * k2;
        unity_ObjectToWorld._13_23_33_43 = s.z * d;
        unity_ObjectToWorld._14_24_34_44 = p;

        // inverse of transformation matrix
        unity_WorldToObject._11_12_13_14 = float4(k1.xyz, -dot(k1, p)) / s.x;
        unity_WorldToObject._21_22_23_24 = float4(k2.xyz, -dot(k2, p)) / s.y;
        unity_WorldToObject._31_32_33_34 = float4(d.xyz, -dot(d, p)) / s.z;
        unity_WorldToObject._41_42_43_44 = float4(0,0,0,1);

        // apply to existing
    	unity_ObjectToWorld = mul(ObjectToWorld, unity_ObjectToWorld);
    	unity_WorldToObject = mul(unity_WorldToObject, WorldToObject);
    }
    
    void setup_billboard_edge_transformation_z(float3 startPosition, float3 endPosition, float2 scale) {
        // Vector along bond axis
        float3 off = endPosition - startPosition;
        
        // Midpoint of the bond
        float3 midpoint = 0.5 * (endPosition + startPosition);
       
        // Length of the bond
        float d = length(off);
       
        // Normalized vector along bond axis (to be z axis of bond)
        float3 ez = off / d;
       
        // Vector from bond midpoint to camera
        float3 v = InstanceSpaceViewDirection(float4(midpoint, 1)).xyz;
       
        // Normalized cross product of bond axis and view vector, perpendicular to both. 
        // Gives the second dimension of a billboard
        float3 ex = normalize(cross(ez, v));
       
        // Third axis, perpendicular to both x and z axes
        float3 ey = cross(ex, ez);
        
        setup_transformation(ex, ey, ez, float3(scale.x,scale.y,d), midpoint);
    }
    
    void setup_billboard_edge_transformation_y(float3 startPosition, float3 endPosition, float scale) {
        // Vector along bond axis
        float3 off = endPosition - startPosition;
        
        // Midpoint of the bond
        float3 midpoint = 0.5 * (endPosition + startPosition);
       
        // Length of the bond
        float d = length(off);
       
        // Normalized vector along bond axis (to be z axis of bond)
        float3 ey = off / d;
       
        // Vector from bond midpoint to camera
        float3 v = InstanceSpaceViewDirection(float4(midpoint, 1)).xyz;
       
        // Normalized cross product of bond axis and view vector, perpendicular to both. 
        // Gives the second dimension of a billboard
        float3 ex = -normalize(cross(ey, v));
       
        // Third axis, perpendicular to both x and z axes
        float3 ez = -cross(ex, ey);
        
        setup_transformation(ex, ey, ez, float3(scale,d,scale), midpoint);
    }
    
    // Set the transformation matrices for an object with a certain position and scale, rotated such that the
    // z axis points towards the camera
    void setup_billboard_transformation(float3 position, float scale) {
        // position is in renderer space
        float3 d = -normalize(InstanceSpaceViewDirection(position));
        
        float s1 = (d.y * d.y + d.x * d.x * d.z) / (1 - d.z * d.z);
        float s2 = (d.z - 1) * d.x * d.y / (1 - d.z * d.z);
        float s3 = (d.x * d.x + d.y * d.y * d.z) / (1 - d.z * d.z);
        
        float3 l1 = float3(s1, s2, -d.x);
        float3 l2 = float3(s2, s3, -d.y);
        
        setup_transformation(l1, l2, d, float3(scale, scale, scale), position);
    }
    
    // Get the transformation matrix associated with mapping to a linear space with the provided axes and origin
    float4x4 get_transformation_matrix(float3 xAxis, float3 yAxis, float3 zAxis, float3 pos) {
        float4x4 m = 0;
        m._11_21_31_41 = float4(xAxis, 0);
        m._12_22_32_42 = float4(yAxis, 0);
        m._13_23_33_43 = float4(zAxis, 0);
        m._14_24_34_44 = float4(pos, 1);
        return m;
    }

    // Get the inverse transpose of the transformation matrix associated with a unit scale transformation
    float4x4 get_unit_transformation_inverse_transpose_matrix(float3 ex, float3 ey, float3 ez, float3 p) {
        float4x4 m = 0;
        m._11_21_31_41 = float4(ex, -dot(ex, p));
        m._12_22_32_42 = float4(ey, -dot(ey, p));
        m._13_23_33_43 = float4(ez, -dot(ez, p));
        m._14_24_34_44 = float4(0,0,0,1);
        return m;
    }

#endif
