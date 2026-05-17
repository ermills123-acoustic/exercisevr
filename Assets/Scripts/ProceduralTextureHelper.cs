using UnityEngine;

public static class ProceduralTextureHelper
{
    public static Texture2D GenerateLeavesTexture()
    {
        int width = 256;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float nx = (float)x / width;
                float ny = (float)y / height;
                
                // Overlay multiple frequencies of Perlin noise for realistic leaf cluster details
                float noise = Mathf.PerlinNoise(nx * 12f, ny * 12f) * 0.4f +
                              Mathf.PerlinNoise(nx * 24f, ny * 24f) * 0.3f +
                              Mathf.PerlinNoise(nx * 48f, ny * 48f) * 0.15f;
                              
                Color leafGreen = new Color(0.12f, 0.45f, 0.12f);
                Color leafLight = new Color(0.24f, 0.65f, 0.24f);
                Color leafDark = new Color(0.06f, 0.28f, 0.06f);
                
                Color c = Color.Lerp(leafGreen, leafLight, noise);
                if (noise < 0.35f)
                {
                    c = Color.Lerp(c, leafDark, (0.35f - noise) * 2f);
                }
                
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D GeneratePegasusHeadTexture()
    {
        int w = 512;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        
        // Fill with transparency
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color baseWhite = new Color(0.98f, 0.98f, 0.98f);
        Color shadowColor = new Color(0.82f, 0.85f, 0.9f);
        Color maneBlue = new Color(0.5f, 0.75f, 0.95f);
        Color maneHighlight = new Color(0.85f, 0.95f, 1f);

        // 1. Draw Neck (Trapezoid from base to mid-height)
        int neckBaseW = 180;
        int neckTopW = 100;
        int neckH = 240;
        int neckYOffset = 60;

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
                    // Muscle shading: shadow on edges, bright highlight in the center
                    float shading = Mathf.Cos(lateralFactor * Mathf.PI * 0.5f);
                    Color col = Color.Lerp(shadowColor, baseWhite, 0.6f + shading * 0.4f);
                    tex.SetPixel(x, y, col);
                }
            }
        }

        // 2. Draw Head (Rounded shape at the top of the neck)
        Vector2 headCenter = new Vector2(w / 2f, neckYOffset + neckH + 20f);
        float headRadiusX = 60f;
        float headRadiusY = 70f;

        for (int x = (int)(headCenter.x - headRadiusX - 10f); x < (int)(headCenter.x + headRadiusX + 10f); x++)
        {
            for (int y = (int)(headCenter.y - headRadiusY - 10f); y < (int)(headCenter.y + headRadiusY + 10f); y++)
            {
                float dx = (x - headCenter.x) / headRadiusX;
                float dy = (y - headCenter.y) / headRadiusY;
                float distSq = dx * dx + dy * dy;
                
                if (distSq <= 1.0f)
                {
                    float depth = 1f - distSq;
                    Color col = Color.Lerp(shadowColor, baseWhite, 0.7f + depth * 0.3f);
                    
                    // Blend head on top of neck
                    Color existing = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(existing, col, 1.0f));
                }
            }
        }

        // 3. Draw Ears (Pointed upright shapes)
        DrawEar(tex, new Vector2(w / 2f - 30f, headCenter.y + 40f), 45f, 15f, 15f * Mathf.Deg2Rad, baseWhite, shadowColor);
        DrawEar(tex, new Vector2(w / 2f + 30f, headCenter.y + 40f), 45f, 15f, -15f * Mathf.Deg2Rad, baseWhite, shadowColor);

        // 4. Draw Flowing Mane (Splines/Strands down the center back of the neck)
        for (int i = 0; i < 30; i++)
        {
            float maneY = neckYOffset + 20f + (i * 9f);
            float offsetAmp = 20f + Mathf.Sin(maneY * 0.05f) * 15f;
            Vector2 start = new Vector2(w / 2f, maneY);
            // Strands flow to the right or left dynamically
            Vector2 end = new Vector2(w / 2f + offsetAmp, maneY - 15f - Random.Range(0f, 10f));
            
            DrawHairStrand(tex, start, end, 8f, maneBlue, maneHighlight);
        }

        // 5. Soft Eyes on the outer edges (highly stylized)
        tex.SetPixel((int)(headCenter.x - headRadiusX + 15f), (int)(headCenter.y), Color.black);
        tex.SetPixel((int)(headCenter.x - headRadiusX + 16f), (int)(headCenter.y), Color.black);
        tex.SetPixel((int)(headCenter.x + headRadiusX - 15f), (int)(headCenter.y), Color.black);
        tex.SetPixel((int)(headCenter.x + headRadiusX - 16f), (int)(headCenter.y), Color.black);

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
                        
                        // Inner ear soft pink accent
                        if (innerFactor < 0.4f)
                        {
                            col = Color.Lerp(col, new Color(0.95f, 0.75f, 0.75f), 0.5f);
                        }
                        
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }
    }

    private static void DrawHairStrand(Texture2D tex, Vector2 start, Vector2 end, float width, Color startCol, Color endCol)
    {
        int steps = 50;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 pos = Vector2.Lerp(start, end, t);
            
            // Add a little curl
            pos.x += Mathf.Sin(t * Mathf.PI) * 8f;

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
        int w = 512;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color featherColor = new Color(0.98f, 0.98f, 1f);
        Color featherShadow = new Color(0.8f, 0.84f, 0.9f);

        // We build the wing by drawing rows of beautifully layered individual feathers.
        // We start with the longest flight feathers at the back/bottom, then overlay shorter coverts.

        // Row 1: Primary flight feathers (Long, angled downwards)
        for (int i = 0; i < 12; i++)
        {
            float progress = i / 11f;
            float angle = Mathf.Lerp(-20f, -65f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(50f + progress * 200f, 320f - progress * 100f);
            float length = Mathf.Lerp(260f, 180f, progress);
            float width = Mathf.Lerp(22f, 18f, progress);
            DrawFeather(tex, origin, length, width, angle, featherColor, featherShadow);
        }

        // Row 2: Secondary coverts (Medium length, overlaying flight feathers)
        for (int i = 0; i < 15; i++)
        {
            float progress = i / 14f;
            float angle = Mathf.Lerp(-15f, -60f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(60f + progress * 220f, 340f - progress * 80f);
            float length = Mathf.Lerp(170f, 120f, progress);
            float width = 18f;
            DrawFeather(tex, origin, length, width, angle, featherColor * 0.95f, featherShadow);
        }

        // Row 3: Greater coverts (Shorter)
        for (int i = 0; i < 16; i++)
        {
            float progress = i / 15f;
            float angle = Mathf.Lerp(-10f, -55f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(70f + progress * 240f, 365f - progress * 70f);
            float length = Mathf.Lerp(110f, 80f, progress);
            float width = 16f;
            DrawFeather(tex, origin, length, width, angle, featherColor * 0.98f, featherShadow);
        }

        // Row 4: Lesser coverts (Tiny feathers clustered at the top wing bone)
        for (int i = 0; i < 20; i++)
        {
            float progress = i / 19f;
            float angle = Mathf.Lerp(-5f, -50f, progress) * Mathf.Deg2Rad;
            Vector2 origin = new Vector2(80f + progress * 250f, 390f - progress * 60f);
            float length = Mathf.Lerp(60f, 40f, progress);
            float width = 14f;
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

        int padding = (int)length + 20;
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
                    // Taper width toward the tip
                    float t = rx / length;
                    float currentWidth = width * (1.0f - t * 0.6f) * Mathf.Sin(t * Mathf.PI * 0.5f);
                    
                    if (Mathf.Abs(ry) <= currentWidth * 0.5f)
                    {
                        float distFromCenter = Mathf.Abs(ry) / (currentWidth * 0.5f);
                        
                        // Shade gradients for realism
                        float sideShade = Mathf.Lerp(1.0f, 0.75f, distFromCenter);
                        float lengthShade = Mathf.Lerp(0.85f, 1.0f, t);
                        
                        Color c = Color.Lerp(shadowColor, baseColor, sideShade * lengthShade);
                        
                        // Golden shafts for legendary aesthetic highlights
                        if (Mathf.Abs(ry) < 1.0f)
                        {
                            c = new Color(0.95f, 0.9f, 0.75f); // Soft gold shaft
                        }

                        // Alpha feather edge softening
                        float alpha = 1f;
                        float edgeDist = (currentWidth * 0.5f) - Mathf.Abs(ry);
                        if (edgeDist < 1.5f)
                        {
                            alpha = edgeDist / 1.5f;
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
        int w = 256;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        Color baseWhite = new Color(0.97f, 0.97f, 0.98f);
        Color shadowCol = new Color(0.8f, 0.83f, 0.88f);
        Color goldHoof = new Color(0.9f, 0.75f, 0.25f);
        Color goldHoofShadow = new Color(0.6f, 0.45f, 0.1f);

        int legCenter = w / 2;
        int startY = 480;
        int endY = 40;

        for (int y = endY; y <= startY; y++)
        {
            float progress = (float)(y - endY) / (startY - endY);
            
            // muscular contours (taper at knee, expand at thigh and ankle)
            float width = 36f;
            if (progress > 0.8f) // Upper thigh
            {
                width = Mathf.Lerp(45f, 75f, (progress - 0.8f) / 0.2f);
            }
            else if (progress < 0.2f) // Lower ankle & hoof base
            {
                width = Mathf.Lerp(40f, 36f, progress / 0.2f);
            }
            else // Knee taper
            {
                float kneeFactor = Mathf.Sin((progress - 0.2f) / 0.6f * Mathf.PI);
                width = Mathf.Lerp(36f, 45f, kneeFactor);
            }

            // Hoof logic at the very bottom
            bool isHoof = (y < endY + 28);

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

                    // Soft alpha blend at the leg borders
                    float edgeDist = (width * 0.5f) - Mathf.Abs(x - legCenter);
                    float alpha = 1.0f;
                    if (edgeDist < 2f)
                    {
                        alpha = edgeDist / 2f;
                    }

                    tex.SetPixel(x, y, Color.Lerp(Color.clear, c, alpha));
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
