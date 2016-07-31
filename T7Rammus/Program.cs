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

namespace T7_Rammus
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;
        private static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 550);
        static readonly string ChampionName = "Rammus";
        static readonly string Version = "1.0";
        static readonly string Date = "31/7/16";
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#A39E12'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;         
            Game.OnTick += OnTick;
            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            RPotion = new Item((int)ItemId.Refillable_Potion);
            Player.LevelSpell(SpellSlot.W);
            DatMenu();
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) { Combo(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > slider(harass, "HMIN")) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > slider(laneclear, "LMIN")) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")) Jungleclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                if (DemSpells.Q.IsReady() && Player.Instance.Spellbook.GetSpell(SpellSlot.Q).SData.ManaCostArray[DemSpells.Q.Level] <= myhero.Mana && !QBuff())
                {
                    DemSpells.Q.Cast();
                    return;
                }
            }

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

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            var U = SpellSlot.Unknown;
            var Q = SpellSlot.Q;
            var W = SpellSlot.W;
            var E = SpellSlot.E;
            var R = SpellSlot.R;

            /*W>E>Q*/
            SpellSlot[] sequence1 = { U, E, Q, W, W, R, W, E, W, E, R, E, E, Q, Q, R, Q, Q, U };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static bool QBuff()
        {
            return myhero.HasBuff("PowerBall");
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                if (check(combo, "CQ") && DemSpells.Q.IsReady() && myhero.CountEnemiesInRange(1200) >= 1 && !QBuff())
                {
                    DemSpells.Q.Cast();
                    return;
                }

                if (check(combo, "CE") && DemSpells.E.IsReady() && myhero.CountEnemiesInRange(DemSpells.E.Range) > 1)
                {                    
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range))
                                                                      .OrderByDescending(x => TargetSelector.GetPriority(x)))
                    {
                        if (check(combo, "CE" + enemy.ChampionName))
                        {
                            DemSpells.E.Cast(enemy);
                            return;
                        }
                    }                          
                }

                if (check(combo, "CR") && DemSpells.R.IsReady() && myhero.CountEnemiesInRange(DemSpells.R.Range) >= slider(combo, "CRMINE") &&
                   !myhero.IsFleeing && DemSpells.R.Cast())
                {
                    return;
                }
            }            
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (myhero.HasBuff("PowerBall"))

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                if (check(harass, "HQ") && DemSpells.Q.IsReady() && myhero.CountEnemiesInRange(700) >= 1 && !QBuff())
                {
                    DemSpells.Q.Cast();
                }

                if (check(harass, "HE") && DemSpells.E.IsReady())
                {                   
                    foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range)))
                    {
                        if (check(harass, "HE" + enemy.ChampionName)) DemSpells.E.Cast(enemy);
                    }                                                    
                }
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range).ToList();

            if (minions != null)
            {

                if (check(laneclear, "LQ") && DemSpells.Q.IsReady() && !QBuff())
                {
                    foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(600) && x.Health > 50))
                    {
                        DemSpells.Q.Cast();
                    }
                }

                if (check(laneclear, "LE") && DemSpells.E.IsReady())
                {
                    switch (comb(laneclear, "LEMODE"))
                    {
                        case 0:
                            foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range) && x.Health > 50 &&
                                                                       x.Name.ToLower().Contains("siege") && x.Name.ToLower().Contains("super")))
                            {
                                DemSpells.E.Cast(minion);
                            }
                            break;
                        case 1:
                            foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range) && x.Health > 50))
                            {
                                DemSpells.E.Cast(minion);
                            }
                            break;
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 600);

            if (Monsters != null)
            {

                if (check(jungleclear, "JQ") && DemSpells.Q.IsReady() && !QBuff())
                {
                    foreach(var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(600) && x.Health > 50))
                    {
                        DemSpells.Q.Cast();
                    }
                }

                if (check(jungleclear, "JE") && DemSpells.E.IsReady())
                {                   
                    switch(comb(jungleclear, "JEMODE"))
                    { 
                        case 0:
                            foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range) && x.Health > 50 &&
                                                                    !x.Name.ToLower().Contains("mini")))
                            {
                                DemSpells.E.Cast(monster);
                            }
                            break;
                        case 1:
                            foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range) && x.Health > 50))
                            {
                                DemSpells.E.Cast(monster);
                            }
                            break;
                    }
                }
            }
        }

        private static void Misc()
        {                
            if (check(misc, "W") && DemSpells.W.IsReady())
            {
                if (check(misc, "WCOMBO") && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) return;

                var Enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValid() && x.Distance(myhero.Position) < 500 && !x.IsFleeing);

                if (Enemies != null && Enemies.Count() >= slider(misc, "WMINE") && myhero.HealthPercent <= slider(misc, "WMINH"))
                {
                    DemSpells.W.Cast();
                }
            }

            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);  

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                if (ignt != null && check(misc, "autoign") && ignt.IsReady() && target.IsValidTarget(ignt.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;

            if (check(draw, "drawE") && DemSpells.E.IsLearned)
            {
                switch(check(draw, "drawonlyrdy"))
                {
                    case true:
                        Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position);
                        break;
                    case false:
                        Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position);
                        break;
                }
            }

            if (check(draw, "DRAWTIME") && QBuff())
            {               
                var endTime = Math.Max(0, myhero.Buffs.Where(x => x.IsActive && x.Name.ToLower().Equals("powerball")).FirstOrDefault().EndTime - Game.Time);

                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X,
                                        Drawing.WorldToScreen(myhero.Position).Y - 30,
                                        Color.Green, "Time: " + Convert.ToString(endTime, CultureInfo.InvariantCulture));              
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

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date);
            menu.AddLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
            combo.AddSeparator();
            combo.Add("CE", new CheckBox("Use E", true));
            combo.AddSeparator();
            combo.AddLabel("Use E On:");
            foreach (AIHeroClient Enemy in EntityManager.Heroes.Enemies)
            {
                combo.Add("CE" + Enemy.ChampionName, new CheckBox(Enemy.ChampionName, true));
            }
            combo.AddSeparator();
            combo.AddSeparator();                      
            combo.Add("CR", new CheckBox("Use R", true));
            combo.Add("CRMINE", new Slider("Min Enemies In Range", 2, 1, 5));
                      
            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", false));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use E", true));
            harass.AddLabel("Use E On:");
            foreach (AIHeroClient Enemy in EntityManager.Heroes.Enemies)
            {
                harass.Add("HE" + Enemy.ChampionName, new CheckBox(Enemy.ChampionName, true));
            }
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", false));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", false));
            laneclear.Add("LEMODE", new ComboBox("E Mode", 0, "Big Minions", "All Minions"));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E", false));
            jungleclear.Add("JEMODE", new ComboBox("E Mode", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 50, 0, 100));

            draw.Add("drawE", new CheckBox("Draw E Range", true));
            draw.AddSeparator();
            draw.Add("DRAWTIME", new CheckBox("Draw Remaining Q Time", true));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));

            misc.AddLabel("W Usage");
            misc.Add("W", new CheckBox("Auto W", true));
            misc.Add("WCOMBO", new CheckBox("Only Auto W In Combo Mode", false));
            misc.AddLabel("If");
            misc.Add("WMINE", new Slider("Min Enemies In Range", 2, 1, 5));
            misc.AddLabel("And");
            misc.Add("WMINH", new Slider("Max Health %", 100, 1, 100));            
            misc.AddSeparator();
            misc.AddLabel("_____________________________________________________________________________");
            misc.AddSeparator();
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));
            misc.AddSeparator();
            misc.Add("FLEE", new CheckBox("Use Q To Flee", true));
            misc.AddSeparator();
            misc.AddLabel("Auto Potion");
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion", true));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack", false));
            misc.Add("skinID", new ComboBox("Skin Hack", 5, "Default", "King", "Chrome", "Molten", "Freljord", "Ninja", "Full Metal", "Guardian Of The Sands"));
        }

    }

    public static class DemSpells
    {
        public static Spell.Active Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Targeted E { get; private set; }
        public static Spell.Active R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 325);
            R = new Spell.Active(SpellSlot.R, 300);
        }
    }
}
