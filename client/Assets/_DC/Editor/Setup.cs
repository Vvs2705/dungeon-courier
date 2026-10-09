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
    /// A cena e' artefato gerado e vazia: o Game nasce sozinho (RuntimeInitializeOnLoadMethod).
    /// </summary>
    public static class Setup
    {
        // ponytail: appId provisorio; depois de publicado na Play nao muda nunca (decisao do Vinicius)
        public const string AppId = "br.com.vstack.dungeoncourier";
        const string Scene = "Assets/_DC/Scenes/Main.unity";
        const string ApkDir = "Builds/android";

        [MenuItem("DC/Aplicar settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "V-STACK";
            PlayerSettings.productName = "Dungeon Courier";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;   // GDD s2: app em segundo plano pausa o contrato

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;      // a Play exige 64 bits
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // ponytail: hipotese ate fixar o aparelho minimo
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.predictiveBackSupport = true; // API 36: o voltar chega ao jogo como Esc (Input legado, ver activeInputHandler)

            // Windows so' de dev: janela retrato para jogar e fotografar no PC.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;

            // 2 = Input System + Input Manager legado (sem API publica; vale na proxima abertura do editor). O legado e' so para o voltar
            // do Android: com o GameActivity o Input System nao recebe a tecla (Forge Street, emulador API 35; forum Unity 1555368)
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
            Debug.Log("DC Setup: ok (" + AppId + ", retrato, Android ARM64 IL2CPP API 26+, Input System)");
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/win/DungeonCourier.exe");
        public static void BuildAndroidDev() => Build(BuildTarget.Android, ApkDir + "/DungeonCourier-dev.apk");

        static void Build(BuildTarget target, string path)
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
                }
                r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = path,
                    target = target,
                    options = BuildOptions.Development,
                });
                Debug.Log($"BuildSummary({target}): result={r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize} path={path}");
            }
            catch (System.Exception e) { Debug.LogError("DC Setup: build abortado: " + e.Message); }
            if (Application.isBatchMode) EditorApplication.Exit(r != null && r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
