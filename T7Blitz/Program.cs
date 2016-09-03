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

namespace T7_Blitz
{
    class Program
    {
        #region Declarations
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, qsett;
        private static int hit = 0;

        private static Prediction.Manager.PredictionInput QDATA = new Prediction.Manager.PredictionInput
        {
            Delay = DemSpells.Q.CastDelay,
            Radius = DemSpells.Q.Radius,
            Range = DemSpells.Q.Range,
            Speed = DemSpells.Q.Speed,
            Type = SkillShotType.Linear,
            CollisionTypes = { EloBuddy.SDK.Spells.CollisionType.AiHeroClient,
                               EloBuddy.SDK.Spells.CollisionType.ObjAiMinion,
                               EloBuddy.SDK.Spells.CollisionType.YasuoWall }
        };

        static readonly string ChampionName = "Blitzcrank";
        static readonly string Version = "1.0";
        static readonly string Date = "3/9/16";
        

        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }
        #endregion

        #region Events
        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }

            Chat.Print("<font color='#0040FF'>T7</font><font color='#CDD411'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#3737E6'>Toyota</font><font color='#D61EBE'>7</font><font color='#FF0000'> <3 </font>");

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Game.OnTick += OnTick;
            Spellbook.OnCastSpell += delegate (Spellbook sender, SpellbookCastSpellEventArgs args2)
            {
                if (sender.Owner.IsMe && args2.Slot.Equals(SpellSlot.E))
                {
                    Orbwalker.ResetAutoAttack();
                }
            };
            Spellbook.OnPostCastSpell += delegate (Spellbook sender, SpellbookCastSpellEventArgs args3)
            {
                if ((check(misc, "EAUTO") || check(combo, "CEONLY")) && sender.Owner.IsMe && args3.Slot.Equals(SpellSlot.Q) &&
                    EntityManager.Heroes.Enemies.Where(x => x.HasBuff("rocketgrab2")).Any() && DemSpells.E.Cast())
                {
                    return;
                }
            };
            Interrupter.OnInterruptableSpell += delegate (Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args4)
            {
                if (check(misc, "RINT") && sender.IsEnemy && DemSpells.R.IsInRange(sender.Position) && DemSpells.R.IsReady() && DemSpells.R.Cast())
                {
                    return;
                }
            };
            Gapcloser.OnGapcloser += delegate (AIHeroClient sender, Gapcloser.GapcloserEventArgs args5)
            {
                if (check(misc, "QGAP") && sender.IsEnemy && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(args5.End))
                {
                    QDATA.Target = sender;

                    Prediction.Manager.PredictionOutput qpred = Prediction.Manager.GetPrediction(QDATA);

                    if (qpred != null && qpred.HitChance == HitChance.Dashing && DemSpells.Q.Cast(qpred.CastPosition))
                    {
                        return;
                    }
                }
            };
            Orbwalker.OnPostAttack += delegate (AttackableUnit sender, EventArgs args6)
            {
                if (check(combo, "CE") && !check(combo, "CEONLY") && sender.IsMe && myhero.CountEnemiesInRange(myhero.GetAutoAttackRange()) > 1 &&
                    Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && DemSpells.E.IsReady() && DemSpells.E.Cast())
                {
                    return;
                }
            };
            Game.OnTick += delegate (EventArgs args7)
            {
                if (DemSpells.E.IsReady() && (check(misc, "EAUTO") || check(combo, "CEONLY")) && EntityManager.Heroes.Enemies.Where(x => x.HasBuff("rocketgrab2")).Any() && 
                    DemSpells.E.Cast())
                {
                    AttackTarget(EntityManager.Heroes.Enemies.Where(x => x.HasBuff("rocketgrab2")).FirstOrDefault());
                }
            };


            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            RPotion = new Item((int)ItemId.Refillable_Potion);

            Player.LevelSpell(SpellSlot.Q);

            DatMenu();
            CheckPrediction();
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > slider(harass, "HMIN")) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > slider(laneclear, "LMIN")) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")) Jungleclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.None) && Orbwalker.DisableAttacking || Orbwalker.DisableMovement)
            {
                Orbwalker.DisableAttacking = false;
                Orbwalker.DisableMovement = false;
            }

            if (key(qsett, "FORCEQ"))
            {
                Orbwalker.MoveTo(Game.CursorPos);
            }

            Misc();
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
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.E) && Player.LevelSpell(SpellSlot.E))
                    {
                        return;
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.W) && Player.LevelSpell(SpellSlot.W))
                    {
                        return;
                    }
                }
            }, 1000);
        }
        
        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Yellow) : SharpDX.Color.Yellow,
                    DemSpells.Q.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Yellow) : SharpDX.Color.Yellow,
                    DemSpells.R.Range,
                    myhero.Position
                );
            }

            AIHeroClient target = TargetSelector.GetTarget(DemSpells.Q.Range, DamageType.Magical, myhero.Position);

            if (target != null)
            {
                if (DemSpells.Q.IsReady())
                {
                    /*  Prediction.Manager.PredictionOutput qpred = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                      {
                          Delay = DemSpells.Q.CastDelay,
                          Radius = DemSpells.Q.Radius,
                          Range = DemSpells.Q.Range,
                          Speed = DemSpells.Q.Speed,
                          Type = SkillShotType.Linear,
                          From = myhero.Position,
                          Target = target,
                          CollisionTypes = { EloBuddy.SDK.Spells.CollisionType.YasuoWall }
                      });*/

                    QDATA.Target = target;
                    var qpred = Prediction.Manager.GetPrediction(QDATA);

                    if (check(draw, "DRAWPRED"))
                    {
                        Geometry.Polygon.Rectangle Prediction = new Geometry.Polygon.Rectangle(myhero.Position.To2D(), qpred.CastPosition.To2D(), DemSpells.Q.Width);
                        Prediction.Draw(Color.Yellow, 1);
                    }

                    if (check(draw, "DRAWHIT"))
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50,
                                    Drawing.WorldToScreen(myhero.Position).Y + 10,
                                    Color.Yellow,
                                    "Hitchance %: ");
                        Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X + 37,
                                            Drawing.WorldToScreen(myhero.Position).Y + 10,
                                            Color.Green,
                                            qpred.HitChancePercent.ToString());
                    }
                }

                if (check(draw, "DRAWTARGET"))
                {
                    Circle.Draw(SharpDX.Color.Yellow, 50, target.Position);
                }

                if (check(draw, "DRAWWAY") && target.Path.Any())
                {
                    for (var i = 1; target.Path.Length > i; i++)
                    {
                        if (target.Path[i - 1].IsValid() && target.Path[i].IsValid() && (target.Path[i - 1].IsOnScreen() || target.Path[i].IsOnScreen()))
                        {
                            Drawing.DrawLine(Drawing.WorldToScreen(target.Position), Drawing.WorldToScreen(target.Path[i]), 3, Color.White);
                        }
                    }
                }
            }
        }
        #endregion

        #region Modes
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(DemSpells.Q.Range, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget((int)DemSpells.Q.Range))
            {
                if (check(combo, "CQ") && DemSpells.Q.IsReady() && check(qsett, "Q" + target.ChampionName))
                {
                    if (check(qsett, "QCLOSE") && target.Distance(myhero.Position) < myhero.GetAutoAttackRange()) return;

                    /*    Prediction.Manager.PredictionOutput Qpred = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                        {
                            Delay = DemSpells.Q.CastDelay,
                            Radius = DemSpells.Q.Radius,
                            Range = DemSpells.Q.Range,
                            Speed = DemSpells.Q.Speed,
                            Type = SkillShotType.Linear,
                            From = myhero.Position,
                            Target = target,
                            CollisionTypes = { EloBuddy.SDK.Spells.CollisionType.YasuoWall }
                        });*/
                    QDATA.Target = target;
                    var Qpred = Prediction.Manager.GetPrediction(QDATA);

                    if (Qpred.HitChancePercent >= slider(qsett, "QPRED") && DemSpells.Q.Cast(Qpred.CastPosition))
                    {
                        return;
                    }
                }

                if (check(combo, "CW") && DemSpells.W.IsReady() && myhero.CountEnemiesInRange(500) > 0 && DemSpells.W.Cast())
                {
                    return;
                }

                if (check(combo, "CE") && DemSpells.E.IsReady() && myhero.CountEnemiesInRange(300) > 0 && DemSpells.E.Cast() && !check(combo, "CEONLY"))
                {
                    AttackTarget(target);
                }

                if (check(combo, "CR") && DemSpells.R.IsReady() && 
                    ((check(combo, "CRAUTO") && EntityManager.Heroes.Enemies.Any(x => (x.HasBuffOfType(BuffType.Knockup) || x.HasBuffOfType(BuffType.Stun)) && DemSpells.R.IsInRange(x))) || 
                    myhero.CountEnemiesInRange(DemSpells.R.Range - 10) >= slider(combo, "CRMIN") || (DemSpells.Q.IsOnCooldown && DemSpells.E.IsOnCooldown && myhero.CountEnemiesInRange(DemSpells.R.Range - 10) > 0)))
                {
                    if (check(combo, "CRAVOID") && target.Health < myhero.GetSpellDamage(target, SpellSlot.R)) return;

                    DemSpells.R.Cast();
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(DemSpells.Q.Range, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget((int)DemSpells.Q.Range))
            {
                if (check(harass, "HQ") && DemSpells.Q.IsReady() && check(qsett, "Q" + target.ChampionName))
                {
                   /* Prediction.Manager.PredictionOutput Qpred = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        Delay = DemSpells.Q.CastDelay,
                        Radius = DemSpells.Q.Radius,
                        Range = DemSpells.Q.Range,
                        Speed = DemSpells.Q.Speed,
                        Type = SkillShotType.Linear,
                        From = myhero.Position,
                        Target = target,
                        CollisionTypes = { EloBuddy.SDK.Spells.CollisionType.YasuoWall,
                                           EloBuddy.SDK.Spells.CollisionType.ObjAiMinion,
                                           EloBuddy.SDK.Spells.CollisionType.AiHeroClient }
                    });*/
                    
                    QDATA.Target = target;
                    var Qpred = Prediction.Manager.GetPrediction(QDATA);

                    if (Qpred.HitChancePercent >= slider(qsett, "QPRED") && DemSpells.Q.Cast(Qpred.CastPosition))
                    {
                        return;
                    }
                }

                if (check(harass, "HW") && DemSpells.W.IsReady() && myhero.CountEnemiesInRange(myhero.GetAutoAttackRange()) >= slider(harass, "HWMIN") && DemSpells.W.Cast())
                {
                    return;
                }

                if (check(harass, "HR") && DemSpells.R.IsReady() && myhero.CountEnemiesInRange(DemSpells.R.Range - 10) >= slider(harass, "HRMIN") && DemSpells.R.Cast())
                {
                    return;
                }
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.Q.Range).ToList();

            if (minions != null)
            {
                if (check(laneclear, "LQ") && DemSpells.Q.IsReady())
                {
                    foreach (Obj_AI_Minion minion in minions.Where(x => x.Distance(myhero.Position) < DemSpells.Q.Range - 75))
                    {
                        QDATA.Target = minion;

                        Prediction.Manager.PredictionOutput qpred = Prediction.Manager.GetPrediction(QDATA);

                        if (qpred.HitChancePercent >= slider(qsett, "QPRED") && DemSpells.Q.Cast(qpred.CastPosition))
                        {
                            return;
                        }
                    }
                }

                if (check(laneclear, "LW") && DemSpells.W.IsReady() && minions.Where(x => x.Distance(myhero.Position) < 250).Count() >= slider(jungleclear, "JWMIN") &&
                    DemSpells.W.Cast())
                {
                    return;
                }

                if (check(laneclear, "LE") && DemSpells.E.IsReady() && minions.Any(x => x.Health > 50 && x.Distance(myhero.Position) < myhero.GetAutoAttackRange()) && 
                    DemSpells.E.Cast())
                {
                    return;
                }

                if (check(laneclear, "LR") && DemSpells.R.IsReady() && minions.Where(x => x.Distance(myhero.Position) < DemSpells.R.Range - 10).Count() >= slider(jungleclear, "JRMIN") &&
                    DemSpells.R.Cast())
                {
                    return;
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, DemSpells.Q.Range);

            if (Monsters != null)
            {
                if (check(jungleclear, "JQ") && DemSpells.Q.IsReady())
                {
                    foreach (Obj_AI_Minion monster in Monsters)
                    {
                        if (comb(jungleclear, "JQMODE") == 0 && monster.BaseSkinName.Contains("Mini")) continue;

                        QDATA.Target = monster;

                        Prediction.Manager.PredictionOutput qpred = Prediction.Manager.GetPrediction(QDATA);

                        if (qpred.HitChancePercent >= slider(qsett, "QPRED") && DemSpells.Q.Cast(qpred.CastPosition))
                        {
                            return;
                        }
                    }
                }

                if (check(jungleclear, "JW") && DemSpells.W.IsReady() && Monsters.Where(x => x.Distance(myhero.Position) < 250).Count() >= slider(jungleclear, "JWMIN") &&
                    DemSpells.W.Cast())
                {
                    return;
                }

                if (check(jungleclear, "JE") && DemSpells.E.IsReady())
                {
                    foreach (Obj_AI_Minion monster in Monsters.Where(x => x.Distance(myhero.Position) < myhero.GetAutoAttackRange()))
                    {
                        if (comb(jungleclear, "JEMODE") == 0 && monster.BaseSkinName.Contains("Mini")) continue;

                        if (DemSpells.E.Cast())
                        {
                            Orbwalker.DisableAttacking = true;
                            Orbwalker.DisableMovement = true;

                            Player.IssueOrder(GameObjectOrder.AttackUnit, monster);

                            Orbwalker.DisableAttacking = false;
                            Orbwalker.DisableMovement = false;
                        }
                    }
                }

                if (check(jungleclear, "JR") && DemSpells.R.IsReady() && Monsters.Where(x => x.Distance(myhero.Position) < DemSpells.R.Range - 10).Count() >= slider(jungleclear, "JRMIN") &&
                    DemSpells.R.Cast())
                {
                    return;
                }
            }
        }

        private static void Misc()
        {
            var Qtarget = TargetSelector.GetTarget(DemSpells.Q.Range, DamageType.Magical, Player.Instance.Position);

            if (Qtarget != null && Qtarget.ValidTarget((int)DemSpells.Q.Range) && key(qsett, "FORCEQ") && DemSpells.Q.IsReady() && check(qsett, "Q" + Qtarget.ChampionName))
            {
                QDATA.Target = Qtarget;

                Prediction.Manager.PredictionOutput Qpred = Prediction.Manager.GetPrediction(QDATA);

                if (DemSpells.Q.Cast(Qpred.CastPosition))
                {
                    return;
                }
            }

            var target = TargetSelector.GetTarget(500, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget(1000) && check(misc, "KSR") && DemSpells.R.CanCast(target) && target.Health < myhero.GetSpellDamage(target, SpellSlot.R) &&
                Prediction.Health.GetPrediction(target, DemSpells.R.CastDelay) > 0 && DemSpells.R.Cast())
            {
                return;
            }

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee) && check(misc, "WFLEE") && DemSpells.W.IsReady() && DemSpells.W.Cast())
            {
                return;
            }

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);
        }
        
        #endregion

        #region Menu
        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 " + ChampionName, ChampionName.ToLower());
            qsett = menu.AddSubMenu("Q Settings", "qsettings");
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            jungleclear = menu.AddSubMenu("Jungleclear", "jclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");
            

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date);
            menu.AddLabel("Author: Toyota7");

            qsett.AddGroupLabel("Q Settings");
            qsett.AddSeparator();
            qsett.AddLabel("Forced Q Casting");
            qsett.Add("FORCEQ", new KeyBind("Force Q To Cast", false, KeyBind.BindTypes.HoldActive, 'B'));
            qsett.Add("QINFO", new CheckBox("Info About Forced Q Casting Keybind", false)).OnValueChange += 
                delegate (ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
                {
                    if (args.NewValue == true)
                    {
                        Chat.Print("<font color='#A5A845'>Force Q Casting Info</font>:");
                        Chat.Print("This Keybind Will Cast Q At The Target Champion Using The Current Q Prediction,");
                        Chat.Print("Which Means That It Will Ignore Collision Checks Or Hitchance Numbers Lower Than The Ones On The Settings.");
                        Chat.Print("You Can See The Current Q Prediction Using The Addon's Drawing Functions.");
                        Chat.Print("I Also Wouldnt Recommend Using This Function Without The Addon's Prediction Drawings(You Wont See The Cast Position Otherwise!).");
                        sender.CurrentValue = false;
                    }
                    else
                    {
                        return;
                    }
                };
            qsett.AddSeparator();
            qsett.AddLabel("Q Hitchance %");
            qsett.Add("QPRED", new Slider("Select Minimum Hitchance %", 65, 1, 100));
            qsett.AddSeparator();
            qsett.AddLabel("Q Targets:");
            foreach(AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                qsett.Add("Q" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
            qsett.AddSeparator();
            qsett.Add("QCLOSE", new CheckBox("Dont Grap If Target Is In AA Range"));


            combo.AddLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q"));
            combo.AddLabel("(For Q Options Go To Q Settings Tab)");
            combo.AddSeparator(10);
            combo.Add("CW", new CheckBox("Use W"));
            combo.Add("CWMIN", new Slider("Min Enemies Nearby To Cast W", 1, 1, 5));
            combo.AddSeparator(10);
            combo.Add("CE", new CheckBox("Use E"));
            combo.Add("CEONLY", new CheckBox("Only Use E After Succesful Grab"));
            combo.AddSeparator(10);
            combo.Add("CR", new CheckBox("Use R"));
            combo.Add("CRAUTO", new CheckBox("Auto R On Knocked-Up/Stunned Targets"));
            combo.Add("CRMIN", new Slider("Min Enemies For R", 2, 1, 5));
            combo.Add("CRAVOID", new CheckBox("Prevent R Killsteals"));


            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", false));
            harass.AddLabel("(For Q Options Go To Q Settings Tab)");
            harass.AddSeparator(10);
            harass.Add("HW", new CheckBox("Use W", false));
            harass.Add("HWMIN", new Slider("Min Enemies For W", 2, 1, 5));
            harass.AddSeparator();
            harass.Add("HR", new CheckBox("Use R", false));
            harass.Add("HRMIN", new Slider("Min Enemies For R", 2, 1, 5));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 1, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q"));
            laneclear.AddSeparator(10);
            laneclear.Add("LW", new CheckBox("Use W"));
            laneclear.Add("LWMIN", new Slider("Min Minions For W", 3, 1, 10));
            laneclear.AddSeparator(20);
            laneclear.Add("LE", new CheckBox("Use E"));
            laneclear.AddSeparator(10);
            laneclear.Add("LR", new CheckBox("Use R", false));
            laneclear.Add("LRMIN", new Slider("Min Minions For R", 4, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 1, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q"));
            jungleclear.Add("JQMODE", new ComboBox("Select Q Targets", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W"));
            jungleclear.Add("JWMIN", new Slider("Min Monsters For W", 2, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E"));
            jungleclear.Add("JEMODE", new ComboBox("Select E Targets", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JR", new CheckBox("Use R"));
            jungleclear.Add("JRMIN", new Slider("Min Monsters For R", 3, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 50, 1, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range"));
            draw.Add("drawR", new CheckBox("Draw R Range"));
            draw.AddSeparator();
            draw.Add("nodrawc", new CheckBox("Draw Only Ready Spells", false));
            draw.AddSeparator();
            draw.AddGroupLabel("Q Drawings");
            draw.Add("DRAWPRED", new CheckBox("Draw Q Prediction", false));
            draw.AddSeparator(1);
            draw.Add("DRAWTARGET", new CheckBox("Draw Q Target", false));
            draw.AddSeparator(1);
            draw.Add("DRAWHIT", new CheckBox("Draw Q Hitchance", false));
            draw.AddSeparator(1);
            draw.Add("DRAWWAY", new CheckBox("Draw Targets Waypoint", false));

            misc.AddLabel("Killsteal");
            misc.Add("KSR", new CheckBox("Killsteal with R"));
            misc.AddSeparator(1);
            misc.Add("WFLEE", new CheckBox("Use W To Flee"));
            misc.AddSeparator(1);
            misc.Add("RINT", new CheckBox("Use R To Interrupt"));
            misc.AddSeparator(1);
            misc.Add("QGAP", new CheckBox("Use Q On Gapclosers", false));
            misc.AddSeparator(1);
            misc.Add("EAUTO", new CheckBox("Auto-Cast E If Q Catches An Enemy", false));
            misc.AddSeparator(1);
            misc.AddLabel("Auto Potion");
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion"));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells"));
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack"));
            misc.Add("skinID", new ComboBox("Skin Hack", 11, new string[]
            {
                "Default",
                "Rusty",
                "Goalkeeper",
                "Boom Boom",
                "Piltover Customs",
                "Definitely Not",
                "iBlitz",
                "Riot",
                "Chroma Red",
                "Chroma Blue",
                "Chroma Gray",
                "Battle Boss"
            }));
        }
        #endregion

        #region Methods
        private static void CheckPrediction()
        {

            string CorrectPrediction = "SDK Beta Prediction";

            if (Prediction.Manager.PredictionSelected == CorrectPrediction)
            {
                return;
            }
            else
            {
                Prediction.Manager.PredictionSelected = CorrectPrediction;
                Chat.Print("<font color='#00D118'>T7 Blitzcrank: Prediction Has Been Automatically Changed!</font>");
                return;
            }
        }

        private static void AttackTarget(AIHeroClient target)
        {
            Orbwalker.DisableMovement = true;
            Orbwalker.DisableAttacking = true;

            Player.IssueOrder(GameObjectOrder.AttackUnit, target);

            Orbwalker.DisableMovement = false;
            Orbwalker.DisableAttacking = false;
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
        public static Spell.Active W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Active R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 925, SkillShotType.Linear, 240, 1800, 70)
            {
                AllowedCollisionCount = 0,
            };
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Active(SpellSlot.E);
            R = new Spell.Active(SpellSlot.R, 550);
        }
    }

    public static class Extensions
    {
        public static bool ValidTarget(this AIHeroClient hero, int range)
        {
            return !hero.HasBuff("UndyingRage") && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("ChronoShift") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("bansheesveil") && !hero.HasBuff("fioraw") &&
                   !hero.IsInvulnerable && !hero.IsDead && hero.IsValidTarget(range) && !hero.IsZombie &&
                   !hero.HasBuffOfType(BuffType.Invulnerability) && !hero.HasBuffOfType(BuffType.SpellImmunity) && !hero.HasBuffOfType(BuffType.SpellShield);

        }
    }
}
