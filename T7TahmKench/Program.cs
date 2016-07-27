using System;
using System.Linq;
using System.Collections;
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

namespace T7_TahmKench
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, misc, draw, pred, jungleclear;
        private static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 550);
        static readonly string ChampionName = "TahmKench";
        static readonly string Version = "1.1";
        static readonly string Date = "27/7/16";
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#A16850'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#3737E6'>Toyota</font><font color='#D61EBE'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;         
            Game.OnTick += OnTick;
            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            RPotion = new Item((int)ItemId.Refillable_Potion);
            Player.LevelSpell(SpellSlot.Q);
            DatMenu();
            
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo(); 

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > harass["HMIN"].Cast<Slider>().CurrentValue) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > laneclear["LMIN"].Cast<Slider>().CurrentValue) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > jungleclear["JMIN"].Cast<Slider>().CurrentValue) Jungleclear();

            Misc();
        }

        private static bool check(Menu submenu, string sig)
        {
            return submenu[sig].Cast<CheckBox>().CurrentValue;
        }

        private static int slider(Menu submenu, string sig)
        {
            return submenu[sig].Cast<Slider>().CurrentValue;
        }

        private static int comb(Menu submenu, string sig)
        {
            return submenu[sig].Cast<ComboBox>().CurrentValue;
        }

        private static bool key(Menu submenu, string sig)
        {
            return submenu[sig].Cast<KeyBind>().CurrentValue;
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            var U = SpellSlot.Unknown;
            var Q = SpellSlot.Q;
            var W = SpellSlot.W;
            var E = SpellSlot.E;
            var R = SpellSlot.R;

            /*Q>W>E*/
            SpellSlot[] sequence1 = { U, W, E, Q, Q, R, Q, W, Q, W, R, W, W, E, E, R, E, E, U };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }       

        private static float QDamage(AIHeroClient target)
        {
            int index = DemSpells.Q.Level - 1;

            var QDamage = new[] { 80, 130, 180, 230, 280 }[index] + 
                          (0.7f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, QDamage);
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);
                      
            if (target != null && !target.IsInvulnerable && check(combo, "FOCUS"))
            {                       
                var Qpred = DemSpells.Q.GetPrediction(target);

                if (check(combo, "CQ") && DemSpells.Q.CanCast(target))
                {
                    switch(check(combo, "CQSTUN"))
                    {
                        case true:
                            if (target.GetStacks() >= 3 && !Qpred.Collision && Qpred.HitChancePercent >= slider(pred, "QPred"))
                            {
                                DemSpells.Q.Cast(Qpred.CastPosition);
                            }
                            break;
                        case false:
                            if (!Qpred.Collision && Qpred.HitChancePercent >= slider(pred, "QPred"))
                            {
                                DemSpells.Q.Cast(Qpred.CastPosition);
                            }
                            break;
                    }
                }

                if (check(combo, "CW") && check(combo, "CAUTOW") && !myhero.HasWBuff() && target.GetStacks() >= 3 && DemSpells.W1.CanCast(target))
                {
                    DemSpells.W1.Cast(target);
                }                
            }
            else if (!check(combo, "FOCUS"))
            {
                var enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range)).OrderBy(x => x.Distance(myhero.Position));

                if (enemies != null)
                {
                    foreach( var enemy in enemies)
                    {
                        var Qpred = DemSpells.Q.GetPrediction(enemy);

                        if (check(combo, "CQ") && DemSpells.Q.CanCast(enemy))
                        {
                            switch (check(combo, "CQSTUN"))
                            {
                                case true:
                                    if (enemy.GetStacks() >= 3 && !Qpred.Collision && Qpred.HitChancePercent >= slider(pred, "QPred"))
                                    {
                                        DemSpells.Q.Cast(Qpred.CastPosition);
                                    }
                                    break;
                                case false:
                                    if (!Qpred.Collision && Qpred.HitChancePercent >= slider(pred, "QPred"))
                                    {
                                        DemSpells.Q.Cast(Qpred.CastPosition);
                                    }
                                    break;
                            }
                        }

                        if (check(combo, "CW") && check(combo, "CAUTOW") && !myhero.HasWBuff() && enemy.GetStacks() >= 3 && DemSpells.W1.CanCast(enemy))
                        {
                            DemSpells.W1.Cast(enemy);
                        } 
                    }
                }
            }                    
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, 900).ToList();

            if (target != null && !target.IsInvulnerable)
            {               
                if (check(harass, "HW") && myhero.HasEatenMinion())
                {
                    switch (myhero.CountEnemiesInRange(DemSpells.W2.Range) >= 1)
                    {
                        case true:
                            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.W2.Range)))
                            {
                                DemSpells.W2.Cast(enemy.Position);
                            }
                            break;
                        case false:
                            break;
                    }
                }

                if (check(harass, "HQ") && check(harass, "HW") && DemSpells.Q.IsReady() && (DemSpells.W1.IsReady() || DemSpells.W2.IsReady()) &&
                    target.IsValidTarget(DemSpells.W2.Range))
                {                    
                    foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range)))
                    {
                        switch(DemSpells.W1.IsInRange(minion))
                        {
                            case true:
                                if (DemSpells.W1.Cast(minion))
                                {
                                    return;
                                }
                                break;
                            case false:
                                if (check(harass, "HQW") && DemSpells.Q.CanCast(minion) && DemSpells.Q.Cast(minion.Position))
                                {
                                    Core.DelayAction(() => DemSpells.WGRAB.Cast(), 100);
                                }
                                
                                break;
                        }
                    }
                }
                else if (check(harass, "HQ") && DemSpells.Q.CanCast(target))
                {
                    var qpred = DemSpells.Q.GetPrediction(target);

                    if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q.Cast(qpred.CastPosition);
                }
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, 900).ToList();

            if (minions != null)
            {
                if (check(laneclear, "LW") && myhero.HasEatenMinion())
                {
                    switch(myhero.CountEnemyMinionsInRange(DemSpells.W2.Range) >= 1)
                    {
                        case true:
                            foreach(var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.W2.Range) && x.Health > 20))
                            {
                                DemSpells.W2.Cast(minion.Position); 
                            }
                            break;
                        case false:
                            break;
                    }
                }

                if (check(laneclear, "LQ") && DemSpells.Q.IsReady())
                {
                    foreach(var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 45))
                    {
                        var Qpred = DemSpells.Q.GetPrediction(minion);
                        if (DemSpells.Q.CanCast(minion)) DemSpells.Q.Cast(Qpred.CastPosition); 
                    }
                }
                if (check(laneclear, "LW") && DemSpells.W1.IsReady())
                {
                    var targets = minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.W2.Range) && x.Health > 30)
                                         .OrderBy(x => x.Distance(myhero.Position));

                    if (targets.Count() >= 2 && DemSpells.W1.CanCast(targets.FirstOrDefault()))
                    {
                        DemSpells.W1.Cast(targets.FirstOrDefault());
                    }  
                }
            }
        }

        private static void Jungleclear()
        {
            var monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, DemSpells.Q.Range);

            if (monsters != null)
            {
                if (check(jungleclear, "JW") && myhero.HasEatenMinion())
                {
                    foreach (var monster in monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.W2.Range) && x.Health > 20))
                    {
                        var wpred = DemSpells.W2.GetPrediction(monster);
                        DemSpells.W2.Cast(wpred.CastPosition);
                    }
                }

                if (check(jungleclear, "JQ") && DemSpells.Q.IsReady())
                {
                    foreach (var monster in monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 50))
                    {
                        var Qpred = DemSpells.Q.GetPrediction(monster);
                        if (DemSpells.Q.CanCast(monster)) DemSpells.Q.Cast(Qpred.CastPosition);
                    }
                }

                if (check(jungleclear, "JW") && DemSpells.W1.IsReady())
                {
                    var CheckRange = myhero.HasWBuff() ? DemSpells.W2.Range : DemSpells.W1.Range;

                    var targets = monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.W2.Range) && x.Health > 30)
                                         .OrderBy(x => x.Distance(myhero.Position));

                    if (targets.Count() >= 2 && DemSpells.W1.CanCast(targets.FirstOrDefault()))
                    {
                        DemSpells.W1.Cast(targets.FirstOrDefault());
                    }                                                         
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                var Qpred = DemSpells.Q.GetPrediction(target);

                if (check(misc, "ksQ") && QDamage(target) > target.Health && DemSpells.Q.CanCast(target) && !Qpred.Collision &&
                    Qpred.HitChancePercent >= slider(pred, "QPred"))
                {
                    DemSpells.Q.Cast(Qpred.CastPosition);
                }

                if (check(misc, "autoign") && ignt.IsReady() && target.IsValidTarget(ignt.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);                  
                }
            }

            if (check(misc, "WSAVE") && !myhero.HasWBuff() && DemSpells.W1.IsReady())
            {
                var allies = EntityManager.Heroes.Allies.Where(x => !x.IsDead && x.IsAlly && !x.IsMe && x.Distance(myhero.Position) < DemSpells.W1.Range);
                var enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsEnemy && x.Distance(myhero.Position) < 1500);

                if (allies.Any() && enemies.Any())
                {
                    var ClosestAlly = allies.OrderBy(x => x.Distance(myhero.Position)).FirstOrDefault();
                    var ClosestEnemy = enemies.OrderBy(x => x.Distance(ClosestAlly.Position)).FirstOrDefault();

                    if (ClosestAlly != null && ClosestEnemy != null)
                    {
                        switch (myhero.CountAlliesInRange(DemSpells.W1.Range) > 1)
                        {
                            case false:
                                if (ClosestAlly.Distance(myhero.Position) <= DemSpells.W1.Range && ClosestAlly.CountEnemiesInRange(800) >= slider(misc, "WMINE") &&
                                    ClosestAlly.HealthPercent <= slider(misc, "WMINH") &&
                                    DemSpells.W1.CanCast(ClosestAlly))
                                {
                                    DemSpells.W1.Cast(ClosestAlly);
                                }
                                break;
                            case true:
                                foreach (var ally in allies)
                                {
                                    if (ally.Distance(myhero.Position) <= DemSpells.W1.Range && ally.CountEnemiesInRange(800) >= slider(misc, "WMINE") &&
                                    ally.HealthPercent <= slider(misc, "WMINH") &&
                                    DemSpells.W1.CanCast(ally))
                                    {
                                        DemSpells.W1.Cast(ally);
                                    }
                                }
                                break;
                        }
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

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.Q.Range, myhero.Position); }

            }

            if (check(draw, "drawW") && DemSpells.W1.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.W1.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, myhero.HasBuff("tahmkenchwhasdevouredtarget") ? DemSpells.W2.Range : 330, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, myhero.HasBuff("tahmkenchwhasdevouredtarget") ? DemSpells.W2.Range : 330, myhero.Position); }

            }
        }
        
        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 " + ChampionName, ChampionName.ToLower());
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            jungleclear = menu.AddSubMenu("Jungleclear", "jclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date);
            menu.AddLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
            combo.Add("CQSTUN", new CheckBox("Use Q Only To Stun", true));
            combo.AddSeparator();
            combo.Add("CW", new CheckBox("Use W", true));
            combo.Add("CAUTOW", new CheckBox("Auto W on Target", false));
            combo.AddSeparator();
            combo.Add("FOCUS", new CheckBox("Focus Spells On Target", false));


            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", true));
            harass.Add("HW", new CheckBox("Use W", true));
            harass.Add("HQW", new CheckBox("Use QW To Catch Minions", true));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", true));
            laneclear.AddSeparator();
            laneclear.Add("LW", new CheckBox("Use W", true));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", true));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W", true));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 50, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawW", new CheckBox("Draw W Range", true));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));

            misc.AddLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));
            misc.AddSeparator();
            misc.AddGroupLabel("Auto W");
            misc.Add("WSAVE", new CheckBox("Save Allies With W", true));
            misc.AddLabel("If");
            misc.Add("WMINH", new Slider("Max Ally Health %", 15, 1, 100));
            misc.AddLabel("And");
            misc.Add("WMINE", new Slider("Min Enemies Close To Ally", 1, 1, 5));
            misc.AddSeparator();
            misc.AddSeparator();
            misc.AddLabel("Auto Potion");
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion", true));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack", true));
            misc.Add("skinID", new ComboBox("Skin Hack", 2, "Default", "Master Chef", "Urf"));

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("Q :");
            pred.Add("QPred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("W :");
            pred.Add("WPred", new Slider("Select % Hitchance", 90, 1, 100));
        }      
    }

    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Active WGRAB { get; private set; }
        public static Spell.Targeted W1 { get; private set; }
        public static Spell.Skillshot W2 { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 800, SkillShotType.Linear, 100, 2000, 70);
            WGRAB = new Spell.Active(SpellSlot.W);
            W1 = new Spell.Targeted(SpellSlot.W, 330);
            W2 = new Spell.Skillshot(SpellSlot.W, 650, SkillShotType.Linear, 100, 900, 75);
        }
    }

    public static class Extensions
    {
        public static bool HasWBuff(this AIHeroClient hero)
        {
            return hero.HasBuff("tahmkenchwhasdevouredtarget");
        }

        public static int GetStacks(this AIHeroClient target)
        {
            if (target.HasBuff("tahmkenchpdebuffcounter"))
            { return target.GetBuffCount("tahmkenchpdebuffcounter"); }

            return 0;
        }

        public static bool HasEatenMinion(this AIHeroClient hero)
        {
            return ObjectManager.Get<Obj_AI_Minion>().Where(x => x.HasBuff("tahmkenchwhasdevouredtarget") && x.Distance(hero.Position) < 100).Count() >= 1 && hero.HasWBuff();
        }
    }
}
