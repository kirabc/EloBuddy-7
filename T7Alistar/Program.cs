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

namespace T7_Alistar
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, misc, draw;
        private static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 600);
        private static Spell.Targeted flash = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerflash"),400);
        static readonly string ChampionName = "Alistar";
        public static Item potion { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#346FC2'> " + ChampionName + "</font> : Loaded!(v1.0)");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp; 
            Gapcloser.OnGapcloser += OnGapcloser;
            Interrupter.OnInterruptableSpell += OnInterrupt;
            Game.OnTick += OnTick;
            potion = new Item((int)ItemId.Health_Potion);
            Player.LevelSpell(SpellSlot.Q);    
            AttackableUnit.OnDamage += OnDamage;
            DatMenu();
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) { Combo(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > harass["HMIN"].Cast<Slider>().CurrentValue) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > laneclear["LMIN"].Cast<Slider>().CurrentValue) Laneclear();

            Misc();
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            /*>>*/
            SpellSlot[] sequence1 = { SpellSlot.Unknown, SpellSlot.Unknown, SpellSlot.W, SpellSlot.E,
                                        SpellSlot.Q, SpellSlot.Q, SpellSlot.R, SpellSlot.Q, 
                                        SpellSlot.W, SpellSlot.Q, SpellSlot.W, SpellSlot.R, 
                                        SpellSlot.W, SpellSlot.W, SpellSlot.E, SpellSlot.E, 
                                        SpellSlot.R, SpellSlot.E, SpellSlot.E };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        #region Extensions
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

    /*    private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += QDamage(target); }

                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { TotalDamage += WDamage(target); }

                if (Item1.IsOwned() && Item1.IsReady() && Item1.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, Item1.Id); }

                return TotalDamage;
            }
            return 0;
        }*/

        private static float QDamage(AIHeroClient target)
        {
            var QDamage = new[] {0, 60, 105, 150, 195, 240}[DemSpells.Q.Level] +
                          (0.5f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, QDamage);
        }

        private static float WDamage(AIHeroClient target)
        {
            var WDamage = new[] {0, 55, 110, 165, 220, 275}[DemSpells.W.Level] +
                          (0.7f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, WDamage);
        }

        private static int WQcost()
        {
            int QCost = new int[] { 0, 65, 70, 75, 80, 85 }[DemSpells.Q.Level];
            int WCost = new int[] { 0, 65, 70, 75, 80, 85 }[DemSpells.W.Level];

            return QCost + WCost;
        }
       
        private static void WQinsec(AIHeroClient target)
        {
            var MaxRange = flash.Range + DemSpells.W.Range - 5;

            var delay = Math.Max(0, target.Distance(myhero.Position) - 365) / 1.2f - 25; 

            if (myhero.Mana < WQcost()) return;

            if(flash.Cast(myhero.Position.Extend(target.Position, flash.Range).To3D()))
            {
                Core.DelayAction(delegate 
                {
                    if(DemSpells.W.Cast(target))
                    {
                        Core.DelayAction(() => DemSpells.Q.Cast(), (int)(target.Distance(myhero.Position) / 1.2f));
                    }
                }, 50);
            }
        }

        #endregion Extensions

        #region Insec
        private static void Insec()
        {
            if (!key(misc, "INSECKEY")) return;

            var MaxRange = flash.Range + DemSpells.W.Range - 5;

           /* switch(comb(misc, "INSECMODE"))
            {
                case 0:
                    if(flash.IsReady() && DemSpells.W.IsReady() && DemSpells.Q.IsReady() &&
                       myhero.CountEnemiesInRange(1500) >= slider(misc, "AOEMIN"))
                    {
                        var Enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsEnemy && x.IsValidTarget(MaxRange));

                        if(Enemies != null)
                        {
                            if(Enemies.Count() == 1)
                            {
                                var enemy = Enemies.ToList().FirstOrDefault();

                                if(Extensions.CountEnemiesInRange(enemy,180) >= slider(misc, "AOEMIN"))
                                {
                                    WQinsec(enemy);
                                }
                            }
                            else if(Enemies.Count() > 1)
                            {
                                foreach(var enemy in Enemies.OrderBy(x => x.Distance(myhero.Position)))
                                {
                                    if(Extensions.CountEnemiesInRange(enemy,180) > slider(misc, "AOEMIN"))
                                    {
                                            WQinsec(enemy);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 1:*/
                    var ClosestTurret = EntityManager.Turrets.Allies.Where(x => x.Health > 50 && x.Distance(myhero.Position) < 1400).OrderBy(x => x.Distance(myhero.Position)).FirstOrDefault();

                    var ClosestEnemy = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.Health > 50 && x.IsValidTarget(flash.Range + 60)).OrderBy(x => x.Distance(myhero.Position)).FirstOrDefault();

                    if(flash.IsReady() && DemSpells.W.IsReady() && ClosestTurret != null && ClosestEnemy != null)
                    {
                         if(ClosestEnemy.Distance(ClosestTurret.Position) < 1400)
                         { 
                            var Distance = ClosestEnemy.Distance(ClosestTurret.Position);
     
                            var flashloc = ClosestTurret.Position.Extend(ClosestEnemy.Position, Distance + 60).To3D();

                            if(flash.Cast(flashloc))
                            {
                                Core.DelayAction( () => DemSpells.W.Cast(ClosestEnemy), 50);
                            }
                         }
                    }
                 //   break;
                    
            //}
        }
        #endregion Insec

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (EntityManager.Heroes.Allies.Count() > 0)
            {
               var ClosestAlly = EntityManager.Heroes.Allies.Where(x => !x.IsDead && x.IsAlly && x.Distance(target.Position) < 1200).OrderBy(x => x.Distance(target.Position)).FirstOrDefault();
            }

            if (target != null && !target.IsInvulnerable)
            {
                var delay = Math.Max(0, target.Distance(myhero.Position) - 365) / 1.2f - 25;
                var CurrentPos = target.Position;
                var KnockbackPos = myhero.Position.Extend(target.Position, 650);

                if (check(combo, "CQ") && check(combo, "CW") && (DemSpells.Q.IsLearned && DemSpells.W.IsLearned) && (DemSpells.Q.IsReady() && DemSpells.W.IsReady()) &&
                    target.IsValidTarget(DemSpells.W.Range) && myhero.Mana >= WQcost())
                {
                    if (DemSpells.Q.IsInRange(target.Position)) DemSpells.Q.Cast();

                    if (DemSpells.W.Cast(target))
                    {
                        Core.DelayAction(() => DemSpells.Q.Cast(), (int)delay);
                    }
                }
                else if (check(combo, "CQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && target.IsValidTarget(DemSpells.Q.Range) &&
                            myhero.CountEnemiesInRange(DemSpells.Q.Range) >= slider(combo, "CQMIN"))
                {
                    DemSpells.Q.Cast();
                }
                else if (check(combo, "CW") && DemSpells.W.IsLearned && DemSpells.W.IsReady() && target.IsValidTarget(DemSpells.W.Range) && EntityManager.Heroes.Allies.Count() > 0)
                {
                    var ClosestAlly = EntityManager.Heroes.Allies.Where(x => !x.IsDead && x.IsAlly && x.Distance(target.Position) < 1200).OrderBy(x => x.Distance(target.Position)).FirstOrDefault();

                    if (ClosestAlly.Distance(KnockbackPos.To3D()) < ClosestAlly.Distance(CurrentPos) ||
                            Extensions.CountAlliesInRange(KnockbackPos, 1000) > 0)
                    {
                        DemSpells.W.Cast(target);
                    }
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                if (check(harass, "HQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() &&
                    myhero.CountEnemiesInRange(DemSpells.Q.Range) > slider(harass, "HQMIN"))
                {
                    DemSpells.Q.Cast();
                }

                if (check(harass, "HW") && DemSpells.W.IsLearned && DemSpells.W.IsReady() &&
                    Extensions.CountEnemiesInRange(target.Position, 1000) <= slider(harass, "HWMAX"))
                {
                    DemSpells.W.Cast(target);
                }
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, 1000);

            if (minions != null)
            {
                if (check(laneclear, "LQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() &&
                    minions.Where(x => x.Distance(myhero.Position) < DemSpells.Q.Range).Count() >= slider(laneclear, "LQMIN"))
                {
                    DemSpells.Q.Cast();
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            var allies = EntityManager.Heroes.Allies.Where(x => !x.IsDead && x.IsAlly && !x.IsMe && x.Distance(myhero.Position) < DemSpells.E.Range);
            var enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsEnemy && x.Distance(myhero.Position) < 1500);

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {

                if (check(misc, "KSQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && target.IsValidTarget(DemSpells.Q.Range) &&
                    QDamage(target) > target.Health && !target.HasUndyingBuff() && !target.IsZombie && !target.IsInvulnerable)
                {
                    DemSpells.Q.Cast();
                }

                if (check(misc, "autoign") && ignt.IsReady() && target.IsValidTarget(ignt.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }

            if (check(misc, "AUTOPOT") && Item.HasItem(potion.Id) && Item.CanUseItem(potion.Id) && !myhero.HasBuff("RegenerationPotion") &&
                    myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                potion.Cast();
            }

            if (key(misc, "EKEY") && DemSpells.E.IsLearned && DemSpells.E.IsReady() && myhero.ManaPercent >= slider(misc, "EMIN"))
            {
                switch(comb(misc, "EMODE"))
                {
                    case 0:
                        if (myhero.HealthPercent <= slider(misc, "ESELF")) DemSpells.E.Cast();
                       //Chat.Print(myhero.HealthPercent + " " + slider(misc, "ESELF"));
                        break;
                    case 1:
                        if (allies.Where(x => x.HealthPercent <= slider(misc, "EALLY")).Count() > 0) DemSpells.E.Cast();
                        break;
                    case 2:
                        if (allies.Where(x => x.HealthPercent <= slider(misc, "EALLY")).Count() > 0 &&
                            myhero.HealthPercent <= slider(misc, "ESELF"))
                        {
                            DemSpells.E.Cast();
                        }
                        break;
                }
            }

            if(enemies != null)
            {
                if (key(misc, "RKEY") && comb(misc, "RMODE") == 0 && DemSpells.R.IsLearned && DemSpells.R.IsReady() && myhero.HealthPercent <= slider(misc, "RMINH") &&
                myhero.CountEnemiesInRange(1000) > 0)
                {
                    if (enemies.Where(x => x.Distance(myhero.Position) < 500).Count() >= slider(misc, "RMINE"))
                    {
                        DemSpells.R.Cast();
                    }
                }

                Insec();
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

            if (check(draw, "drawW") && DemSpells.W.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.W.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position); }

            }

            if (check(draw, "DRAWMODES"))
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50,
                                 Drawing.WorldToScreen(myhero.Position).Y + 10,
                                 Color.White,
                                 "Auto Healing: ");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X + 37,
                                 Drawing.WorldToScreen(myhero.Position).Y + 10,
                                 key(misc, "EKEY") ? Color.Green : Color.Red,
                                 key(misc, "EKEY") ? "ON" : "OFF");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50,
                                 Drawing.WorldToScreen(myhero.Position).Y + 25,
                                 Color.White,
                                 "Auto R: ");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X ,
                                 Drawing.WorldToScreen(myhero.Position).Y + 25,
                                 key(misc, "RKEY") ? Color.Green : Color.Red,
                                 key(misc, "RKEY") ? "ON" : "OFF");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50,
                                 Drawing.WorldToScreen(myhero.Position).Y + 40,
                                 Color.White,
                                 "Insec: ");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 9 ,
                                 Drawing.WorldToScreen(myhero.Position).Y + 40,
                                 key(misc, "INSECKEY") ? Color.Green : Color.Red,
                                 key(misc, "INSECKEY") ? "ON" : "OFF");
          
            }         
        }

        private static void OnGapcloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (sender != null && !sender.IsMe && sender.IsEnemy)
            {
                if (check(misc, "QGAP") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && sender.Distance(myhero.Position) < DemSpells.Q.Range)
                {
                    DemSpells.Q.Cast();
                }
                else if (check(misc, "WGAP") && DemSpells.W.IsLearned && DemSpells.W.IsReady() && sender.IsValidTarget(DemSpells.W.Range))
                {
                    DemSpells.W.Cast(sender);
                }
            }
        }

        private static void OnInterrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (sender != null && !sender.IsMe && sender.IsEnemy)
            {
                switch(comb(misc, "INTDANGER"))
                {
                    case 0:
                        if (args.DangerLevel >= DangerLevel.Low)
                        {
                            if (check(misc, "QINT") && DemSpells.Q.IsReady() && sender.IsValidTarget(DemSpells.Q.Range)) DemSpells.Q.Cast();

                            else if (check(misc, "WINT") && DemSpells.W.IsReady() && sender.IsValidTarget(DemSpells.W.Range)) DemSpells.W.Cast(sender);
                        }
                        break;
                    case 1:
                        if (args.DangerLevel >= DangerLevel.Medium)
                        {
                            if (check(misc, "QINT") && DemSpells.Q.IsReady() && sender.IsValidTarget(DemSpells.Q.Range)) DemSpells.Q.Cast();

                            else if (check(misc, "WINT") && DemSpells.W.IsReady() && sender.IsValidTarget(DemSpells.W.Range)) DemSpells.W.Cast(sender);
                        }
                        break;
                    case 2:
                        if (args.DangerLevel == DangerLevel.High)
                        {
                            if (check(misc, "QINT") && DemSpells.Q.IsReady() && sender.IsValidTarget(DemSpells.Q.Range)) DemSpells.Q.Cast();

                            else if (check(misc, "WINT") && DemSpells.W.IsReady() && sender.IsValidTarget(DemSpells.W.Range)) DemSpells.W.Cast(sender);
                        }
                        break;
                }
            }
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (!key(misc, "RKEY" ) || comb(misc, "RMODE") != 1 || !DemSpells.R.IsReady() ||
                myhero.CountEnemiesInRange(1200) == 0) return;

            if (args.Target.NetworkId != myhero.NetworkId ||
                !args.Target.IsMe) return;

            var player = args.Target;
            int DamageDealt = (int)Math.Floor((args.Damage / player.MaxHealth) * 100);

            if (DamageDealt >= slider(misc, "RMIND") && !player.IsDead && player.IsMe && player.IsAlly && player.NetworkId == myhero.NetworkId &&
                myhero.CountEnemiesInRange(700) >= 1)
            {
                DemSpells.R.Cast();
            }
        }

        private static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 " + ChampionName, ChampionName.ToLower());
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");           

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version 1.0 14/7/2016");
            menu.AddLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
            combo.Add("CQMIN", new Slider("Min Enemies To Hit With Q", 1, 1, 5));
            combo.AddSeparator();
            combo.Add("CW", new CheckBox("Use W", true));
            combo.AddSeparator();
            combo.Add("Cignt", new CheckBox("Use Ignite", false));
            combo.Add("ITEMS", new CheckBox("Use Items", true));

            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", true));
            harass.Add("HQMIN", new Slider("Min Enemies To Hit With Q", 1, 1, 5));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use W", true));
            harass.Add("HWMAX", new Slider("Dont Use W If More Than X Enemies Nearby", 2, 1, 4));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", true));
            laneclear.Add("LQMIN", new Slider("Min Minions To Hit With Q", 3, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawW", new CheckBox("Draw W Range", true));
            draw.Add("drawE", new CheckBox("Draw E Range", true));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("DRAWMODES", new CheckBox("Draw Status Of All Modes", true));

            misc.AddSeparator();
            misc.AddGroupLabel("Auto Healing(E)");
            misc.Add("EKEY", new KeyBind("Auto Heal Hotkey", false, KeyBind.BindTypes.PressToggle, 'H'));
            misc.Add("EMODE", new ComboBox("E Mode", 0, "Heal Self", "Heal Ally", "Heal Both Me And Ally"));
            misc.AddSeparator();
            misc.AddLabel("Self");
            misc.Add("ESELF", new Slider("Min Health %", 25, 1, 100));
            misc.AddLabel("Ally");
            misc.Add("EALLY", new Slider("Min Health %", 25, 1, 100));
            misc.AddLabel("Mana Manager");
            misc.Add("EMIN", new Slider("Min Mana % To Auto Heal", 50, 1, 100));            
            misc.AddSeparator();
            misc.AddLabel("_____________________________________________________________________________");
            misc.AddSeparator();
            misc.AddGroupLabel("R usage");
            misc.Add("RKEY", new KeyBind("Auto R Hotkey", false, KeyBind.BindTypes.PressToggle, 'J'));
            misc.Add("RMODE", new ComboBox("R Mode", 1, "Min Health & Enemies", "% Damage Dealt"));
            misc.AddLabel("R Mode Settings:",6);
            misc.AddLabel("Min Health");
            misc.Add("RMINH", new Slider("My Health Is Less Than X%", 25, 1, 100));
            misc.AddLabel("Min Enemies");
            misc.Add("RMINE", new Slider("More Than X Enemies Nearby", 1, 1, 5));
            misc.AddLabel("Min Damage Dealt %");
            misc.Add("RMIND", new Slider("Min % Damage Dealt By Enemy",80,1,100 ));
            misc.AddSeparator();
            misc.AddLabel("_____________________________________________________________________________");
            misc.AddSeparator();
            misc.AddGroupLabel("Insec");
            misc.Add("INSECKEY", new KeyBind("Insec Key", false, KeyBind.BindTypes.HoldActive, 'I'));
          //  misc.Add("INSECMODE", new ComboBox("Insec Mode", 0, "Flash + WQ", "Flash + W"));
           // misc.Add("AOEMIN", new Slider("Min Enemies To Hit With Flash + WQ", 2, 1, 5));
            misc.AddSeparator();
            misc.AddLabel("_____________________________________________________________________________");
            misc.AddSeparator();
            misc.AddGroupLabel("Killsteal");
            misc.Add("KSQ", new CheckBox("Killsteal with Q", false));
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));
            misc.AddSeparator();
            misc.AddGroupLabel("Anti-Gapcloser");
            misc.Add("QGAP", new CheckBox("Use Q On Gapclosers", true));
            misc.Add("WGAP", new CheckBox("Use W On Gapclosers", true));
            misc.AddSeparator();
            misc.AddGroupLabel("Interrupter");
            misc.Add("QINT", new CheckBox("Use Q To Interrupt", true));
            misc.Add("WINT", new CheckBox("Use W To Interrupt", true));
            misc.Add("INTDANGER", new ComboBox("Min Danger Level To Interrupt", 1, "Low", "Medium", "High"));            
            misc.AddSeparator();
            misc.Add("AUTOPOT", new CheckBox("Auto Potion", true));
            misc.Add("POTMIN", new Slider("Min Health % To Activate Pot", 25, 1, 100));
            misc.AddSeparator();
            misc.AddGroupLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.AddSeparator();
            misc.AddGroupLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack", true));
            misc.Add("skinID", new ComboBox("Skin Hack", 8, "Default", "Black", "Unchained", "Matador", "Longhorn", "Golden", "Infernal", "Sweeper", "Marauder"));
        }
    }

    public static class DemSpells
    {
        public static Spell.Active Q { get; private set; }
        public static Spell.Targeted W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Active R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Active(SpellSlot.Q, 365);
            W = new Spell.Targeted(SpellSlot.W, 650);
            E = new Spell.Active(SpellSlot.E, 575);
            R = new Spell.Active(SpellSlot.R);
        }
    }
}
