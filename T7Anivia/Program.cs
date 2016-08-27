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

namespace T7_Anivia
{
    class Program
    {
        #region Declarations
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;
        private static Spell.Targeted Ignite { get; set; }
        static readonly string ChampionName = "Anivia";
        static readonly string Version = "1.0b";
        static readonly string Date = "27/8/16";

        private static GameObject RMissile = null, QMissile = null;

        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }
        #endregion

        #region Events
        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#2699BF'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Game.OnTick += OnTick;

            GameObject.OnCreate += delegate (GameObject sender, EventArgs args2)
            {
                if (!sender.IsValid || !sender.IsAlly) return;

                if (sender.Name == "cryo_FlashFrost_Player_mis.troy")
                {
                    QMissile = sender;
                }

                if (sender.Name.ToLower().Contains("cryo_storm"))
                {
                    RMissile = sender;
                }

                return;
            };

            GameObject.OnDelete += delegate (GameObject sender, EventArgs args2)
            {
                if (!sender.IsValid || !sender.IsAlly) return;

                if (sender.Name == "cryo_FlashFrost_Player_mis.troy")
                {
                    QMissile = null;
                }

                if (sender.Name.ToLower().Contains("cryo_storm"))
                {
                    RMissile = null;
                }

                return;
            };


            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            RPotion = new Item((int)ItemId.Refillable_Potion);
            Player.LevelSpell(SpellSlot.Q);

            if (EloBuddy.SDK.Spells.SummonerSpells.PlayerHas(EloBuddy.SDK.Spells.SummonerSpellsEnum.Ignite))
            {
                Ignite = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 600);
            }

            DatMenu();
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > slider(harass, "HMIN")) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > slider(laneclear, "LMIN")) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")) Jungleclear();

            Misc();
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            var U = SpellSlot.Unknown;
            var Q = SpellSlot.Q;
            var W = SpellSlot.W;
            var E = SpellSlot.E;
            var R = SpellSlot.R;

            /*>>*/
            SpellSlot[] sequence1 = { U, E, E, W, E, R, E, Q, E, Q, R, Q, Q, W, W, R, W, W, U };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkTurquoise, DemSpells.Q.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.DarkTurquoise, DemSpells.Q.Range, myhero.Position); }

            }

            if (check(draw, "drawW") && DemSpells.W.Level > 0)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.W.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkTurquoise, DemSpells.W.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.DarkTurquoise, DemSpells.W.Range, myhero.Position); }

            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkTurquoise, DemSpells.E.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.DarkTurquoise, DemSpells.E.Range, myhero.Position); }

            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkTurquoise, DemSpells.R.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.DarkTurquoise, DemSpells.R.Range, myhero.Position); }

            }
        }
        #endregion

        #region Modes
        private static void Combo()
        {
            var Enemies = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(DemSpells.Q.Range) && !x.HasUndyingBuff());
            var target = TargetSelector.GetTarget(1200, DamageType.Magical, myhero.Position);

            if (QMissile != null && (QMissile.Distance(target.Position) < 200 || EntityManager.Heroes.Enemies.Where(x => x.Distance(QMissile.Position) < 200).Count() >= 1) &&
                    DemSpells.Q.Cast(myhero.Position))
            { return; }

            if (RMissile != null && EntityManager.Heroes.Enemies.Where(x => x.Distance(RMissile.Position) < 400).Count() == 0 &&
                    DemSpells.R.Cast(myhero.Position))
            { return; }

            if (Enemies == null) return;

            if (check(combo, "CQ") && DemSpells.Q.IsReady() && QMissile == null && target.ValidTarget(DamageType.Magical, (int)DemSpells.Q.Range))
            {
                if (myhero.HasBuff("FlashFrost")) return;

                var AOE = Prediction.Position.PredictCircularMissileAoe(Enemies.ToArray<Obj_AI_Base>(), DemSpells.Q.Range, 190, DemSpells.Q.CastDelay, DemSpells.Q.Speed);

                foreach (var result in AOE.Where(x => x.CollisionObjects.Contains(target) && x.HitChancePercent >= slider(pred, "QPred"))
                                          .OrderByDescending(x => x.CollisionObjects.Count()))
                {
                    DemSpells.Q.Cast(result.CastPosition);
                    break;
                }
                return;
            }

            if (check(combo, "CW") && DemSpells.W.GetPrediction(target).CastPosition.Distance(target.Position) >= 100 && DemSpells.W.Cast(DemSpells.W.GetPrediction(target).CastPosition))
            { return; }

            if (check(combo, "CE") && DemSpells.E.IsReady())
            {
                foreach (AIHeroClient enemy in Enemies.Where(x => x.ValidTarget(DamageType.Magical, (int)DemSpells.E.Range)).OrderByDescending(x => TargetSelector.GetPriority(x)))
                {
                    if (DemSpells.E.CanCast(enemy) && (!check(combo, "CESTUN") ||
                                                         (check(combo, "CESTUN") && HasChill(enemy)) ||
                                                         (check(combo, "CESTUN") && !HasChill(enemy) && !DemSpells.Q.IsReady() && !DemSpells.R.IsReady())))
                    {
                        DemSpells.E.Cast(enemy);
                        break;
                    }
                    else continue;
                }
                return;
            }

            if (check(combo, "CR") && DemSpells.R.IsReady() && RMissile == null)
            {
                if (myhero.HasBuff("GlacialStorm")) return;

                var AoEPred = Prediction.Position.GetPredictionAoe(Enemies.ToArray<Obj_AI_Base>(),
                           new Prediction.Position.PredictionData(Prediction.Position.PredictionData.PredictionType.Circular, (int)DemSpells.R.Range, 300, 0, 250, int.MaxValue));

                foreach (var result in AoEPred.Where(x => x.CollisionObjects.Any()).OrderByDescending(x => x.CollisionObjects.Count()))
                {
                    DemSpells.R.Cast(result.CastPosition);

                    break;
                }
                return;
            }
        }

        private static void Harass()
        {
            var Enemies = EntityManager.Heroes.Enemies.Where(x => x.ValidTarget(DamageType.Magical, (int)DemSpells.Q.Range));

            if (QMissile != null && EntityManager.Heroes.Enemies.Where(x => x.Distance(QMissile.Position) < 200).Count() >= 1 &&
                    DemSpells.Q.Cast(myhero.Position))
            { return; }

            if (Enemies == null) return;

            if (check(harass, "HQ") && DemSpells.Q.IsReady() && QMissile == null)
            {
                if (myhero.HasBuff("FlashFrost")) return;

                var AOE = Prediction.Position.PredictCircularMissileAoe(Enemies.ToArray<Obj_AI_Base>(), DemSpells.Q.Range, 190, DemSpells.Q.CastDelay, DemSpells.Q.Speed);

                foreach (var result in AOE.Where(x => x.CollisionObjects.Where(y => y is AIHeroClient && y.IsEnemy).Count() >= 1 && x.HitChancePercent >= slider(pred, "QPred"))
                                          .OrderByDescending(x => x.CollisionObjects.Count()))
                {
                    DemSpells.Q.Cast(result.CastPosition);
                    break;
                }
                return;
            }

            if (check(harass, "HE") && DemSpells.E.IsReady())
            {
                foreach (AIHeroClient enemy in Enemies.Where(x => x.ValidTarget(DamageType.Magical, (int)DemSpells.E.Range)).OrderByDescending(x => TargetSelector.GetPriority(x)))
                {
                    if (check(harass, "HE" + enemy.ChampionName) && DemSpells.E.CanCast(enemy) && DemSpells.E.Cast(enemy)) break;
                }
                return;
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, 1200);

            if (minions != null)
            {
                if (RMissile != null && EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(RMissile.Position) < 400).Count() < slider(laneclear, "LRMIN") &&
                    DemSpells.R.Cast(myhero.Position))
                { return; }

                if (RMissile != null && myhero.ManaPercent <= slider(laneclear, "LRMANA") && DemSpells.R.Cast(myhero.Position)) return;

                if (check(laneclear, "LQ") && DemSpells.Q.IsReady() && QMissile == null)
                {
                    if (myhero.HasBuff("FlashFrost")) return;

                    var AOE = Prediction.Position.PredictCircularMissileAoe(minions.ToArray<Obj_AI_Base>(), DemSpells.Q.Range, 180, DemSpells.Q.CastDelay, DemSpells.Q.Speed);

                    foreach (var result in AOE.Where(x => x.CollisionObjects.Count() >= slider(laneclear, "LQMIN")).OrderByDescending(x => x.CollisionObjects.Count()))
                    {
                        DemSpells.Q.Cast(result.CastPosition);
                        Core.DelayAction(() => DemSpells.Q.Cast(myhero.Position),
                            (int)((result.CastPosition.Distance(myhero.Position) / DemSpells.Q.Speed) * 1000) + 200);
                        break;
                    }
                    return;
                }

                if (check(laneclear, "LE") && DemSpells.E.IsReady())
                {
                    foreach (var minion in minions.Where(x => x.Health > 30 && DemSpells.E.CanCast(x)))
                    {
                        if (comb(laneclear, "LEMODE") == 0 && !minion.Model.ToLower().Contains("siege") && !minion.Model.ToLower().Contains("super")) continue;

                        DemSpells.E.Cast(minion);

                        break;
                    }
                    return;
                }

                if (check(laneclear, "LR") && DemSpells.R.IsReady() && RMissile == null && myhero.ManaPercent >= slider(laneclear, "LRMANA"))
                {
                    if (myhero.HasBuff("GlacialStorm")) return;

                    var AoEPred = Prediction.Position.GetPredictionAoe(minions.ToArray<Obj_AI_Base>(),
                            new Prediction.Position.PredictionData(Prediction.Position.PredictionData.PredictionType.Circular, (int)DemSpells.R.Range, 300, 0, 250, int.MaxValue));

                    foreach (var result in AoEPred.Where(x => x.CollisionObjects.Count() >= slider(laneclear, "LRMIN")))
                    {
                        DemSpells.R.Cast(result.CastPosition);

                        break;
                    }
                    return;
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1200).ToList();

            if (Monsters != null)
            {
                if (RMissile != null && Monsters.Where(x => x.Distance(RMissile.Position) < 400).Count() < slider(jungleclear, "JRMIN") &&
                    DemSpells.R.Cast(myhero.Position))
                { return; }

                if (RMissile != null && myhero.ManaPercent <= slider(jungleclear, "JRMANA") && DemSpells.R.Cast(myhero.Position)) return;

                if (check(jungleclear, "JQ") && DemSpells.Q.IsReady() && QMissile == null)
                {
                    if (myhero.HasBuff("FlashFrost")) return;

                    var AOE = Prediction.Position.PredictCircularMissileAoe(Monsters.ToArray<Obj_AI_Base>(), DemSpells.Q.Range, 180, DemSpells.Q.CastDelay, DemSpells.Q.Speed);

                    foreach (var result in AOE.Where(x => x.CollisionObjects.Count() >= slider(jungleclear, "JQMIN")).OrderByDescending(x => x.CollisionObjects.Count()))
                    {
                        DemSpells.Q.Cast(result.CastPosition);
                        Core.DelayAction(() => DemSpells.Q.Cast(myhero.Position),
                            (int)((result.CastPosition.Distance(myhero.Position) / DemSpells.Q.Speed) * 1000) + 200);
                        break;
                    }
                    return;
                }

                if (check(jungleclear, "JE") && DemSpells.E.IsReady())
                {
                    foreach (var monster in Monsters.Where(x => x.Health > 30 && DemSpells.E.CanCast(x)))
                    {
                        if (comb(jungleclear, "JEMODE") == 0 && monster.Name.Contains("Mini")) continue;

                        DemSpells.E.Cast(monster);

                        break;
                    }
                    return;
                }

                if (check(jungleclear, "JR") && DemSpells.R.IsReady() && RMissile == null && myhero.ManaPercent >= slider(jungleclear, "JRMANA"))
                {
                    if (myhero.HasBuff("GlacialStorm")) return;

                    var AoEPred = Prediction.Position.GetPredictionAoe(Monsters.ToArray<Obj_AI_Base>(),
                            new Prediction.Position.PredictionData(Prediction.Position.PredictionData.PredictionType.Circular, (int)DemSpells.R.Range, 300, 0, 250, int.MaxValue));

                    foreach (var result in AoEPred.Where(x => x.CollisionObjects.Count() >= slider(jungleclear, "JRMIN")))
                    {
                        if (check(jungleclear, "JRBIG") && !result.CollisionObjects.Where(x => !x.Name.Contains("Mini")).Any()) break;

                        DemSpells.R.Cast(result.CastPosition);

                        break;
                    }
                    return;
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Magical, Player.Instance.Position);

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();

                return;
            }

            if (target != null && target.ValidTarget(DamageType.Magical, 1000))
            {
                if (check(misc, "KSE") && DemSpells.E.CanCast(target) && EDamage(target) > Prediction.Health.GetPrediction(target, DemSpells.E.CastDelay) &&
                    Prediction.Health.GetPrediction(target, DemSpells.E.CastDelay) > 0 && !target.HasBuff("bansheesveil") && DemSpells.E.Cast(target))
                { return; }

                if (Ignite != null && check(misc, "autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health && Ignite.Cast(target))
                { return; }
            }
        }
        #endregion        

        #region Menu
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
            combo.AddSeparator();
            combo.Add("CW", new CheckBox("Use W", true));
            combo.AddSeparator();
            combo.Add("CE", new CheckBox("Use E", true));
            combo.Add("CESTUN", new CheckBox("Keep E For Double Damage"));
            combo.AddSeparator();
            combo.Add("CR", new CheckBox("Use R", true));

            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", false));
            harass.AddSeparator();
            harass.Add("HE", new CheckBox("Use E", false));
            harass.AddLabel("Use E On:");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                harass.Add("HE" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", false));
            laneclear.Add("LQMIN", new Slider("Min Minions For Q", 3, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", false));
            laneclear.Add("LEMODE", new ComboBox("E Targets", 0, "Big Minions", "All Minions"));
            laneclear.AddSeparator();
            laneclear.Add("LR", new CheckBox("Use R", false));
            laneclear.Add("LRMIN", new Slider("Min Minions For R", 5, 1, 10));
            laneclear.Add("LRMANA", new Slider("Min Mana % To Use R", 75, 1, 100));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));
            jungleclear.Add("JQMIN", new Slider("Min Monsters For Q", 2, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E", false));
            jungleclear.Add("JEMODE", new ComboBox("E Targets", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JR", new CheckBox("Use R", false));
            jungleclear.Add("JRBIG", new CheckBox("Only Cast If Big Monsters Are In The Camp"));
            jungleclear.Add("JRMIN", new Slider("Min Monsters For R", 3, 1, 4));
            jungleclear.Add("JRMANA", new Slider("Min Mana % To Cast R", 70, 1, 100));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 50, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawW", new CheckBox("Draw W Range", true));
            draw.Add("drawE", new CheckBox("Draw E Range", true));
            draw.Add("drawR", new CheckBox("Draw R Range", true));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));

            misc.AddLabel("Killsteal");
            misc.Add("KSE", new CheckBox("Killsteal with E", false));
            if (Ignite != null) misc.Add("autoign", new CheckBox("Auto Ignite If Killable"));
            misc.AddSeparator();
            misc.Add("EGAP", new CheckBox("Use E On Gapclosers", false));
            misc.AddSeparator();
            misc.AddLabel("Auto Potion");
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion"));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells"));

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("Q :");
            pred.Add("QPred", new Slider("Select % Hitchance", 90, 1, 100));
        }
        #endregion

        #region Methods
        private static bool HasChill(AIHeroClient target)
        {
            return target.HasBuff("chilled");
        }

        private static float EDamage(AIHeroClient target)
        {
            int index = DemSpells.E.Level - 1;

            var EDamage = (HasChill(target) ? new[] { 110, 170, 230, 290, 350 }[index] : new[] { 55, 85, 115, 145, 175 }[index]) +
                          ((HasChill(target) ? 1 : 0.5f) * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, EDamage);
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
        #endregion
    }

    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Targeted E { get; private set; }
        public static Spell.Skillshot R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1050, SkillShotType.Linear, 250, 850, 110)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Circular, 250, int.MaxValue, 1);
            E = new Spell.Targeted(SpellSlot.E, 600);
            R = new Spell.Skillshot(SpellSlot.R, 750, SkillShotType.Circular, 250, int.MaxValue, 300)
            {
                AllowedCollisionCount = int.MaxValue
            };
        }
    }

    public static class MyExtensions
    {
        public static bool ValidTarget(this AIHeroClient hero, DamageType type, int range)
        {
            return !hero.HasBuff("UndyingRage") && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("ChronoShift") && !hero.HasBuff("kindredrnodeathbuff") &&
                   !hero.IsInvulnerable && !hero.IsDead && (type == DamageType.Magical ? !hero.MagicImmune : !hero.IsPhysicalImmune) && hero.IsValidTarget(range) && !hero.IsZombie &&
                   !hero.HasBuffOfType(BuffType.Invulnerability) && !hero.HasBuffOfType(BuffType.SpellImmunity);

        }
    }
}
