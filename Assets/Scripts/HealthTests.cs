using System;
using UnityEngine;

public static class HealthTests
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run()
    {
        var hp=new CatHealth();Check(hp.HP==5,"Initial HP");
        Check(hp.Hurt() && hp.HP==4 && !hp.Hurt(),"Damage or invulnerability failed");
        hp.Tick(.5f);Check(!hp.Hurt(),"Invulnerability too short");
        hp.Tick(.51f);Check(hp.Hurt() && hp.HP==3,"Invulnerability did not expire");
        for(int i=0;i<8;i++) { hp.Tick(2);hp.Hurt(); }
        Check(hp.Dead && hp.HP==0 && !hp.Hurt(),"Death/clamp failed");
        hp.Reset();Check(hp.HP==5 && !hp.Invulnerable && !hp.Dead,"HP reset failed");
        var hunter=new RatBrain(new Vector2(0,3),true);
        hunter.Tick(Vector2.zero,.05f);
        Check(hunter.Chasing && !hunter.Fleeing && hunter.Position.y<3,"Hunter did not approach");
        hunter.Position=new Vector2(0,.7f);hunter.Tick(Vector2.zero,.05f);
        Check(hunter.WindingUp && !hunter.AttackLanded,"Missing attack telegraph");
        int hits=0;for(int i=0;i<11;i++) { hunter.Tick(Vector2.zero,.05f);if(hunter.AttackLanded) hits++; }
        Check(hits==1,"Attack did not land exactly once");
        for(int i=0;i<15;i++) { hunter.Tick(Vector2.zero,.05f);Check(!hunter.AttackLanded,"Attack cooldown ignored"); }
        hunter=new RatBrain(new Vector2(0,.7f),true);hunter.Tick(Vector2.zero,.05f);
        for(int i=0;i<11;i++) { hunter.Tick(new Vector2(0,-3),.05f);Check(!hunter.AttackLanded,"Dodged attack still hit"); }
        hunter=new RatBrain(new Vector2(0,.7f),true);hunter.Tick(Vector2.zero,.05f);hunter.Defeat();
        for(int i=0;i<20;i++) { hunter.Tick(Vector2.zero,.05f);Check(!hunter.AttackLanded && !hunter.WindingUp,"Dead hunter attacked"); }
        var timid=new RatBrain(new Vector2(0,.5f));for(int i=0;i<100;i++) { timid.Tick(Vector2.zero,.05f);Check(!timid.AttackLanded,"Timid mouse attacked"); }
        hunter=new RatBrain(new Vector2(3.7f,3.7f),true);
        for(int i=0;i<100;i++) hunter.Tick(new Vector2(5,5),.05f);
        Check(hunter.Position.x<=RatBrain.RoomLimit && hunter.Position.y<=RatBrain.RoomLimit,"Hunter crossed wall");
    }
}
