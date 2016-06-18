using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7_Kalista
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        private static AIHeroClient myhero { get { return ObjectManager.Player; } }
        public static Menu menu, combo, harass, laneclear, misc, draw;
        private static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 550);

        private static void OnLoad(EventArgs args)
        {
            DemSpells.Q.AllowedCollisionCount = 1;
            if (Player.Instance.ChampionName != "Kalista") { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#009DFF'> Kalista</font> : Loaded!(v1.0)");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            // Game.OnUpdate += OnUpdate;
            // Gapcloser.OnGapcloser += OnGapcloser
            DatMenu();
            Game.OnTick += OnTick;
            Player.LevelSpell(SpellSlot.E);
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) { Combo(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > harass["HMIN"].Cast<Slider>().CurrentValue) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > laneclear["LMIN"].Cast<Slider>().CurrentValue) Laneclear();

     //       if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.HealthPercent > jungleclear["jminhealth"].Cast<Slider>().CurrentValue) Jungleclear();
            
            Misc();
        }

        public static bool check(Menu submenu, string sig)
        {
            return submenu[sig].Cast<CheckBox>().CurrentValue;
        }

        public static int slider(Menu submenu, string sig)
        {
            return submenu[sig].Cast<Slider>().CurrentValue;
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            /*E>Q>W*/
            SpellSlot[] sequence1 = { SpellSlot.Unknown, SpellSlot.Q, SpellSlot.W, SpellSlot.E,
                                        SpellSlot.E, SpellSlot.R, SpellSlot.E, SpellSlot.Q, 
                                        SpellSlot.E, SpellSlot.Q, SpellSlot.R, SpellSlot.Q, 
                                        SpellSlot.Q, SpellSlot.W, SpellSlot.W, SpellSlot.R, 
                                        SpellSlot.W , SpellSlot.W };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += QDamage(target); }
                if (DemSpells.E.IsLearned && DemSpells.E.IsReady()) { TotalDamage += EDamage(target); }

                return TotalDamage;
            }
            return 0;
        }

        private static int GetStacks(AIHeroClient target)
        {
            int stacks = 0;
           
            if (target.HasBuff("kalistaexpungemarker"))
            {
                foreach (var rendbuff in target.Buffs.Where(x => x.Name.ToLower().Contains("kalistaexpungemarker")))
                {
                    stacks = rendbuff.Count;
                }
            }
            else
            {
                return 0;
            }
            return stacks;
        }

        private static int GetMinionStacks(Obj_AI_Base minion)
        {
            int stacks = 0;
            foreach (var rendbuff in minion.Buffs.Where(x => x.Name.ToLower().Contains("kalistaexpungemarker")))
            {
                stacks = rendbuff.Count;
            }

            if (stacks == 0 || !minion.HasBuff("kalistaexpungemarker")) return 0;
            return stacks;
        }

        private static float RendTime(AIHeroClient target)
        {
            float endTime = 0;
            foreach (var buff in target.Buffs.Where(x => x.IsValid() && x.Name.ToLower().Contains("kalistaexpungemarker")))
            {
                endTime = Math.Max(0, buff.EndTime - Game.Time);           
            }
            return endTime;
        }

        private static float QDamage(AIHeroClient target)
        {
            var QDamage = new[] {0, 10, 70, 130, 190, 250 }[DemSpells.Q.Level] + myhero.TotalAttackDamage;
            return myhero.CalculateDamageOnUnit(target, DamageType.Physical, QDamage);
        }

        private static float EDamage(AIHeroClient target)
        {
            int stacks = GetStacks(target);
            var EDamage = new[] { 0, 20, 30, 40, 50, 60 }[DemSpells.E.Level] + (0.6 * myhero.TotalAttackDamage);

            if(stacks > 1)
            {
                EDamage += ((new[] { 0, 10, 14, 19, 25, 32 }[DemSpells.E.Level] + (new[] { 0, 0.2, 0.225, 0.25, 0.275, 0.3 }[DemSpells.E.Level] * myhero.TotalAttackDamage)) * (stacks - 1));
            }

            return myhero.CalculateDamageOnUnit(target, DamageType.Physical, (float)EDamage);
        }

        private static float EMinionDamage(Obj_AI_Base minion)
        {
            int stacks = GetMinionStacks(minion);
            var EDamage = new[] { 0, 20, 30, 40, 50, 60 }[DemSpells.E.Level] + (0.6 * myhero.TotalAttackDamage);

            if (stacks > 1)
            {
                EDamage += ((new[] { 0, 10, 14, 19, 25, 32 }[DemSpells.E.Level] + (new[] { 0, 0.2, 0.225, 0.25, 0.275, 0.3 }[DemSpells.E.Level] * myhero.TotalAttackDamage)) * (stacks - 1));
            }

            return myhero.CalculateDamageOnUnit(minion, DamageType.Physical, (float)EDamage);
        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);
            Chat.Print(EDamage(target) + " " + target.Health);

            if (target != null)
            {
                var QPred = DemSpells.Q.GetPrediction(target);
                if (check(combo, "CQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target.Position) &&
                    QPred.HitChancePercent >= misc["QPred"].Cast<Slider>().CurrentValue)
                {
                    DemSpells.Q.Cast(QPred.CastPosition);
                }

                if (check(combo, "CE") && DemSpells.E.IsLearned && DemSpells.E.IsReady() && DemSpells.E.IsInRange(target.Position))
                {
                    if (GetStacks(target) >= slider(combo, "CEMIN"))
                    {
                        if (check(combo, "AUTOE") && target.Distance(myhero, true) > (DemSpells.E.Range * 0.8).Pow())
                        {
                            DemSpells.E.Cast();
                        }
                    }

                    if (!check(misc, "ksE") && EDamage(target) > (target.Health + 10))
                    {
                        DemSpells.E.Cast();
                    }
                }

                if (check(combo, "Cignt") && ignt.IsReady() && target.Health > ComboDamage(target) && ignt.IsInRange(target.Position) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null)
            {
                var QPred = DemSpells.Q.GetPrediction(target);

                if (check(harass, "HQ") && DemSpells.Q.IsReady() && target.IsValidTarget() && DemSpells.Q.IsInRange(target.Position) &&
                    slider(misc, "QPred") >= QPred.HitChancePercent)
                {
                    DemSpells.Q.Cast(QPred.CastPosition);
                }

                if (check(harass, "HE") && DemSpells.E.IsReady() && GetStacks(target) >= slider(harass, "EMIN") &&
                    DemSpells.E.IsInRange(target.Position))
                {
                    DemSpells.E.Cast();
                }
            }

        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range);

            if (minions != null)
            {
                var qpred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, DemSpells.Q.Width, (int)DemSpells.Q.Range);

                if (check(laneclear, "LQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() &&
                    qpred.HitNumber > 0)
                {
                    DemSpells.Q.Cast(qpred.CastPosition);
                }

                if (check(laneclear, "LE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    foreach (var minion in minions.Where(x => x.Distance(myhero) < DemSpells.E.Range))
                    {
                        if (slider(laneclear, "LEMIN") <= GetMinionStacks(minion))
                        {
                            if (check(laneclear, "LEONLY") && EMinionDamage(minion) > (minion.Health + 10))
                            {
                                DemSpells.E.Cast();
                            }
                            else
                            {
                                DemSpells.E.Cast();
                            }             
                        }
                    }
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);
            

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);            
            
            if (target != null)
            {
                var qpred = DemSpells.Q.GetPrediction(target);

                if (check(misc, "ksE") && EDamage(target) > (target.Health + 10) &&
                    DemSpells.E.IsInRange(target) && DemSpells.E.IsReady() &&
                    !target.IsInvulnerable)
                {
                    DemSpells.E.Cast();
                }
                
               if (check(misc, "ksQ") && QDamage(target) > (target.Health + 20) &&
                   (target.Distance(myhero.Position) + 10) < DemSpells.Q.Range && DemSpells.Q.IsReady() && !target.IsInvulnerable &&
                    slider(misc, "QPred") <= qpred.HitChancePercent)
                {
                    DemSpells.Q.Cast(qpred.CastPosition);
                }

               if (RendTime(target) < 0.3 && check(misc, "AUTOE2") && target.Distance(myhero.Position) < DemSpells.E.Range)
               {
                   DemSpells.E.Cast();
               }

                if (check(misc, "autoign") && ignt.IsReady() &&
                    ignt.IsInRange(target) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }

            if (check(misc, "ksD") && DemSpells.E.IsReady())
            {
                foreach (var monster in EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1000f).Where(x => x.Name.ToLower().Contains("dragon")))
                {
                    if (monster.IsValidTarget() && EMinionDamage(monster) > monster.Health && DemSpells.E.IsInRange(monster.Position))
                    {
                        DemSpells.E.Cast();
                    }
                }
            }
        }
        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.Q.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.SkyBlue, DemSpells.W.Range, myhero.Position); }

            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.SkyBlue, DemSpells.E.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.SkyBlue, DemSpells.E.Range, myhero.Position); }

            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.SkyBlue, DemSpells.R.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.SkyBlue, DemSpells.R.Range, myhero.Position); }

            }

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (check(draw, "drawkillable") && !check(draw, "nodraw") && enemy.IsVisible &&
                    enemy.IsHPBarRendered && !enemy.IsDead && ComboDamage(enemy) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X,
                                     Drawing.WorldToScreen(enemy.Position).Y - 30,
                                     Color.Green, "Killable With Combo");
                }
                else if (check(draw, "drawkillable") && !check(draw, "nodraw") && enemy.IsVisible &&
                         enemy.IsHPBarRendered && !enemy.IsDead && ignt.IsReady() &&
                         ComboDamage(enemy) + myhero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) > enemy.Health )
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, Color.Green, "Combo + Ignite");
                }
            }
        }

        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 Kalista", "kalista");
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");

            menu.AddGroupLabel("Welcome to T7 Kalista And Thank You For Using!");
            menu.AddGroupLabel("Version 1.0 18/6/2016");
            menu.AddGroupLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddGroupLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddGroupLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
          //  combo.Add("CQAA", new CheckBox("Only Use Q After Auto Attack", true));
            combo.AddSeparator();
            combo.Add("CE", new CheckBox("Use E", false));
            combo.Add("AUTOE", new CheckBox("Auto Cast E When Target Is Escaping", false)); 
            combo.Add("CEMIN", new Slider("Min Stacks To Use E", 10, 1, 50));
            combo.AddSeparator();
            combo.Add("Cignt", new CheckBox("Use Ignite", false));
           // combo.Add("CORB", new CheckBox("Orbwalk On Minions To Gapclose", true));
            
            harass.AddGroupLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", true));
            harass.Add("HE", new CheckBox("Use E", true));
            harass.Add("EMIN", new Slider("Min Stacks To Use E", 5, 1, 20));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", true));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", true));
            laneclear.Add("LEONLY", new CheckBox("Only Use E On Killable Minions", true));
            laneclear.Add("LEMIN", new Slider("Min Stacks To Use E", 3, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawE", new CheckBox("Draw E Range", true));
            draw.Add("drawR", new CheckBox("Draw R Range", true));
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies", false));
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
       
            misc.AddGroupLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            misc.Add("ksE", new CheckBox("Killsteal with E", true));
            misc.Add("ksD", new CheckBox("Steal Dragon With E", true));
            misc.Add("AUTOE2", new CheckBox("Auto Cast E When Running Out Of time", true)); 
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));         
            misc.AddSeparator();
            misc.AddGroupLabel("Prediction");
            misc.AddGroupLabel("Q :");
            misc.Add("QPred", new Slider("Select % Hitchance", 80, 1, 100));
            misc.AddSeparator();
            misc.AddGroupLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.AddSeparator();
            misc.AddGroupLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack",true));
            misc.Add("skinID", new ComboBox("Skin Hack", 2, "Default", "Blood Moon", "Championship"));
        }
    }
    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Targeted W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Active R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1150, SkillShotType.Linear, 250, 2100, 40);
            W = new Spell.Targeted(SpellSlot.W, 5000);
            E = new Spell.Active(SpellSlot.E, 1000);
            R = new Spell.Active(SpellSlot.R, 1100);
        }
    }
}
