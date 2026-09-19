using UnityEngine;

public sealed class CatHealth
{
    public const int MaxHP=5;
    public int HP { get; private set; }=MaxHP;
    public bool Dead => HP==0;
    public bool Invulnerable => protection>0;
    float protection;
    public void Reset() { HP=MaxHP;protection=0; }
    public void Tick(float dt) { protection=Mathf.Max(0,protection-Mathf.Max(0,dt)); }
    public bool Hurt()
    {
        if(Dead || Invulnerable) return false;
        HP=Mathf.Max(0,HP-1);protection=1f;return true;
    }
}
