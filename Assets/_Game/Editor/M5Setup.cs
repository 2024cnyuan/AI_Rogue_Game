using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

namespace Starfall.Editor
{
    public static class M5Setup
    {
        const string Root = "Assets/_Game/";
        [MenuItem("Starfall/Setup M5 Presentation")]
        public static void Setup()
        {
            foreach (var dir in new[] { "Resources", "UI/Fonts/Generated", "Art/Materials", "Data/Rendering/VolumeProfiles", "Audio/Mixers", "UI/Prefabs" }) Directory.CreateDirectory(Root + dir);
            AssetDatabase.Refresh();
            var c = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(Root + "Resources/PresentationCatalog.asset");
            if (c == null) { c = ScriptableObject.CreateInstance<PresentationCatalog>(); AssetDatabase.CreateAsset(c, Root + "Resources/PresentationCatalog.asset"); }
            c.explorer = Slice(Root + "Art/Characters/Player/explorer_walk_atlas.png", 4, 4, Enumerable.Range(0, 16).Select(i => "explorer." + i).ToArray(), new Vector2(.5f, .18f));
            var art = new List<ArtEntry>();
            string[] items = { "pistol", "shotgun", "smg", "crossbow", "launcher", "arc", "workshop_smg", "coins", "medkit", "shield", "slow", "shock", "decoy", "grenade", "energy", "health", "rapid", "magnet", "vitality", "agile", "pierce", "bounce", "critical", "recharge", "lowhealth", "blast", "controlled", "flawless", "beacon", "chest", "workstation", "gate" };
            Add(art, items, Slice(Root + "Art/Items/items.png", 8, 4, items, new Vector2(.5f, .5f), true));
            string[] enemies = { "enemy.basic", "enemy.archer", "enemy.shield", "enemy.bomber", "enemy.flanker", "enemy.frost", "enemy.spore", "enemy.blocker", "enemy.assault", "enemy.sniper", "boss.1", "boss.2", "boss.3", "boss.4", "boss.5", "boss.6" };
            var enemySprites = Slice(Root + "Art/Characters/Enemies/enemies.png", 4, 4, enemies, new Vector2(.5f, .2f), true); Add(art, enemies, enemySprites);
            string[] extras={"enemy.turret","enemy.drone","enemy.sporelet"}; Add(art,extras,Slice(Root+"Art/Characters/Enemies/enemies-extra.png",3,1,extras,new Vector2(.5f,.2f),true));
            var worldIds = new List<string>(); foreach (string kind in new[] { "floor", "wall", "arch", "landmark" }) for (int stage = 1; stage <= 6; stage++) worldIds.Add(kind + "." + stage);
            var world = Slice(Root + "Art/World/world.png", 6, 4, worldIds.ToArray(), new Vector2(.5f, .5f));
            if(File.Exists(Root+"Art/World/floors.png")) world=Slice(Root+"Art/World/floors.png",3,2,worldIds.Take(6).ToArray(),new Vector2(.5f,.5f));
            for (int i = 0; i < 6; i++) art.Add(new ArtEntry { id = worldIds[i], sprite = world[i] });
            if (File.Exists(Root + "Art/World/props.png")) {
                var ids = worldIds.Skip(6).ToArray(); Add(art, ids, Slice(Root + "Art/World/props.png", 6, 3, ids, new Vector2(.5f, .5f), true));
            } else for (int i = 6; i < worldIds.Count; i++) art.Add(new ArtEntry { id = worldIds[i], sprite = world[i] });
            c.art = art.ToArray();
            c.stageCards = Slice(Root + "UI/StageCards/stagecards.png", 3, 2, StageRewards.StageIds, new Vector2(.5f, .5f));
            c.menu = Slice(Root + "UI/Backgrounds/menu.png", 1, 1, new[] { "menu" }, new Vector2(.5f, .5f))[0];
            c.lit = Material("M5_SpriteLit", "Universal Render Pipeline/2D/Sprite-Lit-Default");
            c.unlit = Material("M5_SpriteUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            CreateVisualPrefabs(c);
            SetupTextSettings(null); AssetDatabase.SaveAssets();
            c.body = Font("StarfallBodySC", "SourceHanSansSC/SourceHanSansSC-Regular.otf");
            c.emphasis = Font("StarfallEmphasisSC", "SourceHanSansSC/SourceHanSansSC-Medium.otf");
            c.title = Font("StarfallTitleSC", "SourceHanSerifSC/SourceHanSerifSC-SemiBold.otf");
            SetupTextSettings(c.body);
            c.standard = Profile("M5_Default", false); c.minimal = Profile("M5_Minimal", true);
            string[] sounds = { "shot", "hit", "hurt", "dodge", "pickup", "ui", "clear", "boss" };
            c.sounds = sounds.Select(id => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/SFX/" + id + ".wav")).ToArray();
            c.weapons = items.Take(7).Select(id => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/SFX/Weapons/" + id + "_fire.wav")).ToArray();
            c.ambience = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Ambience/starport.wav");
            SetupMixer(c);
            var rules = AssetDatabase.LoadAssetAtPath<FirstLevelConfig>(Root + "Resources/FirstLevelConfig.asset"); rules.balanceVersion = "3"; rules.timingVersion = "2"; EditorUtility.SetDirty(rules);
            EditorUtility.SetDirty(c); AssetDatabase.SaveAssets(); M5Art.ResetCache(); Validate(); Debug.Log("STARFALL_M5_SETUP_OK");
        }
        static void Add(List<ArtEntry> entries, string[] ids, Sprite[] sprites) { for (int i = 0; i < ids.Length; i++) entries.Add(new ArtEntry { id = ids[i], sprite = sprites[i] }); }
        static void CreateVisualPrefabs(PresentationCatalog c) {
            string folder=Root+"Prefabs/Visuals"; Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            GameObject Create(string id,Sprite sprite) {
                string path=folder+"/"+id.Replace('.','_')+".prefab"; var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
                var temporary=new GameObject(id); var renderer=temporary.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;renderer.sharedMaterial=c.lit;renderer.sortingOrder=40;
                var prefab=PrefabUtility.SaveAsPrefabAsset(temporary,path); UnityEngine.Object.DestroyImmediate(temporary); return prefab;
            }
            foreach(var entry in c.art) entry.prefab=Create(entry.id,entry.sprite);
            c.explorerVisual=Create("Explorer",c.explorer[0]);
        }
        static Sprite[] Slice(string path, int cols, int rows, string[] names, Vector2 pivot, bool trim = false)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter; if (importer == null) throw new InvalidOperationException("Missing art " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 128;
            var textureSettings=new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings); textureSettings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(textureSettings);
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096; importer.isReadable = true;
            importer.SaveAndReimport(); var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var factory = new SpriteDataProviderFactories(); factory.Init(); var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects();
            var rects = new SpriteRect[names.Length]; var pixels = trim ? texture.GetPixels32() : null;
            for (int i = 0; i < names.Length; i++) {
                int x = Mathf.RoundToInt((float)(i % cols) * texture.width / cols), y = Mathf.RoundToInt((float)(rows - 1 - i / cols) * texture.height / rows);
                int w = Mathf.RoundToInt((float)(i % cols + 1) * texture.width / cols) - x, h = Mathf.RoundToInt((float)(rows - i / cols) * texture.height / rows) - y;
                if(path.EndsWith("props.png")) { float[] cuts={0,.245f,.57f,1}; int row=i/cols; y=texture.height-Mathf.RoundToInt(cuts[row+1]*texture.height); h=Mathf.RoundToInt(cuts[row+1]*texture.height)-Mathf.RoundToInt(cuts[row]*texture.height); }
                var rect = new Rect(x + 2, y + 2, w - 4, h - 4);
                if(path.EndsWith("floors.png")) rect = new Rect(x,y,w,h);
                if (trim) {
                    int minX = x + w, minY = y + h, maxX = x, maxY = y;
                    for (int yy = y + 2; yy < y + h - 2; yy++) for (int xx = x + 2; xx < x + w - 2; xx++) if (pixels[yy * texture.width + xx].a > 35) { minX = Mathf.Min(minX, xx); minY = Mathf.Min(minY, yy); maxX = Mathf.Max(maxX, xx); maxY = Mathf.Max(maxY, yy); }
                    if (maxX > minX && maxY > minY) rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                }
                if (path.EndsWith("world.png") && i < 6) rect = new Rect(x + w * .075f, y + h * .075f, w * .85f, h * .85f);
                var old = previous.FirstOrDefault(r => r.name == names[i]);
                rects[i] = new SpriteRect { name = names[i], rect = rect, pivot = pivot, alignment = SpriteAlignment.Custom, spriteID = old?.spriteID ?? new GUID(Hash128.Compute(path + names[i]).ToString()) };
            }
            provider.SetSpriteRects(rects); provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID))); provider.Apply();
            importer.SaveAndReimport(); importer.isReadable = false; importer.SaveAndReimport();
            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return names.Select(name => all.Single(s => s.name == name)).ToArray();
        }
        static Material Material(string name, string shader)
        {
            string path = Root + "Art/Materials/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); } return m;
        }
        static TMP_FontAsset Font(string name, string source)
        {
            string path = Root + "UI/Fonts/Generated/" + name + ".asset"; var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f == null) {
                var font = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(Root + "UI/Fonts/" + source);
                f = TMP_FontAsset.CreateFontAsset(font, 42, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                f.name = name; AssetDatabase.CreateAsset(f, path); AssetDatabase.AddObjectToAsset(f.material, f);
                foreach (var texture in f.atlasTextures) AssetDatabase.AddObjectToAsset(texture, f);
            }
            var chars = new HashSet<char>(); foreach (string file in Directory.GetFiles(Root + "Resources/Localization", "*.json")) foreach (char ch in File.ReadAllText(file)) chars.Add(ch);
            foreach (char ch in "✓◆→←＋/!0123456789.%[]") chars.Add(ch);
            f.TryAddCharacters(new string(chars.ToArray()), out string missing); if (!string.IsNullOrEmpty(missing)) Debug.Log("M5 optional missing font characters: " + missing);
            foreach (var texture in f.atlasTextures) if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, f);
            EditorUtility.SetDirty(f); return f;
        }
        static void SetupTextSettings(TMP_FontAsset font)
        {
            const string path = "Assets/_Game/Resources/TMP Settings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(path);
            if (settings == null) { settings = ScriptableObject.CreateInstance<TMP_Settings>(); AssetDatabase.CreateAsset(settings, path); }
            var serialized = new SerializedObject(settings); if (font != null) serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            serialized.FindProperty("assetVersion").stringValue="2";
            serialized.FindProperty("m_leadingCharacters").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "UI/TMPResources/LineBreaking Leading Characters.txt");
            serialized.FindProperty("m_followingCharacters").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "UI/TMPResources/LineBreaking Following Characters.txt");
            serialized.FindProperty("m_warningsDisabled").boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings);
        }
        static VolumeProfile Profile(string name, bool minimal)
        {
            string path = Root + "Data/Rendering/VolumeProfiles/" + name + ".asset"; var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p != null) return p;
            p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path);
            var grade = p.Add<ColorAdjustments>(true); grade.saturation.value = 3; grade.contrast.value = 3;
            var tone = p.Add<Tonemapping>(true); tone.mode.value = TonemappingMode.Neutral;
            if (!minimal) { var bloom = p.Add<Bloom>(true); bloom.threshold.value = 1.25f; bloom.intensity.value = .18f; var vignette = p.Add<Vignette>(true); vignette.intensity.value = .08f; }
            foreach (var component in p.components) AssetDatabase.AddObjectToAsset(component, p); EditorUtility.SetDirty(p); return p;
        }
        static void SetupMixer(PresentationCatalog c)
        {
            string path = Root + "Audio/Mixers/StarfallMixer.mixer";
            c.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (c.mixer == null) {
                var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
                if (type == null) type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t => t != null);
                var method = type?.GetMethod("CreateMixerControllerAtPath", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                method?.Invoke(null, new object[] { path }); c.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            }
            if (c.mixer != null) {
                var master = c.mixer.FindMatchingGroups("Master").First(); var type = c.mixer.GetType();
                var childrenProperty = master.GetType().GetProperty("children", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                var groups = new List<AudioMixerGroup>();
                foreach (string name in new[] { "Music", "SFX", "UI" }) {
                    var group = c.mixer.FindMatchingGroups(name).FirstOrDefault();
                    if (group == null) group = (AudioMixerGroup)type.GetMethod("CreateNewGroup", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(c.mixer,new object[] { name, true });
                    groups.Add(group);
                }
                var array = Array.CreateInstance(master.GetType(), groups.Count); for (int i=0;i<groups.Count;i++) array.SetValue(groups[i],i); childrenProperty.SetValue(master,array);
                c.musicGroup = groups[0]; c.effectsGroup = groups[1]; c.uiGroup = groups[2]; EditorUtility.SetDirty(master); EditorUtility.SetDirty(c.mixer);
            }
        }
        [MenuItem("Starfall/Validate M5 Assets")]
        public static void Validate()
        {
            var c = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(Root + "Resources/PresentationCatalog.asset");
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root + "Resources/ItemCatalog.asset");
            if (c == null || c.explorer.Length != 16 || c.stageCards.Length != 6 || c.body == null || c.title == null || c.standard == null) throw new InvalidOperationException("M5 catalog missing assets");
            foreach (var item in items.items) if (!c.art.Any(a => a.id == item.id && a.sprite != null) || StageRewards.Source(item.id) < 0) throw new InvalidOperationException("Missing equipment art/reward source " + item.id);
            if (c.sounds.Any(s => s == null) || c.weapons.Any(s => s == null) || c.ambience == null) throw new InvalidOperationException("M5 audio missing");
            if(c.explorerVisual==null || c.art.Any(a=>a.prefab==null)) throw new InvalidOperationException("M5 visual prefabs missing");
            var text = new LocalizationService("zh-CN"); if (text.Validate().Count != 0) throw new InvalidOperationException("Localization mismatch");
            Debug.Log("STARFALL_M5_ASSETS_OK: " + c.art.Length + " sprites; 25 equipment; 6 stage illustrations; 16 explorer frames; bundled fonts/audio");
        }
        [MenuItem("Starfall/Build M5 Windows")]
        public static void BuildWindows()
        {
            Validate(); var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/_Game/Scenes/Boot.unity" }, locationPathName = "Builds/M5/StarcoreLabyrinth.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("M5 build failed " + report.summary.result);
            string licenseDir="Builds/M5/Licenses";Directory.CreateDirectory(licenseDir); foreach(var file in Directory.GetFiles(Root+"UI/Fonts/Licenses")) if(!file.EndsWith(".meta")) File.Copy(file,Path.Combine(licenseDir,Path.GetFileName(file)),true); File.Copy(Root+"UI/TMPResources/Unity-UGUI-LICENSE.md",licenseDir+"/Unity-UGUI-LICENSE.md",true);
            Debug.Log("STARFALL_M5_BUILD_OK: " + report.summary.totalSize + " bytes, errors=" + report.summary.totalErrors);
        }
    }
}
