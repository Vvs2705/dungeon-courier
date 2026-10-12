using System.IO;
using UnityEditor;
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
        public const string Version = "1.1.0-beta";

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
            AssetDatabase.SaveAssets();
            Debug.Log("DC Setup: ok (" + AppId + " " + Version + " code " + VersionCode(Version) + ", retrato, Android ARM64 IL2CPP API 26+, Input System)");
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/win/DungeonCourier.exe", false);
        public static void BuildAndroidDev() => Build(BuildTarget.Android, ApkDir + "/DungeonCourier-dev.apk", false);
        /// <summary>APK de release: nao-development, ARM64, IL2CPP (Apply), chave de debug do Unity; apaga os APKs anteriores.</summary>
        public static void BuildAndroidRelease() => Build(BuildTarget.Android, ApkDir + "/DungeonCourier-" + Version + ".apk", true);
        public static void BuildWindowsRelease() => Build(BuildTarget.StandaloneWindows64, "Builds/win-release/DungeonCourier.exe", true);

        /// <summary>Dependencias nativas do LevelPlay (Assets/LevelPlay/Editor/*Dependencies.xml, instaladas pelo Network Manager) entram no
        /// Gradle pelo Mobile Dependency Resolver (Assets/MobileDependencyResolver), que remenda os templates em Assets/Plugins/Android. Em batch
        /// a resolucao automatica pode nao rodar antes do build: forcamos o ResolveSync. O Unity IAP poe o billing sozinho.</summary>
        static void ResolveAndroid()
        {
            System.Type r = System.Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
            object ok = r?.GetMethod("ResolveSync", new[] { typeof(bool) })?.Invoke(null, new object[] { true });
            Debug.Log("DC Setup: Mobile Dependency Resolver " + (ok is bool b ? (b ? "ok" : "FALHOU") : "ausente"));
        }

        static void Build(BuildTarget target, string path, bool release)
        {
            Apply();
            BuildReport r = null;
            try
            {
                if (target == BuildTarget.Android)
                {
                    // regra do Vinicius (2026-10-09): fica so' o APK atual; os anteriores sao apagados antes de gerar o novo
                    if (Directory.Exists(ApkDir)) foreach (string apk in Directory.GetFiles(ApkDir, "*.apk")) File.Delete(apk);
                    // o empacotamento incremental do Gradle reaproveita o APK anterior e ja' deixou buracos/sem libunity no Forge Street
                    const string gradleOut = "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build";
                    if (Directory.Exists(gradleOut)) Directory.Delete(gradleOut, true);
                    ResolveAndroid();
                    EditorUserBuildSettings.buildAppBundle = false;            // APK para instalar direto no aparelho do testador
                    PlayerSettings.Android.useCustomKeystore = false;          // chave de debug do Unity (a da Play vem depois)
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
            if (Application.isBatchMode) EditorApplication.Exit(r != null && r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
