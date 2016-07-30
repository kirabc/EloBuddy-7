using System;
using System.Linq;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace MyTemplate
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu;
        private static Spell.Skillshot Poro = new Spell.Skillshot(myhero.GetSpellSlotFromName("SummonerPoroThrow"), 2500, SkillShotType.Linear, 330, 1600, 50);
        private static Spell.Active PoroDash = new Spell.Active(myhero.GetSpellSlotFromName("PoroThrowFollowupCast"));       
        static readonly string ChampionName = "Poro Thrower"; // Best Champion EU
        static readonly string Version = "1.0";
        static readonly string Date = "30/7/16";

        private static void OnLoad(EventArgs args)
        {
            if ((Poro == null && PoroDash == null) || Game.MapId != GameMapId.HowlingAbyss) return;
            Chat.Print("<font color='#0040FF'>T7</font><font color='#FFFFFF'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;   
            Game.OnTick += OnTick;
            DatMenu();    
        }

        private static void OnTick(EventArgs args)
        { if (myhero.IsDead) return; Core(); }

        private static bool check(Menu submenu, string sig)
        { return submenu[sig].Cast<CheckBox>().CurrentValue; }

        private static int slider(Menu submenu, string sig)
        { return submenu[sig].Cast<Slider>().CurrentValue; }

        private static bool key(Menu submenu, string sig)
        { return submenu[sig].Cast<KeyBind>().CurrentValue;  }

        private static void Core()
        {
            if (!Poro.IsReady() && !PoroDash.IsReady()) return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && !check(menu, "HARASS") && !key(menu, "DASHKEY")) return;

            var target = TargetSelector.GetTarget(1000, DamageType.Physical, Player.Instance.Position);

            if (target != null)
            {                              
                var ppred = Poro.GetPrediction(target);

                if (key(menu, "DASHKEY"))
                {
                    Orbwalker.OrbwalkTo(Game.CursorPos);

                    if (Poro.IsReady() && !ppred.Collision && ppred.HitChancePercent >= slider(menu, "PRED"))
                    {
                        Poro.Cast(ppred.CastPosition);
                    }
                    else if (PoroDash.IsReady())
                    {
                        PoroDash.Cast();
                    }
                }
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && check(menu, "HARASS"))
                {
                    if (Poro.IsReady() && !ppred.Collision && ppred.HitChancePercent >= slider(menu, "PRED") && Poro.Name == "SummonerPoroThrow")
                    {
                        Poro.Cast(ppred.CastPosition);
                    }
                }                
            }           
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;     
            
            if (check(menu, "DRAW"))
            {
                Circle.Draw(SharpDX.Color.SkyBlue, Poro.Range, myhero.Position);
            }
        }

        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 " + ChampionName, ChampionName.ToLower());

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date);
            menu.AddLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.Add("DASHKEY", new KeyBind("Throw + Dash",false,KeyBind.BindTypes.HoldActive,'G'));
            menu.AddSeparator();          
            menu.Add("PRED", new Slider("Poro Hitchance %", 85, 1, 100));
            menu.AddSeparator();
            menu.Add("HARASS", new CheckBox("Throw Poros On Harass Mode", false));
            menu.Add("DRAW", new CheckBox("Draw Throwing Range"));           
            menu.AddSeparator();
            menu.AddLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");
        }
    }
}
