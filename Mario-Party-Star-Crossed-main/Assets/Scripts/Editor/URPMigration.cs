using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Migração Built-in -> URP feita por linha de comando (unity command eval).
/// Passos idempotentes: CreateAndAssignAssets(), ConvertMaterials(), FixBuiltinDefaultMaterials(), Report().
/// </summary>
public static class URPMigration {
    const string Folder = "Assets/Settings/URP";
    const string RendererPath = Folder + "/URP_Renderer.asset";

    // Tiers por nível de qualidade: Very Low, Low -> Mobile_Low | Medium, High -> Mobile_Medium | Very High, Ultra -> PC_High
    static readonly string[] TierNames = { "URP_Low", "URP_Medium", "URP_High" };
    static readonly int[] QualityToTier = { 0, 0, 1, 1, 2, 2 };

    // ---------------------------------------------------------------- 1. Assets
    public static string CreateAndAssignAssets() {
        var log = new StringBuilder();
        if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Settings", "URP");

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null) {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
            log.AppendLine("Renderer criado");
        }
        if (renderer.postProcessData == null) {
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            EditorUtility.SetDirty(renderer);
        }

        var tiers = new UniversalRenderPipelineAsset[3];
        for (int t = 0; t < 3; t++) {
            string path = $"{Folder}/{TierNames[t]}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null) {
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, path);
                log.AppendLine($"{TierNames[t]} criado");
            }
            ConfigureTier(asset, t);
            tiers[t] = asset;
        }
        AssetDatabase.SaveAssets();

        GraphicsSettings.defaultRenderPipeline = tiers[1];

        int original = QualitySettings.GetQualityLevel();
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++) {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = tiers[QualityToTier[Mathf.Min(i, QualityToTier.Length - 1)]];
        }
        QualitySettings.SetQualityLevel(original, false);

        AssetDatabase.SaveAssets();
        log.AppendLine("Graphics + Quality atribuídos");
        return log.ToString();
    }

    static void ConfigureTier(UniversalRenderPipelineAsset asset, int tier) {
        var so = new SerializedObject(asset);
        void SetBool(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
        void SetInt(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
        void SetFloat(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }

        SetBool("m_SupportsHDR", tier > 0);
        SetInt("m_MSAA", tier == 0 ? 1 : (tier == 1 ? 2 : 4));
        SetFloat("m_RenderScale", 1f);
        SetBool("m_MainLightShadowsSupported", tier > 0);
        SetInt("m_MainLightShadowmapResolution", tier == 2 ? 2048 : 1024);
        SetFloat("m_ShadowDistance", tier == 2 ? 80f : 50f);
        SetInt("m_ShadowCascadeCount", tier == 2 ? 2 : 1);
        SetBool("m_SoftShadowsSupported", tier > 0);
        SetInt("m_AdditionalLightsRenderingMode", tier == 0 ? 0 : 1); // 0 desabilitado, 1 per-pixel
        SetInt("m_AdditionalLightsPerObjectLimit", tier == 2 ? 4 : 2);
        SetBool("m_AdditionalLightShadowsSupported", false);
        SetBool("m_SupportsCameraDepthTexture", false);
        SetBool("m_SupportsCameraOpaqueTexture", false);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    // ---------------------------------------------------------------- 2. Materiais
    public static string ConvertMaterials() {
        var log = new StringBuilder();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var simpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        var particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (lit == null || simpleLit == null || particles == null) return "ERRO: shaders URP não encontrados";

        int converted = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;
            string src = mat.shader.name;

            // Snapshot ANTES de trocar o shader
            Texture mainTex = Tex(mat, "_MainTex");
            Vector2 scale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 offset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture normal = Tex(mat, "_BumpMap");
            float bumpScale = Flt(mat, "_BumpScale", 1f);
            Texture metalMap = Tex(mat, "_MetallicGlossMap");
            Texture specMap = Tex(mat, "_SpecGlossMap");
            Texture occl = Tex(mat, "_OcclusionMap");
            float occlStr = Flt(mat, "_OcclusionStrength", 1f);
            Texture emisMap = Tex(mat, "_EmissionMap");
            Color emisColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            bool emisOn = mat.IsKeywordEnabled("_EMISSION");
            float metallic = Flt(mat, "_Metallic", 0f);
            float gloss = Flt(mat, "_Glossiness", 0.5f);
            float glossScale = Flt(mat, "_GlossMapScale", 1f);
            Color specColor = mat.HasProperty("_SpecColor") ? mat.GetColor("_SpecColor") : new Color(0.2f, 0.2f, 0.2f);
            float cutoff = Flt(mat, "_Cutoff", 0.5f);
            int mode = (int)Flt(mat, "_Mode", 0f);
            int queue = mat.renderQueue;

            switch (src) {
                case "Standard":
                case "Standard (Specular setup)":
                    mat.shader = lit;
                    bool specular = src.StartsWith("Standard (Specular");
                    mat.SetFloat("_WorkflowMode", specular ? 0f : 1f);
                    mat.SetFloat("_Metallic", metallic);
                    mat.SetFloat("_Smoothness", (metalMap != null || specMap != null) ? glossScale : gloss);
                    if (metalMap != null) mat.SetTexture("_MetallicGlossMap", metalMap);
                    if (specular) { mat.SetColor("_SpecColor", specColor); if (specMap != null) mat.SetTexture("_SpecGlossMap", specMap); }
                    if (occl != null) { mat.SetTexture("_OcclusionMap", occl); mat.SetFloat("_OcclusionStrength", occlStr); }
                    ApplyStandardMode(mat, mode, cutoff);
                    break;
                case "Legacy Shaders/Diffuse":
                case "Mobile/Diffuse":
                    mat.shader = simpleLit;
                    mat.SetFloat("_SpecularHighlights", 0f);
                    mat.SetFloat("_Smoothness", 0f);
                    ApplyStandardMode(mat, 0, cutoff);
                    break;
                case "Particles/Standard Surface":
                case "Particles/Standard Unlit":
                    mat.shader = particles;
                    ApplyParticleMode(mat, mode);
                    break;
                default:
                    continue; // TMP, GUI/Text, Skybox etc. já funcionam em URP
            }

            // Restaura dados do snapshot
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetTextureScale("_BaseMap", scale);
            mat.SetTextureOffset("_BaseMap", offset);
            mat.SetColor("_BaseColor", color);
            if (normal != null && mat.HasProperty("_BumpMap")) { mat.SetTexture("_BumpMap", normal); mat.SetFloat("_BumpScale", bumpScale); }
            if (mat.HasProperty("_EmissionColor")) {
                mat.SetColor("_EmissionColor", emisColor);
                if (emisMap != null) mat.SetTexture("_EmissionMap", emisMap);
            }
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);

            BaseShaderGUI.SetMaterialKeywords(mat);
            if (emisOn && emisColor.maxColorComponent > 0f) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
            if (queue >= 2000 && queue != mat.shader.renderQueue && mode == 0) mat.renderQueue = queue;

            if (mainTex != null && mat.GetTexture("_BaseMap") == null)
                log.AppendLine($"PERDA DE TEXTURA: {path}");
            EditorUtility.SetDirty(mat);
            converted++;
        }
        AssetDatabase.SaveAssets();
        log.Insert(0, $"Convertidos: {converted}\n");
        return log.ToString();
    }

    // Standard _Mode: 0 Opaque, 1 Cutout, 2 Fade, 3 Transparent
    static void ApplyStandardMode(Material m, int mode, float cutoff) {
        m.SetFloat("_Surface", mode >= 2 ? 1f : 0f);
        m.SetFloat("_AlphaClip", mode == 1 ? 1f : 0f);
        m.SetFloat("_Blend", mode == 3 ? 1f : 0f); // 0 Alpha, 1 Premultiply
        m.SetFloat("_Cull", 2f);
        if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", cutoff);
    }

    // Particles _Mode: 0 Opaque, 1 Cutout, 2 Fade, 3 Transparent, 4 Additive, 5 Subtractive, 6 Modulate
    static void ApplyParticleMode(Material m, int mode) {
        m.SetFloat("_Surface", mode >= 2 ? 1f : 0f);
        m.SetFloat("_AlphaClip", mode == 1 ? 1f : 0f);
        float blend = 0f;
        if (mode == 3) blend = 1f;
        else if (mode == 4 || mode == 5) blend = 2f;
        else if (mode == 6) blend = 3f;
        m.SetFloat("_Blend", blend);
    }

    static Texture Tex(Material m, string n) => m.HasProperty(n) ? m.GetTexture(n) : null;
    static float Flt(Material m, string n, float d) => m.HasProperty(n) ? m.GetFloat(n) : d;

    // ---------------------------------------------------------------- 3. Default-Material embutido
    public static string FixBuiltinDefaultMaterials() {
        var log = new StringBuilder();
        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        var urpDefault = urp != null ? urp.defaultMaterial : null;
        if (urpDefault == null) return "ERRO: material padrão URP indisponível";

        foreach (var path in new[] { "Assets/Prefabs/Board/Player.prefab", "Assets/Prefabs/Board/SkeletonGate.prefab" }) {
            var root = PrefabUtility.LoadPrefabContents(path);
            int fixedCount = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++) {
                    if (mats[i] != null && mats[i].name == "Default-Material" && AssetDatabase.GetAssetPath(mats[i]) == "Resources/unity_builtin_extra") {
                        mats[i] = urpDefault; changed = true; fixedCount++;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            log.AppendLine($"{path}: {fixedCount} trocados");
        }
        return log.ToString();
    }

    // ---------------------------------------------------------------- 4. Relatório
    public static string Report() {
        var sb = new StringBuilder();
        var rp = GraphicsSettings.defaultRenderPipeline;
        sb.AppendLine("Graphics: " + (rp == null ? "NULL" : rp.name));
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++) {
            var a = QualitySettings.GetRenderPipelineAssetAt(i);
            sb.AppendLine($"Q{i} {names[i]}: {(a == null ? "NULL" : a.name)}");
        }
        foreach (var t in TierNames) {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>($"{Folder}/{t}.asset");
            if (a == null) { sb.AppendLine(t + ": ausente"); continue; }
            var rd = a.rendererDataList.Length > 0 ? a.rendererDataList[0] as UniversalRendererData : null;
            sb.AppendLine($"{t}: renderer={(rd != null ? rd.name : "NULL")} ppData={(rd != null && rd.postProcessData != null)} msaa={a.msaaSampleCount} hdr={a.supportsHDR} shadowDist={a.shadowDistance} cascades={a.shadowCascadeCount}");
        }
        var counts = new Dictionary<string, int>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" })) {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            string s = m == null || m.shader == null ? "NULL" : m.shader.name;
            counts[s] = counts.TryGetValue(s, out int c) ? c + 1 : 1;
        }
        foreach (var kv in counts) sb.AppendLine($"  {kv.Key} = {kv.Value}");
        return sb.ToString();
    }
}

public static class URPMigrationLighting {
    // Ambiente Trilight: não depende de bake (o probe Skybox ficava preto em URP/Linear sem "Generate Lighting")
    public static string ApplyTrilight(string scenePath) {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.84f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.54f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.24f);
        RenderSettings.ambientIntensity = 1f;
        DynamicGI.UpdateEnvironment();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        var c = new Color[1];
        RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up }, c);
        return $"{scenePath}: mode={RenderSettings.ambientMode} probeUp={c[0]}";
    }
}

public static class URPPostProcessing {
    const string ProfilePath = "Assets/Settings/URP/PostFX_Tabuleiro.asset";

    // Perfil "Stylized/Mobile": cores vivas sem efeitos caros (sem DOF, grain, motion blur)
    public static string CreateProfile() {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null) {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        var tone = Override<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.Neutral);

        var color = Override<ColorAdjustments>(profile);
        color.postExposure.Override(0.15f);
        color.contrast.Override(12f);
        color.saturation.Override(18f);

        var bloom = Override<Bloom>(profile);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.6f);
        bloom.scatter.Override(0.65f);
        bloom.highQualityFiltering.Override(false);

        var vignette = Override<Vignette>(profile);
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.45f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ProfilePath);

        var reloaded = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        var names = new List<string>();
        foreach (var c in reloaded.components) names.Add(c == null ? "NULL" : c.GetType().Name);
        return $"{ProfilePath}: [{string.Join(", ", names)}]";
    }

    static T Override<T>(VolumeProfile profile) where T : VolumeComponent {
        if (profile.TryGet<T>(out var existing) && existing != null) return existing;
        var comp = profile.Add<T>(false);
        comp.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(comp, profile);
        EditorUtility.SetDirty(comp);
        return comp;
    }

    // Volume global na cena + liga pós na câmera de cada jogador (prefab + instâncias)
    public static string SetupScene(string scenePath) {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        // Carregar DEPOIS de abrir a cena: a troca de cena descarrega assets soltos
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);

        Volume volume = null;
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (v.gameObject.name == "Global Volume") volume = v;
        if (volume == null) {
            var go = new GameObject("Global Volume");
            go.layer = 0;
            volume = go.AddComponent<Volume>();
        }
        volume.isGlobal = true;
        volume.priority = 0;
        volume.weight = 1f;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);

        int cams = 0;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            var data = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // MSAA já vem do asset
            data.volumeLayerMask |= 1 << volume.gameObject.layer;
            EditorUtility.SetDirty(data);
            cams++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        return $"{scenePath}: volume={volume.name} profile={(volume.sharedProfile != null ? volume.sharedProfile.name : "NULL")} cameras={cams}";
    }

    public static string SetupPlayerPrefab() {
        const string path = "Assets/Prefabs/Board/Player.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        int n = 0;
        foreach (var cam in root.GetComponentsInChildren<Camera>(true)) {
            var data = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            n++;
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return $"{path}: {n} câmera(s)";
    }
}
