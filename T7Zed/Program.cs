using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Spells;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace T7_Zed
{
    class Program
    {
        private static void Main(string[] args) { Loading.OnLoadingComplete += new Loading.LoadingCompleteHandler(OnLoad); }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        public static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;

        public static Spell.Skillshot Q;
        public static Spell.Targeted W;
        public static Spell.Skillshot JungleW;
        private static Spell.Active E;
        private static Spell.Targeted R;
        private static Spell.Targeted Ignite;

        public static GameObject WShadow = null;
        private static GameObject RShadow = null;
        static bool BlockW = false;
        static bool BlockR = false;
        static string SkinName = "ZedShadow", WBuffName = "zedwshadowbuff", WHeroBuffName = "ZedWHandler",
                      RBuffName = "zedrshadowbuff", RMarkName = "zedulttargetmark", RHeroBuffName = "ZedRHandler";
        static float TotalRDamage;

        private static string ChampionName { get { return "Zed"; } }
        private static string Version { get { return "1.0"; } }
        private static string Date { get { return "11/9/16"; } }

        public static Item tiamat { get; private set; }
        public static Item rhydra { get; private set; }
        public static Item thydra { get; private set; }
        public static Item cutl { get; private set; }
        public static Item blade { get; private set; }
        public static Item yomus { get; private set; }
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName == ChampionName)
            {
                Chat.Print("<font color='#0040FF'>T7</font><font color='#FF0505'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
                Chat.Print("<font color='#04B404'>By </font><font color='#3737E6'>Toyota</font><font color='#D61EBE'>7</font><font color='#FF0000'> <3 </font>");
                Drawing.OnDraw += OnDraw;
                Obj_AI_Base.OnLevelUp += OnLvlUp;
                Game.OnTick += OnTick;
                Orbwalker.OnUnkillableMinion += OnUnkillable;
                Obj_AI_Base.OnBuffGain += OnBuffGain;
                Obj_AI_Base.OnBuffLose += OnBuffLoss;
                Spellbook.OnCastSpell += OnCast;
                AttackableUnit.OnDamage += OnDamage;

                tiamat = new Item(3077, 400f);
                rhydra = new Item(3074, 400f);
                thydra = new Item(3748, 0f);
                cutl = new Item(3144, 550f);
                blade = new Item(3153, 550f);
                yomus = new Item(3142, 0f);
                Potion = new Item(2003, 0f);
                Biscuit = new Item(2010, 0f);
                RPotion = new Item(2031, 0f);

                Player.LevelSpell(SpellSlot.Q);

                SetSpells();
                SetMenu();
            }
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (!args.Source.IsMe) return;

            var target = args.Target as AIHeroClient;

            if (target != null && target.HasBuff(RMarkName))
            {
                TotalRDamage += args.Damage;
            }
        }

        static void OnCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe) return;

            if (args.Slot == SpellSlot.W && HasFlag(Orbwalker.ActiveModes.LaneClear) || HasFlag(Orbwalker.ActiveModes.JungleClear) || HasFlag(Orbwalker.ActiveModes.Flee) || HasFlag(Orbwalker.ActiveModes.Harass))
            {
                if (!BlockW)
                {
                    BlockW = true;
                }
                else
                {
                    args.Process = false;
                }
            }

            if (args.Slot == SpellSlot.R && HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (!BlockR)
                {
                    BlockR = true;
                }
                else
                {
                    args.Process = false;
                }
            }
        }

        private static void OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (sender.BaseSkinName == SkinName)
            {
                if (args.Buff.Name == WBuffName) WShadow = sender;

                else if (args.Buff.Name == RBuffName) RShadow = sender;
            }

            if (sender.IsEnemy && args.Buff.Name.Equals(RMarkName)) TotalRDamage = myhero.TotalAttackDamage;
        }

        private static void OnBuffLoss(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            if (sender.BaseSkinName == SkinName)
            {
                if (args.Buff.Name == WBuffName) WShadow = null;

                else if (args.Buff.Name == RBuffName) RShadow = null;
            }

            if (sender.IsMe && args.Buff.Name.Equals(WHeroBuffName)) BlockW = false;

            if (sender.IsMe && args.Buff.Name.Equals(WHeroBuffName)) BlockR = false;

            if (sender.IsEnemy && args.Buff.Name.Equals(RMarkName)) TotalRDamage = 0f;
        }

        private static void OnUnkillable(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (target != null && !target.IsUnderEnemyturret() && target.IsEnemy && target.IsMinion && target.IsValidTarget(Q.Range))
            {
                if (Q.IsReady() && check(laneclear, "QUNKILL") && myhero.GetSpellDamage(target, SpellSlot.Q) > args.RemainingHealth &&
                    Prediction.Health.GetPrediction(target, Q.CastDelay + (int)(Dist(target.Position) / (float)Q.Speed) * 1000) > 5 && Q.CastMinimumHitchance(target, HitChance.Medium))
                    return;

                if (E.IsReady() && target.IsValidTarget(E.Range) && check(laneclear, "EUNKILL") && myhero.GetSpellDamage(target, SpellSlot.E, DamageLibrary.SpellStages.Default) > args.RemainingHealth &&
                    Prediction.Health.GetPrediction(target, E.CastDelay) > 5 && E.Cast())
                    return;
            }
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    Harass();
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    Laneclear();
                    break;
                case Orbwalker.ActiveModes.JungleClear:
                    Jungleclear();
                    break;
                case Orbwalker.ActiveModes.LastHit:
                    Lasthit();
                    break;
                case Orbwalker.ActiveModes.Flee:
                    Flee();
                    break;
            }

            Misc();
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe || !check(misc, "autolevel")) return;

            Core.DelayAction(() =>
            {
                if (myhero.Level > 1 && myhero.Level < 4)
                {
                    switch(myhero.Level)
                    {
                        case 2:
                            Player.LevelSpell(SpellSlot.W);
                            break;
                        case 3:
                            Player.LevelSpell(SpellSlot.E);
                            break;
                    }
                }
                else if (myhero.Level >= 4 && myhero.Level < 19)
                {
                    if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.R) && Player.LevelSpell(SpellSlot.R))
                        return;

                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.Q) && Player.LevelSpell(SpellSlot.Q))
                        return;

                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.E) && Player.LevelSpell(SpellSlot.E))
                        return;

                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.W) && Player.LevelSpell(SpellSlot.W))
                        return;
                }
            }, new Random().Next(400, 600));
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && Q.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkRed) : SharpDX.Color.DarkRed,
                    Q.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawW") && W.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkRed) : SharpDX.Color.DarkRed,
                    W.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawE") && E.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkRed) : SharpDX.Color.DarkRed,
                    E.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawR") && R.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.DarkRed) : SharpDX.Color.DarkRed,
                    R.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawk"))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered && x.IsNearSight && x.IsVisible && x.VisibleOnScreen))
                {
                    if (ComboDamage(enemy) > enemy.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30,
                                            Color.Green, "Killable With Combo");
                    }
                    else if (ComboDamage(enemy) + myhero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) > enemy.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30,
                                            Color.Green, "Combo + Ignite");
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1300f, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.ValidTarget(1300))
            {
                CastR(target);

                ItemManager(target);

                if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && Q.IsReady() && check(combo, "CE") && check(combo, "CW") && check(combo, "CQ") && GetEnergyCost('W', 'E', 'Q') <= myhero.Mana)
                {
                    if (Dist(target.Position) < W.Range)
                    {
                        var castPos = Prediction.Position.PredictUnitPosition(target, W.CastDelay).To3D();//myhero.Position.Extend(target.Position, Dist(target.Position) * 0.8f).To3D();
                        var delay = (int)(Dist(castPos) / 1750) * 1000;

                        if (W.Cast(castPos))
                        {
                            Core.DelayAction(() =>
                            {
                                E.Cast();
                                CastQWithShadow(target);
                            }, delay);
                        }
                    }
                    else
                    {
                        var castPos = target.Position;//myhero.Position.Extend(target.Position, E.Range).To3D();
                        var delay = (int)(Dist(castPos) / 1750) * 1000;

                        if (W.Cast(castPos))
                        {
                            Core.DelayAction(() =>
                            {
                                BlockW = false;
                                W.Cast(target.Position);
                                Core.DelayAction(() =>
                                {
                                    if (E.IsInRange(target.Position)) E.Cast();

                                    CastQWithShadow(target);
                                }, 50);
                            }, delay);
                        }
                    }
                }
                else if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && check(combo, "CE") && check(combo, "CW") && GetEnergyCost('W', 'E') <= myhero.Mana)
                {
                    var pred = Prediction.Position.PredictCircularMissile(target, W.Range, (int)E.Range, W.CastDelay + E.CastDelay, 1750, myhero.Position, true);

                    if (W.Cast(pred.CastPosition))
                        Core.DelayAction(() =>
                        {
                            E.Cast();
                        }, (int)(Dist(pred.CastPosition) / 1750) * 1050);
                }
                else if (W.IsReady() && W.Name == "ZedW" && Q.IsReady() && check(combo, "CW") && check(combo, "CQ") && GetEnergyCost('W', 'Q') <= myhero.Mana)
                {

                    var castPos = comb(pred, "QPRED2") == 0 ? Q.GetPrediction(target).CastPosition : Q.GetICPrediction(target).CastPosition;
                   // var loc = myhero.Position.Extend(castPos, Dist(castPos) * 0.2f).To3D();

                    if (W.Cast(castPos))
                        Core.DelayAction(() => Q.Cast(castPos), (int)(Dist(castPos) / 1750) * 1000);
                }
                else
                {
                    if (check(combo, "CQ") && Q.IsReady() && Q.Cast(target, slider(pred, "QPred"), myhero.Position, comb(pred, "QPRED2") == 0 ? false : true))
                        return;

                    if (check(combo, "CE") && E.IsReady() && (myhero.CountEnemiesInRange(E.Range) >= 1 || WShadow !=null && EloBuddy.SDK.Extensions.CountEnemiesInRange(WShadow.Position, E.Range) >= 1) && E.Cast())
                        return;
                }

                if (WShadow != null && W.IsReady() && check(combo, "CW") && target.Distance(WShadow.Position) < target.Distance(myhero.Position))
                {
                    var shadow = WShadow as Obj_AI_Base;

                    if (shadow.UnderEnemyTurret() || (target.HasBuff(RMarkName) && myhero.IsInAutoAttackRange(target))) return;

                    W.Cast(myhero.Position);
                }

                if (RShadow != null && R.IsReady() && check(combo, "CR") && TotalRDamage > target.Health && EloBuddy.SDK.Extensions.CountEnemiesInRange(RShadow.Position, E.Range) > 0)
                {
                    var ClosestEnemyPos = EntityManager.Heroes.Enemies.Where(x => x.ValidTarget((int)R.Range)).OrderBy(x => Dist(x.Position)).FirstOrDefault().Position;

                    if (ClosestEnemyPos != null && myhero.CountEnemiesInRange(R.Range) > 1 && Dist(ClosestEnemyPos) < ClosestEnemyPos.Distance(RShadow.Position)) return;

                    BlockR = false;
                    R.Cast(myhero);
                }

                if (Ignite != null && Ignite.CanCast(target) && ComboDamage(target) > target.Health || target.HasBuff(RMarkName) && Ignite.Cast(target)) return;

         //       if (Ignite != null && Ignite.CanCast(target) && !target.HasBuff(RMarkName) && ComboDamage(target) > target.Health && )
            
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200f, DamageType.Physical, new Vector3?(Player.Instance.Position), false);

            if (target != null && target.ValidTarget(1200))
            {
                var Wpred = Prediction.Position.PredictUnitPosition(target, W.CastDelay + (int)(Dist(target.Position) / 1750) * 1000).To3D();

                if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && Q.IsReady() && check(harass, "HE") && check(harass, "HW") && check(harass, "HQ") && GetEnergyCost('W', 'E', 'Q') <= myhero.Mana)
                {
                    if (Wpred != null && W.Cast(Wpred))
                    {
                        Core.DelayAction(() =>
                        {
                            E.Cast();
                            Core.DelayAction(() => CastQWithShadow(target), 50);
                        }, (int)(Dist(Wpred) / 1750) * 1050);
                    }
                }

                else if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && check(harass, "HE") && check(harass, "HW") && GetEnergyCost('W', 'E') <= myhero.Mana)
                {
                    if (Wpred != null && W.Cast(Wpred))
                        Core.DelayAction(() => E.Cast(), (int)(Dist(Wpred) / 1750) * 1050);
                }

                else if (W.IsReady() && W.Name == "ZedW" && Q.IsReady() && check(harass, "HW") && check(harass, "HQ") && GetEnergyCost('W', 'Q') <= myhero.Mana)
                {
                    var castPos = comb(pred, "QPRED2") == 0 ? Q.GetPrediction(target).CastPosition : Q.GetICPrediction(target).CastPosition;

                    if (W.Cast(castPos))
                        Core.DelayAction(() => Q.Cast(castPos), (int)(Dist(castPos) / 1750) * 1000);
                }

                else
                {
                    if (check(laneclear, "LQ") && Q.IsReady() && Q.CastOnBestFarmPosition(slider(laneclear, "LQMIN")))
                        return;

                    if (check(laneclear, "LE") && E.IsReady() && myhero.CountEnemyMinionsInRange(E.Range) >= slider(laneclear, "LEMIN") && E.Cast())
                        return;
                }

            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, Q.Range);

            if (minions != null)
            {
                Prediction.Manager.PredictionSelected = "SDK Prediction";

                var pred = Prediction.Position.GetPredictionAoe(minions.ToArray<Obj_AI_Base>(), new Prediction.Position.PredictionData
                                                                (Prediction.Position.PredictionData.PredictionType.Circular, (int)W.Range, (int)E.Range, 0, W.CastDelay + E.CastDelay, 1750, int.MaxValue, myhero.Position))
                           .Where(location => location.CollisionObjects.Count() > slider(laneclear, "LEMIN"))
                           .OrderByDescending(x => x.CollisionObjects.Count())
                           .FirstOrDefault();

                var ShouldSwap = WShadow != null && comb(laneclear, "LWMIN") != 0 && EloBuddy.SDK.Extensions.CountEnemiesInRange(WShadow.Position, slider(laneclear, "LWRANGE")) < comb(laneclear, "LWMIN") &&
                                 !WShadow.Position.IsUnderTurret();

                if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && Q.IsReady() && check(laneclear, "LE") && check(laneclear, "LW") && check(laneclear, "LQ") && GetEnergyCost('W', 'E', 'Q') <= myhero.Mana)
                {
                    if (pred != null && W.Cast(pred.CastPosition))
                    {
                        Core.DelayAction(() =>
                        {
                            E.Cast();
                            Core.DelayAction(() =>
                            {
                                CastQWithShadow(minions, slider(laneclear, "LQMIN"));
                                SwapW(ShouldSwap, true);
                            }, 50);
                        }, (int)(Dist(pred.CastPosition) / 1750) * 1050);
                    }
                }

                else if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && check(laneclear, "LE") && check(laneclear, "LW") && GetEnergyCost('W', 'E') <= myhero.Mana)
                {
                    if (pred != null && W.Cast(pred.CastPosition))
                        Core.DelayAction(() =>
                        {
                            E.Cast();
                            SwapW(ShouldSwap, false);
                        }, (int)(Dist(pred.CastPosition) / 1750) * 1050);
                }

                else if (W.IsReady() && W.Name == "ZedW" && Q.IsReady() && check(laneclear, "LW") && check(laneclear, "LQ") && GetEnergyCost('W', 'Q') <= myhero.Mana)
                {
                    foreach (var minion in minions.Where(x => Q.CanCast(x) && Q.GetPrediction(x).CollisionObjects.Count() >= slider(laneclear, "LQMIN")))
                    {
                        var qpred = Q.GetPrediction(minion);
                        var loc = myhero.Position.Extend(qpred.CastPosition, Dist(qpred.CastPosition) * 0.2f).To3D();

                        if (W.Cast(loc))
                            Core.DelayAction(() => Q.Cast(qpred.CastPosition), (int)(Dist(qpred.CastPosition) / 1750) * 1000);

                        break;
                    }
                }

                else if (check(laneclear, "LRANDOM") && myhero.Mana >= slider(laneclear, "LMIN"))
                {
                    if (check(laneclear, "LQ") && Q.IsReady() && Q.CastOnBestFarmPosition(slider(laneclear, "LQMIN")))
                        return;

                    if (check(laneclear, "LE") && E.IsReady() && myhero.CountEnemyMinionsInRange(E.Range) >= slider(laneclear, "LEMIN") && E.Cast())
                        return;
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, Q.Range);

            if (Monsters != null)
            {
                var pred = Prediction.Position.GetPredictionAoe(Monsters.ToArray<Obj_AI_Base>(), new Prediction.Position.PredictionData
                                                                (Prediction.Position.PredictionData.PredictionType.Circular, (int)W.Range, (int)E.Range, 0, W.CastDelay + E.CastDelay, 1750, int.MaxValue, myhero.Position))
                           .OrderByDescending(x => x.CollisionObjects.Count())
                           .FirstOrDefault();

                if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && Q.IsReady() && check(jungleclear, "JE") && check(jungleclear, "JW") && check(jungleclear, "JQ") && GetEnergyCost('W', 'E', 'Q') <= myhero.Mana)
                {
                    if (pred != null && W.Cast(pred.CastPosition))
                    {
                        Core.DelayAction(() =>
                        {
                            E.Cast();
                            Core.DelayAction(() =>
                            {
                                CastQWithShadow(Monsters, slider(jungleclear, "JQMIN"), true);
                                SwapW(check(jungleclear, "JWMODE"), true);
                            }, 50);
                        }, (int)(Dist(pred.CastPosition) / 1750) * 1050);
                    }
                }

                else if (W.IsReady() && W.Name == "ZedW" && E.IsReady() && check(jungleclear, "JE") && check(jungleclear, "JW") && GetEnergyCost('W', 'E') <= myhero.Mana)
                {
                    if (pred != null && W.Cast(pred.CastPosition))
                        Core.DelayAction(() =>
                        {
                            E.Cast();
                            SwapW(check(jungleclear, "JWMODE"), false);
                        }, (int)(Dist(pred.CastPosition) / 1750) * 1050);
                }

                else if (W.IsReady() && W.Name == "ZedW" && Q.IsReady() && check(jungleclear, "JW") && check(jungleclear, "JQ") && GetEnergyCost('W', 'Q') <= myhero.Mana)
                {
                    foreach (var monster in Monsters.Where(x => Q.CanCast(x) && Q.GetICPrediction(x).CollisionObjects.Count() >= slider(jungleclear, "JQMIN")))
                    {
                        var PredPos = Q.GetICPrediction(monster).CastPosition;
                        var loc = myhero.Position.Extend(PredPos, Dist(PredPos) * 0.2f).To3D();

                        if (W.Cast(loc))
                            Core.DelayAction(() => Q.Cast(PredPos), (int)(Dist(PredPos) / 1750) * 1000);

                        break;
                    }
                }

                else if (check(jungleclear, "JRANDOM"))
                {
                    if (check(jungleclear, "JQ") && Q.IsReady() && Q.CastOnAOELocation(true))
                        return;

                    if (check(jungleclear, "JE") && E.IsReady() && Monsters.Count(x => Dist(x.Position) < E.Range - 5) >= slider(jungleclear, "JEMIN") && E.Cast())
                        return;
                }
            }
        }

        private static void Lasthit()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, Q.Range);

            if (minions != null)
            {
                if (check(misc, "QLAST") && Q.IsReady())
                {
                    foreach (var minion in minions.Where(x => Q.CanCast(x) && Prediction.Health.GetPrediction(x, Q.CastDelay + (int)(Dist(x.Position) / Q.Speed) * 1000) > 10 &&
                                                              myhero.GetSpellDamage(x, SpellSlot.Q) > x.Health + 10 && !(E.CanCast(x) && E.IsInRange(x.Position) && myhero.GetSpellDamage(x, SpellSlot.E) > x.Health + 10)))
                    {
                        Q.Cast(minion, 50);
                    }
                }

                if (check(misc, "ELAST") && E.IsReady())
                {
                    foreach (var minion in minions.Where(x => E.CanCast(x) && Prediction.Health.GetPrediction(x, E.CastDelay) > 10 && myhero.GetSpellDamage(x, SpellSlot.E) > x.Health + 10))
                    {
                        E.Cast();
                    }
                }
            }
        }

        static void Assassinate()
        {
            //Orbwalker.MoveTo(Game.CursorPos);

            var target = EntityManager.Heroes.Enemies.Where(x => Dist(x.Position) < W.Range + R.Range - 25 && x.Health < myhero.TotalAttackDamage + 200 - 10 && check(misc, x.ChampionName) &&
                                                                 Prediction.Health.GetPrediction(x, (R.IsInRange(x) ? R.CastDelay + 750 : R.CastDelay + 750) + W.CastDelay + ((int)((Dist(x.Position) / 2) / 1750) * 1000)) > 40)
                                                     .OrderByDescending(x => TargetSelector.GetPriority(x))
                                                     .FirstOrDefault();

            if (target != null && target.ValidTarget(1400) && R.IsReady())
            {
                var castpos = myhero.Position.Extend(target.Position, Dist(target.Position) / 2f).To3D();

                if (R.IsInRange(target.Position) && R.Cast(target))
                {
                    Core.DelayAction(() =>
                    {
                        if (Q.IsReady() && Q.Cast(target.Position))
                        {
                            BlockR = false;
                            R.Cast(myhero);
                        }
                        else
                        {
                            Orbwalker.DisableAttacking = true;
                            Orbwalker.DisableMovement = true;

                            Player.IssueOrder(GameObjectOrder.AttackUnit, target);

                            Orbwalker.DisableAttacking = false;
                            Orbwalker.DisableMovement = false;

                            Core.DelayAction(() =>
                            {
                                BlockR = false;
                                R.Cast(myhero);
                            }, 100);
                        }
                    }, 500);
                }
                else if (!R.IsInRange(target.Position) && W.IsReady() && W.Cast(castpos))
                {
                    Core.DelayAction(() =>
                    {
                        if (R.Cast(target))
                        {
                            Core.DelayAction(() =>
                            {
                                if (Q.CanCast(target)) Q.Cast(target.Position);
                                BlockR = false;
                                R.Cast(myhero);
                            }, 800);
                        }
                    }, (int)(Dist(castpos) / 1750) * 1010);
                }
            }
        }

        static void Flee()
        {
            if (check(misc, "WFLEE") && W.IsReady() && W.Cast(myhero.Position.Extend(Game.CursorPos, W.Range - 1).To3D()))
            {
                Core.DelayAction(() =>
                {
                    BlockW = false;
                    W.Cast(myhero.Position);
                }, (int)(Dist(myhero.Position.Extend(Game.CursorPos, W.Range - 1).To3D()) / 1750) * 1040);
            }

            if (yomus.IsOwned() && yomus.IsReady() && check(misc, "YOMUSFLEE") && yomus.Cast()) return;
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget((int)Q.Range))
            {
                if (check(misc, "KSQ") && Q.CanCast(target) && myhero.GetSpellDamage(target, SpellSlot.Q) > target.Health &&
                    Prediction.Health.GetPrediction(target, Q.CastDelay + (int)((target.Distance(myhero.Position) / Q.Speed) * 1000)) > 0)
                {

                }

                if (check(misc, "KSE") && E.CanCast(target) && myhero.GetSpellDamage(target, SpellSlot.E) > target.Health &&
                    Prediction.Health.GetPrediction(target, E.CastDelay) > 0 && E.Cast())
                    return;

                if (Ignite != null && check(misc, "autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) && Dist(target.Position) > myhero.GetAutoAttackRange() &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health && Ignite.Cast(target) && !target.HasBuff(RMarkName))
                    return;

                if (Ignite != null && check(misc, "autoign") && Ignite.CanCast(target) && Dist(target.Position) > myhero.GetAutoAttackRange() && !target.HasBuff(RMarkName) && 
                    (myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > (target.TotalShieldHealth() + (target.HPRegenRate * 5))))
                {
                    Ignite.Cast(target);
                }
            }

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            if (check(misc, "skinhax")) myhero.SetSkinId(misc["skinID"].Cast<ComboBox>().CurrentValue);

        //    Assassinate();
        }

        public static void SetMenu()
        {
            menu = MainMenu.AddMenu("T7 " + ChampionName, ChampionName.ToLower(), null);
            combo = menu.AddSubMenu("Combo", "combo", null);
            harass = menu.AddSubMenu("Harass", "harass", null);
            laneclear = menu.AddSubMenu("Laneclear", "lclear", null);
            jungleclear = menu.AddSubMenu("Jungleclear", "jclear", null);
            draw = menu.AddSubMenu("Drawings", "draw", null);
            misc = menu.AddSubMenu("Misc", "misc", null);
            pred = menu.AddSubMenu("Prediction", "pred", null);

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date, 25);
            menu.AddLabel("Author: Toyota7", 25);

            combo.AddLabel("Spells", 25);
            combo.Add("CQ", new CheckBox("Use Q"));
            combo.Add("CW", new CheckBox("Use W"));
            combo.Add("CE", new CheckBox("Use E"));
            combo.Add("CR", new CheckBox("Use R"));
            combo.Add("CRONLY", new CheckBox("Only Use R On Killable Targets"));
            combo.Add("CIgnite", new CheckBox("Use Ignite", false));
            combo.Add("ITEMS", new CheckBox("Use Items"));

            harass.AddLabel("Spells", 25);
            harass.Add("HQ", new CheckBox("Use Q", false));
            harass.Add("HW", new CheckBox("Use W", false));
            harass.Add("HE", new CheckBox("Use E", false));
            harass.AddSeparator(25);
            harass.Add("HMIN", new Slider("Min Energy % To Harass", 100, 1, 200));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", false));
            laneclear.Add("LQMIN", new Slider("Min Minions For Q", 2, 1, 10));
            laneclear.AddSeparator(25);
            laneclear.Add("LW", new CheckBox("Use W", false));
            laneclear.Add("LWMIN", new ComboBox("Swap With Shadow If Enemies Less Than X", 0, "Off", "1", "2", "3", "4", "5"));
            laneclear.Add("LWRANGE", new Slider("Min Range For Enemy Check", 800, 100, 1500));
            laneclear.AddSeparator(25);
            laneclear.Add("LE", new CheckBox("Use E", false));
            laneclear.Add("LEMIN", new Slider("Min Minions For E", 2, 1, 10));
            laneclear.AddSeparator(25);
            laneclear.Add("LRANDOM", new CheckBox("Enable Random Casting(No Combos)"));
            laneclear.AddSeparator(1);
            laneclear.Add("QUNKILL", new CheckBox("Auto Q On Unkillable Minions"));
            laneclear.Add("EUNKILL", new CheckBox("Auto E On Unkillable Minions", false));
            laneclear.AddSeparator(25);
            laneclear.Add("LMIN", new Slider("Min Energy % To Laneclear", 100, 1, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));
            jungleclear.Add("JQMIN", new Slider("Min Monsters For Q", 2, 1, 4));
            jungleclear.AddSeparator(25);
            jungleclear.Add("JW", new CheckBox("Use W", false));
            jungleclear.Add("JWMODE", new CheckBox("Swap To Shadow", false));
            jungleclear.AddSeparator(25);
            jungleclear.Add("JE", new CheckBox("Use E", false));
            jungleclear.Add("JEMIN", new Slider("Min Monsters For E", 1, 1, 4));
            jungleclear.AddSeparator(15);
            jungleclear.Add("JRANDOM", new CheckBox("Enable Random Casting(No Combos)"));
            jungleclear.AddSeparator(15);
            jungleclear.Add("JMIN", new Slider("Min Energy % To Jungleclear", 100, 1, 200));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator(25);
            draw.Add("drawQ", new CheckBox("Draw Q Range"));
            draw.Add("drawW", new CheckBox("Draw W Range"));
            draw.Add("drawE", new CheckBox("Draw E Range"));
            draw.Add("drawR", new CheckBox("Draw R Range"));
            draw.AddSeparator(25);
            draw.Add("nodrawc", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawk", new CheckBox("Draw Killable Enemies"));

            misc.AddLabel("Killsteal", 25);
            misc.Add("KSQ", new CheckBox("Killsteal with Q", false));
            misc.Add("KSE", new CheckBox("Killsteal with E"));
            if (Ignite != null) misc.Add("autoign", new CheckBox("Auto Ignite If Killable"));
            misc.AddSeparator(5);
            misc.AddLabel("_____________________________________________________________________________", 25);
            misc.AddSeparator(5);
            misc.AddLabel("Assassinating", 25);
            misc.Add("ASSKEY", new KeyBind("Keybind", false, KeyBind.BindTypes.HoldActive, 'N'));
            misc.Add("ASSINFO", new CheckBox("Print Info About This Function", false)).OnValueChange += delegate (ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue == true)
                {
                    Chat.Print("This Function Will Use R (If Target Is In R Range) Or W + R (If Out of R Range) To Mark A Killable Target And Then Go Back To A Safe Location Using The Available Shadows");
                    sender.CurrentValue = false;
                }
                else return;
            };
            misc.AddLabel("Select Targets:", 25);
            EntityManager.Heroes.Enemies.ToList().ForEach(x => misc.Add(x.ChampionName, new CheckBox(x.ChampionName)));
            misc.AddSeparator(5);
            misc.AddLabel("_____________________________________________________________________________", 25);
            misc.AddSeparator(5);
            misc.AddLabel("Gapclosers", 25);
            misc.Add("QGAP", new CheckBox("Use Q", false));
            misc.Add("EGAP", new CheckBox("Use E"));
            misc.AddSeparator(5);
            misc.AddLabel("_____________________________________________________________________________", 25);
            misc.AddSeparator(5);
            misc.AddLabel("Flee", 25);
            misc.Add("WFLEE", new CheckBox("Use W", false));
            misc.Add("YOMUSFLEE", new CheckBox("Use Youmuu's Ghostblade", false));
            misc.AddSeparator(5);
            misc.AddLabel("_____________________________________________________________________________", 25);
            misc.AddSeparator(5);
            misc.AddLabel("LastHit", 25);
            misc.Add("QLAST", new CheckBox("Use Q", false));
            misc.Add("ELAST", new CheckBox("Use E", false));
            misc.AddSeparator(5);
            misc.AddLabel("_____________________________________________________________________________", 25);
            misc.AddSeparator(5);
            misc.AddLabel("Auto Potion", 25);
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion"));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 25, 1, 100));
            misc.AddSeparator(5);
            misc.AddLabel("_____________________________________________________________________________", 25);
            misc.AddSeparator(5);
            misc.AddLabel("Auto Level Up Spells", 25);
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells"));
            misc.AddSeparator(5);
            misc.AddLabel("_____________________________________________________________________________", 25);
            misc.AddSeparator(5);
            misc.AddLabel("Skin Hack", 25);
            misc.Add("skinhax", new CheckBox("Activate Skin hack"));
            misc.Add("skinID", new ComboBox("Skin Hack", 10, new string[]
            {
                "Default",
                "Shockblade",
                "SKT T1",
                "PROJECT:",
                "Chroma Pink",
                "Chroma Yellow",
                "Chroma Blue",
                "Chroma Red",
                "Chroma Purple",
                "Chroma Green",
                "Championship"
            }));
            pred.AddGroupLabel("Prediction");
            pred.AddLabel("Q :", 25);
            pred.Add("QPred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.Add("QPRED2", new ComboBox("Sleect Prediction", 0, "Normal(old) Prediction", "ICPrediction"));
        }

        #region Methods
        static void CastR(AIHeroClient target)
        {
            if (
                 !check(combo, "CR") || !R.CanCast(target)  
                    ||
                  ComboDamage(target) - (myhero.GetSpellDamage(target, SpellSlot.R) +
                                        (Ignite.IsReady() ? myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) : 0) +
                                        myhero.TotalAttackDamage * PossibleAAs(target))
                                            > target.Health
                    ||
                  (check(combo, "CRONLY") && ComboDamage(target) < target.Health)
               )
                return;

            R.Cast(target);
        }

        static int PossibleAAs(AIHeroClient target)
        {
            return (int)Math.Max(0, Math.Floor(myhero.GetAutoAttackRange() / (target.MoveSpeed * Orbwalker.AttackCastDelay)));
        }

        static void CastQWithShadow(IEnumerable<Obj_AI_Base> Targets, int MinCollisions = 0, bool JungleMode = false)
        {
            var PredCheck = !JungleMode ? false : true;
            
            foreach (var minion in Targets.Where(x => Q.CanCast(x) && (!JungleMode ? Q.GetPrediction(x).CollisionObjects.Count() : Q.GetICPrediction(x).CollisionObjects.Count()) >= MinCollisions))
            {
                if (Q.Cast(minion, 50,myhero.Position, PredCheck)) return;

                else if (Q.Cast(minion, 50,WShadow.Position, PredCheck)) return;
            }
        }

        static void CastQWithShadow(AIHeroClient target)
        {
            var PredCheck = comb(pred, "QPRED2") == 0 ? false : true;

            if (Q.Cast(target, slider(pred, "QPred"), myhero.Position, PredCheck)) return;

            else if (WShadow != null && Q.Cast(target, slider(pred, "QPred"), WShadow.Position, PredCheck)) return;
        }

        static void SwapW(bool Checks, bool QCheck = true)
        {
            Core.DelayAction(() =>
            {
                if (Checks)
                {
                    BlockW = false;
                    W.Cast(myhero.Position);
                }
            }, (QCheck ? Q.CastDelay : E.CastDelay) + 100);
        }

        private static void ItemManager(AIHeroClient target)
        {
            if (target != null && target.IsValidTarget() && check(combo, "ITEMS"))
            {
                if (tiamat.IsOwned() && tiamat.IsReady() && tiamat.IsInRange(target.Position))
                {
                    tiamat.Cast();
                }

                if (rhydra.IsOwned() && rhydra.IsReady() && rhydra.IsInRange(target.Position))
                {
                    rhydra.Cast();
                }

                if (thydra.IsOwned() && thydra.IsReady() && target.Distance(myhero.Position) < Player.Instance.GetAutoAttackRange() && !Orbwalker.CanAutoAttack)
                {
                    thydra.Cast();
                }

                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position))
                {
                    cutl.Cast(target);
                }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position))
                {
                    blade.Cast(target);
                }

                if (yomus.IsOwned() && yomus.IsReady() && target.Distance(myhero.Position) < 1000)
                {
                    yomus.Cast();
                }
            }
        }

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (Q.IsLearned && Q.IsReady()) { TotalDamage += myhero.GetSpellDamage(target, SpellSlot.Q); }

                if (E.IsLearned && E.IsReady()) { TotalDamage += myhero.GetSpellDamage(target, SpellSlot.E); }

                if (R.IsLearned && R.IsReady()) { TotalDamage += myhero.GetSpellDamage(target, SpellSlot.R) + myhero.TotalAttackDamage * PossibleAAs(target); }

                if (tiamat.IsOwned() && tiamat.IsReady() && tiamat.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, tiamat.Id); }

                if (rhydra.IsOwned() && rhydra.IsReady() && rhydra.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, rhydra.Id); }

                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, cutl.Id); }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, blade.Id); }

                return TotalDamage;
            }
            return 0;
        }
        

    static bool HasFlag(Orbwalker.ActiveModes Mode)
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Mode);
        }

        private static float Dist(Vector3 Location)
        {
            return Location.Distance(myhero.Position, false);
        }

        private static int GetEnergyCost(params char[] Spells)
        {
            int[] QCost = new int[] { 75, 70, 65, 60, 55 };
            int[] WCost = new int[] { 40, 35, 30, 25, 20 };
            int TotalCost = 0;

            Spells.ToList().ForEach(x =>
            {
                if (x.Equals('Q')) TotalCost += QCost[Q.Level - 1];

                if (x.Equals('W')) TotalCost += QCost[W.Level - 1];

                if (x.Equals('E')) TotalCost += 50;
            });

            return TotalCost;
        }

        private static void SetSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 900, SkillShotType.Linear, 250, 1700, 50)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Targeted(SpellSlot.W, 700);
            JungleW = new Spell.Skillshot(SpellSlot.W, 700, SkillShotType.Circular, 250, 1750, 290);
            E = new Spell.Active(SpellSlot.E, 290);
            R = new Spell.Targeted(SpellSlot.R, 625);
            if (SummonerSpells.PlayerHas(SummonerSpellsEnum.Ignite))
            {
                Ignite = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 600);
            }
        }

        private static bool check(Menu submenu, string sig)
        {
            return submenu[sig].Cast<CheckBox>().CurrentValue;
        }

        private static int slider(Menu submenu, string sig)
        {
            return submenu[sig].Cast<Slider>().CurrentValue;
        }

        public static int comb(Menu submenu, string sig)
        {
            return submenu[sig].Cast<ComboBox>().CurrentValue;
        }

        private static bool key(Menu submenu, string sig)
        {
            return submenu[sig].Cast<KeyBind>().CurrentValue;
        }
        #endregion
    }

    public static class Extensions
    {
        public static bool CastOnAOELocation(this Spell.Skillshot spell, bool JungleMode = false)
        {
            var targets = JungleMode ? EntityManager.MinionsAndMonsters.GetJungleMonsters(Program.myhero.Position, (float)Program.Q.Range).ToArray<Obj_AI_Base>() :
                                        EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Program.myhero.Position, (float)Program.Q.Range).ToArray<Obj_AI_Base>();

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

        public static Prediction.Manager.PredictionOutput GetICPrediction(this Spell.Skillshot spell, Obj_AI_Base Target)
        {
            if (Target != null)
            {
                var pred = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                {
                    Delay = spell.CastDelay / 1000f,
                    Radius = spell.Width,
                    Range = spell.Range,
                    Speed = spell.Speed,
                    Type = spell.Type,
                    Target = Target
                });

                return pred;
            }

            return null;
        }

        public static bool Cast(this Spell.Skillshot spell, Obj_AI_Base target, int MinChance = 65, Vector3? From = null, bool UseICPred = false)
        {
            if (!UseICPred)
            {
                Prediction.Manager.PredictionSelected = "SDK Prediction";

                var SpellPred = Prediction.Position.PredictLinearMissile
                    (target, spell.Range, spell.Width, spell.CastDelay, spell.Speed, spell.AllowedCollisionCount, From.HasValue ? From.Value : Player.Instance.Position);

                if (SpellPred.HitChancePercent >= MinChance && spell.Cast(SpellPred.CastPosition)) return true;
            }
            else
            {
                Prediction.Manager.PredictionSelected = "SDK Beta Prediction";

                var SpellPred = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                {
                    Delay = (float)spell.CastDelay / 1000,
                    Radius = spell.Width,
                    Range = spell.Range,
                    Speed = spell.Speed,
                    Type = spell.Type,
                    Target = target,
                    From = From.HasValue ? From.Value : Player.Instance.Position
                });

                if (SpellPred.CollisionObjects.Count() <= spell.AllowedCollisionCount && SpellPred.HitChancePercent >= MinChance && spell.Cast(SpellPred.CastPosition)) return true;
            }

            return false;
        }

        public static bool UnderEnemyTurret(this Obj_AI_Base target)
        {
            if (EntityManager.Turrets.Enemies.Any(x => x.Health > 0 && x.IsValid && target.Distance(x.Position) < x.AttackRange))
                return true;
            else return false;
        }

        public static bool ValidTarget(this AIHeroClient hero, int range)
        {
            return !hero.HasBuff("UndyingRage") && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("ChronoShift") && !hero.HasBuff("kindredrnodeathbuff") &&
                   !hero.IsInvulnerable && !hero.IsDead && hero.IsValidTarget(range) && !hero.IsZombie &&
                   !hero.HasBuffOfType(BuffType.Invulnerability) && !hero.HasBuffOfType(BuffType.SpellImmunity) && !hero.HasBuffOfType(BuffType.SpellShield);
        }
    }
}

