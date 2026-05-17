using UnityEngine;

public static class ProceduralTextureHelper
{
    public static Texture2D GenerateLeavesTexture()
    {
        int width = 128; // Reduced for ultra-fast startup
        int height = 128;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float nx = (float)x / width;
                float ny = (float)y / height;
                
                float noise = Mathf.PerlinNoise(nx * 8f, ny * 8f) * 0.5f +
                              Mathf.PerlinNoise(nx * 16f, ny * 16f) * 0.3f;
                              
                Color leafGreen = new Color(0.12f, 0.42f, 0.12f);
                Color leafLight = new Color(0.24f, 0.62f, 0.24f);
                
                tex.SetPixel(x, y, Color.Lerp(leafGreen, leafLight, noise));
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D GeneratePegasusHeadTexture()
    {
        int w = 256; 
        int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color baseWhite = new Color(0.95f, 0.95f, 0.96f); // Light grey/white CGTrader base coat
        Color shadowColor = new Color(0.80f, 0.82f, 0.85f);
        Color stripeColor = new Color(0.72f, 0.75f, 0.80f); // Polygonal/faceted light-grey stripe
        Color maneGrey = new Color(0.85f, 0.86f, 0.88f); // Cropped grey/white mane
        Color maneHighlight = new Color(0.98f, 0.98f, 1f);

        // 1. Draw Neck (Trapezoid from base to mid-height) with Zebra Horizontal Facet Striping
        int neckBaseW = 90;
        int neckTopW = 50;
        int neckH = 120;
        int neckYOffset = 30;

        for (int y = neckYOffset; y < neckYOffset + neckH; y++)
        {
            float t = (float)(y - neckYOffset) / neckH;
            float currentWidth = Mathf.Lerp(neckBaseW, neckTopW, t);
            
            // Alternating horizontal zebra stripes
            bool isStripe = (Mathf.Sin(y * 0.15f) > 0.4f);

            for (int x = 0; x < w; x++)
            {
                float dx = x - (w / 2f);
                if (Mathf.Abs(dx) <= currentWidth * 0.5f)
                {
                    float lateralFactor = Mathf.Abs(dx) / (currentWidth * 0.5f);
                    float shading = Mathf.Cos(lateralFactor * Mathf.PI * 0.5f);
                    
                    Color baseCol = Color.Lerp(shadowColor, baseWhite, 0.6f + shading * 0.4f);
                    if (isStripe)
                    {
                        baseCol = Color.Lerp(stripeColor, baseCol, 0.4f); // blend in the beautiful stripe
                    }
                    
                    tex.SetPixel(x, y, baseCol);
                }
            }
        }

        // 2. Draw Head (Rounded shape at the top of the neck)
        Vector2 headCenter = new Vector2(w / 2f, neckYOffset + neckH + 10f);
        float headRadiusX = 30f;
        float headRadiusY = 35f;

        for (int x = (int)(headCenter.x - headRadiusX - 5f); x < (int)(headCenter.x + headRadiusX + 5f); x++)
        {
            for (int y = (int)(headCenter.y - headRadiusY - 5f); y < (int)(headCenter.y + headRadiusY + 5f); y++)
            {
                float dx = (x - headCenter.x) / headRadiusX;
                float dy = (y - headCenter.y) / headRadiusY;
                float distSq = dx * dx + dy * dy;
                
                if (distSq <= 1.0f)
                {
                    float depth = 1f - distSq;
                    Color col = Color.Lerp(shadowColor, baseWhite, 0.7f + depth * 0.3f);
                    
                    // Draw a realistic dark grey/black muzzle at the lower/front of the face
                    float muzzleDist = Vector2.Distance(new Vector2(x, y), new Vector2(headCenter.x, headCenter.y - 18f));
                    if (muzzleDist < 14f)
                    {
                        col = Color.Lerp(new Color(0.2f, 0.2f, 0.22f), col, muzzleDist / 14f); // dark muzzle blend
                    }

                    tex.SetPixel(x, y, col);
                }
            }
        }

        // 3. Draw Ears with Prominent Dark Tips
        DrawEar(tex, new Vector2(w / 2f - 15f, headCenter.y + 20f), 22f, 8f, 15f * Mathf.Deg2Rad, baseWhite, shadowColor);
        DrawEar(tex, new Vector2(w / 2f + 15f, headCenter.y + 20f), 22f, 8f, -15f * Mathf.Deg2Rad, baseWhite, shadowColor);

        // 4. Draw Flowing/Cropped Mane
        for (int i = 0; i < 15; i++) 
        {
            float maneY = neckYOffset + 10f + (i * 8f);
            float offsetAmp = 10f + Mathf.Sin(maneY * 0.1f) * 6f;
            Vector2 start = new Vector2(w / 2f, maneY);
            Vector2 end = new Vector2(w / 2f + offsetAmp, maneY - 4f - Random.Range(0f, 3f));
            
            DrawHairStrand(tex, start, end, 3.5f, maneGrey, maneHighlight);
        }

        // 5. Dark Soft Eyes
        if ((int)(headCenter.x - headRadiusX + 8f) < w)
            tex.SetPixel((int)(headCenter.x - headRadiusX + 8f), (int)(headCenter.y), new Color(0.1f, 0.1f, 0.12f));
        if ((int)(headCenter.x + headRadiusX - 8f) < w)
            tex.SetPixel((int)(headCenter.x + headRadiusX - 8f), (int)(headCenter.y), new Color(0.1f, 0.1f, 0.12f));

        tex.Apply();
        return tex;
    }

    private static void DrawEar(Texture2D tex, Vector2 origin, float length, float baseW, float angle, Color mainCol, Color shadowCol)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        int w = tex.width;
        int h = tex.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                float dx = x - origin.x;
                float dy = y - origin.y;
                float rx = dx * cos + dy * sin;
                float ry = -dx * sin + dy * cos;

                if (rx >= 0 && rx <= length)
                {
                    float widthAtLength = baseW * (1.0f - (rx / length));
                    if (Mathf.Abs(ry) <= widthAtLength * 0.5f)
                    {
                        float innerFactor = Mathf.Abs(ry) / (widthAtLength * 0.5f);
                        Color col = Color.Lerp(shadowCol, mainCol, 0.7f - innerFactor * 0.3f);
                        
                        // Prominent dark ear tips like CGTrader Pegasus
                        if (rx > length * 0.7f)
                        {
                            col = Color.Lerp(col, new Color(0.15f, 0.15f, 0.17f), (rx - length * 0.7f) / (length * 0.3f));
                        }

                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }
    }

    private static void DrawHairStrand(Texture2D tex, Vector2 start, Vector2 end, float width, Color startCol, Color endCol)
    {
        int steps = 15; 
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 pos = Vector2.Lerp(start, end, t);
            pos.x += Mathf.Sin(t * Mathf.PI) * 2f;

            float currentRadius = width * 0.5f * (1.0f - t * 0.8f);
            Color col = Color.Lerp(startCol, endCol, t);

            for (int x = (int)(pos.x - currentRadius); x <= (int)(pos.x + currentRadius); x++)
            {
                for (int y = (int)(pos.y - currentRadius); y <= (int)(pos.y + currentRadius); y++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), pos);
                        if (dist <= currentRadius)
                        {
                            float alpha = 1f - (dist / currentRadius);
                            Color existing = tex.GetPixel(x, y);
                            tex.SetPixel(x, y, Color.Lerp(existing, col, alpha * 0.8f));
                        }
                    }
                }
            }
        }
    }

    public static Texture2D GeneratePegasusWingTexture()
    {
        int w = 256; 
        int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color featherColor = new Color(0.98f, 0.98f, 1f);
        Color featherShadow = new Color(0.8f, 0.84f, 0.9f);

        // Highly optimized feather grid layout (fewer feathers and scaled down bounding boxes for speed!)
        
        // Row 1: Primary flight feathers (beautiful dark-to-light gradient, type 0)
        for (int i = 0; i < 6; i++) 
        {
            float progress = i / 5f;
            float angle = Mathf.Lerp(-20f, -65f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(25f + progress * 100f, 160f - progress * 50f);
            float length = Mathf.Lerp(130f, 90f, progress);
            float width = Mathf.Lerp(11f, 9f, progress);
            DrawFeather(tex, origin, length, width, angle, featherColor, featherShadow, 0);
        }

        // Row 2: Secondary coverts (type 1)
        for (int i = 0; i < 8; i++) 
        {
            float progress = i / 7f;
            float angle = Mathf.Lerp(-15f, -60f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(30f + progress * 110f, 170f - progress * 40f);
            float length = Mathf.Lerp(85f, 60f, progress);
            float width = 9f;
            DrawFeather(tex, origin, length, width, angle, featherColor * 0.95f, featherShadow, 1);
        }

        // Row 3: Greater coverts (type 1)
        for (int i = 0; i < 8; i++) 
        {
            float progress = i / 7f;
            float angle = Mathf.Lerp(-10f, -55f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(35f + progress * 120f, 182f - progress * 35f);
            float length = Mathf.Lerp(55f, 40f, progress);
            float width = 8f;
            DrawFeather(tex, origin, length, width, angle, featherColor * 0.98f, featherShadow, 1);
        }

        // Row 4: Lesser coverts / Shoulder (dark charcoal/brown, type 2)
        for (int i = 0; i < 10; i++) 
        {
            float progress = i / 9f;
            float angle = Mathf.Lerp(-5f, -50f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(40f + progress * 125f, 195f - progress * 30f);
            float length = Mathf.Lerp(30f, 20f, progress);
            float width = 7f;
            DrawFeather(tex, origin, length, width, angle, Color.white, featherShadow * 1.05f, 2);
        }

        tex.Apply();
        return tex;
    }

    private static void DrawFeather(Texture2D tex, Vector2 origin, float length, float width, float angle, Color baseColor, Color shadowColor, int gradientType)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        int w = tex.width;
        int h = tex.height;

        int padding = (int)length + 10;
        int minX = Mathf.Clamp((int)origin.x - padding, 0, w);
        int maxX = Mathf.Clamp((int)origin.x + padding, 0, w);
        int minY = Mathf.Clamp((int)origin.y - padding, 0, h);
        int maxY = Mathf.Clamp((int)origin.y + padding, 0, h);

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                float dx = x - origin.x;
                float dy = y - origin.y;
                float rx = dx * cos + dy * sin;
                float ry = -dx * sin + dy * cos;

                if (rx >= 0 && rx <= length)
                {
                    float t = rx / length;
                    float currentWidth = width * (1.0f - t * 0.6f) * Mathf.Sin(t * Mathf.PI * 0.5f);
                    
                    if (Mathf.Abs(ry) <= currentWidth * 0.5f)
                    {
                        float distFromCenter = Mathf.Abs(ry) / (currentWidth * 0.5f);
                        float sideShade = Mathf.Lerp(1.0f, 0.75f, distFromCenter);
                        
                        Color c;
                        if (gradientType == 0) // Flight feathers: dark charcoal to white gradient!
                        {
                            c = Color.Lerp(new Color(0.18f, 0.18f, 0.20f), new Color(0.98f, 0.98f, 1.0f), t * sideShade);
                        }
                        else if (gradientType == 1) // Mid-coverts: medium grey gradient
                        {
                            c = Color.Lerp(new Color(0.28f, 0.28f, 0.30f), new Color(0.85f, 0.85f, 0.90f), t * sideShade);
                        }
                        else // Shoulder lesser coverts: solid dark charcoal/brown
                        {
                            c = Color.Lerp(new Color(0.20f, 0.18f, 0.18f), new Color(0.32f, 0.30f, 0.30f), t * sideShade);
                        }
                        
                        if (Mathf.Abs(ry) < 0.6f && gradientType == 0)
                        {
                            c = new Color(0.95f, 0.9f, 0.75f); // quill
                        }

                        float alpha = 1f;
                        float edgeDist = (currentWidth * 0.5f) - Mathf.Abs(ry);
                        if (edgeDist < 1.0f)
                        {
                            alpha = edgeDist / 1.0f;
                        }

                        Color existing = tex.GetPixel(x, y);
                        tex.SetPixel(x, y, Color.Lerp(existing, c, alpha));
                    }
                }
            }
        }
    }

    public static Texture2D GeneratePegasusLegTexture()
    {
        int w = 128; 
        int h = 256; 
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color baseWhite = new Color(0.95f, 0.95f, 0.96f); // Light grey base
        Color shadowCol = new Color(0.78f, 0.80f, 0.84f);
        Color stripeColor = new Color(0.68f, 0.70f, 0.75f); // Facet zebra stripe
        
        Color darkHoof = new Color(0.12f, 0.12f, 0.14f); // Solid dark grey/black hooves
        Color darkHoofShadow = new Color(0.06f, 0.06f, 0.08f);

        int legCenter = w / 2;
        int startY = 240;
        int endY = 20;

        for (int y = endY; y <= startY; y++)
        {
            float progress = (float)(y - endY) / (startY - endY);
            
            float width = 18f;
            if (progress > 0.8f)
            {
                width = Mathf.Lerp(22f, 37f, (progress - 0.8f) / 0.2f);
            }
            else if (progress < 0.2f)
            {
                width = Mathf.Lerp(20f, 18f, progress / 0.2f);
            }
            else
            {
                float kneeFactor = Mathf.Sin((progress - 0.2f) / 0.6f * Mathf.PI);
                width = Mathf.Lerp(18f, 22f, kneeFactor);
            }

            bool isHoof = (y < endY + 14);
            bool isStripe = (!isHoof && (Mathf.Sin(y * 0.15f) > 0.6f));

            for (int x = (int)(legCenter - width * 0.5f); x <= (int)(legCenter + width * 0.5f); x++)
            {
                if (x >= 0 && x < w)
                {
                    float factor = Mathf.Abs(x - legCenter) / (width * 0.5f);
                    float shading = Mathf.Cos(factor * Mathf.PI * 0.5f);

                    Color c;
                    if (isHoof)
                    {
                        c = Color.Lerp(darkHoofShadow, darkHoof, 0.5f + shading * 0.5f);
                    }
                    else
                    {
                        c = Color.Lerp(shadowCol, baseWhite, 0.6f + shading * 0.4f);
                        if (isStripe)
                        {
                            c = Color.Lerp(stripeColor, c, 0.4f); // apply dark-grey zebra leg band
                        }
                    }

                    float edgeDist = (width * 0.5f) - Mathf.Abs(x - legCenter);
                    float alpha = 1.0f;
                    if (edgeDist < 1f)
                    {
                        alpha = edgeDist / 1f;
                    }

                    tex.SetPixel(x, y, Color.Lerp(Color.clear, c, alpha));
                }
            }
        }

        tex.Apply();
        return tex;
    }

    public static float GetTerrainHeight(float x, float z)
    {
        // Two-octave Perlin noise for highly detailed, photorealistic farm topography
        return Mathf.PerlinNoise(x * 0.005f, z * 0.005f) * 14.0f + 
               Mathf.PerlinNoise(x * 0.02f, z * 0.02f) * 2.5f;
    }
}
