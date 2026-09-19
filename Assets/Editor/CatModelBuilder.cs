using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CatModelBuilder
{
    [MenuItem("Dungeon/Rebuild Cat Model")]
    public static void Build()
    {
        const string folder="Assets/Resources/CatModel";
        System.IO.Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var root=new GameObject("Explorer Cat");
        try {
            root.AddComponent<CatAvatar>().BuildModel(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Stone.mat"));
            var meshes=new Dictionary<Mesh,Mesh>(); var materials=new Dictionary<Material,Material>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
                var mesh=filter.sharedMesh;
                if(!meshes.ContainsKey(mesh)) meshes[mesh]=Save(mesh,folder+"/"+mesh.name+".asset");
                filter.sharedMesh=meshes[mesh];
            }
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()) {
                var material=renderer.sharedMaterial;
                if(!materials.ContainsKey(material)) materials[material]=Save(material,folder+"/"+material.name+".mat");
                renderer.sharedMaterial=materials[material];
            }
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/ExplorerCat.prefab");
            AssetDatabase.SaveAssets();
            foreach(var entry in meshes) if(entry.Key!=entry.Value) Object.DestroyImmediate(entry.Key);
            foreach(var entry in materials) if(entry.Key!=entry.Value) Object.DestroyImmediate(entry.Key);
        } finally { Object.DestroyImmediate(root); }
    }
    static T Save<T>(T source,string path) where T:Object
    {
        var saved=AssetDatabase.LoadAssetAtPath<T>(path);
        if(saved) { EditorUtility.CopySerialized(source,saved); EditorUtility.SetDirty(saved); return saved; }
        AssetDatabase.CreateAsset(source,path); return source;
    }
}
