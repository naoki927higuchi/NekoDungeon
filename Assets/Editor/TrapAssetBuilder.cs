using UnityEditor;
using UnityEngine;

// Wrap the downloaded Kenney meshes; no replacement trap geometry is generated.
public static class TrapAssetBuilder
{
    [MenuItem("Dungeon/Import Trap Prefabs")]
    public static void Build()
    {
        const string target="Assets/Resources/Traps";
        const string source="Assets/ThirdParty/KenneyPlatformer/Models/";
        System.IO.Directory.CreateDirectory(target);AssetDatabase.Refresh();
        var material=AssetDatabase.LoadAssetAtPath<Material>(target+"/KenneyColormap.mat");
        if(!material) {material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,target+"/KenneyColormap.mat");}
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(source+"Textures/colormap.png");
        material.color=Color.white;material.SetFloat("_Glossiness",.18f);
        material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);
        var models=new[]{"trap-spikes","saw","spike-block"};
        for(int i=0;i<models.Length;i++) {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(source+models[i]+".obj");
            if(!model) throw new System.Exception("Missing downloaded model: "+models[i]);
            var root=new GameObject(((TrapKind)i).ToString());
            try {
                var visual=Object.Instantiate(model,root.transform);visual.name="Kenney model";
                foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
                    var slots=renderer.sharedMaterials;
                    for(int j=0;j<slots.Length;j++) slots[j]=material;
                    renderer.sharedMaterials=slots;
                }
                root.AddComponent<TrapActor>().SetModel(visual.transform);
                PrefabUtility.SaveAsPrefabAsset(root,target+"/"+((TrapKind)i)+".prefab");
            } finally {Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();
    }
}
