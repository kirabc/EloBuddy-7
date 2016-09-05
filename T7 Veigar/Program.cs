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

namespace T7_Veigar_V2
{
    class Program
    {
        #region Declarations
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;

        public static Prediction.Manager.PredictionInput QDATA = new Prediction.Manager.PredictionInput
        {
            Delay = 0.25f,
            Radius = DemSpells.Q.Width,
            Range = DemSpells.Q.Range - 30,
            Speed = DemSpells.Q.Speed,
            Type = SkillShotType.Linear,
            CollisionTypes = { EloBuddy.SDK.Spells.CollisionType.YasuoWall,
                               EloBuddy.SDK.Spells.CollisionType.AiHeroClient,
                               EloBuddy.SDK.Spells.CollisionType.ObjAiMinion }
        };

        public static Prediction.Manager.PredictionInput WDATA = new Prediction.Manager.PredictionInput
        {
            Delay = 1.25f,
            Radius = DemSpells.W.Width,
            Range = DemSpells.W.Range - 5,
            Speed = DemSpells.W.Speed,
            Type = SkillShotType.Circular
        };

        public static Prediction.Manager.PredictionInput EDATA = new Prediction.Manager.PredictionInput
        {
            Delay = 0.5f,
            Radius = DemSpells.E.Width,
            Range = DemSpells.E.Range,
            Speed = DemSpells.E.Speed,
            Type = SkillShotType.Circular
        };

        private static Spell.Targeted Ignite { get; set; }

        static readonly string ChampionName = "Veigar";
        static readonly string Version = "2.0";
        static readonly string Date = "5/9/16";

        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }
        #endregion

        #region Events
        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }

            Chat.Print("<font color='#0040FF'>T7</font><font color='#FF0505'> Veigar</font><font color='#CC3939'>:R </font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#7752FF'>By </font><font color='#0FA348'>Toyota</font><font color='#7752FF'>7</font><font color='#FF0000'> <3 </font>");

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Game.OnTick += OnTick;
            Gapcloser.OnGapcloser += delegate (AIHeroClient sender, Gapcloser.GapcloserEventArgs args2)
            {
                if (DemSpells.E.CanCast(sender) && sender.IsEnemy && comb(misc, "gapmode") != 0 && sender != null)
                {
                    EDATA.Target = sender;

                    var Epred = Prediction.Manager.GetPrediction(EDATA);

                    if (comb(misc, "gapmode") == 1 && !sender.IsFleeing && sender.IsFacing(myhero) && DemSpells.E.Cast(myhero.Position))
                        return;

                    else if (comb(misc, "gapmode") == 2 && Epred.RealHitChancePercent >= slider(pred, "EPred") && DemSpells.E.Cast(Epred.CastPosition))
                        return;
                }
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

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) || check(harass, "AUTOH") && myhero.ManaPercent > slider(harass, "HMIN")) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) || check(laneclear, "AUTOL") && myhero.ManaPercent > slider(laneclear, "LMIN")) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")) Jungleclear();

            if (key(laneclear, "QSTACK") && slider(laneclear, "LMIN") <= myhero.ManaPercent) QStack();

            Misc();
            CheckPrediction();
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe || !check(misc, "autolevel")) return;

            Core.DelayAction(delegate
            {
                if (myhero.Level > 1 && myhero.Level < 4)
                {
                    switch (myhero.Level)
                    {
                        case 2:
                            Player.LevelSpell(SpellSlot.W);
                            break;
                        case 3:
                            Player.LevelSpell(SpellSlot.E);
                            break;
                    }
                }
                else if (myhero.Level >= 4)
                {
                    if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.R) && Player.LevelSpell(SpellSlot.R))
                    {
                        return;
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.Q) && Player.LevelSpell(SpellSlot.Q))
                    {
                        return;
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(comb(misc, "LEVELMODE") == 1 ? SpellSlot.E : SpellSlot.W) &&
                             Player.LevelSpell(comb(misc, "LEVELMODE") == 1 ? SpellSlot.E : SpellSlot.W))
                    {
                        return;
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(comb(misc, "LEVELMODE") == 1 ? SpellSlot.W : SpellSlot.E) &&
                             Player.LevelSpell(comb(misc, "LEVELMODE") == 1 ? SpellSlot.W : SpellSlot.E))
                    {
                        return;
                    }
                }
            }, new Random().Next(300, 600));
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia) : SharpDX.Color.Fuchsia,
                    DemSpells.Q.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawW") && DemSpells.W.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia) : SharpDX.Color.Fuchsia,
                    DemSpells.W.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia) : SharpDX.Color.Fuchsia,
                    DemSpells.E.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia) : SharpDX.Color.Fuchsia,
                    DemSpells.R.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawk"))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    if (enemy.VisibleOnScreen && ComboDamage(enemy) > enemy.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30,
                                            Color.Green, "Killable With Combo");
                    }
                    else if (enemy.VisibleOnScreen && ComboDamage(enemy) + myhero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) > enemy.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30,
                                            Color.Green, "Combo + Ignite");
                    }
                }
            }

            if (check(draw, "drawStacks"))
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50, Drawing.WorldToScreen(myhero.Position).Y + 10,
                                 Color.Red, key(laneclear, "QSTACK") ? "Auto Stacking: ON" : "Auto Stacking: OFF");
            }

            if (check(draw, "drawStackCount") && myhero.HasBuff("veigarphenomenalevilpower"))
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 25, Drawing.WorldToScreen(myhero.Position).Y + 25,
                                 Color.Red, "Count: " + myhero.GetBuffCount("veigarphenomenalevilpower").ToString());
            }
        }
        #endregion

        #region Modes
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(950, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.ValidTarget(950))
            {
                if (DemSpells.E.CanCast(target) && check(combo, "CE"))
                {
                    if (check(combo, "EIMMO") && target.HasBuffOfType(BuffType.Stun)) return;

                    EDATA.Target = target;

                    var Epred = Prediction.Manager.GetPrediction(EDATA);

                    switch (comb(combo, "CEMODE"))
                    {
                        case 0:
                            if (Epred.RealHitChancePercent >= slider(pred, "EPred")) DemSpells.E.Cast(Epred.CastPosition);
                            break;
                        case 1:
                            if (Epred.PredictedPosition.Distance(myhero.Position) < DemSpells.E.Range - 5)
                            {
                                switch (target.IsFleeing)
                                {
                                    case true:
                                        DemSpells.E.Cast(Epred.PredictedPosition.Shorten(myhero.Position, target.IsMoving ? 200 : 190));
                                        break;
                                    case false:
                                        DemSpells.E.Cast(Epred.PredictedPosition.Extend(myhero.Position, target.IsMoving ? 200 : 190).To3D());
                                        break;
                                }
                            }
                            break;
                        case 2:
                            Prediction.Manager.PredictionSelected = "SDK Prediction";

                            if (DemSpells.E.CastIfItWillHit(slider(combo, "CEAOE"))) return;
                            break;
                    }
                }

                if (DemSpells.Q.CanCast(target) && check(combo, "CQ"))
                {
                    QDATA.Target = target;

                    var Qpred = Prediction.Manager.GetPrediction(QDATA);

                    if (Qpred.CollisionObjects.Count() < 2 && Qpred.HitChancePercent >= slider(pred, "QPred") && DemSpells.Q.Cast(Qpred.CastPosition))
                    {
                        return;
                    }
                }

                if (DemSpells.W.CanCast(target) && check(combo, "CW"))
                {
                    WDATA.Target = target;

                    var Wpred = Prediction.Manager.GetPrediction(WDATA);

                    switch (comb(combo, "CWMODE"))
                    {
                        case 0:
                            if (Wpred.RealHitChancePercent >= slider(pred, "WPred") || Wpred.HitChance == HitChance.Immobile ||
                               (target.HasBuffOfType(BuffType.Slow) && Wpred.HitChance == HitChance.High))
                            {
                                DemSpells.W.Cast(Wpred.CastPosition);
                            }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;
                        case 2:
                            if (target.HasBuffOfType(BuffType.Stun) || Wpred.HitChance == HitChance.Immobile)
                            {
                                DemSpells.W.Cast(Wpred.CastPosition);
                            }
                            break;
                    }
                }

                if (check(combo, "CR") && DemSpells.R.IsReady() &&
                    DemSpells.R.IsInRange(target.Position) && ComboDamage(target) > target.Health &&
                    RDamage(target) > target.Health && !target.HasBuff("bansheesveil") && !target.HasBuff("fioraw"))
                {
                    if ((ComboDamage(target) - RDamage(target)) > target.Health) return;
                    DemSpells.R.Cast(target);
                }

                if (check(combo, "IgniteC") && Ignite.IsReady() && ComboDamage(target) < target.Health &&
                    Ignite.IsInRange(target.Position) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health &&
                    !check(misc, "autoign"))
                    Ignite.Cast(target);
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(950, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.ValidTarget(950))
            {
                if (DemSpells.Q.CanCast(target) && check(harass, "HQ"))
                {
                    QDATA.Target = target;

                    var Qpred = Prediction.Manager.GetPrediction(QDATA);

                    if (Qpred.CollisionObjects.Count() < 2 && Qpred.HitChancePercent >= slider(pred, "QPred") && DemSpells.Q.Cast(Qpred.CastPosition)) return;
                }

                if (DemSpells.W.CanCast(target) && check(harass, "HW"))
                {
                    WDATA.Target = target;

                    var Wpred = Prediction.Manager.GetPrediction(WDATA);

                    switch (comb(harass, "HWMODE"))
                    {
                        case 0:
                            if (Wpred.RealHitChancePercent >= slider(pred, "WPred") || Wpred.HitChance == HitChance.Immobile ||
                               (target.HasBuffOfType(BuffType.Slow) && Wpred.HitChance == HitChance.High))
                            {
                                DemSpells.W.Cast(Wpred.CastPosition);
                            }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;
                        case 2:
                            if (target.HasBuffOfType(BuffType.Stun) || Wpred.HitChance == HitChance.Immobile)
                            {
                                DemSpells.W.Cast(Wpred.CastPosition);
                            }
                            break;
                    }
                }
            }
        }

        private static void Laneclear()
        {
            var Minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, 950);

            if (Minions != null)
            {
                if (!key(laneclear, "QSTACK") && DemSpells.Q.IsReady() && check(laneclear, "LQ"))
                {
                    Minions.ToList().ForEach(x =>
                    {
                        QDATA.Target = x;

                        DemSpells.Q.Cast(Prediction.Manager.GetPrediction(QDATA).CastPosition);
                    });
                }

                if (DemSpells.W.IsReady() && check(laneclear, "LW") && DemSpells.W.CastOnBestFarmPosition(slider(laneclear, "LWMIN"))) return;
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 950);

            if (Monsters != null)
            {
                if (check(jungleclear, "JQ") && DemSpells.Q.IsReady())
                {
                    foreach (Obj_AI_Minion monster in Monsters.Where(x => DemSpells.Q.CanCast(x)))
                    {
                        if (comb(jungleclear, "JQMODE") == 0 && monster.BaseSkinName.Contains("Mini")) return;

                        QDATA.Target = monster;

                        var Qpred = Prediction.Manager.GetPrediction(QDATA);

                        if (Qpred.CollisionObjects.Count() < 2 && DemSpells.Q.Cast(Qpred.CastPosition)) return;
                    }
                }

                if (check(jungleclear, "JW") && DemSpells.W.IsReady() && DemSpells.W.CastOnAOELocation(true)) return;

                if (check(jungleclear, "JE") && DemSpells.E.IsReady() && DemSpells.E.CastOnAOELocation(true)) return;
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget(1000))
            {

                if (check(misc, "KSQ") && DemSpells.Q.CanCast(target) && myhero.GetSpellDamage(target, SpellSlot.Q) > target.Health &&
                    Prediction.Health.GetPrediction(target, DemSpells.Q.CastDelay + (int)((target.Distance(myhero.Position) / DemSpells.Q.Speed) * 1000)) > 0)
                {
                    QDATA.Target = target;

                    var Qpred = Prediction.Manager.GetPrediction(QDATA);

                    if (Qpred.HitChancePercent >= slider(pred, "QPred") && DemSpells.Q.Cast(Qpred.CastPosition)) return;
                }

                if (check(misc, "KSW") && DemSpells.W.CanCast(target) && myhero.GetSpellDamage(target, SpellSlot.W) > target.Health &&
                    Prediction.Health.GetPrediction(target, DemSpells.W.CastDelay) > 0)
                {
                    WDATA.Target = target;

                    var Wpred = Prediction.Manager.GetPrediction(WDATA);

                    if (Wpred.RealHitChancePercent >= slider(pred, "WPred") && DemSpells.Q.Cast(Wpred.CastPosition)) return;
                }

                if (check(misc, "KSR") && DemSpells.R.CanCast(target) && RDamage(target) > target.Health &&
                    Prediction.Health.GetPrediction(target, 200) > 0 && !target.HasBuff("bansheesveil") && !target.HasBuff("fioraw") && DemSpells.R.Cast(target))
                {
                    return;
                }

                if (Ignite != null && check(misc, "autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    Ignite.Cast(target);
                }
            }

            if (check(misc, "KSJ") && DemSpells.W.IsLearned && DemSpells.W.IsReady() && EntityManager.MinionsAndMonsters.Monsters.Count(x => x.IsValidTarget(DemSpells.W.Range)) > 0)
            {
                foreach (var monster in EntityManager.MinionsAndMonsters.GetJungleMonsters().Where(x => !x.Name.ToLower().Contains("mini") && !x.IsDead &&
                                                                                                   x.Health > 200 && x.IsValidTarget(DemSpells.W.Range) &&
                                                                                                   Prediction.Health.GetPrediction(x, DemSpells.W.CastDelay + 100) > x.Health &&
                                                                                                   (x.Name.ToLower().Contains("dragon") ||
                                                                                                    x.Name.ToLower().Contains("baron") ||
                                                                                                    x.Name.ToLower().Contains("herald"))))
                {
                    WDATA.Target = monster;

                    var Wpred = Prediction.Manager.GetPrediction(QDATA);

                    if (monster.Name.ToLower().Contains("herald") && Wpred.HitChance == HitChance.High && DemSpells.W.Cast(Wpred.CastPosition)) return;

                    else if (!monster.Name.ToLower().Contains("herald") && DemSpells.W.Cast(monster.Position)) return;
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
        #endregion

        #region Menu
        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 Veigar:R", "veigarxd");
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            jungleclear = menu.AddSubMenu("Jungleclear", "jclear");
            misc = menu.AddSubMenu("Misc", "misc");
            draw = menu.AddSubMenu("Drawings", "draw");
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 Veigar:R And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date);
            menu.AddLabel("Author: Toyota7");

            combo.AddGroupLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q"));
            combo.Add("CW", new CheckBox("Use W"));
            combo.Add("CE", new CheckBox("Use E"));
            combo.Add("CR", new CheckBox("Use R"));
            if (Ignite != null) combo.Add("IgniteC", new CheckBox("Use Ignite", false));
            combo.AddSeparator();
            combo.AddLabel("W Mode:");
            combo.Add("CWMODE", new ComboBox("Select Mode", 0, "With Prediciton", "Without Prediction", "Only On Stunned Enemies"));
            combo.AddSeparator();
            combo.AddLabel("E Options:");
            combo.Add("CEMODE", new ComboBox("E Mode: ", 1, "Target On The Center", "Target On The Edge(stun)", "AOE"));
            combo.Add("CEAOE", new Slider("Min Champs For AOE Function", 2, 1, 5));
            combo.Add("EIMMO", new CheckBox("Dont Use E On Immobile Enemies"));

            harass.AddGroupLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", false));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use W", false));
            harass.Add("HWMODE", new ComboBox("Select Mode", 2, "With Prediciton", "Without Prediction(Not Recommended)", "Only On Stunned Enemies"));
            harass.AddSeparator();
            harass.Add("AUTOH", new CheckBox("Auto harass", false));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 40, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q"));
            laneclear.AddSeparator(10);
            laneclear.AddLabel("Q Stacking");
            laneclear.Add("QSTACK", new KeyBind("Auto Stacking", true, KeyBind.BindTypes.PressToggle, 'F'));
            laneclear.Add("QSTACKMODE", new ComboBox("Select Mode", 0, "LastHit 1 Minion", "LastHit 2 Minions"));
            laneclear.AddSeparator(35);
            laneclear.Add("LW", new CheckBox("Use W", false));
            laneclear.Add("LWMIN", new Slider("Min Minions For W", 2, 1, 6));
            laneclear.AddSeparator();
            laneclear.Add("AUTOL", new CheckBox("Auto Laneclear", false));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));
            jungleclear.Add("JQMODE", new ComboBox("Q Mode", 1, "All Monsters", "Big Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W", false));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E", false));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 10, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range"));
            draw.Add("drawW", new CheckBox("Draw W Range"));
            draw.Add("drawE", new CheckBox("Draw E Range"));
            draw.Add("drawR", new CheckBox("Draw R Range"));
            draw.AddSeparator();
            draw.Add("drawk", new CheckBox("Draw Killable Enemies", false));
            draw.Add("nodrawc", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawStacks", new CheckBox("Draw Auto Stack Mode"));
            draw.Add("drawStackCount", new CheckBox("Draw Stack Count", false));

            misc.AddGroupLabel("Killsteal");
            misc.Add("KSQ", new CheckBox("Killsteal with Q", false));
            misc.Add("KSW", new CheckBox("Killsteal with W", false));
            misc.Add("KSR", new CheckBox("Killsteal with R", false));
            if (Ignite != null) misc.Add("autoign", new CheckBox("Auto Ignite If Killable"));
            misc.AddSeparator();
            misc.Add("KSJ", new CheckBox("Steal Dragon/Baron/Rift Herald With W", false));
            misc.AddSeparator();
            misc.Add("AUTOPOT", new CheckBox("Auto Potion"));
            misc.Add("POTMIN", new Slider("Min Health % To Activate Potion", 50, 1, 100));
            misc.AddSeparator();
            misc.AddGroupLabel("Gapcloser");
            misc.Add("gapmode", new ComboBox("Use E On Gapcloser                                               Mode:", 0, "Off", "Self", "Enemy(Pred)"));
            misc.AddSeparator();
            misc.AddGroupLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells"));
            misc.Add("LEVELMODE", new ComboBox("Select Sequence", 0, "Q>E>W", "Q>W>E"));
            misc.AddSeparator();
            misc.AddGroupLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack"));
            misc.Add("skinID", new ComboBox("Skin Hack", 8, "Default", "White Mage", "Curling", "Veigar Greybeard", "Leprechaun", "Baron Von", "Superb Villain", "Bad Santa", "Final Boss"));

            pred.AddGroupLabel("Q HitChance");
            pred.Add("QPred", new Slider("% Hitchance", 85, 0, 100));
            pred.AddSeparator();
            pred.AddGroupLabel("W HitChance");
            pred.Add("WPred", new Slider("% Hitchance", 85, 0, 100));
            pred.AddSeparator();
            pred.AddGroupLabel("E HitChance");
            pred.Add("EPred", new Slider("% Hitchance", 85, 0, 100));
        }
        #endregion

        #region Methods
        private static void CheckPrediction()
        {
            string CorrectPrediction = "SDK Beta Prediction";

            switch(Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.LaneClear:
                case Orbwalker.ActiveModes.JungleClear:
                    CorrectPrediction = "SDK Prediction";
                    break;
                default:
                    CorrectPrediction = "SDK Beta Prediction";
                    break;
            }

            if (Prediction.Manager.PredictionSelected == CorrectPrediction)
            {
                return;
            }
            else
            {
                Prediction.Manager.PredictionSelected = CorrectPrediction;
         //       Chat.Print("<font color='#00D118'>T7 Veigar: Prediction Has Been Automatically Changed!</font>");
                return;
            }
        }

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += myhero.GetSpellDamage(target, SpellSlot.Q); }

                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { TotalDamage += myhero.GetSpellDamage(target, SpellSlot.W); }

                if (DemSpells.R.IsLearned && DemSpells.R.IsReady()) { TotalDamage += RDamage(target); }

                return TotalDamage;
            }
            return 0;
        }

        private static void QStack()
        {
            if (!DemSpells.Q.IsReady() || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || myhero.IsRecalling()) return;

            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.Q.Range)
                                                          .OrderBy(x => x.Distance(myhero.Position));

            if (minions != null)
            {
                foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range - 10) && x.Health < QDamage(x) - 10 &&
                                                     Prediction.Health.GetPrediction(x, (int)(((x.Distance(myhero.Position) / DemSpells.Q.Speed) * 1000) + 350)) > 30))
                {
                    var Qpred = DemSpells.Q.GetPrediction(minion);

                    var collisions = Qpred.CollisionObjects.ToList();

                    switch (comb(laneclear, "QSTACKMODE"))
                    {
                        case 0:
                            if (Qpred.Collision && collisions.Count() <= 1)
                            {
                                DemSpells.Q.Cast(Qpred.CastPosition);

                            }
                            else
                            {
                                DemSpells.Q.Cast(Qpred.CastPosition);
                            }
                            break;
                        case 1:
                            if (Qpred.Collision && (collisions.Count() == 1 &&
                                collisions.FirstOrDefault().Health < QDamage(collisions.FirstOrDefault()) - 10))
                            {
                                DemSpells.Q.Cast(Qpred.CastPosition);
                            }
                            else if (collisions.Count() == 2 && collisions[0].Health < QDamage(collisions[0]) - 10 &&
                                                               collisions[1].Health < QDamage(collisions[1]) - 10)
                            {
                                DemSpells.Q.Cast(Qpred.CastPosition);
                            }
                            break;
                    }
                }
            }
        }

        private static float QDamage(Obj_AI_Base target)
        {
            var index = DemSpells.Q.Level - 1;

            var QDamage = new[] { 70, 110, 150, 190, 230 }[index] +
                          (0.6f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)QDamage);
        }

        private static float RDamage(AIHeroClient target)
        {
            var level = DemSpells.R.Level;

            var damage = new float[] { 0, 175, 250, 325 }[level] + (((100 - target.HealthPercent) * 1.5) / 100) * new float[] { 0, 175, 250, 325 }[level] +
                0.75 * myhero.FlatMagicDamageMod;
            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)damage);
        }

        public static bool check(Menu submenu, string sig)
        {
            return submenu[sig].Cast<CheckBox>().CurrentValue;
        }

        public static int slider(Menu submenu, string sig)
        {
            return submenu[sig].Cast<Slider>().CurrentValue;
        }

        public static int comb(Menu submenu, string sig)
        {
            return submenu[sig].Cast<ComboBox>().CurrentValue;
        }

        public static bool key(Menu submenu, string sig)
        {
            return submenu[sig].Cast<KeyBind>().CurrentValue;
        }
        #endregion
    }

    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 950, SkillShotType.Linear, 250, 2000, 70);

            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 1250, int.MaxValue, 225)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Skillshot(SpellSlot.E, 700, SkillShotType.Circular, 500, int.MaxValue, 380)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Targeted(SpellSlot.R, 650);
        }


    }

    public static class Extensions
    {
        public static bool CastOnAOELocation(this Spell.Skillshot spell, bool JungleMode = false)
        {
            var targets = JungleMode ? EntityManager.MinionsAndMonsters.GetJungleMonsters(Program.myhero.Position, (float)DemSpells.Q.Range).ToArray<Obj_AI_Base>() :
                                        EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Program.myhero.Position, (float)DemSpells.Q.Range).ToArray<Obj_AI_Base>();

            var AOE = Prediction.Position.GetPredictionAoe(targets, new Prediction.Position.PredictionData(
                                                Prediction.Position.PredictionData.PredictionType.Circular,
                                                (int)spell.Range,
                                                spell.Width,
                                                0,
                                                spell.CastDelay,
                                                spell.Speed,
                                                spell.AllowedCollisionCount,
                                                Player.Instance.Position))
                                                .OrderByDescending(x => x.GetCollisionObjects<Obj_AI_Minion>().Count())
                                                .FirstOrDefault();

            if (AOE != null && spell.Cast(AOE.CastPosition)) return true;

            return false;
        }

        public static bool ValidTarget(this AIHeroClient hero, int range)
        {
            return !hero.HasBuff("UndyingRage") && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("ChronoShift") && !hero.HasBuff("kindredrnodeathbuff") &&
                   !hero.IsInvulnerable && !hero.IsDead && hero.IsValidTarget(range) && !hero.IsZombie &&
                   !hero.HasBuffOfType(BuffType.Invulnerability) && !hero.HasBuffOfType(BuffType.SpellImmunity) && !hero.HasBuffOfType(BuffType.SpellShield);

        }
    }
}
