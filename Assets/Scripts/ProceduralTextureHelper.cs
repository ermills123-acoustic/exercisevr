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
        int w = 256; // Optimized from 512 to 256 (4x fewer pixels)
        int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color baseWhite = new Color(0.98f, 0.98f, 0.98f);
        Color shadowColor = new Color(0.82f, 0.85f, 0.9f);
        Color maneBlue = new Color(0.5f, 0.75f, 0.95f);
        Color maneHighlight = new Color(0.85f, 0.95f, 1f);

        // 1. Draw Neck (Trapezoid from base to mid-height)
        int neckBaseW = 90;
        int neckTopW = 50;
        int neckH = 120;
        int neckYOffset = 30;

        for (int y = neckYOffset; y < neckYOffset + neckH; y++)
        {
            float t = (float)(y - neckYOffset) / neckH;
            float currentWidth = Mathf.Lerp(neckBaseW, neckTopW, t);
            
            for (int x = 0; x < w; x++)
            {
                float dx = x - (w / 2f);
                if (Mathf.Abs(dx) <= currentWidth * 0.5f)
                {
                    float lateralFactor = Mathf.Abs(dx) / (currentWidth * 0.5f);
                    float shading = Mathf.Cos(lateralFactor * Mathf.PI * 0.5f);
                    Color col = Color.Lerp(shadowColor, baseWhite, 0.6f + shading * 0.4f);
                    tex.SetPixel(x, y, col);
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
                    tex.SetPixel(x, y, col);
                }
            }
        }

        // 3. Draw Ears (Pointed upright shapes)
        DrawEar(tex, new Vector2(w / 2f - 15f, headCenter.y + 20f), 22f, 8f, 15f * Mathf.Deg2Rad, baseWhite, shadowColor);
        DrawEar(tex, new Vector2(w / 2f + 15f, headCenter.y + 20f), 22f, 8f, -15f * Mathf.Deg2Rad, baseWhite, shadowColor);

        // 4. Draw Flowing Mane
        for (int i = 0; i < 15; i++) // Reduced from 30 to 15
        {
            float maneY = neckYOffset + 10f + (i * 8f);
            float offsetAmp = 10f + Mathf.Sin(maneY * 0.1f) * 8f;
            Vector2 start = new Vector2(w / 2f, maneY);
            Vector2 end = new Vector2(w / 2f + offsetAmp, maneY - 8f - Random.Range(0f, 5f));
            
            DrawHairStrand(tex, start, end, 4f, maneBlue, maneHighlight);
        }

        // 5. Soft Eyes
        if ((int)(headCenter.x - headRadiusX + 8f) < w)
            tex.SetPixel((int)(headCenter.x - headRadiusX + 8f), (int)(headCenter.y), Color.black);
        if ((int)(headCenter.x + headRadiusX - 8f) < w)
            tex.SetPixel((int)(headCenter.x + headRadiusX - 8f), (int)(headCenter.y), Color.black);

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
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }
    }

    private static void DrawHairStrand(Texture2D tex, Vector2 start, Vector2 end, float width, Color startCol, Color endCol)
    {
        int steps = 20; // Reduced from 50
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 pos = Vector2.Lerp(start, end, t);
            pos.x += Mathf.Sin(t * Mathf.PI) * 4f;

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
        int w = 256; // Optimized from 512 to 256 (4x fewer pixels)
        int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color featherColor = new Color(0.98f, 0.98f, 1f);
        Color featherShadow = new Color(0.8f, 0.84f, 0.9f);

        // Highly optimized feather grid layout (fewer feathers and scaled down bounding boxes for speed!)
        
        // Row 1: Primary flight feathers
        for (int i = 0; i < 6; i++) // Reduced from 12
        {
            float progress = i / 5f;
            float angle = Mathf.Lerp(-20f, -65f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(25f + progress * 100f, 160f - progress * 50f);
            float length = Mathf.Lerp(130f, 90f, progress);
            float width = Mathf.Lerp(11f, 9f, progress);
            DrawFeather(tex, origin, length, width, angle, featherColor, featherShadow);
        }

        // Row 2: Secondary coverts
        for (int i = 0; i < 8; i++) // Reduced from 15
        {
            float progress = i / 7f;
            float angle = Mathf.Lerp(-15f, -60f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(30f + progress * 110f, 170f - progress * 40f);
            float length = Mathf.Lerp(85f, 60f, progress);
            float width = 9f;
            DrawFeather(tex, origin, length, width, angle, featherColor * 0.95f, featherShadow);
        }

        // Row 3: Greater coverts
        for (int i = 0; i < 8; i++) // Reduced from 16
        {
            float progress = i / 7f;
            float angle = Mathf.Lerp(-10f, -55f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(35f + progress * 120f, 182f - progress * 35f);
            float length = Mathf.Lerp(55f, 40f, progress);
            float width = 8f;
            DrawFeather(tex, origin, length, width, angle, featherColor * 0.98f, featherShadow);
        }

        // Row 4: Lesser coverts
        for (int i = 0; i < 10; i++) // Reduced from 20
        {
            float progress = i / 9f;
            float angle = Mathf.Lerp(-5f, -50f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(40f + progress * 125f, 195f - progress * 30f);
            float length = Mathf.Lerp(30f, 20f, progress);
            float width = 7f;
            DrawFeather(tex, origin, length, width, angle, Color.white, featherShadow * 1.05f);
        }

        tex.Apply();
        return tex;
    }

    private static void DrawFeather(Texture2D tex, Vector2 origin, float length, float width, float angle, Color baseColor, Color shadowColor)
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
                        float lengthShade = Mathf.Lerp(0.85f, 1.0f, t);
                        
                        Color c = Color.Lerp(shadowColor, baseColor, sideShade * lengthShade);
                        
                        if (Mathf.Abs(ry) < 0.6f)
                        {
                            c = new Color(0.95f, 0.9f, 0.75f);
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
        int w = 128; // Optimized from 256 to 128
        int h = 256; // Optimized from 512 to 256
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color baseWhite = new Color(0.97f, 0.97f, 0.98f);
        Color shadowCol = new Color(0.8f, 0.83f, 0.88f);
        Color goldHoof = new Color(0.9f, 0.75f, 0.25f);
        Color goldHoofShadow = new Color(0.6f, 0.45f, 0.1f);

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

            for (int x = (int)(legCenter - width * 0.5f); x <= (int)(legCenter + width * 0.5f); x++)
            {
                if (x >= 0 && x < w)
                {
                    float factor = Mathf.Abs(x - legCenter) / (width * 0.5f);
                    float shading = Mathf.Cos(factor * Mathf.PI * 0.5f);

                    Color c;
                    if (isHoof)
                    {
                        c = Color.Lerp(goldHoofShadow, goldHoof, 0.5f + shading * 0.5f);
                    }
                    else
                    {
                        c = Color.Lerp(shadowCol, baseWhite, 0.6f + shading * 0.4f);
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
}
