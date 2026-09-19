using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class BuildGame
{
    [MenuItem("Dungeon/Build Windows")]
    public static void Build()
    {
        // This fixed directory contains generated player files only. Never merge builds.
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(projectRoot, "Builds", "Windows");
        CheckNoLinks(new DirectoryInfo(projectRoot), false);
        CheckNoLinks(new DirectoryInfo(Path.Combine(projectRoot, "Builds")), false);
        CheckNoLinks(new DirectoryInfo(output), true);
        if (Directory.Exists(output)) Directory.Delete(output, true);
        Directory.CreateDirectory(output);
        System.IO.Directory.CreateDirectory("Assets/Resources");
        if (!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Stone.mat"))
            AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), "Assets/Resources/Stone.mat");
        CatModelBuilder.Build();
        TrapAssetBuilder.Build();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Dungeon", typeof(DungeonGame));
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Dungeon.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Dungeon.unity", true) };
        PlayerSettings.productName = "猫パンチとひみつの迷宮";
        PlayerSettings.companyName = "Independent";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/NekoDungeon.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Build failed: " + report.summary.result);
        string licenses = Path.Combine(output, "Licenses");
        Directory.CreateDirectory(licenses);
        File.Copy("Distribution/README.txt", Path.Combine(output, "README.txt"), true);
        File.Copy("THIRD_PARTY_NOTICES.md", Path.Combine(output, "THIRD_PARTY_NOTICES.md"), true);
        File.Copy("Assets/ThirdParty/KenneyPlatformer/License.txt", Path.Combine(licenses, "Kenney-Platformer-Kit.txt"), true);
        var inputPackage = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.InputSystem.InputSystem).Assembly);
        File.Copy(Path.Combine(inputPackage.resolvedPath, "LICENSE.md"), Path.Combine(licenses, "Unity-Input-System.md"), true);
    }

    static void CheckNoLinks(DirectoryInfo directory, bool recursive)
    {
        if (!directory.Exists) return;
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Build output must not contain links: " + directory.FullName);
        if (!recursive) return;
        foreach (var entry in directory.GetFileSystemInfos()) {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Build output must not contain links: " + entry.FullName);
            if (entry is DirectoryInfo child) CheckNoLinks(child, true);
        }
    }
}
