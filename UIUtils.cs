using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ExportElectricityMod;
using System;
using System.IO;
//using System.Linq;
using UnityEngine;

public static class UIUtils
{

    static PluginManager.PluginInfo FindPlugin()
    {
        ulong WORKSHOP_ID = 2727358367;
        string pluginName = "ExportElectricity";

        foreach (var mod in Singleton<PluginManager>.instance.GetPluginsInfo())
        {
            if (mod.name == pluginName || mod.name == "csm-exportelectricity" || mod.publishedFileID.AsUInt64 == WORKSHOP_ID)
            {
                return mod;
            }
        }

        return null;
    }

    static string FindModPath()
    {
        var plugin = FindPlugin();
        if (plugin != null)
        {
            return plugin.modPath;
        }
        else
        {
            throw new Exception("Cannot find plugin path.");
        }        
    }

    public static UITextureAtlas CreateTextureAtlas(string modName, string textureFile, string atlasName, int spriteWidth, int spriteHeight, string[] spriteNames)
    {
        Shader shader = Shader.Find("UI/Default UI Shader");
        if (shader == null)
        {
            throw new Exception("shader null");
        }

        Material atlasMaterial = new Material(shader);
        if (atlasMaterial == null)
        {
            throw new Exception("atlasMaterial null");
        }

        var bytes = File.ReadAllBytes(Path.Combine(FindModPath(), textureFile));

        Texture2D tex = new Texture2D(spriteWidth * spriteNames.Length, spriteHeight, TextureFormat.ARGB32, false);
        tex.LoadImage(bytes);
        FixTransparency(tex);

        UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

        { // Setup atlas
            Material material = (Material)Material.Instantiate(atlasMaterial);
            material.mainTexture = tex;
            atlas.material = material;
            atlas.name = atlasName;
        }

        // Add sprites
        for (int i = 0; i < spriteNames.Length; ++i)
        {
            float uw = 1.0f / spriteNames.Length;

            var spriteInfo = new UITextureAtlas.SpriteInfo()
            {
                name = spriteNames[i],
                texture = tex,
                region = new Rect(i * uw, 0, uw, 1),
            };

            atlas.AddSprite(spriteInfo);
        }

        return atlas;
    }

    //=========================================================================
    // Methods created by petrucio -> http://answers.unity3d.com/questions/238922/png-transparency-has-white-borderhalo.html
    //
    // Copy the values of adjacent pixels to transparent pixels color info, to
    // remove the white border artifact when importing transparent .PNGs.
    public static void FixTransparency(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int w = texture.width;
        int h = texture.height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                Color32 pixel = pixels[idx];
                if (pixel.a == 0)
                {
                    bool done = false;
                    if (!done && x > 0) done = TryAdjacent(ref pixel, pixels[idx - 1]);        // Left   pixel
                    if (!done && x < w - 1) done = TryAdjacent(ref pixel, pixels[idx + 1]);        // Right  pixel
                    if (!done && y > 0) done = TryAdjacent(ref pixel, pixels[idx - w]);        // Top    pixel
                    if (!done && y < h - 1) done = TryAdjacent(ref pixel, pixels[idx + w]);        // Bottom pixel
                    pixels[idx] = pixel;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }

    private static bool TryAdjacent(ref Color32 pixel, Color32 adjacent)
    {
        if (adjacent.a == 0) return false;

        pixel.r = adjacent.r;
        pixel.g = adjacent.g;
        pixel.b = adjacent.b;
        return true;
    }
    //=========================================================================

    public class ImageButton : UIButton
    {
        public string SetDetail(string btnName, string imgFilename, string btnTooltip, int btnWidth, int btnHeight, string[] types)
        {
            string status = "";

            try
            {
                name = btnName;
                tooltip = btnTooltip;
                width = btnWidth;
                height = btnHeight;
                atlas = CreateTextureAtlas("ExportElectricity", imgFilename, "eebtn", btnWidth, btnHeight, types);
                foreach (var t in types)
                {
                    switch (t)
                    {
                        case "normalBg":
                            normalBgSprite = t;
                            break;
                        case "disabledBg":
                            disabledBgSprite = t;
                            break;
                        case "hoveredBg":
                            hoveredBgSprite = t;
                            break;
                        case "pressedBg":
                            pressedBgSprite = t;
                            break;
                        case "focusedBg":
                            focusedBgSprite = t;
                            break;
                        case "normalFg":
                            normalFgSprite = t;
                            break;
                        case "pressedFg":
                            pressedFgSprite = t;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                status = ex.ToString();
            }

            return status;
        }
    }

    /*
    public static UITextureAtlas CreateTextureAtlas(string modName, string textureFile, string atlasName, Material baseMaterial, int spriteWidth, int spriteHeight, string[] spriteNames)
    {

        Texture2D tex = new Texture2D(spriteWidth * spriteNames.Length, spriteHeight, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        { // LoadTexture
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            if (assembly == null)
            {
                throw new Exception("assembly null");
            }

            System.IO.Stream textureStream = assembly.GetManifestResourceStream(modName + "." + textureFile);
            if (textureStream == null)
            {
                throw new Exception("textureStream null");
            }

            byte[] buf = new byte[textureStream.Length];  //declare arraysize
            textureStream.Read(buf, 0, buf.Length); // read from stream to byte array

            tex.LoadImage(buf);

            tex.Apply(true, true);
        }
        
        UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

        { // Setup atlas
            Material material = (Material)Material.Instantiate(baseMaterial);
            material.mainTexture = tex;

            atlas.material = material;
            atlas.name = atlasName;
        }

        // Add sprites
        for (int i = 0; i < spriteNames.Length; ++i)
        {
            float uw = 1.0f / spriteNames.Length;

            var spriteInfo = new UITextureAtlas.SpriteInfo()
            {
                name = spriteNames[i],
                texture = tex,
                region = new Rect(i * uw, 0, uw, 1),
            };

            atlas.AddSprite(spriteInfo);
        }

        return atlas;
    }    

    public class ImageButton : UIButton
    {
        public string SetDetail(string btnName, string imgFilename, string btnTooltip, int btnWidth, int btnHeight, string[] types)
        {
            string status = "";

            try
            {
                Shader shader = Shader.Find("UI/Default UI Shader");
                if (shader == null)
                {
                    throw new Exception("shader null");
                }
                Material atlasMaterial = new Material(shader);
                if (atlasMaterial == null)
                {
                    throw new Exception("atlasMaterial null");
                }

                name = btnName;
                tooltip = btnTooltip;
                width = btnWidth;
                height = btnHeight;
                atlas = CreateTextureAtlas("ExportElectricity", imgFilename, "eebtn", atlasMaterial, btnWidth, btnHeight, types);
                foreach (var t in types)
                {
                    switch (t)
                    {
                        case "normalBg":
                            normalBgSprite = t;
                            break;
                        case "disabledBg":
                            disabledBgSprite = t;
                            break;
                        case "hoveredBg":
                            hoveredBgSprite = t;
                            break;
                        case "pressedBg":
                            pressedBgSprite = t;
                            break;
                        case "focusedBg":
                            focusedBgSprite = t;
                            break;
                        case "normalFg":
                            normalFgSprite = t;
                            break;
                        case "pressedFg":
                            pressedFgSprite = t;
                            break;
                    }
                }
            } catch (Exception ex)
            {
                status = ex.ToString();
            }

            return status;
        }
    }
    */
}
