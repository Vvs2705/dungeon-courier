using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DC.EditorTools
{
    /// <summary>
    /// Settings, cena e builds por script (mesmo padrao do Forge Street / Rune Relay). Idempotente. Em batch:
    ///   Unity -batchmode -quit -projectPath client -executeMethod DC.EditorTools.Setup.Apply
    ///   Unity -batchmode -quit -projectPath client -executeMethod DC.EditorTools.Setup.BuildWindows (ou BuildAndroidDev)
    ///   Release (CONTRATO_V1 §14): ... -executeMethod DC.EditorTools.Setup.BuildAndroidRelease (ou BuildWindowsRelease)
    /// A cena e' artefato gerado e vazia: o Game nasce sozinho (RuntimeInitializeOnLoadMethod).
    /// Assinatura do APK de release: a chave de DEBUG padrao do Unity (teste com jogadores por APK direto). A Play exige chave propria
    /// (keystore de upload fora do repo, senha por variavel de ambiente) e AAB: isso entra quando for subir para a loja.
    /// </summary>
    public static class Setup
    {
        // ponytail: appId provisorio; depois de publicado na Play nao muda nunca (decisao do Vinicius)
        public const string AppId = "br.com.vstack.dungeoncourier";
        const string Scene = "Assets/_DC/Scenes/Main.unity";
        const string ApkDir = "Builds/android";
        const string FakeStoreDefine = "IAP_FAKE_STORE_DEFAULT";
        /// <summary>Versao mostrada ao jogador (Application.version) e no diario.</summary>
        public const string Version = "1.4.2-beta";

        /// <summary>versionCode do Android, gerado da versao (nunca editado a mao, nunca reutilizado): (maior*10000 + menor*100 + patch)*10
        /// + estagio (alpha 1, beta 2, rc 5, final 9). 1.0.0-beta = 100002; 1.0.0 = 100009; 1.0.1-beta = 100012. A v0.1 usava 1.</summary>
        public static int VersionCode(string v)
        {
            int dash = v.IndexOf('-');
            string[] n = (dash < 0 ? v : v.Substring(0, dash)).Split('.');
            string tag = dash < 0 ? "" : v.Substring(dash + 1);
            int stage = tag.StartsWith("alpha") ? 1 : tag.StartsWith("beta") ? 2 : tag.StartsWith("rc") ? 5 : 9;
            int Part(int i) => i < n.Length && int.TryParse(n[i], out int x) ? x : 0;
            return (Part(0) * 10000 + Part(1) * 100 + Part(2)) * 10 + stage;
        }

        [MenuItem("DC/Aplicar settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "V-STACK";
            PlayerSettings.productName = "Dungeon Courier";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = VersionCode(Version);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;   // GDD s2: app em segundo plano pausa o contrato

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;      // a Play exige 64 bits
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // ponytail: hipotese ate fixar o aparelho minimo
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.predictiveBackSupport = true; // API 36: o voltar chega ao jogo como Esc (Input legado, ver activeInputHandler)

            // Windows: janela retrato 540x960 (dev e release) para jogar e fotografar no PC.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;

            // 2 = Input System + Input Manager legado (sem API publica; vale na proxima abertura do editor). O legado e' so para o voltar
            // do Android: com o GameActivity o Input System nao recebe a tecla (Forge Street, emulador API 35; forum Unity 1555368)
            // Unity IAP: loja falsa sem a janela propria do pacote (a confirmacao e a da LojaTeste; docs/MONETIZACAO.md)
            foreach (NamedBuildTarget t in new[] { NamedBuildTarget.Android, NamedBuildTarget.Standalone })
            {
                string defs = PlayerSettings.GetScriptingDefineSymbols(t);
                if (!defs.Contains(FakeStoreDefine)) PlayerSettings.SetScriptingDefineSymbols(t, string.IsNullOrEmpty(defs) ? FakeStoreDefine : defs + ";" + FakeStoreDefine);
            }

            var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty handler = ps.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != 2) { handler.intValue = 2; ps.ApplyModifiedPropertiesWithoutUndo(); }

            if (!File.Exists(Scene))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Scene));
                EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), Scene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            Icones();
            AssetDatabase.SaveAssets();
            Debug.Log("DC Setup: ok (" + AppId + " " + Version + " code " + VersionCode(Version) + ", retrato, Android ARM64 IL2CPP API 26+, Input System)");
        }

        /// <summary>Icone do app (client/tools/icone_app.py): adaptativo do Android (fundo + frente; com API 26+ o Unity 6 so usa ele, o
        /// redondo e o legado sao obsoletos) e o icone padrao (Windows). As texturas vao sem compressao nem mipmap: o Unity gera os tamanhos de cada densidade.</summary>
        static void Icones()
        {
            Texture2D T(string nome)
            {
                string p = "Assets/_DC/Icon/" + nome + ".png";
                if (AssetImporter.GetAtPath(p) is TextureImporter ti && (ti.textureCompression != TextureImporterCompression.Uncompressed || ti.mipmapEnabled))
                {
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            Texture2D fundo = T("icone_fundo"), frente = T("icone_frente"), padrao = T("icone");
            if (fundo == null || frente == null || padrao == null) { Debug.LogWarning("DC Setup: icone do app faltando (rode client/tools/icone_app.py)"); return; }
            PlatformIcon[] icones = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
            foreach (PlatformIcon ic in icones) ic.SetTextures(fundo, frente);   // camada 0 = fundo, 1 = frente
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, icones);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { padrao }, IconKind.Any);
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/win/DungeonCourier.exe", false);
        public static void BuildAndroidDev() => Build(BuildTarget.Android, ApkDir + "/DungeonCourier-dev.apk", false);
        /// <summary>APK de release: nao-development, ARM64, IL2CPP (Apply), chave de debug do Unity; apaga os APKs anteriores.</summary>
        public static void BuildAndroidRelease() => Build(BuildTarget.Android, ApkDir + "/DungeonCourier-" + Version + ".apk", true);
        public static void BuildWindowsRelease() => Build(BuildTarget.StandaloneWindows64, "Builds/win-release/DungeonCourier.exe", true);
        /// <summary>AAB para a Google Play (faixa interna/fechada/producao): release, assinado com a chave de upload do arquivo local de
        /// UploadProps (fora do git; docs/LOJA.md §8). Apaga os AABs anteriores. A Play reassina com a chave do app (Play App Signing).</summary>
        public static void BuildAndroidBundle() => Build(BuildTarget.Android, ApkDir + "/DungeonCourier-" + Version + ".aab", true, true);

        /// <summary>Chave de upload: keystore=, alias=, storepass=, keypass= em %USERPROFILE%/.dungeoncourier/upload.properties (ou no caminho de
        /// DC_UPLOAD_PROPS). Nunca no repositorio: guardar uma copia do .jks e do arquivo fora deste PC (perder = pedir reset ao suporte da Play).</summary>
        static string UploadProps => System.Environment.GetEnvironmentVariable("DC_UPLOAD_PROPS") is string e && e.Length > 0 ? e
            : Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".dungeoncourier", "upload.properties");

        static bool UseUploadKey()
        {
            if (!File.Exists(UploadProps)) { Debug.LogError("DC Setup: sem a chave de upload em " + UploadProps + " (docs/LOJA.md §8)"); return false; }
            var kv = new System.Collections.Generic.Dictionary<string, string>();
            foreach (string l in File.ReadAllLines(UploadProps)) { int i = l.IndexOf('='); if (i > 0) kv[l.Substring(0, i).Trim()] = l.Substring(i + 1).Trim(); }
            if (!kv.ContainsKey("keystore") || !File.Exists(kv["keystore"])) { Debug.LogError("DC Setup: keystore nao achado (" + UploadProps + ")"); return false; }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = kv["keystore"];
            PlayerSettings.Android.keystorePass = kv.TryGetValue("storepass", out string sp) ? sp : "";
            PlayerSettings.Android.keyaliasName = kv.TryGetValue("alias", out string al) ? al : "upload";
            PlayerSettings.Android.keyaliasPass = kv.TryGetValue("keypass", out string kp) ? kp : "";
            return true;
        }

        /// <summary>So com o LevelPlay de volta (DC_LEVELPLAY, docs/MONETIZACAO.md §4): as dependencias nativas dele entram no Gradle pelo
        /// Mobile Dependency Resolver, que em batch pode nao resolver antes do build; forcamos o ResolveSync. Sem o resolver (hoje), nada.
        /// O Unity IAP poe o billing sozinho.</summary>
        static void ResolveAndroid()
        {
            System.Type r = System.Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
            if (r == null) return;
            object ok = r.GetMethod("ResolveSync", new[] { typeof(bool) })?.Invoke(null, new object[] { true });
            Debug.Log("DC Setup: Mobile Dependency Resolver " + (ok is bool b && b ? "ok" : "FALHOU"));
        }

        static void Build(BuildTarget target, string path, bool release, bool bundle = false)
        {
            Apply();
            BuildReport r = null;
            try
            {
                if (target == BuildTarget.Android)
                {
                    // regra do Vinicius (2026-10-09): fica so' o APK atual; os anteriores sao apagados antes de gerar o novo
                    if (Directory.Exists(ApkDir)) foreach (string apk in Directory.GetFiles(ApkDir, bundle ? "*.aab" : "*.apk")) File.Delete(apk);
                    // o empacotamento incremental do Gradle reaproveita o APK anterior e ja' deixou buracos/sem libunity no Forge Street
                    const string gradleOut = "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build";
                    if (Directory.Exists(gradleOut)) Directory.Delete(gradleOut, true);
                    ResolveAndroid();
                    EditorUserBuildSettings.buildAppBundle = bundle;           // APK para o aparelho do testador; AAB so para a Play
                    PlayerSettings.Android.useCustomKeystore = false;          // APK: chave de debug do Unity
                    if (bundle && !UseUploadKey()) throw new System.Exception("AAB sem a chave de upload");
                    if (release) PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
                }
                r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = path,
                    target = target,
                    options = release ? BuildOptions.None : BuildOptions.Development,
                });
                Debug.Log($"BuildSummary({target}{(release ? " release" : " dev")} {Version}): result={r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize} path={path}");
            }
            catch (System.Exception e) { Debug.LogError("DC Setup: build abortado: " + e.Message); }
            finally
            {
                // o caminho do keystore nao fica no ProjectSettings (que vai para o git); as senhas o Unity nao grava
                if (bundle) { PlayerSettings.Android.useCustomKeystore = false; PlayerSettings.Android.keystoreName = ""; PlayerSettings.Android.keyaliasName = "";
                    EditorUserBuildSettings.buildAppBundle = false; AssetDatabase.SaveAssets(); }
            }
            if (Application.isBatchMode) EditorApplication.Exit(r != null && r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }

    /// <summary>Anuncio nao personalizado para todos (ECA Digital, publico 13+ com adolescentes; docs/MONETIZACAO.md §2.4): tira do APK o ID
    /// de publicidade e as APIs do Privacy Sandbox que o SDK de anuncio traz. Marcadores tools:node="remove" no manifesto do unityLibrary,
    /// que tem prioridade sobre as dependencias (AARs) na fusao do Gradle. Conferir com aapt dump permissions.</summary>
    sealed class SemIdDePublicidade : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        static readonly string[] Tirar =
        {
            "com.google.android.gms.permission.AD_ID", "android.permission.ACCESS_ADSERVICES_AD_ID",
            "android.permission.ACCESS_ADSERVICES_ATTRIBUTION", "android.permission.ACCESS_ADSERVICES_TOPICS",
        };
        public int callbackOrder => 999;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string f = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(f)) { Debug.LogWarning("DC Setup: manifesto do unityLibrary nao achado: " + f); return; }
            const string tools = "http://schemas.android.com/tools", android = "http://schemas.android.com/apk/res/android";
            var doc = new System.Xml.XmlDocument();
            doc.Load(f);
            System.Xml.XmlElement root = doc.DocumentElement;
            if (!root.HasAttribute("xmlns:tools")) root.SetAttribute("xmlns:tools", tools);
            foreach (string p in Tirar)
            {
                System.Xml.XmlElement e = doc.CreateElement("uses-permission");
                e.SetAttribute("name", android, p);
                e.SetAttribute("node", tools, "remove");
                root.AppendChild(e);
            }
            doc.Save(f);
            Debug.Log("DC Setup: AD_ID e Privacy Sandbox removidos do manifesto (" + Tirar.Length + ")");
        }
    }
}
