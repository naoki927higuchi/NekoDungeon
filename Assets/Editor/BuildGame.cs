using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildGame
{
    [MenuItem("Dungeon/Build Windows")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources");
        if (!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Stone.mat"))
            AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), "Assets/Resources/Stone.mat");
        CatModelBuilder.Build();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Dungeon", typeof(DungeonGame));
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Dungeon.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Dungeon.unity", true) };
        PlayerSettings.productName = "The Quiet Vault";
        PlayerSettings.companyName = "Independent";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/QuietVault.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Build failed: " + report.summary.result);
    }
}
