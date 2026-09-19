using UnityEngine;

public sealed class TrapActor : MonoBehaviour
{
    [SerializeField] Transform model;
    public TrapState State { get; private set; }
    MeshRenderer[] renderers;
    MaterialPropertyBlock tint;
    public void SetModel(Transform value) {model=value;}
    public void Initialize(TrapState state)
    {State=state;renderers=model.GetComponentsInChildren<MeshRenderer>();tint=new MaterialPropertyBlock();Sync();}
    public void Sync()
    {
        var p=State.Position;transform.localPosition=new Vector3(p.x,.065f,p.y);
        if(State.Spawn.Kind==TrapKind.Spikes) {
            model.localScale=new Vector3(2,State.Active?2:State.Warning?.6f:.22f,2);
            model.localPosition=Vector3.zero;
        } else if(State.Spawn.Kind==TrapKind.Saw) {
            model.localScale=Vector3.one*1.5f;model.localPosition=Vector3.up*.32f;
            model.localRotation=Quaternion.Euler(90,0,State.Time*350);
        } else {model.localScale=Vector3.one*1.15f;model.localPosition=Vector3.zero;}
        var glow=State.Warning?new Color(.8f,.32f,.015f)*(Mathf.Sin(State.Time*18)*.3f+.7f):State.Spawn.Kind==TrapKind.Spikes && State.Active?new Color(.30f,.02f,0):Color.black;
        tint.SetColor("_EmissionColor",glow);
        foreach(var renderer in renderers) renderer.SetPropertyBlock(tint);
    }
}
