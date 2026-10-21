using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// Herramienta del equipo: dibuja sprites provisionales, genera sonidos y arma el nivel completo de Lumbra.
// Menú: Lumbra > Construir nivel (y Construir menú). Romina puede reemplazar los sprites de 03_Sprites por el arte final.
public static class LumbraBuilder
{
    const string SpritesPath = "Assets/03_Sprites/";
    const string ScenePath = "Assets/00_Scenes/Santuario.unity";
    const float LayerWidth = 40f;
    const int GroundLayer = 8;

    static Material unlit, lit;
    static Sprite square, circle, glow, blob, ring, thorns, stone, moss, column, brokenColumn, heart, bars, archWindow, banner, cracked, arrow;
    static Transform level, levelRoot, systems, background;
    static ParticleSystem shadowDeath, rootBurn;
    static Material addParticles;
    static Font bodyFont, boldFont, titleFont;
    static ShadowEnemy hunterPrefab;

    [MenuItem("Lumbra/Construir nivel")]
    public static void Build()
    {
        Random.InitState(7);
        PlayerSettings.productName = "Lumbra";
        EnsureGroundLayer();

        // En Unity 6.5 el SRP Batcher hace desaparecer algunas capas 2D al dibujarlas; lo apagamos
        var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        urp.useSRPBatcher = false;
        EditorUtility.SetDirty(urp);
        foreach (string folder in new[] { "00_Scenes", "02_Prefabs", "03_Sprites/Player", "04_Materials", "05_Audio" })
        {
            Directory.CreateDirectory("Assets/" + folder);
        }

        LoadFonts();
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

        // ---------- Sprites provisionales ----------
        square = MakeSprite("Square", 32, 32, 32, (u, v) => 1f);
        circle = MakeSprite("Circle", 128, 128, 128, (u, v) => Edge(1f - Dist(u, v) * 2f, 40f));
        glow = MakeSprite("Glow", 128, 128, 128, (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Dist(u, v) * 2f), 2.2f));
        ring = MakeSprite("Ring", 256, 256, 128, (u, v) => { float d = Dist(u, v) * 2f; return Edge(1f - d, 60f) * Edge(d - 0.8f, 60f); });
        blob = MakeSprite("ShadowBlob", 128, 128, 100, Blob);
        thorns = MakeSprite("Thorns", 32, 16, 32, (u, v) => Edge(1f - Mathf.Abs((u * 2f) % 1f * 2f - 1f) - v, 30f), null, new Vector2(0.5f, 0f));
        heart = MakeSprite("Heart", 64, 64, 64, Heart);
        arrow = MakeSprite("Arrow", 64, 64, 64, (u, v) => Edge(0.42f - v * 0.5f - Mathf.Abs(u - 0.5f), 80f) * Edge(v - 0.08f, 80f));
        bars = MakeSprite("Bars", 32, 32, 32, (u, v) => Edge(0.12f - Mathf.Abs(u - 0.5f), 60f) + Edge(0.08f - Mathf.Abs(v - 0.5f), 60f), null, null, true);
        archWindow = MakeSprite("ArchWindow", 64, 128, 32, ArchWindow);
        banner = MakeSprite("Banner", 32, 128, 32, (u, v) => v > 0.15f || Mathf.Abs(u - 0.5f) > v * 2.5f ? 1f : 0f, null, new Vector2(0.5f, 1f));
        stone = MakeSprite("StoneTile", 64, 64, 32, (u, v) => 1f, StoneColor, null, true);
        cracked = MakeSprite("CrackedStone", 64, 16, 32, (u, v) => 1f, CrackedColor, null, true);
        moss = MakeSprite("MossTop", 64, 16, 32, Moss, null, new Vector2(0.5f, 0f), true);
        column = MakeSprite("Column", 64, 256, 32, (u, v) => ColumnShape(u, v, false), ColumnColor, new Vector2(0.5f, 0f));
        brokenColumn = MakeSprite("ColumnBroken", 64, 256, 32, (u, v) => ColumnShape(u, v, true), ColumnColor, new Vector2(0.5f, 0f));
        Sprite sky = MakeSprite("Sky", 64, 256, 10, (u, v) => 1f, SkyColor);
        Sprite stars = MakeStars();
        Sprite farRuins = MakeRuins("RuinsFar", 9);
        Sprite midRuins = MakeRuins("RuinsMid", 6);
        Sprite hills = MakeSilhouette("Hills", 0.2f, 0.12f, 10);
        Sprite darkEdge = MakeSprite("DarkEdge", 128, 64, 6.4f, DarkEdge, null, new Vector2(0.72f, 0.5f));

        addParticles = MakeParticleMaterial("ParticlesAdd", glow.texture, true);
        Material darkParticles = MakeParticleMaterial("ParticlesDark", glow.texture, false);

        // ---------- Prefabs de efectos ----------
        shadowDeath = SaveBurst("ShadowDeath", darkParticles, new Color(0.35f, 0.1f, 0.5f), Color.black, 30);
        rootBurn = SaveBurst("RootBurn", addParticles, new Color(0.6f, 0.9f, 1f), new Color(0.5f, 0.2f, 0.8f), 40);
        ParticleSystem playerDeath = SaveBurst("PlayerDeath", addParticles, new Color(1f, 0.9f, 0.6f), new Color(1f, 0.6f, 0.3f), 60);


        // ---------- Escena ----------
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Noche cerrada: casi todo se ve por la luz del personaje, las almas y los faroles
        systems = new GameObject("Sistemas").transform;
        background = new GameObject("Fondo").transform;
        var globalLight = new GameObject("Global Light 2D").AddComponent<Light2D>();
        globalLight.transform.SetParent(systems);
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.45f, 0.45f, 0.85f);
        globalLight.intensity = 0.32f;

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(5f, 2.5f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camGO.AddComponent<AudioListener>();
        var follow = camGO.AddComponent<CameraFollow>();

        // Cielo y luna pegados a la cámara
        var skyMaterial = new Material(unlit);
        AssetDatabase.DeleteAsset("Assets/04_Materials/Sky.mat");
        AssetDatabase.CreateAsset(skyMaterial, "Assets/04_Materials/Sky.mat");
        SpriteRenderer skyRenderer = MakeRenderer("Sky", camGO.transform, sky, Color.white, -100, skyMaterial);
        skyRenderer.transform.localPosition = new Vector3(0f, 0f, 60f);
        skyRenderer.transform.localScale = new Vector3(4.5f, 1f, 1f);
        SpriteRenderer moonGlow = MakeRenderer("MoonGlow", camGO.transform, glow, new Color(0.55f, 0.55f, 0.95f, 0.35f), -96, unlit);
        moonGlow.transform.localPosition = new Vector3(6.5f, 3.5f, 55f);
        moonGlow.transform.localScale = Vector3.one * 8f;
        SpriteRenderer moon = MakeRenderer("Moon", camGO.transform, circle, new Color(0.85f, 0.85f, 1f, 0.85f), -95, unlit);
        moon.transform.localPosition = new Vector3(6.5f, 3.5f, 55f);
        moon.transform.localScale = Vector3.one * 1.4f;

        // Fondo en capas: estrellas, ruinas lejanas, niebla, ruinas cercanas, colinas y niebla baja
        MakeLayer("Stars", stars, new Color(1f, 1f, 1f, 0.7f), 0.95f, -6f, -90, Vector3.one);
        MakeLayer("RuinsFar", farRuins, new Color(0.17f, 0.13f, 0.32f), 0.8f, -7f, -50, Vector3.one);
        MakeLayer("Fog", glow, new Color(0.4f, 0.3f, 0.75f, 0.22f), 0.7f, 0f, -45, new Vector3(45f, 6f, 1f));
        MakeLayer("RuinsMid", midRuins, new Color(0.07f, 0.05f, 0.15f), 0.6f, -7.5f, -40, Vector3.one);
        MakeLayer("Hills", hills, new Color(0.03f, 0.02f, 0.07f), 0.4f, -8f, -30, Vector3.one);
        MakeLayer("LowFog", glow, new Color(0.35f, 0.25f, 0.6f, 0.18f), 0.3f, -2f, -25, new Vector3(45f, 4f, 1f));

        ParticleSystem spores = MakeParticles("Spores", camGO.transform, addParticles, new Color(0.3f, 0.9f, 1f), new Color(0.8f, 0.5f, 1f), -20);
        spores.transform.localPosition = new Vector3(0f, 0f, 10f);
        var sporesMain = spores.main;
        sporesMain.startLifetime = 8f;
        sporesMain.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        sporesMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        sporesMain.prewarm = true;
        var sporesEmission = spores.emission;
        sporesEmission.rateOverTime = 12f;
        var sporesShape = spores.shape;
        sporesShape.shapeType = ParticleSystemShapeType.Box;
        sporesShape.scale = new Vector3(26f, 14f, 1f);

        GameObject player = MakePlayer(playerDeath);
        Set(follow, "target", player.transform);

        var managers = new GameObject("GameManager");
        managers.transform.SetParent(systems);
        managers.AddComponent<GameManager>();
        var soulManager = managers.AddComponent<SoulManager>();
        MakeAudio();

        levelRoot = level = new GameObject("Level").transform;
        Soul[] souls = BuildLevel();
        SetList(soulManager, "souls", souls);

        MakeChaser(darkEdge, darkParticles);

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.transform.SetParent(systems);
        Set(volume.gameObject.AddComponent<PanicEffects>(), "volume", volume);
        volume.isGlobal = true;
        volume.sharedProfile = MakeVolumeProfile();

        MakeHUD();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Nivel de Lumbra construido: " + ScenePath);
        BuildMenu();
    }

    const string MenuPath = "Assets/00_Scenes/Menu.unity";

    // Pantalla de título: fondo del santuario con Lumbra, almas flotando y los ojos del Devorador a lo lejos.
    // Usa los sprites que ya creó "Construir nivel".
    [MenuItem("Lumbra/Construir menú")]
    public static void BuildMenu()
    {
        LoadFonts();
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        square = LoadSprite("Square");
        circle = LoadSprite("Circle");
        glow = LoadSprite("Glow");
        stone = LoadSprite("StoneTile");
        moss = LoadSprite("MossTop");
        column = LoadSprite("Column");
        addParticles = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/ParticlesAdd.mat");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var globalLight = new GameObject("Global Light 2D").AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.45f, 0.45f, 0.85f);
        globalLight.intensity = 0.25f;

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camGO.AddComponent<AudioListener>();
        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/04_Materials/LumbraVolume.asset");

        // Cielo, luna, ruinas y niebla
        SpriteRenderer sky = MakeRenderer("Sky", camGO.transform, LoadSprite("Sky"), Color.white, -100, AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Sky.mat"));
        sky.transform.localPosition = new Vector3(0f, 0f, 60f);
        sky.transform.localScale = new Vector3(4.5f, 1f, 1f);
        MakeRenderer("MoonGlow", null, glow, new Color(0.55f, 0.55f, 0.95f, 0.35f), -96, unlit).transform.localScale = Vector3.one * 8f;
        GameObject.Find("MoonGlow").transform.position = new Vector3(-2f, 3.5f, 0f);
        SpriteRenderer moonDisc = MakeRenderer("Moon", null, circle, new Color(0.85f, 0.85f, 1f, 0.85f), -95, unlit);
        moonDisc.transform.position = new Vector3(-2f, 3.5f, 0f);
        moonDisc.transform.localScale = Vector3.one * 1.4f;
        MakeRenderer("RuinsFar", null, LoadSprite("RuinsFar"), new Color(0.17f, 0.13f, 0.32f), -50, unlit).transform.position = new Vector3(0f, -9f, 0f);
        SpriteRenderer fog = MakeRenderer("Fog", null, glow, new Color(0.4f, 0.3f, 0.75f, 0.25f), -45, unlit);
        fog.transform.position = new Vector3(0f, -2f, 0f);
        fog.transform.localScale = new Vector3(40f, 6f, 1f);
        MakeRenderer("RuinsMid", null, LoadSprite("RuinsMid"), new Color(0.07f, 0.05f, 0.15f), -40, unlit).transform.position = new Vector3(6f, -9.5f, 0f);

        // Los ojos del Devorador a lo lejos
        foreach (float x in new[] { 10.2f, 10.9f })
        {
            SpriteRenderer eye = MakeRenderer("DevourerEye", null, circle, new Color(1f, 0.2f, 0.25f, 0.6f), -44, unlit);
            eye.transform.position = new Vector3(x, -1.2f, 0f);
            eye.transform.localScale = new Vector3(0.35f, 0.2f, 1f);
        }

        // Suelo de piedra con musgo y columnas
        SpriteRenderer floor = MakeRenderer("Floor", null, stone, new Color(0.75f, 0.72f, 0.9f), 0, lit);
        floor.drawMode = SpriteDrawMode.Tiled;
        floor.size = new Vector2(30f, 4f);
        floor.transform.position = new Vector3(0f, -5f, 0f);
        SpriteRenderer top = MakeRenderer("Moss", floor.transform, moss, new Color(0.45f, 0.85f, 0.75f), 1, lit);
        top.drawMode = SpriteDrawMode.Tiled;
        top.size = new Vector2(30f, 0.5f);
        top.transform.localPosition = new Vector3(0f, 1.88f, 0f);
        foreach (float x in new[] { -9.5f, -1.5f })
        {
            MakeRenderer("Column", null, column, new Color(0.55f, 0.5f, 0.7f), -6, lit).transform.position = new Vector3(x, -3.2f, 0f);
        }

        // Lumbra con su luz y tres almas flotando
        var hero = new GameObject("Hero").transform;
        hero.position = new Vector3(-5.5f, -3f, 0f);
        hero.localScale = Vector3.one * 1.6f;
        MakeRig(hero);
        MakeRenderer("Halo", hero, glow, new Color(1f, 0.8f, 0.45f, 0.4f), 8, unlit).transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Light2D aura = MakeLight("Aura", hero, new Color(1f, 0.82f, 0.55f), 6f, 1.5f);
        aura.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var souls = new Object[3];
        Vector2[] soulPos = { new Vector2(-7.3f, -0.4f), new Vector2(-4f, 0.6f), new Vector2(-2.8f, -1.4f) };
        for (int i = 0; i < souls.Length; i++)
        {
            var soul = new GameObject("Soul").transform;
            soul.position = soulPos[i];
            MakeRenderer("Halo", soul, glow, new Color(0.55f, 0.95f, 1f, 0.7f), 7, unlit).transform.localScale = Vector3.one * 1.3f;
            MakeRenderer("Core", soul, circle, Color.white, 8, unlit).transform.localScale = Vector3.one * 0.32f;
            MakeLight("Light", soul, new Color(0.55f, 0.95f, 1f), 2.8f, 1f);
            souls[i] = soul;
        }

        ParticleSystem spores = MakeParticles("Spores", camGO.transform, addParticles, new Color(0.3f, 0.9f, 1f), new Color(0.8f, 0.5f, 1f), -20);
        spores.transform.localPosition = new Vector3(0f, 0f, 10f);
        var sporesMain = spores.main;
        sporesMain.startLifetime = 8f;
        sporesMain.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        sporesMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        sporesMain.prewarm = true;
        var sporesEmission = spores.emission;
        sporesEmission.rateOverTime = 15f;
        var sporesShape = spores.shape;
        sporesShape.shapeType = ParticleSystemShapeType.Box;
        sporesShape.scale = new Vector3(26f, 14f, 1f);

        // Sonido: ambiente de fondo y el clic de los botones
        var audioGO = new GameObject("Audio");
        AudioSource ambience = audioGO.AddComponent<AudioSource>();
        ambience.clip = Real("Ambience");
        ambience.loop = true;
        ambience.volume = 0.4f;
        AudioSource click = audioGO.AddComponent<AudioSource>();
        click.clip = Real("Click");
        click.playOnAwake = false;

        // ---------- Interfaz ----------
        var canvas = new GameObject("Menu").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        MakeEventSystem();
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 column2 = new Vector2(440f, 0f);

        MakeImage("TitleGlow", canvas.transform, glow, new Color(1f, 0.8f, 0.45f, 0.15f), center, column2 + new Vector2(0f, 300f), new Vector2(900f, 400f), center);
        Text title = Titled(MakeText("Title", canvas.transform, "LUMBRA", 160, center, column2 + new Vector2(0f, 300f), TextAnchor.MiddleCenter));
        title.color = new Color(1f, 0.88f, 0.6f);
        title.fontStyle = FontStyle.Bold;
        Text tagline = MakeText("Tagline", canvas.transform, "", 32, center, column2 + new Vector2(0f, 190f), TextAnchor.MiddleCenter);
        tagline.fontStyle = FontStyle.Italic;
        tagline.color = new Color(0.75f, 0.7f, 1f);
        Localize(tagline.gameObject, "tagline");

        var main = new GameObject("MainPanel", typeof(RectTransform));
        main.transform.SetParent(canvas.transform, false);
        Button continueButton = MenuButton(main.transform, "continue", column2 + new Vector2(0f, 60f));
        Button newGame = MenuButton(main.transform, "newGame", column2 + new Vector2(0f, -20f));
        Button options = MenuButton(main.transform, "options", column2 + new Vector2(0f, -100f));
        Button quit = MenuButton(main.transform, "quit", column2 + new Vector2(0f, -180f));

        var optionsPanel = new GameObject("OptionsPanel", typeof(RectTransform));
        optionsPanel.transform.SetParent(canvas.transform, false);
        Slider volumeSlider = VolumeSlider(optionsPanel.transform, column2 + new Vector2(0f, 40f));
        LanguageButtons(optionsPanel.transform, column2 + new Vector2(0f, -70f), out Button spanish, out Button english);
        Button back = MenuButton(optionsPanel.transform, "back", column2 + new Vector2(0f, -180f));

        Text credits = MakeText("Credits", canvas.transform, "GameJamPlus 26/27  ·  Brandon & Romina", 20, new Vector2(1f, 0f), new Vector2(-30f, 30f), TextAnchor.LowerRight);
        credits.color = new Color(1f, 1f, 1f, 0.35f);

        // Pantalla de carga
        Image loading = FullPanel("Loading", canvas.transform, new Color(0.01f, 0.01f, 0.03f, 1f));
        var loadingGroup = loading.gameObject.AddComponent<CanvasGroup>();
        Text loadingText = Titled(MakeText("LoadingText", loading.transform, "", 40, center, new Vector2(0f, 40f), TextAnchor.MiddleCenter));
        loadingText.color = new Color(1f, 0.88f, 0.6f);
        Localize(loadingText.gameObject, "loading");
        MakeImage("BarBack", loading.transform, square, new Color(1f, 1f, 1f, 0.1f), center, new Vector2(0f, -20f), new Vector2(600f, 8f), center);
        Image bar = MakeImage("Bar", loading.transform, square, new Color(1f, 0.82f, 0.45f), center, new Vector2(0f, -20f), new Vector2(600f, 8f), center);
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        Text tip = MakeText("Tip", loading.transform, "", 28, center, new Vector2(0f, -90f), TextAnchor.MiddleCenter);
        tip.fontStyle = FontStyle.Italic;
        tip.color = new Color(0.75f, 0.85f, 1f, 0.8f);
        loadingGroup.blocksRaycasts = false;

        var menu = canvas.gameObject.AddComponent<TitleMenu>();
        Set(menu, "mainPanel", main);
        Set(menu, "optionsPanel", optionsPanel);
        Set(menu, "loadingPanel", loadingGroup);
        Set(menu, "loadingBar", bar);
        Set(menu, "tipText", tip);
        Set(menu, "continueButton", continueButton);
        Set(menu, "newGameButton", newGame);
        Set(menu, "optionsButton", options);
        Set(menu, "quitButton", quit);
        Set(menu, "backButton", back);
        Set(menu, "spanishButton", spanish);
        Set(menu, "englishButton", english);
        Set(menu, "volumeSlider", volumeSlider);
        SetList(menu, "souls", souls);
        Set(menu, "click", click);

        EditorSceneManager.SaveScene(scene, MenuPath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuPath, true), new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Menú de Lumbra construido: " + MenuPath);
    }

    static void LoadFonts()
    {
        bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/06_Fonts/AlegreyaSans-Regular.ttf");
        boldFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/06_Fonts/AlegreyaSans-Bold.ttf");
        titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/06_Fonts/Cinzel.ttf");
    }

    static Text Titled(Text text)
    {
        text.font = titleFont;
        return text;
    }

    static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritesPath + name + ".png");
    }

    // ================= Nivel =================

    static DialogueLine L(string speaker, string es, string en) => new DialogueLine(speaker, es, en);

    // Tres zonas: El santuario caído (tutorial), El camino de las mentiras (bifurcación) y No mires atrás (persecución).
    // Los huecos miden como máximo 3 unidades y los escalones 2.2 (lo que alcanza un salto).
    static Soul[] BuildLevel()
    {
        // Sombra cazadora (la usan el jefe y la traición del alma corrupta)
        GameObject hunter = Shadow(ShadowType.Wanderer, 0f, 0f, false, 30f);
        hunterPrefab = PrefabUtility.SaveAsPrefabAsset(hunter, "Assets/02_Prefabs/ShadowHunter.prefab").GetComponent<ShadowEnemy>();
        Object.DestroyImmediate(hunter);

        // ----- Zona 1: El santuario caído -----
        Zone("1 - Santuario caido");
        Ground(-14f, -10f, 20f);
        Ground(-10f, 14f, 0f);
        Columns(0f, -6f, 4f, 10f);
        SetVoice(Story(2f, L("???", "Lumbra... despierta. Rescata las almas y llega al portal.", "Lumbra... wake up. Rescue the souls and reach the portal.")), "Wake");
        Ground(16.5f, 26f, 0f);
        Pillar(22.4f, 23.2f, 2.2f, 9.5f);
        Ground(26f, 40f, 7f);
        Columns(7f, 30f, 36f);
        PromptText(24.6f, 3.5f, "", null, "pressWall");
        Story(37f, L("???", "Tu luz revela lo que la oscuridad esconde.", "Your light reveals what the darkness hides."));
        PromptText(38.5f, 9f, "E", null);
        Hidden(42.8f, 6.4f, 2.2f);
        Hidden(46.6f, 6.6f, 2.2f);
        Ground(50f, 66f, 6f);
        Checkpoint(52f, 6f);

        // Desafío 1 — El pozo: trepar entre el pilar y la torre con espinas abajo y una sombra rondando
        Story(57f, L("???", "Un alma espera en lo alto. Trepa... y cuidado con lo que vuela.", "A soul waits up high. Climb... and beware of what flies."));
        Pillar(61.6f, 62.4f, 9.5f, 19f);
        Spikes(level, 64.2f, 6f, 1.6f, false);
        Ground(65f, 70f, 17f);
        Shadow(ShadowType.Wanderer, 63.6f, 13f, false, 4f);
        Soul soul1 = TrappedSoul(67.5f, 18.4f, SoulPower.DoubleJump, L("Alma", "¡Gracias! Te doy mi salto: ahora puedes saltar otra vez en el aire.", "Thank you! I give you my leap: now you can jump again in mid-air."));
        Platform(73.5f, 14.5f, 2.4f);
        Platform(77.5f, 11.5f, 2.4f);
        LookBackLantern(75f, 6f);
        Ground(70f, 96f, 6f);
        WallText(81f, 10.5f, "wallVoice", "Trust");
        Shadow(ShadowType.Wanderer, 86f, 10f, false, 7f);

        // ----- Zona 2: El camino de las mentiras -----
        Zone("2 - Camino de las mentiras");
        Checkpoint(84f, 6f);
        Story(87f, L("???", "Aquí un alma miente. Su sombra sube en vez de caer.", "Here, a soul lies. Its shadow rises instead of falling."), L("", "Descúbrela con E y suéltala con Q.", "Expose it with E and release it with Q."));

        // Desafío 2 — El abismo: plataformas que se caen, una de sombra (no uses tu luz encima) y una acechadora
        Story(93f, L("???", "Las plataformas de sombra se deshacen con tu luz. Cuidado con el destello.", "Shadow platforms fade under your light. Careful with the flash."));
        Crumble(98.2f, 7f);
        Crumble(101.8f, 8f);
        ShadowPlat(105.4f, 9f, 2.2f);
        Crumble(109f, 8.5f);
        Shadow(ShadowType.Ambusher, 104f, 14.5f, false, 7f);
        Pillar(111.8f, 114f, -12f, 11f);
        Soul soul2 = TrappedSoul(112.9f, 12.4f, SoulPower.BigFlash, L("Alma", "Mi luz se suma a la tuya: tu destello llegará más lejos.", "My light joins yours: your flash will reach further."));
        Ground(116f, 190f, 6f);
        Checkpoint(118f, 6f);

        // Bifurcación: ruta de arriba (plataformas) o de abajo (suelo). Una de las dos esconde espinas.
        Platform(122f, 8f, 2.5f);
        Platform(139f, 8f, 2.5f);
        foreach (float x in new[] { 125.7f, 130.2f, 134.7f }) Platform(x, 10f, 3f);
        var upperTrap = new GameObject("Trap_Upper").transform;
        upperTrap.SetParent(level);
        Spikes(upperTrap, 130.2f, 10f, 3f, true);
        Spikes(upperTrap, 134.7f, 10f, 3f, true);
        var lowerTrap = new GameObject("Trap_Lower").transform;
        lowerTrap.SetParent(level);
        Spikes(lowerTrap, 130.75f, 6f, 11.5f, true);
        MakeFork(120f, new Vector2(122f, 9.3f), new Vector2(124f, 7.2f), upperTrap.gameObject, lowerTrap.gameObject);
        LookBackSign(131.5f, 14f, "signLie", "signLie2");
        Shadow(ShadowType.Wanderer, 128.5f, 8f, true, 7f); // falsa: solo humo

        // Secreto: el alma del recuerdo, arriba, solo alcanzable con plataformas ocultas
        Hidden(145f, 7.9f, 2f);
        Hidden(148.3f, 9.9f, 2f);
        Soul memory = MakeSoul("Soul_Memory", new Vector2(151.2f, 11.6f), true, SoulPower.None, L("Alma del recuerdo", "Te conozco... Llévame al portal.", "I know you... Take me to the portal."));

        // Desafío 3 — Las guardianas: sombras de plataforma y plataformas ocultas juntas, con dos sombras cuidando
        Checkpoint(156f, 6f);
        Story(158f, L("???", "La última alma está custodiada. Piensa antes de usar tu luz.", "The last soul is guarded. Think before using your light."));
        ShadowPlat(161f, 8.2f, 2f);
        ShadowPlat(164.4f, 10.2f, 2f);
        Hidden(167.8f, 12.2f, 2f);
        Platform(171.6f, 13.6f, 2.4f);
        Shadow(ShadowType.Wanderer, 166f, 15f, false, 6f);
        Shadow(ShadowType.Ambusher, 172f, 18f, false, 7f);
        Soul soul3 = TrappedSoul(171.6f, 15f, SoulPower.Shield, L("Alma", "Te protegeré: aguantaré un golpe por ti.", "I'll protect you: I'll take a hit for you."));
        FakeTrappedSoul(141.5f, 7.4f);
        WatchingEyesAt(150f, 15f, 178f, 12f, 186f, 15f);
        Checkpoint(184f, 6f);

        // ----- Zona 3: No mires atrás (la oscuridad te persigue) -----
        Zone("3 - No mires atras");
        Story(188f, L("???", "¡Corre! Y no mires atrás.", "Run! And don't look back."));
        Ground(193f, 201f, 6f);
        Crumble(203.6f, 6.3f);
        Crumble(207.2f, 6.9f);
        Crumble(210.8f, 7.4f);
        Ground(213.5f, 226f, 7f);
        Pillar(222.4f, 223.2f, 9.3f, 16f);
        Ground(226f, 237f, 11f);
        Ground(240f, 290f, 9f);
        Columns(7f, 217f);
        Columns(11f, 229f, 234f);
        Shadow(ShadowType.Wanderer, 217f, 11f, false, 7f);
        FakeTrappedSoul(197f, 7.4f);
        Shadow(ShadowType.Ambusher, 231f, 15f, false, 7f);
        WallText(232f, 15f, "wallWaits", "Waits");
        LookBackLantern(219f, 7f);

        // ----- Arena del Devorador -----
        Zone("4 - Castillo del Devorador");
        MakeBossArena();
        Ground(290f, 296f, 30f);
        WatchingEyesAt(30f, 12f, 60f, 14f, 100f, 15f, 140f, 13f, 205f, 13f);

        return new[] { soul1, soul2, soul3, memory, ExtraSouls[0], ExtraSouls[1], ExtraSouls[2] };
    }

    // Todo lo que se cree después queda dentro de esta zona en la jerarquía
    static void Zone(string name)
    {
        level = new GameObject(name).transform;
        level.SetParent(levelRoot);
    }

    static readonly Soul[] ExtraSouls = new Soul[3];

    // Tres braseros en lo alto, cada uno con su propio parkour, y dos almas perdidas más para reemplazar a la traidora
    static void MakeBossArena()
    {
        Checkpoint(242.5f, 9f);
        Torch[] torches = MakeCastle();

        // Rastrillo de hierro: baja de golpe y te encierra al despertar al jefe
        SpriteRenderer gate = MakeRenderer("BossGate", level, bars, new Color(0.25f, 0.22f, 0.3f), 3, lit);
        gate.drawMode = SpriteDrawMode.Tiled;
        gate.size = new Vector2(1.2f, 15f);
        gate.transform.position = new Vector3(240.4f, 16.5f, 0f);
        gate.gameObject.layer = GroundLayer;
        gate.gameObject.AddComponent<BoxCollider2D>().size = gate.size;
        gate.gameObject.SetActive(false);

        // El suelo tiene espinas: hay que moverse por arriba
        Spikes(level, 253.5f, 9f, 9f, false);
        Spikes(level, 271.5f, 9f, 7f, false);

        // Brasero 1: escalón, plataforma que se cae y una columna alta
        Platform(244.8f, 11.6f, 1.6f);
        Crumble(247.6f, 13.6f);
        Platform(250.6f, 15.8f, 1.4f);
        Brazier b1 = MakeBrazier(250.6f, 15.8f);
        Platform(253.8f, 18.4f, 1.2f);
        ExtraSouls[0] = TrappedSoul(253.8f, 19.8f, SoulPower.None, L("Alma perdida", "Por fin... úsame contra él.", "At last... use me against him."));

        // Brasero 2: al centro, solo por plataformas de sombra sobre las espinas (tu destello las deshace)
        ShadowPlat(257.2f, 12.2f, 1.6f);
        ShadowPlat(260f, 14.6f, 1.6f);
        Platform(263.2f, 17f, 1.6f);
        Brazier b2 = MakeBrazier(263.2f, 17f);
        Platform(266.8f, 19.4f, 1.2f);
        ExtraSouls[1] = TrappedSoul(266.8f, 20.8f, SoulPower.None, L("Alma perdida", "Llévame a la luz.", "Take me to the light."));

        // Brasero 3: a la derecha, dos plataformas que se derrumban sobre espinas
        Crumble(269.8f, 12f);
        Crumble(272.8f, 14.2f);
        Platform(276.2f, 16.6f, 1.6f);
        Brazier b3 = MakeBrazier(276.2f, 16.6f);

        // Brasero 4: el más alto, sobre la columna del alma perdida de la izquierda
        Platform(257.8f, 20.4f, 1.6f);
        Brazier b4 = MakeBrazier(257.8f, 20.4f);
        ExtraSouls[2] = TrappedSoul(280.6f, 19.8f, SoulPower.None, L("Alma perdida", "Aún brillo... llévame.", "I still shine... take me."));

        // Lugares altos y difíciles donde el Devorador lanza las almas que roba
        Platform(243f, 15.4f, 1.2f);
        Platform(280.6f, 18.4f, 1.2f);
        var perches = new Object[4];
        Vector2[] perchSpots = { new Vector2(253.8f, 19.8f), new Vector2(266.8f, 20.8f), new Vector2(243f, 16.6f), new Vector2(280.6f, 19.6f) };
        for (int i = 0; i < perches.Length; i++)
        {
            var perch = new GameObject("Perch").transform;
            perch.SetParent(level);
            perch.position = perchSpots[i];
            perches[i] = perch;
        }

        // El Devorador: una sombra enorme con ojos detrás de la arena
        var boss = new GameObject("Devorador");
        boss.transform.position = new Vector3(264f, 9f, 0f);
        // Un halo morado detrás para que su silueta negra se recorte contra el cielo
        SpriteRenderer aura = MakeRenderer("Aura", boss.transform, glow, new Color(0.55f, 0.1f, 0.7f, 0.55f), -9, unlit);
        aura.transform.position = new Vector3(264f, 18f, 0f);
        aura.transform.localScale = Vector3.one * 18f;
        SpriteRenderer body = MakeRenderer("Body", boss.transform, blob, Color.black, -8, unlit);
        body.transform.position = new Vector3(264f, 18f, 0f);
        body.transform.localScale = Vector3.one * 10f;
        var eyes = new GameObject("Eyes").transform;
        eyes.SetParent(body.transform, false);
        eyes.localPosition = new Vector3(0f, 0.08f, 0f);
        eyes.localScale = new Vector3(1f, 0.1f, 1f); // cerrados hasta que despierta
        foreach (float x in new[] { -0.12f, 0.12f })
        {
            SpriteRenderer eye = MakeRenderer("Eye", eyes, circle, new Color(1f, 0.2f, 0.25f), -7, unlit);
            eye.transform.localPosition = new Vector3(x, 0f, 0f);
            eye.transform.localScale = Vector3.one * 0.09f;
            MakeLight("Light", eye.transform, new Color(1f, 0.2f, 0.25f), 4f, 1.2f).transform.localScale = Vector3.one;
        }

        SpriteRenderer claw = MakeRenderer("Claw", boss.transform, thorns, new Color(0.05f, 0f, 0.08f), -1, unlit);
        claw.transform.position = new Vector3(260f, 4f, 0f);
        claw.transform.localScale = new Vector3(1.2f, 7f, 1f);
        var clawCol = claw.gameObject.AddComponent<BoxCollider2D>();
        clawCol.isTrigger = true;
        claw.gameObject.AddComponent<Hazard>();
        SpriteRenderer warning = MakeRenderer("Warning", boss.transform, glow, Color.clear, 3, unlit);
        warning.transform.localScale = new Vector3(2f, 0.8f, 1f);
        var bossClaw = claw.gameObject.AddComponent<BossClaw>();
        Set(bossClaw, "warning", warning);
        var spawned = new GameObject("Spawned").transform;
        spawned.SetParent(boss.transform);

        var portal = MakePortal(new Vector2(285f, 11f));
        portal.SetActive(false);

        var component = boss.AddComponent<Boss>();
        SetList(component, "braziers", new Object[] { b1, b2, b3, b4 });
        Set(component, "gate", gate.gameObject);
        SetList(component, "torches", torches);
        Set(component, "portal", portal);
        Set(component, "body", body);
        Set(component, "eyes", eyes);
        Set(component, "claw", bossClaw);
        Set(component, "shadowPrefab", hunterPrefab);
        Set(component, "spawned", spawned);
        SetVoice(component, "Boss");
        SetVoice(component, "Devour", "devourSpanish", "devourEnglish");
        SetList(component, "perches", perches);
        SetFloat(component, "timeBtwClaws", 2.6f);
        SetFloat(component, "timeBtwSpawns", 7f);
        SetDialogue(component, "introLines", new[]
        {
            L("???", "Gracias, Lumbra... me trajiste sus almas.", "Thank you, Lumbra... you brought me their souls."),
            L("El Devorador", "La voz que te guiaba... siempre fui yo.", "The voice that guided you... it was always me."),
            L("", "Lleva las almas a los cuatro braseros: acércate y pulsa F.", "Bring the souls to the four braziers: get close and press F."),
        });
        SetDialogue(component, "defeatLines", new[]
        {
            L("El Devorador", "No... la luz... otra vez...", "No... the light... again..."),
            L("", "El portal se abrió.", "The portal has opened."),
        });
    }

    // Salón del castillo: muro de piedra al fondo, ventanales con luz de luna, columnas, estandartes,
    // cadenas, techo y antorchas (que recargan tu luz). Devuelve las antorchas para que el jefe pueda apagarlas.
    static Torch[] MakeCastle()
    {
        var hall = new GameObject("Castle").transform;
        hall.SetParent(level);

        SpriteRenderer back = MakeRenderer("BackWall", hall, stone, new Color(0.8f, 0.74f, 0.95f), -12, lit);
        back.drawMode = SpriteDrawMode.Tiled;
        back.size = new Vector2(52f, 20f);
        back.transform.position = new Vector3(265f, 19f, 0f);

        foreach (float x in new[] { 249f, 258f, 270f, 280f })
        {
            SpriteRenderer window = MakeRenderer("Window", hall, archWindow, new Color(0.25f, 0.3f, 0.6f), -11, unlit);
            window.transform.position = new Vector3(x, 17f, 0f);
            Light2D moon = MakeLight("Moonlight", window.transform, new Color(0.45f, 0.5f, 0.95f), 6f, 0.9f);
            moon.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        }

        foreach (float x in new[] { 244f, 253.5f, 264f, 274.5f, 286f })
        {
            SpriteRenderer pillar = MakeRenderer("CastlePillar", hall, column, new Color(0.6f, 0.55f, 0.75f), -10, lit);
            pillar.transform.position = new Vector3(x, 9f, 0f);
            pillar.transform.localScale = new Vector3(1.3f, 2.1f, 1f);
        }

        foreach (float x in new[] { 246.5f, 261.5f, 267f, 282f })
        {
            SpriteRenderer flag = MakeRenderer("Banner", hall, banner, new Color(0.45f, 0.06f, 0.12f), -9, lit);
            flag.transform.position = new Vector3(x, 25f, 0f);
        }

        foreach (float x in new[] { 251f, 256f, 272f, 277f })
        {
            SpriteRenderer chain = MakeRenderer("Chain", hall, square, new Color(0.2f, 0.18f, 0.25f), -9, lit);
            chain.transform.position = new Vector3(x, 26f, 0f);
            chain.transform.localScale = new Vector3(0.08f, Random.Range(3f, 6f), 1f);
        }

        Block("Ceiling", new Vector2(265f, 31f), new Vector2(52f, 4f));

        var torches = new List<Torch>();
        foreach (Vector2 pos in new[] { new Vector2(243f, 12f), new Vector2(251f, 11.5f), new Vector2(256.5f, 12f), new Vector2(267.5f, 12f), new Vector2(276f, 11.5f), new Vector2(283f, 12f) })
        {
            torches.Add(MakeTorch(hall, pos));
        }
        // Luz suave de todo el salón, para que la arena se lea aunque se apaguen antorchas
        Light2D hallLight = MakeLight("HallLight", hall, new Color(0.55f, 0.5f, 0.9f), 26f, 0.45f);
        hallLight.transform.position = new Vector3(265f, 17f, 0f);
        return torches.ToArray();
    }

    static Torch MakeTorch(Transform parent, Vector2 pos)
    {
        SpriteRenderer stick = MakeRenderer("Torch", parent, square, new Color(0.3f, 0.2f, 0.12f), -8, lit);
        stick.transform.position = pos;
        stick.transform.localScale = new Vector3(0.12f, 0.7f, 1f);
        var flameRoot = new GameObject("Flame").transform;
        flameRoot.SetParent(parent);
        flameRoot.position = pos + new Vector2(0f, 0.45f);
        Light2D flame = MakeLight("Light", flameRoot, new Color(1f, 0.6f, 0.25f), 6.5f, 1.7f);
        ParticleSystem fire = MakeParticles("Fire", flameRoot, addParticles, new Color(1f, 0.75f, 0.3f), new Color(1f, 0.35f, 0.1f), -7);
        var main = fire.main;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        var emission = fire.emission;
        emission.rateOverTime = 30f;
        var shape = fire.shape;
        shape.radius = 0.08f;
        fire.Play();

        var torch = flameRoot.gameObject.AddComponent<Torch>();
        Set(torch, "flame", flame);
        Set(torch, "fire", fire);
        return torch;
    }

    static Brazier MakeBrazier(float x, float top)
    {
        SpriteRenderer bowl = MakeRenderer("Brazier", level, square, new Color(0.3f, 0.25f, 0.35f), 2, lit);
        bowl.transform.position = new Vector3(x, top + 0.25f, 0f);
        bowl.transform.localScale = new Vector3(0.9f, 0.5f, 1f);
        Light2D fire = MakeLight("Fire", bowl.transform, new Color(0.5f, 0.9f, 1f), 5f, 1.6f);
        fire.transform.localScale = Vector3.one;
        ParticleSystem flames = MakeParticles("Flames", bowl.transform, addParticles, new Color(0.5f, 0.95f, 1f), Color.white, 4);
        flames.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var main = flames.main;
        main.startLifetime = 0.7f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.playOnAwake = false;
        var emission = flames.emission;
        emission.rateOverTime = 25f;
        var brazier = bowl.gameObject.AddComponent<Brazier>();
        Set(brazier, "fire", fire);
        Set(brazier, "flames", flames);
        Set(brazier, "shadowPrefab", hunterPrefab);
        PromptText(x, top + 2.2f, "F", null);
        return brazier;
    }

    // Pares de ojos en la oscuridad del fondo (posiciones x, y alternadas)
    static void WatchingEyesAt(params float[] xy)
    {
        for (int i = 0; i + 1 < xy.Length; i += 2)
        {
            var pair = new GameObject("WatchingEyes");
            pair.transform.SetParent(level);
            pair.transform.position = new Vector3(xy[i], xy[i + 1], 0f);
            foreach (float x in new[] { -0.18f, 0.18f })
            {
                SpriteRenderer eye = MakeRenderer("Eye", pair.transform, circle, new Color(1f, 0.9f, 0.7f, 0f), -15, unlit);
                eye.transform.localPosition = new Vector3(x, 0f, 0f);
                eye.transform.localScale = new Vector3(0.14f, 0.09f, 1f);
            }
            pair.AddComponent<WatchingEyes>();
        }
    }

    // Mensaje tallado en la pared, apenas visible
    static void WallText(float x, float y, string key, string voice)
    {
        GameObject message = WorldText("WallText", level, "", new Color(0.7f, 0.15f, 0.25f, 0.45f));
        Localize(message, key);
        message.transform.position = new Vector3(x, y, 0f);
        message.GetComponent<MeshRenderer>().sortingOrder = -5;
        SetVoice(message.AddComponent<VoiceLine>(), voice, "spanish", "english");
    }

    // Farol que se apaga si miras atrás
    static void LookBackLantern(float x, float ground)
    {
        var lantern = new GameObject("LookBackLantern");
        lantern.transform.SetParent(level);
        lantern.transform.position = new Vector3(x, ground, 0f);
        SpriteRenderer post = MakeRenderer("Post", lantern.transform, square, new Color(0.12f, 0.1f, 0.18f), 1, unlit);
        post.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        post.transform.localScale = new Vector3(0.12f, 1.6f, 1f);
        SpriteRenderer on = MakeRenderer("On", lantern.transform, circle, new Color(1f, 0.85f, 0.5f), 2, unlit);
        on.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        on.transform.localScale = Vector3.one * 0.3f;
        MakeLight("Light", on.transform, new Color(1f, 0.75f, 0.4f), 4f, 1f).transform.localScale = Vector3.one;
        SpriteRenderer off = MakeRenderer("Off", lantern.transform, circle, new Color(0.15f, 0.12f, 0.2f), 2, unlit);
        off.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        off.transform.localScale = Vector3.one * 0.3f;
        off.gameObject.SetActive(false);
        var look = lantern.AddComponent<LookBackChange>();
        Set(look, "before", on.gameObject);
        Set(look, "after", off.gameObject);
        SetVoice(look, "Looked");
        SetFloat(look, "range", 6f);
    }

    static GameObject MakePlayer(ParticleSystem deathPrefab)
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(2f, 0.7f, 0f);
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.45f, 1.2f);

        // Sin fricción para no quedarse pegado a las paredes al saltar
        var noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f };
        AssetDatabase.DeleteAsset("Assets/04_Materials/NoFriction.physicsMaterial2D");
        AssetDatabase.CreateAsset(noFriction, "Assets/04_Materials/NoFriction.physicsMaterial2D");
        col.sharedMaterial = noFriction;

        // Lumbra armado con las partes del dibujo: el cuerpo se voltea y "Visual" se estira y aplasta
        var body = new GameObject("Body");
        body.transform.SetParent(player.transform, false);
        MakeRenderer("Halo", body.transform, glow, new Color(1f, 0.8f, 0.45f, 0.35f), 5, unlit).transform.localScale = Vector3.one * 2.4f;
        var visual = new GameObject("Visual").transform;
        visual.SetParent(body.transform, false);
        visual.localPosition = new Vector3(0f, -0.6f, 0f);
        Animator rig = MakeRig(visual);
        ShieldFx shield = MakeShield(player.transform);

        var neck = new GameObject("ScarfAnchor").transform;
        neck.SetParent(body.transform, false);
        neck.localPosition = new Vector3(-0.04f, 0.3f, 0f);
        MakeScarf(player.transform, neck);

        Light2D aura = MakeLight("Aura", player.transform, new Color(1f, 0.82f, 0.55f), 5.5f, 1.4f);
        aura.pointLightInnerRadius = 0.6f;

        ParticleSystem flash = MakeParticles("Flash", player.transform, addParticles, new Color(1f, 0.95f, 0.75f), new Color(0.5f, 0.9f, 1f), 12);
        var flashMain = flash.main;
        flashMain.loop = false;
        flashMain.playOnAwake = false;
        flashMain.startLifetime = 0.4f;
        flashMain.startSpeed = 13f;
        flashMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        var flashEmission = flash.emission;
        flashEmission.rateOverTime = 0f;
        flashEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

        ParticleSystem dust = MakeParticles("Dust", player.transform, addParticles, new Color(0.6f, 0.5f, 0.9f, 0.6f), new Color(0.9f, 0.8f, 0.6f, 0.4f), 9);
        dust.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        var dustMain = dust.main;
        dustMain.loop = false;
        dustMain.playOnAwake = false;
        dustMain.startLifetime = 0.4f;
        dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
        dustMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        var dustEmission = dust.emission;
        dustEmission.rateOverTime = 0f;
        dustEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
        var dustShape = dust.shape;
        dustShape.shapeType = ParticleSystemShapeType.Cone;
        dustShape.angle = 70f;
        dustShape.rotation = new Vector3(-90f, 0f, 0f);

        var controller = player.AddComponent<PlayerController>();
        Set(controller, "body", body.transform);
        SetMask(controller, "groundMask");
        var animator = player.AddComponent<PlayerAnimator>();
        Set(animator, "animator", rig);
        Set(animator, "visual", visual);
        Set(animator, "dust", dust);
        var lightFlash = player.AddComponent<LightFlash>();
        Set(lightFlash, "aura", aura);
        Set(lightFlash, "flashParticles", flash);
        var health = player.AddComponent<PlayerHealth>();
        Set(health, "body", body);
        Set(health, "visual", visual);
        Set(health, "deathParticles", deathPrefab);
        Set(health, "shield", shield);
        return player;
    }

    // Escudo: burbuja de luz con runas que giran, chispas al romperse y puntitos que muestran la recarga
    static ShieldFx MakeShield(Transform player)
    {
        var root = new GameObject("Shield");
        root.transform.SetParent(player, false);
        SpriteRenderer bubble = MakeRenderer("Bubble", root.transform, glow, new Color(0.55f, 0.95f, 1f, 0.3f), 13, unlit);
        bubble.transform.localScale = Vector3.one * 2.1f;
        SpriteRenderer runes = MakeRenderer("Runes", root.transform, MakeSprite("Runes", 128, 128, 80, Runes), new Color(0.7f, 1f, 1f, 0.85f), 14, unlit);
        var dots = new Object[8];
        for (int i = 0; i < dots.Length; i++)
        {
            float a = Mathf.PI / 2f - i * Mathf.PI * 2f / dots.Length;
            SpriteRenderer dot = MakeRenderer("Dot", root.transform, circle, new Color(0.6f, 0.95f, 1f, 0.8f), 14, unlit);
            dot.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.85f;
            dot.transform.localScale = Vector3.one * 0.09f;
            dots[i] = dot;
        }
        ParticleSystem shatter = MakeParticles("Shatter", root.transform, addParticles, new Color(0.6f, 1f, 1f), Color.white, 15);
        var main = shatter.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        var emission = shatter.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
        var shape = shatter.shape;
        shape.radius = 0.8f;

        var fx = root.AddComponent<ShieldFx>();
        Set(fx, "bubble", bubble);
        Set(fx, "runes", runes);
        SetList(fx, "rechargeDots", dots);
        Set(fx, "shatter", shatter);
        return fx;
    }

    // Bufanda de luz hecha con una línea que se afina hacia la punta
    static void MakeScarf(Transform parent, Transform anchor)
    {
        var scarf = new GameObject("Scarf");
        scarf.transform.SetParent(parent, false);
        var line = scarf.AddComponent<LineRenderer>();
        const string path = "Assets/04_Materials/Scarf.mat";
        AssetDatabase.DeleteAsset(path);
        var mat = new Material(Shader.Find("Sprites/Default"));
        AssetDatabase.CreateAsset(mat, path);
        line.sharedMaterial = mat;
        line.widthCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 0.01f);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.85f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.3f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        line.colorGradient = gradient;
        line.numCapVertices = 2;
        line.sortingOrder = 9;
        Set(scarf.AddComponent<ScarfTrail>(), "anchor", anchor);
    }

    static Soul MakeSoul(string name, Vector2 pos, bool memory, SoulPower power, params DialogueLine[] lines)
    {
        var go = new GameObject(name);
        go.transform.SetParent(level);
        go.transform.position = pos;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        // Lo que se ve normalmente: una lucecita con carita (dorada si es el alma del recuerdo)
        Color color = memory ? new Color(1f, 0.85f, 0.4f) : new Color(0.55f, 0.95f, 1f);
        var light = new GameObject("Glow");
        light.transform.SetParent(go.transform, false);
        MakeRenderer("Halo", light.transform, glow, new Color(color.r, color.g, color.b, 0.7f), 7, unlit).transform.localScale = Vector3.one * 1.3f;
        MakeRenderer("Core", light.transform, circle, Color.white, 8, unlit).transform.localScale = Vector3.one * 0.32f;
        foreach (float x in new[] { -0.045f, 0.045f })
        {
            SpriteRenderer eye = MakeRenderer("Eye", light.transform, circle, new Color(0.15f, 0.2f, 0.35f), 9, unlit);
            eye.transform.localPosition = new Vector3(x, 0.02f, 0f);
            eye.transform.localScale = Vector3.one * 0.045f;
        }
        MakeLight("Light", light.transform, color, 2.8f, 1f);
        ParticleSystem sparkle = MakeParticles("Sparkle", light.transform, addParticles, color, Color.white, 6);
        var sparkleMain = sparkle.main;
        sparkleMain.startLifetime = 0.8f;
        sparkleMain.startSpeed = 0.2f;
        sparkleMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        var sparkleEmission = sparkle.emission;
        sparkleEmission.rateOverTime = 8f;

        // Su verdadera forma (solo en las corruptas se llega a ver)
        var trueForm = new GameObject("TrueForm");
        trueForm.transform.SetParent(go.transform, false);
        MakeRenderer("Body", trueForm.transform, blob, new Color(0.05f, 0.02f, 0.08f), 7, unlit).transform.localScale = Vector3.one * 0.7f;
        foreach (float x in new[] { -0.1f, 0.1f })
        {
            SpriteRenderer eye = MakeRenderer("Eye", trueForm.transform, circle, new Color(1f, 0.15f, 0.2f), 8, unlit);
            eye.transform.localPosition = new Vector3(x, 0.08f, 0f);
            eye.transform.localScale = Vector3.one * 0.1f;
        }
        trueForm.SetActive(false);

        // La pista: su sombra. En las corruptas cae hacia arriba.
        SpriteRenderer shadow = MakeRenderer("CastShadow", go.transform, circle, new Color(0f, 0f, 0f, 0.6f), 6, unlit);
        shadow.transform.localScale = new Vector3(0.5f, 0.12f, 1f);
        shadow.transform.localPosition = new Vector3(0f, -0.7f, 0f);

        AudioSource moan = go.AddComponent<AudioSource>();
        moan.loop = true;
        moan.volume = 0f;
        moan.playOnAwake = false;

        var soul = go.AddComponent<Soul>();
        Set(soul, "glow", light);
        Set(soul, "trueForm", trueForm);
        Set(soul, "castShadow", shadow.transform);
        Set(soul, "moan", moan);
        SetVoice(soul, "Plea", "pleaSpanish", "pleaEnglish");
        SetDialogue(soul, "freedLines", lines);
        SetBool(soul, "isMemory", memory);
        SetEnum(soul, "power", (int)power);
        return soul;
    }

    static Soul TrappedSoul(float x, float y, SoulPower power, params DialogueLine[] lines)
    {
        Soul soul = MakeSoul("Soul", new Vector2(x, y), false, power, lines);
        SpriteRenderer roots = MakeRenderer("Roots", level, blob, new Color(0.08f, 0.02f, 0.12f, 0.8f), 9, unlit);
        roots.transform.position = new Vector3(x, y, 0f);
        roots.transform.localScale = Vector3.one * 1.6f;
        roots.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
        var rootsComponent = roots.gameObject.AddComponent<Roots>();
        Set(rootsComponent, "soul", soul);
        Set(rootsComponent, "burnParticles", rootBurn);
        PromptText(x, y + 1.4f, "E", roots.gameObject);
        return soul;
    }

    // Parece un alma atrapada, pero no tiene sombra: es una sombra disfrazada que ataca al liberarla
    static void FakeTrappedSoul(float x, float y)
    {
        var fake = new GameObject("FakeSoul");
        fake.transform.SetParent(level);
        fake.transform.position = new Vector3(x, y, 0f);
        MakeRenderer("Halo", fake.transform, glow, new Color(0.55f, 0.95f, 1f, 0.7f), 7, unlit).transform.localScale = Vector3.one * 1.3f;
        MakeRenderer("Core", fake.transform, circle, Color.white, 8, unlit).transform.localScale = Vector3.one * 0.32f;
        MakeLight("Light", fake.transform, new Color(0.55f, 0.95f, 1f), 2.8f, 1f);

        SpriteRenderer roots = MakeRenderer("Roots", level, blob, new Color(0.08f, 0.02f, 0.12f, 0.8f), 9, unlit);
        roots.transform.position = new Vector3(x, y, 0f);
        roots.transform.localScale = Vector3.one * 1.6f;
        roots.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
        var component = roots.gameObject.AddComponent<Roots>();
        Set(component, "fakeSoul", fake);
        Set(component, "ambushPrefab", hunterPrefab);
        Set(component, "burnParticles", rootBurn);
        PromptText(x, y + 1.4f, "E", roots.gameObject);
    }

    static GameObject Shadow(ShadowType type, float x, float y, bool fake, float detectRange)
    {
        var shadow = new GameObject(fake ? "Shadow_Fake" : "Shadow_" + type);
        shadow.transform.SetParent(level);
        shadow.transform.position = new Vector3(x, y, 0f);
        shadow.transform.localScale = Vector3.one * (type == ShadowType.Ambusher ? 1.4f : 1f);
        shadow.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var col = shadow.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        SpriteRenderer body = MakeRenderer("Body", shadow.transform, blob, Color.black, 5, unlit);
        var eyes = new GameObject("Eyes").transform;
        eyes.SetParent(shadow.transform, false);
        foreach (float ex in new[] { -0.15f, 0.15f })
        {
            SpriteRenderer eye = MakeRenderer("Eye", eyes, circle, Color.white, 6, unlit);
            eye.transform.localPosition = new Vector3(ex, 0.12f, 0f);
            eye.transform.localScale = Vector3.one * 0.14f;
        }

        var enemy = shadow.AddComponent<ShadowEnemy>();
        SetEnum(enemy, "type", (int)type);
        SetBool(enemy, "isFake", fake);
        Set(enemy, "body", body.transform);
        Set(enemy, "eyes", eyes);
        Set(enemy, "deathParticles", shadowDeath);
        SetFloat(enemy, "detectRange", detectRange);
        return shadow;
    }

    static void MakeFork(float x, Vector2 upperEntrance, Vector2 lowerEntrance, GameObject upperTrap, GameObject lowerTrap)
    {
        var fork = new GameObject("PathFork");
        fork.transform.SetParent(level);
        fork.transform.position = new Vector3(x, 10f, 0f);
        var col = fork.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 20f);
        var upper = new GameObject("Entrance_Upper").transform;
        upper.SetParent(fork.transform);
        upper.position = upperEntrance;
        var lower = new GameObject("Entrance_Lower").transform;
        lower.SetParent(fork.transform);
        lower.position = lowerEntrance;

        var component = fork.AddComponent<PathFork>();
        SetList(component, "entrances", new Object[] { upper, lower });
        SetList(component, "traps", new Object[] { upperTrap, lowerTrap });
    }

    static GameObject MakePortal(Vector2 pos)
    {
        SpriteRenderer portal = MakeRenderer("Portal", level, ring, new Color(0.5f, 0.95f, 1f), 3, unlit);
        portal.transform.position = pos;
        portal.transform.localScale = new Vector3(1.4f, 2f, 1f);
        SpriteRenderer swirl = MakeRenderer("Swirl", portal.transform, ring, new Color(1f, 0.5f, 0.95f, 0.7f), 4, unlit);
        swirl.transform.localScale = Vector3.one * 0.7f;
        MakeLight("Light", portal.transform, new Color(0.5f, 0.9f, 1f), 7f, 1.6f);
        portal.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
        Set(portal.gameObject.AddComponent<Portal>(), "swirl", swirl.transform);
        return portal.gameObject;
    }

    static void MakeChaser(Sprite darkEdge, Material darkParticles)
    {
        var wall = new GameObject("DarknessChaser");
        wall.transform.position = new Vector3(-100f, 6f, 0f); // fuera de vista hasta que despierta
        MakeRenderer("Edge", wall.transform, darkEdge, Color.black, 40, unlit).transform.localScale = new Vector3(1.5f, 3f, 1f);
        SpriteRenderer rim = MakeRenderer("Rim", wall.transform, glow, new Color(0.65f, 0.15f, 1f, 0.55f), 39, unlit);
        rim.transform.localScale = new Vector3(4f, 34f, 1f);
        Light2D menace = MakeLight("Menace", wall.transform, new Color(0.6f, 0.15f, 0.9f), 7f, 1.2f);
        menace.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        for (int i = 0; i < 7; i++)
        {
            foreach (float ex in new[] { -0.18f, 0.18f })
            {
                SpriteRenderer eye = MakeRenderer("Eye", wall.transform, circle, new Color(1f, 0.15f, 0.25f), 42, unlit);
                eye.transform.localPosition = new Vector3(-1.2f - (i % 3) * 1.4f + ex, -9f + i * 3f, 0f);
                eye.transform.localScale = new Vector3(0.16f, 0.1f, 1f);
            }
        }
        ParticleSystem tendrils = MakeParticles("Tendrils", wall.transform, darkParticles, Color.black, new Color(0.25f, 0.05f, 0.35f), 41);
        tendrils.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
        var main = tendrils.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = tendrils.emission;
        emission.rateOverTime = 40f;
        var shape = tendrils.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2f, 16f, 1f);
        var velocity = tendrils.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var chaser = wall.AddComponent<DarknessChaser>();
        SetFloat(chaser, "speed", 3f);
        SetFloat(chaser, "startX", 190f);
        SetFloat(chaser, "stopX", 236f);
    }

    // ----- Piezas del nivel -----

    static void Ground(float x0, float x1, float top)
    {
        Block("Ground", new Vector2((x0 + x1) / 2f, (top - 12f) / 2f), new Vector2(x1 - x0, top + 12f));
    }

    static GameObject Pillar(float x0, float x1, float y0, float y1)
    {
        return Block("Pillar", new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f), new Vector2(x1 - x0, y1 - y0), true);
    }

    static void Platform(float x, float top, float width)
    {
        Block("Platform", new Vector2(x, top - 0.3f), new Vector2(width, 0.6f), true);
    }

    // Piedra del santuario (se ilumina con tu luz), musgo arriba y un filo de neón para leer bien los bordes
    // bright: plataformas y pilares sueltos se ven aunque estén lejos de tu luz
    static GameObject Block(string name, Vector2 center, Vector2 size, bool bright = false)
    {
        SpriteRenderer block = bright
            ? MakeRenderer(name, level, stone, new Color(0.55f, 0.52f, 0.7f), 0, unlit)
            : MakeRenderer(name, level, stone, new Color(0.75f, 0.72f, 0.9f), 0, lit);
        block.drawMode = SpriteDrawMode.Tiled;
        block.size = size;
        block.transform.position = center;
        block.gameObject.layer = GroundLayer;
        block.gameObject.AddComponent<BoxCollider2D>().size = size;

        SpriteRenderer top = MakeRenderer("Moss", block.transform, moss, new Color(0.45f, 0.85f, 0.75f), 1, lit);
        top.drawMode = SpriteDrawMode.Tiled;
        top.size = new Vector2(size.x + 0.1f, 0.5f);
        top.transform.localPosition = new Vector3(0f, size.y / 2f - 0.12f, 0f);

        SpriteRenderer rim = MakeRenderer("Rim", block.transform, square, new Color(0.55f, 0.4f, 1f, 0.35f), 2, unlit);
        rim.transform.localPosition = new Vector3(0f, size.y / 2f, 0f);
        rim.transform.localScale = new Vector3(size.x, 0.04f, 1f);
        return block.gameObject;
    }

    // Columnas en ruinas detrás del camino (solo decoración)
    static void Columns(float ground, params float[] xs)
    {
        foreach (float x in xs)
        {
            bool broken = Random.value < 0.5f;
            SpriteRenderer c = MakeRenderer("Column", level, broken ? brokenColumn : column, new Color(0.55f, 0.5f, 0.7f), -6, lit);
            c.transform.position = new Vector3(x, ground - 0.2f, 0f);
            c.transform.localScale = Vector3.one * Random.Range(0.8f, 1.1f);
        }
    }

    static void Hidden(float x, float top, float width)
    {
        SpriteRenderer sprite = MakeRenderer("HiddenPlatform", level, square, new Color(0.5f, 0.95f, 1f), 2, unlit);
        sprite.transform.position = new Vector3(x, top - 0.2f, 0f);
        sprite.transform.localScale = new Vector3(width, 0.4f, 1f);
        var solid = sprite.gameObject.AddComponent<BoxCollider2D>();
        sprite.gameObject.AddComponent<BoxCollider2D>().isTrigger = true; // para que el destello la encuentre aunque esté apagada
        var hidden = sprite.gameObject.AddComponent<FlashPlatform>();
        Set(hidden, "sprite", sprite);
        Set(hidden, "solid", solid);
    }

    static void ShadowPlat(float x, float top, float width)
    {
        SpriteRenderer sprite = MakeRenderer("ShadowPlatform", level, blob, new Color(0.12f, 0.05f, 0.2f), 2, unlit);
        sprite.transform.position = new Vector3(x, top - 0.25f, 0f);
        sprite.transform.localScale = new Vector3(width, 0.6f, 1f);
        sprite.gameObject.layer = GroundLayer;
        var solid = sprite.gameObject.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(0.9f, 0.7f);
        var platform = sprite.gameObject.AddComponent<FlashPlatform>();
        SetBool(platform, "appearsWithLight", false);
        SetFloat(platform, "duration", 2.5f);
        Set(platform, "sprite", sprite);
        Set(platform, "solid", solid);
    }

    static void Crumble(float x, float top)
    {
        var platform = new GameObject("CrumblePlatform");
        platform.transform.SetParent(level);
        platform.transform.position = new Vector3(x, top - 0.2f, 0f);
        platform.layer = GroundLayer;
        platform.AddComponent<BoxCollider2D>().size = new Vector2(2.2f, 0.4f);
        // Piedra agrietada que se ve aun en la oscuridad: borde naranja de aviso, grietas y una luz tenue propia
        SpriteRenderer sprite = MakeRenderer("Sprite", platform.transform, cracked, new Color(0.78f, 0.62f, 0.6f), 1, unlit);
        sprite.drawMode = SpriteDrawMode.Tiled;
        sprite.size = new Vector2(2.2f, 0.4f);
        SpriteRenderer edge = MakeRenderer("Edge", sprite.transform, square, new Color(1f, 0.55f, 0.2f, 0.9f), 2, unlit);
        edge.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        edge.transform.localScale = new Vector3(2.2f, 0.05f, 1f);
        SpriteRenderer under = MakeRenderer("Glow", sprite.transform, glow, new Color(1f, 0.45f, 0.15f, 0.35f), 0, unlit);
        under.transform.localScale = new Vector3(3.2f, 0.9f, 1f);
        MakeLight("Light", sprite.transform, new Color(1f, 0.55f, 0.25f), 2.2f, 0.6f);
        Set(platform.AddComponent<CrumblePlatform>(), "sprite", sprite);
    }

    static void Spikes(Transform parent, float x, float bottom, float width, bool popUp)
    {
        SpriteRenderer spikes = MakeRenderer("Thorns", parent, thorns, new Color(0.5f, 0.05f, 0.25f), 2, unlit);
        spikes.transform.position = new Vector3(x, bottom, 0f);
        spikes.transform.localScale = new Vector3(width, 1f, 1f);
        spikes.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        SetBool(spikes.gameObject.AddComponent<Hazard>(), "popUp", popUp);
    }

    static void Checkpoint(float x, float ground)
    {
        var point = new GameObject("Checkpoint");
        point.transform.SetParent(level);
        point.transform.position = new Vector3(x, ground, 0f);
        var col = point.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 6f);
        SpriteRenderer post = MakeRenderer("Post", point.transform, square, new Color(0.12f, 0.1f, 0.18f), 1, unlit);
        post.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        post.transform.localScale = new Vector3(0.12f, 1.6f, 1f);
        SpriteRenderer head = MakeRenderer("Lantern", point.transform, circle, new Color(1f, 0.85f, 0.5f), 2, unlit);
        head.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        head.transform.localScale = Vector3.one * 0.3f;
        Light2D lamp = MakeLight("Light", head.transform, new Color(1f, 0.75f, 0.4f), 4f, 0.2f);
        Set(point.AddComponent<Checkpoint>(), "lamp", lamp);
    }

    static StoryTrigger Story(float x, params DialogueLine[] lines)
    {
        var trigger = new GameObject("Story");
        trigger.transform.SetParent(level);
        trigger.transform.position = new Vector3(x, 10f, 0f);
        var col = trigger.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 30f);
        var story = trigger.AddComponent<StoryTrigger>();
        SetDialogue(story, "lines", lines);
        return story;
    }

    // Voz susurrada (en Assets/05_Audio/Voices) en español e inglés
    static void SetVoice(Component target, string name, string esField = "voiceSpanish", string enField = "voiceEnglish")
    {
        Set(target, esField, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05_Audio/Voices/Voice" + name + "_Es.wav"));
        Set(target, enField, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05_Audio/Voices/Voice" + name + "_En.wav"));
    }

    // Tecla o pista flotante en el mundo; aparece al acercarse
    static void PromptText(float x, float y, string text, GameObject target, string key = null)
    {
        GameObject prompt = WorldText("Prompt", level, text, new Color(1f, 0.9f, 0.6f, 0f));
        if (key != null) Localize(prompt, key);
        prompt.transform.position = new Vector3(x, y, 0f);
        prompt.GetComponent<TextMesh>().characterSize = 0.035f;
        prompt.GetComponent<MeshRenderer>().sortingOrder = 30;
        var component = prompt.AddComponent<Prompt>();
        if (target != null)
        {
            Set(component, "target", target);
        }
    }

    static void LookBackSign(float x, float y, string key, string changedKey)
    {
        var sign = new GameObject("LookBack");
        sign.transform.SetParent(level);
        sign.transform.position = new Vector3(x, y, 0f);
        GameObject before = WorldText("Before", sign.transform, "", new Color(0.75f, 0.7f, 1f, 0.7f));
        GameObject after = WorldText("After", sign.transform, "", new Color(1f, 0.2f, 0.3f));
        Localize(before, key);
        Localize(after, changedKey);
        after.SetActive(false);
        var look = sign.AddComponent<LookBackChange>();
        Set(look, "before", before);
        Set(look, "after", after);
        SetVoice(look, "Lie");
        SetVoice(sign.AddComponent<VoiceLine>(), "LieSign", "spanish", "english");
    }

    // Agrega la traducción automática a un texto (de interfaz o del mundo)
    static void Localize(GameObject go, string key)
    {
        var localized = go.AddComponent<LocalizedText>();
        var so = new SerializedObject(localized);
        so.FindProperty("key").stringValue = key;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject WorldText(string name, Transform parent, string text, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tm = go.AddComponent<TextMesh>();
        tm.font = titleFont;
        tm.text = text;
        tm.fontSize = 128;
        tm.characterSize = 0.045f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = tm.font.material;
        mr.sortingOrder = 3;
        return go;
    }

    // ================= Personaje =================
    // Las partes salen del dibujo Player.png (Tools/RecortarPartes.py las recorta en Player/Partes con sus uniones).
    // Aquí se arman con huesos (cada parte gira desde su unión) y se crean los clips y el Animator Controller.

    const string PartsPath = SpritesPath + "Player/Partes/";
    const string AnimationsPath = "Assets/07_Animations/";
    const float PartsPpu = 600f; // el dibujo es grande: 600 píxeles = 1 unidad (Lumbra mide unas 1.5)

    [Serializable] class PartData { public string name; public int width; public int height; public float pivotX; public float pivotY; }
    [Serializable] class Joints { public float legLength; public float neckOffsetX; public float hemToNeck; public float handX; public float handY; }
    [Serializable] class PartsFile { public PartData[] parts; public Joints Normal; public Joints Tired; }

    // Postura para una animación (grados; positivo = antihorario, o sea hacia adelante para piernas y brazo del farol)
    class Pose
    {
        public float bob, crouch, lean, legF, legB, arm, larm, swing, head;
    }

    static readonly string[] PartPaths = { "LegBack", "LegFront", "Torso/ArmBack", "Torso/Cloak", "Torso/Head", "Torso/ArmFront", "Torso/ArmFront/Lantern" };
    static readonly string[] PartNames = { "Leg", "Leg", "Arm", "Cloak", "Head", "LanternArm", "Lantern" };
    static PartsFile partsFile;

    // Arma a Lumbra dentro de "parent" y le pone el Animator con todas sus animaciones
    static Animator MakeRig(Transform parent)
    {
        partsFile = JsonUtility.FromJson<PartsFile>(File.ReadAllText(PartsPath + "Partes.json"));
        partSprites.Clear();
        var root = new GameObject("Lumbra");
        root.transform.SetParent(parent, false);
        var torso = new GameObject("Torso").transform;
        torso.SetParent(root.transform, false);

        int[] orders = { 8, 9, 7, 10, 11, 12, 13 };
        for (int i = 0; i < PartPaths.Length; i++)
        {
            string[] path = PartPaths[i].Split('/');
            Transform owner = path.Length == 1 ? root.transform : path.Length == 2 ? torso : torso.Find("ArmFront");
            bool back = PartPaths[i].Contains("Back");
            MakeRenderer(path[path.Length - 1], owner, PartSprite("Normal_" + PartNames[i]), back ? new Color(0.72f, 0.7f, 0.78f) : Color.white, orders[i], unlit);
        }

        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = MakeController();
        return animator;
    }

    static readonly Dictionary<string, Sprite> partSprites = new Dictionary<string, Sprite>();

    // Importa una parte con su punto de giro en la unión (una sola vez por construcción)
    static Sprite PartSprite(string name)
    {
        if (!partSprites.TryGetValue(name, out Sprite sprite) || sprite == null)
        {
            PartData part = Array.Find(partsFile.parts, p => p.name == name);
            sprite = ImportSprite(PartsPath + name + ".png", PartsPpu, new Vector2(part.pivotX / part.width, 1f - part.pivotY / part.height), false);
            partSprites[name] = sprite;
        }
        return sprite;
    }

    static AnimatorController MakeController()
    {
        Directory.CreateDirectory(AnimationsPath);
        string path = AnimationsPath + "Lumbra.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        var idle = new Pose[4];
        var tiredIdle = new Pose[4];
        for (int i = 0; i < 4; i++)
        {
            float b = Mathf.Sin(i / 4f * Mathf.PI * 2f);
            idle[i] = new Pose { bob = 4f * b, arm = 3f * b, larm = 3f * b, swing = 4f * b };
            tiredIdle[i] = new Pose { bob = 6f * b, crouch = 12f, lean = 4f, arm = -4f, larm = -8f, swing = 2f * b, head = -6f + 3f * b };
        }
        var run = new Pose[8];
        var tiredRun = new Pose[8];
        for (int i = 0; i < 8; i++)
        {
            float t = i / 8f * Mathf.PI * 2f;
            float sn = Mathf.Sin(t);
            float bob = 14f * Mathf.Abs(Mathf.Cos(t));
            run[i] = new Pose { bob = bob, lean = 6f, legF = 30f * sn, legB = -30f * sn, arm = 25f * sn, larm = -15f * sn, swing = 12f * sn };
            tiredRun[i] = new Pose { bob = bob * 0.5f, crouch = 14f, lean = 10f, legF = 16f * sn, legB = -16f * sn, arm = 8f * sn, larm = -10f, swing = 6f * sn, head = -8f };
        }

        AddState(machine, MakeClip("Idle", idle, 1.2f, false), true);
        AddState(machine, MakeClip("Run", run, 0.6f, false), false);
        AddState(machine, MakeClip("Jump", new[] { new Pose { bob = 20f, lean = 4f, legF = 35f, legB = -20f, arm = -50f, larm = 35f, swing = 15f } }, 0f, false), false);
        AddState(machine, MakeClip("Fall", new[] { new Pose { bob = 10f, lean = -3f, legF = 12f, legB = -10f, arm = -100f, larm = 60f, swing = -10f } }, 0f, false), false);
        AddState(machine, MakeClip("Wall", new[] { new Pose { bob = 8f, lean = -6f, legF = 40f, legB = 15f, arm = 120f, larm = 85f } }, 0f, false), false);
        AddState(machine, MakeClip("TiredIdle", tiredIdle, 1.6f, true), false);
        AddState(machine, MakeClip("TiredRun", tiredRun, 0.8f, true), false);
        return controller;
    }

    static void AddState(AnimatorStateMachine machine, AnimationClip clip, bool isDefault)
    {
        AnimatorState state = machine.AddState(clip.name);
        state.motion = clip;
        if (isDefault)
        {
            machine.defaultState = state;
        }
    }

    // Convierte una lista de posturas en un clip que se repite (con una sola postura queda quieto en ella)
    static AnimationClip MakeClip(string name, Pose[] poses, float length, bool tired)
    {
        Joints j = tired ? partsFile.Tired : partsFile.Normal;
        const float U = 1f / PartsPpu;
        var clip = new AnimationClip { name = name, frameRate = 30f };
        int keys = poses.Length == 1 ? 1 : poses.Length + 1;
        var curves = new Dictionary<string, AnimationCurve>();
        Action<string, float, float> key = (binding, time, value) =>
        {
            if (!curves.ContainsKey(binding)) curves[binding] = new AnimationCurve();
            curves[binding].AddKey(time, value);
        };

        float hip = j.legLength * U;
        Vector2 neck = new Vector2(j.neckOffsetX, j.hemToNeck) * U;
        for (int k = 0; k < keys; k++)
        {
            Pose p = poses[k % poses.Length];
            float time = poses.Length == 1 ? 0f : length * k / poses.Length;

            // Posiciones de cada hueso (dependen de la versión del dibujo)
            key("|m_LocalPosition.y", time, (p.bob - p.crouch) * U);
            key("LegBack|m_LocalPosition.x", time, -0.07f);
            key("LegBack|m_LocalPosition.y", time, hip);
            key("LegFront|m_LocalPosition.x", time, 0.05f);
            key("LegFront|m_LocalPosition.y", time, hip);
            key("Torso|m_LocalPosition.y", time, hip - 60f * U); // el borde de la capa tapa la parte de arriba de las piernas
            key("Torso/Cloak|m_LocalPosition.x", time, neck.x);
            key("Torso/Cloak|m_LocalPosition.y", time, neck.y);
            key("Torso/Head|m_LocalPosition.x", time, neck.x + 15f * U);
            key("Torso/Head|m_LocalPosition.y", time, neck.y - 25f * U);
            key("Torso/ArmBack|m_LocalPosition.x", time, neck.x - 75f * U);
            key("Torso/ArmBack|m_LocalPosition.y", time, neck.y - 110f * U);
            key("Torso/ArmFront|m_LocalPosition.x", time, neck.x + 70f * U);
            key("Torso/ArmFront|m_LocalPosition.y", time, neck.y - 115f * U);
            key("Torso/ArmFront/Lantern|m_LocalPosition.x", time, j.handX * U);
            key("Torso/ArmFront/Lantern|m_LocalPosition.y", time, -j.handY * U);

            // Giros (el farol se compensa para colgar derecho)
            float armFront = -105f + p.larm;
            key("Torso|localEulerAnglesRaw.z", time, -p.lean);
            key("Torso/Head|localEulerAnglesRaw.z", time, p.head);
            key("LegFront|localEulerAnglesRaw.z", time, p.legF);
            key("LegBack|localEulerAnglesRaw.z", time, p.legB);
            key("Torso/ArmBack|localEulerAnglesRaw.z", time, p.arm);
            key("Torso/ArmFront|localEulerAnglesRaw.z", time, armFront);
            key("Torso/ArmFront/Lantern|localEulerAnglesRaw.z", time, p.swing - armFront + p.lean);
        }

        foreach (var pair in curves)
        {
            string[] parts = pair.Key.Split('|');
            for (int k = 0; k < pair.Value.length; k++)
            {
                pair.Value.SmoothTangents(k, 0f);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(parts[0], typeof(Transform), parts[1]), pair.Value);
        }

        // Cada clip pone los dibujos de su versión (normal o encorvada)
        for (int i = 0; i < PartPaths.Length; i++)
        {
            var sprite = new ObjectReferenceKeyframe { time = 0f, value = PartSprite((tired ? "Tired_" : "Normal_") + PartNames[i]) };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(PartPaths[i], typeof(SpriteRenderer), "m_Sprite"), new[] { sprite });
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = poses.Length > 1;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = AnimationsPath + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ================= Sonido =================

    static void MakeAudio()
    {
        var go = new GameObject("Audio");
        go.transform.SetParent(systems);
        AudioSource effects = go.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.volume = 0.8f;
        AudioSource ambience = MakeLoop(go, Real("Ambience"), 0.4f);
        AudioSource heartbeat = MakeLoop(go, Oga("HeartbeatSlow"), 0f);
        AudioSource panting = MakeLoop(go, Oga("Panting"), 0f);

        var audio = go.AddComponent<AudioManager>();
        // Efectos reales (Kenney, licencia CC0, en 05_Audio/Kenney)
        foreach (string clip in new[] { "jump", "flash", "empty", "death", "hurt", "splat", "soul", "whisper", "checkpoint", "portal", "fire", "gate", "click" })
        {
            Set(audio, clip, Real(char.ToUpper(clip[0]) + clip.Substring(1)));
        }
        var steps = new Object[5];
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i] = Real("Step" + i);
        }
        SetList(audio, "steps", steps);

        // Sonidos de OpenGameArt (CC0): latidos, jefe, gritos y ambiente de terror
        Set(audio, "roar", Real("Rumble"));
        Set(audio, "snarl", Real("Slash"));
        Set(audio, "shieldBreak", Real("ShieldBreak"));
        AudioSource voice = go.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        Set(audio, "voice", voice);
        Set(audio, "scream", Oga("GhostScream"));
        Set(audio, "heartbeatSlow", Oga("HeartbeatSlow"));
        Set(audio, "heartbeatFast", Oga("HeartbeatFast"));
        Set(audio, "bossAmbience", Oga("AmbientHorror"));
        AudioSource music = MakeLoop(go, Oga("MusicExplore"), 0.3f);
        Set(audio, "music", music);
        Set(audio, "musicExplore", Oga("MusicExplore"));
        Set(audio, "musicBoss", Oga("MusicBoss"));
        Set(audio, "effects", effects);
        Set(audio, "ambience", ambience);
        Set(audio, "heartbeat", heartbeat);
        Set(audio, "panting", panting);
    }

    static AudioClip Oga(string name)
    {
        string[] found = AssetDatabase.FindAssets(name + " t:AudioClip", new[] { "Assets/05_Audio/OpenGameArt" });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(found[0]));
    }

    static AudioClip Real(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05_Audio/Kenney/" + name + ".ogg");
    }

    static AudioSource MakeLoop(GameObject go, AudioClip clip, float volume)
    {
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.volume = volume;
        return source;
    }

    // ================= HUD =================

    static void MakeHUD()
    {
        var canvas = new GameObject("HUD").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 top = new Vector2(0.5f, 1f);
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 bottom = new Vector2(0.5f, 0f);

        // Arriba a la izquierda: corazones, barra de luz y almas
        var hearts = new Image[3];
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i] = MakeImage("Heart" + i, canvas.transform, heart, Color.white, topLeft, new Vector2(46f + i * 52f, -34f), new Vector2(44f, 44f), topLeft);
        }
        MakeImage("EnergyBack", canvas.transform, square, new Color(1f, 1f, 1f, 0.1f), topLeft, new Vector2(50f, -92f), new Vector2(300f, 12f), topLeft);
        Image energy = MakeImage("EnergyFill", canvas.transform, square, new Color(1f, 0.82f, 0.4f), topLeft, new Vector2(50f, -92f), new Vector2(300f, 12f), topLeft);
        energy.type = Image.Type.Filled;
        energy.fillMethod = Image.FillMethod.Horizontal;
        Text lightLabel = MakeText("LightLabel", canvas.transform, "LUZ", 20, topLeft, new Vector2(360f, -86f), TextAnchor.UpperLeft);
        lightLabel.color = new Color(1f, 0.85f, 0.5f, 0.8f);
        Localize(lightLabel.gameObject, "light");
        var slots = new Image[6];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = MakeImage("SoulSlot" + i, canvas.transform, glow, Color.white, topLeft, new Vector2(42f + i * 40f, -118f), new Vector2(38f, 38f), topLeft);
        }
        Text soulsLabel = MakeText("SoulsLabel", canvas.transform, "ALMAS", 20, topLeft, new Vector2(250f, -128f), TextAnchor.UpperLeft);
        soulsLabel.color = new Color(0.6f, 0.95f, 1f, 0.8f);
        Localize(soulsLabel.gameObject, "souls");
        Text powers = MakeText("PowersText", canvas.transform, "", 22, topLeft, new Vector2(50f, -166f), TextAnchor.UpperLeft);
        powers.color = new Color(1f, 0.9f, 0.6f, 0.85f);

        Text time = MakeText("TimeText", canvas.transform, "0:00", 40, new Vector2(1f, 1f), new Vector2(-50f, -34f), TextAnchor.UpperRight);
        time.color = new Color(1f, 1f, 1f, 0.6f);
        Text saved = MakeText("SavedText", canvas.transform, "", 24, new Vector2(1f, 1f), new Vector2(-50f, -90f), TextAnchor.UpperRight);
        saved.color = Color.clear;

        // Tutorial corto al empezar (se va solo)
        Image card = MakeImage("Tutorial", canvas.transform, square, new Color(0.02f, 0.02f, 0.07f, 0.75f), top, new Vector2(0f, -40f), new Vector2(980f, 120f), top);
        var tutorial = card.gameObject.AddComponent<CanvasGroup>();
        Text keys = MakeText("Keys", card.transform, "A / D  moverte        ESPACIO  saltar        E  destello        Q  soltar alma", 30, center, new Vector2(0f, 14f), TextAnchor.MiddleCenter);
        keys.color = new Color(1f, 0.92f, 0.75f);
        Localize(keys.gameObject, "tutorialKeys");
        Text goal = MakeText("Goal", card.transform, "Rescata 3 almas y llega al portal", 24, center, new Vector2(0f, -28f), TextAnchor.MiddleCenter);
        goal.color = new Color(0.6f, 0.95f, 1f);
        Localize(goal.gameObject, "tutorialGoal");

        // Globo de diálogo: se coloca encima de la cabeza del personaje (lo mueve DialogueUI)
        Image box = MakeImage("DialogueBubble", canvas.transform, square, new Color(0.03f, 0.03f, 0.09f, 0.88f), center, Vector2.zero, new Vector2(720f, 104f), bottom);
        var boxGroup = box.gameObject.AddComponent<CanvasGroup>();
        boxGroup.alpha = 0f;
        Image tail = MakeImage("Tail", box.transform, square, new Color(0.03f, 0.03f, 0.09f, 0.88f), bottom, new Vector2(0f, -8f), new Vector2(22f, 22f), center);
        tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        MakeImage("Line", box.transform, square, new Color(1f, 0.82f, 0.45f, 0.7f), top, Vector2.zero, new Vector2(720f, 2f), top);
        Text speaker = MakeText("Speaker", box.transform, "", 22, topLeft, new Vector2(20f, -10f), TextAnchor.UpperLeft);
        speaker.color = new Color(1f, 0.82f, 0.45f);
        speaker.font = boldFont;
        Text body = MakeText("Body", box.transform, "", 26, topLeft, new Vector2(20f, -40f), TextAnchor.UpperLeft);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.rectTransform.sizeDelta = new Vector2(680f, 60f);
        var dialogue = canvas.gameObject.AddComponent<DialogueUI>();
        Set(dialogue, "box", boxGroup);
        Set(dialogue, "speakerText", speaker);
        Set(dialogue, "bodyText", body);

        // Pantalla final: título, historia, estadísticas, galería de finales y cómo volver a jugar
        Image panel = MakeImage("EndPanel", canvas.transform, square, new Color(0.01f, 0.01f, 0.04f, 0.92f), center, Vector2.zero, Vector2.zero, center);
        panel.rectTransform.anchorMin = Vector2.zero;
        panel.rectTransform.anchorMax = Vector2.one;
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        MakeImage("Glow", panel.transform, glow, new Color(1f, 0.85f, 0.5f, 0.12f), center, new Vector2(0f, 200f), new Vector2(1400f, 700f), center);
        Text endTitle = Titled(MakeText("Title", panel.transform, "", 84, center, new Vector2(0f, 260f), TextAnchor.MiddleCenter));
        endTitle.fontStyle = FontStyle.Bold;
        Text endStory = MakeText("Story", panel.transform, "", 34, center, new Vector2(0f, 150f), TextAnchor.MiddleCenter);
        endStory.fontStyle = FontStyle.Italic;
        endStory.color = new Color(0.9f, 0.88f, 1f);
        MakeImage("Divider", panel.transform, square, new Color(1f, 1f, 1f, 0.15f), center, new Vector2(0f, 80f), new Vector2(700f, 2f), center);
        Text endStats = MakeText("Stats", panel.transform, "", 32, center, new Vector2(0f, 20f), TextAnchor.MiddleCenter);
        Text galleryLabel = MakeText("GalleryLabel", panel.transform, "FINALES DESCUBIERTOS", 22, center, new Vector2(0f, -90f), TextAnchor.MiddleCenter);
        galleryLabel.color = new Color(1f, 1f, 1f, 0.5f);
        Localize(galleryLabel.gameObject, "gallery");
        var endingSlots = new Text[3];
        for (int i = 0; i < endingSlots.Length; i++)
        {
            Image frame = MakeImage("Ending" + i, panel.transform, square, new Color(1f, 1f, 1f, 0.06f), center, new Vector2((i - 1) * 340f, -160f), new Vector2(300f, 80f), center);
            endingSlots[i] = MakeText("Name", frame.transform, "? ? ?", 30, center, Vector2.zero, TextAnchor.MiddleCenter);
        }
        Text endPrompt = MakeText("Prompt", panel.transform, "", 28, center, new Vector2(0f, -290f), TextAnchor.MiddleCenter);

        var ui = canvas.gameObject.AddComponent<GameUI>();
        SetList(ui, "hearts", hearts);
        Set(ui, "energyFill", energy);
        SetList(ui, "soulSlots", slots);
        Set(ui, "powersText", powers);
        Set(ui, "timeText", time);
        Set(ui, "savedText", saved);

        // Barra del Devorador (arriba al centro) y flecha hacia el alma que robó
        Image bossBack = MakeImage("BossBar", canvas.transform, square, new Color(0f, 0f, 0f, 0.6f), top, new Vector2(0f, -70f), new Vector2(720f, 18f), top);
        var bossGroup = bossBack.gameObject.AddComponent<CanvasGroup>();
        bossGroup.alpha = 0f;
        Image bossFill = MakeImage("Fill", bossBack.transform, square, new Color(0.75f, 0.15f, 0.45f), center, Vector2.zero, new Vector2(716f, 14f), center);
        bossFill.type = Image.Type.Filled;
        bossFill.fillMethod = Image.FillMethod.Horizontal;
        foreach (float x in new[] { -180f, 0f, 180f })
        {
            MakeImage("Divider", bossBack.transform, square, new Color(0f, 0f, 0f, 0.8f), center, new Vector2(x, 0f), new Vector2(3f, 18f), center);
        }
        Text bossName = Titled(MakeText("BossName", bossBack.transform, "", 30, top, new Vector2(0f, 40f), TextAnchor.LowerCenter));
        bossName.color = new Color(1f, 0.55f, 0.65f);
        Localize(bossName.gameObject, "bossName");
        Image stolenArrow = MakeImage("StolenArrow", canvas.transform, arrow, new Color(0.6f, 0.95f, 1f), center, Vector2.zero, new Vector2(54f, 54f), center);
        stolenArrow.gameObject.SetActive(false);
        Set(ui, "bossBar", bossGroup);
        Set(ui, "bossFill", bossFill);
        Set(ui, "stolenArrow", stolenArrow.rectTransform);
        Set(ui, "tutorial", tutorial);
        Set(ui, "endPanel", group);
        Set(ui, "endTitle", endTitle);
        Set(ui, "endStory", endStory);
        Set(ui, "endStats", endStats);
        SetList(ui, "endingSlots", endingSlots);
        Set(ui, "endPrompt", endPrompt);

        MakePause(canvas);

        // Fundido negro encima de todo (al empezar, morir y reaparecer)
        Image fade = MakeImage("ScreenFade", canvas.transform, square, Color.black, center, Vector2.zero, Vector2.zero, center);
        fade.rectTransform.anchorMin = Vector2.zero;
        fade.rectTransform.anchorMax = Vector2.one;
        var fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.blocksRaycasts = false;
        Set(canvas.gameObject.AddComponent<ScreenFade>(), "black", fadeGroup);
    }

    // Pausa: fondo oscurecido y una tarjeta con la partida, botones, volumen, idioma y controles
    static void MakePause(Canvas canvas)
    {
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        MakeEventSystem();
        Vector2 center = new Vector2(0.5f, 0.5f);

        Image dim = FullPanel("PauseMenu", canvas.transform, new Color(0f, 0f, 0.02f, 0.75f));
        Image card = MakeImage("Card", dim.transform, square, new Color(0.03f, 0.03f, 0.08f, 0.96f), center, Vector2.zero, new Vector2(720f, 700f), center);
        MakeImage("Line", card.transform, square, new Color(1f, 0.82f, 0.45f, 0.8f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(720f, 3f), new Vector2(0.5f, 1f));
        MakeImage("Glow", card.transform, glow, new Color(1f, 0.8f, 0.45f, 0.1f), center, new Vector2(0f, 250f), new Vector2(700f, 300f), center);
        Text title = Titled(MakeText("Title", card.transform, "", 70, center, new Vector2(0f, 280f), TextAnchor.MiddleCenter));
        title.color = new Color(1f, 0.88f, 0.6f);
        title.fontStyle = FontStyle.Bold;
        Localize(title.gameObject, "paused");
        Text stats = MakeText("Stats", card.transform, "", 26, center, new Vector2(0f, 215f), TextAnchor.MiddleCenter);
        stats.color = new Color(0.75f, 0.85f, 1f, 0.8f);

        Button resume = MenuButton(card.transform, "resume", new Vector2(0f, 130f));
        Button restart = MenuButton(card.transform, "restart", new Vector2(0f, 45f));
        Button menu = MenuButton(card.transform, "mainMenu", new Vector2(0f, -40f));
        Slider volume = VolumeSlider(card.transform, new Vector2(0f, -135f));
        LanguageButtons(card.transform, new Vector2(0f, -220f), out Button spanish, out Button english);
        Text help = MakeText("Controls", card.transform, "", 20, center, new Vector2(0f, -305f), TextAnchor.MiddleCenter);
        help.color = new Color(1f, 1f, 1f, 0.45f);
        Localize(help.gameObject, "controls");
        dim.gameObject.SetActive(false);

        var pause = canvas.gameObject.AddComponent<PauseMenu>();
        Set(pause, "panel", dim.gameObject);
        Set(pause, "statsText", stats);
        Set(pause, "resumeButton", resume);
        Set(pause, "restartButton", restart);
        Set(pause, "menuButton", menu);
        Set(pause, "spanishButton", spanish);
        Set(pause, "englishButton", english);
        Set(pause, "volumeSlider", volume);
    }

    static void MakeEventSystem()
    {
        var events = new GameObject("EventSystem");
        events.AddComponent<EventSystem>();
        events.AddComponent<StandaloneInputModule>();
    }

    static void LanguageButtons(Transform parent, Vector2 pos, out Button spanish, out Button english)
    {
        Text label = MakeText("LanguageLabel", parent, "", 22, new Vector2(0.5f, 0.5f), pos + new Vector2(0f, 45f), TextAnchor.MiddleCenter);
        label.color = new Color(1f, 1f, 1f, 0.6f);
        Localize(label.gameObject, "language");
        spanish = MenuButton(parent, null, pos + new Vector2(-110f, 0f), "Español", 200f);
        english = MenuButton(parent, null, pos + new Vector2(110f, 0f), "English", 200f);
    }

    static Image FullPanel(string name, Transform parent, Color color)
    {
        Image panel = MakeImage(name, parent, square, color, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        panel.rectTransform.anchorMin = Vector2.zero;
        panel.rectTransform.anchorMax = Vector2.one;
        panel.raycastTarget = true;
        return panel;
    }

    // Botón con texto traducido (key) o fijo (label)
    static Button MenuButton(Transform parent, string key, Vector2 pos, string label = "", float width = 420f)
    {
        Image back = MakeImage("Button_" + (key ?? label), parent, square, new Color(1f, 1f, 1f, 0.08f), new Vector2(0.5f, 0.5f), pos, new Vector2(width, 64f), new Vector2(0.5f, 0.5f));
        back.raycastTarget = true;
        var button = back.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.5f, 1f);
        colors.pressedColor = new Color(1f, 0.7f, 0.3f, 1f);
        button.colors = colors;
        Text text = Titled(MakeText("Label", back.transform, label, 30, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter));
        text.color = new Color(1f, 0.92f, 0.8f);
        if (key != null) Localize(text.gameObject, key);
        return button;
    }

    static Slider VolumeSlider(Transform parent, Vector2 pos)
    {
        Text label = MakeText("VolumeLabel", parent, "", 22, new Vector2(0.5f, 0.5f), pos + new Vector2(0f, 32f), TextAnchor.MiddleCenter);
        label.color = new Color(1f, 1f, 1f, 0.6f);
        Localize(label.gameObject, "volume");
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(420f, 20f);
        var slider = go.GetComponent<Slider>();
        slider.transform.Find("Background").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        slider.fillRect.GetComponent<Image>().color = new Color(1f, 0.82f, 0.45f);
        slider.handleRect.GetComponent<Image>().color = Color.white;
        return slider;
    }

    static VolumeProfile MakeVolumeProfile()
    {
        const string path = "Assets/04_Materials/LumbraVolume.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var bloom = Add<Bloom>(profile);
        bloom.threshold.Override(0.75f);
        bloom.intensity.Override(1.5f);
        bloom.scatter.Override(0.75f);

        var vignette = Add<Vignette>(profile);
        vignette.intensity.Override(0.45f);
        vignette.smoothness.Override(0.55f);
        vignette.color.Override(new Color(0.02f, 0f, 0.05f));

        Add<FilmGrain>(profile).intensity.Override(0.25f);
        var color = Add<ColorAdjustments>(profile);
        color.saturation.Override(10f);
        color.contrast.Override(20f);
        color.postExposure.Override(-0.2f);
        return profile;
    }

    static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = profile.Add<T>(true);
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    // ================= Ayudantes =================

    static void EnsureGroundLayer()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tagManager.FindProperty("layers").GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    static ParticleSystem SaveBurst(string name, Material mat, Color a, Color b, int count)
    {
        ParticleSystem ps = MakeParticles(name, null, mat, a, b, 20);
        var main = ps.main;
        main.loop = false;
        main.startLifetime = 0.8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ps.gameObject, "Assets/02_Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(ps.gameObject);
        return prefab.GetComponent<ParticleSystem>();
    }

    static void MakeLayer(string name, Sprite sprite, Color color, float factor, float y, int order, Vector3 scale)
    {
        var layer = new GameObject("Parallax_" + name);
        layer.transform.SetParent(background);
        layer.transform.position = new Vector3(0f, y, 0f);
        foreach (float x in new[] { -LayerWidth, 0f, LayerWidth })
        {
            SpriteRenderer part = MakeRenderer(name, layer.transform, sprite, color, order, unlit);
            part.transform.localPosition = new Vector3(x, 0f, 0f);
            part.transform.localScale = scale;
        }
        var parallax = layer.AddComponent<Parallax>();
        SetFloat(parallax, "factor", factor);
        SetFloat(parallax, "width", LayerWidth);
    }

    static SpriteRenderer MakeRenderer(string name, Transform parent, Sprite sprite, Color color, int order, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        sr.sharedMaterial = mat;
        return sr;
    }

    static Light2D MakeLight(string name, Transform parent, Color color, float radius, float intensity)
    {
        var light = new GameObject(name).AddComponent<Light2D>();
        light.transform.SetParent(parent, false);
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.pointLightOuterRadius = radius;
        light.intensity = intensity;
        return light;
    }

    static ParticleSystem MakeParticles(string name, Transform parent, Material mat, Color a, Color b, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = order;
        return ps;
    }

    static Material MakeParticleMaterial(string name, Texture texture, bool additive)
    {
        string path = "Assets/04_Materials/" + name + ".mat";
        var source = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        var mat = new Material(source);
        mat.SetTexture("_BaseMap", texture);
        if (additive)
        {
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
        }
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var img = new GameObject(name).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    static Text MakeText(string name, Transform parent, string text, int size, Vector2 anchor, Vector2 pos, TextAnchor align)
    {
        var t = new GameObject(name).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = bodyFont;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1600f, 60f);
        return t;
    }

    static SerializedProperty Find(Component target, string field, out SerializedObject so)
    {
        so = new SerializedObject(target);
        return so.FindProperty(field);
    }

    static void Set(Component target, string field, Object value)
    {
        Find(target, field, out var so).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetFloat(Component target, string field, float value)
    {
        Find(target, field, out var so).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetBool(Component target, string field, bool value)
    {
        Find(target, field, out var so).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetEnum(Component target, string field, int value)
    {
        Find(target, field, out var so).enumValueIndex = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetMask(Component target, string field)
    {
        Find(target, field, out var so).intValue = 1 << GroundLayer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetList(Component target, string field, Object[] values)
    {
        SerializedProperty list = Find(target, field, out var so);
        list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetDialogue(Component target, string field, DialogueLine[] lines)
    {
        SerializedProperty list = Find(target, field, out var so);
        list.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            list.GetArrayElementAtIndex(i).FindPropertyRelative("speaker").stringValue = lines[i].speaker;
            list.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue = lines[i].text;
            list.GetArrayElementAtIndex(i).FindPropertyRelative("textEn").stringValue = lines[i].textEn;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= Dibujo de texturas =================

    static float Dist(float u, float v) => Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
    static float Edge(float value, float sharpness) => Mathf.Clamp01(value * sharpness);
    static float Hash(float x, float y) => Mathf.Abs(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.55f) % 1f;

    // Bloques de piedra: 4 hileras de ladrillos alternados, con junta oscura y algo de desgaste
    static Color StoneColor(float u, float v)
    {
        float row = Mathf.Floor(v * 4f);
        float bu = u * 2f + (row % 2f) * 0.5f;
        float brick = Mathf.Floor(bu);
        float fu = bu - brick;
        float fv = v * 4f - row;
        bool mortar = fu < 0.04f || fv < 0.08f;
        float shade = 0.75f + 0.25f * Hash(brick, row) - Mathf.PerlinNoise(u * 9f, v * 9f) * 0.15f;
        return mortar ? new Color(0.1f, 0.09f, 0.15f) : new Color(0.56f, 0.53f, 0.68f) * shade;
    }

    // Anillo de runas: una banda fina con seis rombos alrededor
    static float Runes(float u, float v)
    {
        float x = u - 0.5f, y = v - 0.5f;
        float r = Mathf.Sqrt(x * x + y * y);
        float band = Edge(0.012f - Mathf.Abs(r - 0.42f), 200f);
        float angle = Mathf.Atan2(y, x) / (Mathf.PI * 2f) * 6f;
        float slot = Mathf.Abs(angle - Mathf.Round(angle)) * 2.6f;
        float diamond = Edge(0.05f - (Mathf.Abs(r - 0.42f) + slot * 0.12f), 120f);
        return Mathf.Max(band, diamond);
    }

    // Corazón: dos círculos arriba y una punta abajo
    static float Heart(float u, float v)
    {
        float x = (u - 0.5f) * 2.4f;
        float y = (v - 0.45f) * 2.4f;
        float a = Mathf.Pow(x * x + y * y - 1f, 3f) - x * x * y * y * y;
        return a < 0f ? 1f : 0f;
    }

    // Ventanal gótico: rectángulo con arco en punta y una cruz de piedra
    static float ArchWindow(float u, float v)
    {
        float x = (u - 0.5f) * 2f;
        float y = v * 4f;
        bool arch = y < 2.8f ? Mathf.Abs(x) < 0.85f : Mathf.Abs(x) < 0.85f * (1f - (y - 2.8f) / 1.1f);
        bool cross = Mathf.Abs(x) < 0.06f || Mathf.Abs(y - 1.9f) < 0.06f;
        return arch && !cross && y > 0.1f ? 1f : 0f;
    }

    // Piedra de las plataformas que se caen: tono cálido con grietas en zigzag
    static Color CrackedColor(float u, float v)
    {
        float crack = Mathf.Abs(v - 0.5f - 0.3f * Mathf.Sin(u * Mathf.PI * 2f * 3f) * Mathf.Sin(u * 17f));
        bool line = crack < 0.06f || Mathf.Abs(u - 0.33f) < 0.012f || Mathf.Abs(u - 0.8f) < 0.012f;
        float shade = 0.8f + 0.2f * Mathf.PerlinNoise(u * 8f, v * 8f);
        return line ? new Color(0.25f, 0.12f, 0.1f) : new Color(0.62f, 0.5f, 0.48f) * shade;
    }

    // Borde de musgo y hierba irregular (se repite a lo ancho)
    static float Moss(float u, float v)
    {
        float h = 0.35f + 0.15f * Mathf.Sin(u * Mathf.PI * 2f * 3f) + 0.35f * Mathf.Pow(Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f * 9f)), 8f);
        return Edge(h - v, 20f);
    }

    // Columna con base, fuste estriado y capitel; la rota termina en un corte irregular
    static float ColumnShape(float u, float v, bool broken)
    {
        float x = Mathf.Abs(u - 0.5f);
        float top = broken ? 0.55f + 0.08f * Mathf.Sin(u * 40f) : 0.97f;
        if (v > top) return 0f;
        if (v < 0.05f) return Edge(0.45f - x, 60f);
        if (!broken && v > 0.92f) return Edge(0.48f - x, 60f);
        return Edge(0.3f - x, 60f);
    }

    static Color ColumnColor(float u, float v)
    {
        float flute = 0.85f + 0.15f * Mathf.Cos(u * Mathf.PI * 2f * 5f);
        return new Color(0.5f, 0.47f, 0.6f) * flute * (0.8f + 0.2f * Mathf.PerlinNoise(u * 4f, v * 20f));
    }

    static float Blob(float u, float v)
    {
        Vector3[] balls = { new(0.5f, 0.55f, 0.3f), new(0.3f, 0.3f, 0.12f), new(0.52f, 0.18f, 0.09f), new(0.72f, 0.3f, 0.11f), new(0.38f, 0.75f, 0.12f), new(0.66f, 0.74f, 0.1f) };
        float field = 0f;
        foreach (Vector3 b in balls)
        {
            float d2 = (u - b.x) * (u - b.x) + (v - b.y) * (v - b.y);
            field += b.z * b.z / Mathf.Max(d2, 0.0001f);
        }
        return Edge(field - 1f, 6f);
    }

    static float DarkEdge(float u, float v)
    {
        float edge = 0.72f + 0.12f * Mathf.Sin(v * Mathf.PI * 2f * 3f) + 0.06f * Mathf.Sin(v * Mathf.PI * 2f * 7f + 1f);
        return Mathf.Clamp01((edge - u) * 14f);
    }

    static Color SkyColor(float u, float v)
    {
        Color bottom = new Color(0.14f, 0.05f, 0.2f);
        Color middle = new Color(0.07f, 0.07f, 0.24f);
        Color top = new Color(0.01f, 0.01f, 0.04f);
        return v < 0.4f ? Color.Lerp(bottom, middle, v / 0.4f) : Color.Lerp(middle, top, (v - 0.4f) / 0.6f);
    }

    // Ruinas del santuario: columnas (algunas rotas), arcos entre columnas y el suelo
    static Sprite MakeRuins(string name, int count)
    {
        var cx = new float[count];
        var cw = new float[count];
        var ch = new float[count];
        var broken = new bool[count];
        var arch = new bool[count];
        for (int i = 0; i < count; i++)
        {
            cx[i] = (i + 0.5f) * LayerWidth / count + Random.Range(-1f, 1f);
            cw[i] = Random.Range(0.35f, 0.6f);
            ch[i] = Random.Range(6f, 13f);
            broken[i] = Random.value < 0.4f;
            arch[i] = Random.value < 0.6f;
        }

        Func<float, float, float> shape = (u, v) =>
        {
            float x = u * LayerWidth;
            float y = v * 20f;
            if (y < 1.2f + 0.4f * Mathf.Sin(x * 0.5f)) return 1f;
            for (int i = 0; i < count; i++)
            {
                float dx = Mathf.Abs(x - cx[i]);
                float top = broken[i] ? ch[i] - 1.5f * Mathf.Abs(Mathf.Sin(x * 7f + i)) : ch[i];
                if (dx < cw[i] && y < top) return 1f;
                if (!broken[i] && dx < cw[i] * 1.7f && y > ch[i] - 0.5f && y < ch[i]) return 1f;

                // Arco hasta la siguiente columna si las dos están enteras
                int j = i + 1;
                if (arch[i] && j < count && !broken[i] && !broken[j])
                {
                    float r = (cx[j] - cx[i]) / 2f;
                    float baseY = Mathf.Min(ch[i], ch[j]) - 0.5f;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx[i] + r, baseY));
                    if (y > baseY && d < r && d > r - 0.5f) return 1f;
                }
            }
            return 0f;
        };
        return MakeSprite(name, 1024, 512, 1024f / LayerWidth, shape, null, new Vector2(0.5f, 0f));
    }

    static Sprite MakeSilhouette(string name, float baseHeight, float bumpHeight, int bumps)
    {
        float[] phases = { Random.value * 6f, Random.value * 6f };
        Func<float, float, float> shape = (u, v) =>
        {
            float h = baseHeight + Mathf.Sin(u * Mathf.PI * 2f * 2f + phases[0]) * bumpHeight * 0.5f
                + Mathf.Sin(u * Mathf.PI * 2f * bumps + phases[1]) * bumpHeight * 0.2f;
            return Edge(h - v, 600f);
        };
        return MakeSprite(name, 1024, 512, 1024f / LayerWidth, shape, null, new Vector2(0.5f, 0f));
    }

    static Sprite MakeStars()
    {
        const int size = 512;
        var pixels = new Color[size * size];
        for (int i = 0; i < 220; i++)
        {
            int px = Random.Range(2, size - 2);
            int py = Random.Range(2, size - 2);
            pixels[py * size + px] = new Color(1f, 1f, 1f, Random.Range(0.3f, 1f));
        }
        return SaveSprite("Stars", size, size, size / LayerWidth, pixels, new Vector2(0.5f, 0f), false);
    }

    static Sprite MakeSprite(string name, int w, int h, float ppu, Func<float, float, float> alpha, Func<float, float, Color> color = null, Vector2? pivot = null, bool repeat = false)
    {
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float v = (y + 0.5f) / h;
                Color c = color != null ? color(u, v) : Color.white;
                c.a = alpha(u, v);
                pixels[y * w + x] = c;
            }
        }
        return SaveSprite(name, w, h, ppu, pixels, pivot ?? new Vector2(0.5f, 0.5f), repeat);
    }

    static Sprite SaveSprite(string name, int w, int h, float ppu, Color[] pixels, Vector2 pivot, bool repeat)
    {
        string path = SpritesPath + name + ".png";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        return ImportSprite(path, ppu, pivot, repeat);
    }

    static Sprite ImportSprite(string path, float ppu, Vector2 pivot, bool repeat)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
