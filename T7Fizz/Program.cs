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
using T7_Fizz.Evade;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7_Fizz
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        public static Menu menu, combo, harass, laneclear, misc, draw, pred, blocking, jungleclear, flee;
        private static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 550);
        static readonly string ChampionName = "Fizz";
        static readonly string Version = "1.0";
        static readonly string Date = "21/7/16";
        public static Item potion { get; private set; }
        public static Item biscuit { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#9FD2E3'> " + ChampionName + "</font> : Loaded!(" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Game.OnTick += OnTick;
            potion = new Item((int)ItemId.Health_Potion);
            biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            Player.LevelSpell(SpellSlot.E);
            DatMenu();
            SpellBlock.Initialize();
            DemSpells.R.AllowedCollisionCount = 0;
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;
              
            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) { Combo(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > slider(harass, "HMIN")) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > slider(laneclear, "LMIN")) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")) Jungleclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                if (check(flee, "EFLEE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    DemSpells.E.Cast(myhero.Position.Extend(Game.CursorPos, DemSpells.E.Range).To3D());
                    DemSpells.E.Cast(myhero.Position.Extend(Game.CursorPos, DemSpells.E.Range).To3D());
                   // Core.DelayAction(() => DemSpells.E.Cast(myhero.Position.Extend(Game.CursorPos, DemSpells.E.Range).To3D()), 50);
                }
            }

            Misc();

       //     WallJump();           
        }

        public static bool check(Menu submenu, string sig)
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

            /*E>W>Q*/
            SpellSlot[] sequence1 = { U, W, Q, E, E, R, E, W, E, W, R, W, W, Q, Q, R, Q, Q, U};

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static float ComboDamage(AIHeroClient Nigga)
        {
            if (Nigga != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += QDamage(Nigga); }

                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { TotalDamage += WDamage(Nigga); }

                if (DemSpells.E.IsLearned && DemSpells.E.IsReady()) { TotalDamage += EDamage(Nigga); }

                if (DemSpells.R.IsLearned && DemSpells.R.IsReady()) { TotalDamage += RDamage(Nigga); }

                TotalDamage += myhero.GetAutoAttackDamage(Nigga) * 2;

                return TotalDamage;
            }
            return 0;
        }

        private static int GetManaCost(params char[] Spells)
        {
            int TotalCost = 0,
                QCost = new int[] {0, 50, 55, 60, 65, 70}[DemSpells.Q.Level],
                WCost = 40,
                ECost = new int[] {0, 90, 100, 110, 120, 130}[DemSpells.E.Level],
                RCost = 100;

            if (Spells.Contains('Q')) TotalCost += QCost;
            if (Spells.Contains('W')) TotalCost += WCost;
            if (Spells.Contains('E')) TotalCost += ECost;
            if (Spells.Contains('R')) TotalCost += RCost;

            return TotalCost;
        }

        private static float QDamage(AIHeroClient target)
        {
            int index = DemSpells.Q.Level - 1;

            var QDamage = (target.HasBuff("fizzrbonusbuff") ? new[] { 12, 30, 48, 66, 84}[index] : new[] { 10, 25, 40, 55, 70}[index]) +
                          ((target.HasBuff("fizzrbonusbuff") ? 0.35f : 0.42f) * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Physical, QDamage);
        }

        private static float WDamage(AIHeroClient target)
        {
            int index = DemSpells.W.Level - 1;

            var WDamage = (target.HasBuff("fizzrbonusbuff") ? new[] {24, 36, 48, 60, 72}[index] : new[] {20, 30, 40, 50, 60}[index]) +
                          ((target.HasBuff("fizzrbonusbuff") ? 0.54f : 0.45f) * myhero.FlatMagicDamageMod) +
                          ((target.HasBuff("fizzrbonusbuff") ? new[] {0.048f, 0.054f, 0.06f, 0.066f, 0.072f}[index] : new[] {0.04f, 0.045f, 0.05f, 0.055f, 0.06f}[index]) *
                          (target.MaxHealth - target.Health));

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, WDamage);
        }

        private static float EDamage(AIHeroClient target)
        {
            int index = DemSpells.E.Level - 1;

            var EDamage = (target.HasBuff("fizzrbonusbuff") ? new[] { 84, 144, 204, 264, 324 }[index] : new[] {70, 120, 170, 220, 270}[index]) +
                          ((target.HasBuff("fizzrbonusbuff") ? 0.9f : 0.75f) * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, EDamage);
        }

        private static float RDamage(AIHeroClient target)
        {
            int index = DemSpells.R.Level - 1;

            var RDamage = new[] {200, 325, 450}[index] + 
                          myhero.FlatMagicDamageMod;

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, RDamage);
        }

        private static void MainCombo(AIHeroClient target)
        {
            switch (DemSpells.Q.IsInRange(target.Position))
            {
                case true:
                    if (DemSpells.Q.Cast(target))
                    {
                        if (DemSpells.E.IsInRange(target.Position))
                        {
                            if (DemSpells.E.Cast(target.Position))
                            {
                                DemSpells.W.Cast();
                            }
                        }
                        else if (target.Distance(myhero.Position) > 399 && target.Distance(myhero.Position) < 799)
                        {
                            DemSpells.E.Cast(myhero.Position.Extend(target.Position, (target.Distance(myhero.Position) / 2)).To3D());
                            DemSpells.E.Cast(target.Position);
                            DemSpells.W.Cast();
                        }
                    }
                    break;
                case false:
                    if (target.Distance(myhero.Position) < (DemSpells.E.Range * 2) + 148 + DemSpells.Q.Range)
                    {
                        switch (target.Distance(myhero.Position) < 400)
                        {
                            case true:
                               /* if (DemSpells.E.Cast(target.Position))
                                {
                                    if (DemSpells.E.Cast(target.Position))
                                    {
                                        if (DemSpells.Q.Cast(target))
                                        {
                                            DemSpells.W.Cast();
                                        }
                                    }
                                }*/
                                CastE(target);
                                if (DemSpells.Q.Cast(target))
                                {
                                    DemSpells.W.Cast();
                                }
                                break;
                            case false:
                                if (target.Distance(myhero.Position) > 400 && target.Distance(myhero.Position) < 799)
                                {
                                  /*  if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, (target.Distance(myhero.Position) / 2)).To3D()))
                                    {
                                        if (DemSpells.E.Cast(target.Position))
                                        {
                                            if (DemSpells.Q.Cast(target))
                                            {
                                                DemSpells.W.Cast();
                                            }
                                        }
                                    }*/
                                    CastE(target);
                                    if (DemSpells.Q.Cast(target))
                                    {
                                        DemSpells.W.Cast();
                                    }
                                }
                                else if ((target.Distance(myhero.Position) > 799 && target.Distance(myhero.Position) < 799 + 150) ||
                                         (target.Distance(myhero.Position) > (-1 + DemSpells.E.Range * 2) + 260 && target.Distance(myhero.Position) < ((-1 + DemSpells.E.Range * 2) + DemSpells.Q.Range - 1)))
                                {
                                    if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, DemSpells.E.Range).To3D()))
                                    {
                                        if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, DemSpells.E.Range).To3D()))
                                        {
                                            if (DemSpells.Q.Cast(target))
                                            {
                                                DemSpells.W.Cast();
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    break;
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Magical, myhero.Position);

            if (target != null && !target.IsInvulnerable)
            {         
                
                if (check(combo, "CQ") && check(combo, "CW") && check(combo, "CE") && check(combo, "CR") && 
                    DemSpells.Q.IsReady() && DemSpells.W.IsReady() && DemSpells.E.IsReady() && DemSpells.R.IsReady() &&
                    target.IsValidTarget(DemSpells.R.Range - 10) && 
                    (ComboDamage(target) > target.Health || (ComboDamage(target) + myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health && ignt.IsReady())))
                {
                    var Rpred = DemSpells.R.GetPrediction(target);

                    if (Rpred.HitChancePercent >= slider(pred, "Rpred") &&
                        Rpred.CollisionObjects.Where(x => x is AIHeroClient || x.Name.ToLower().Contains("yasuo")).Count() == 0 &&
                        DemSpells.R.Cast(Rpred.CastPosition))
                    {
                        MainCombo(target);
                    }
                   /* if (ComboDamage(target) > target.Health)
                    {
                        var Rpred = DemSpells.R.GetPrediction(target);

                        if (Rpred.HitChancePercent >= slider(pred, "Rpred") && !Rpred.Collision && DemSpells.R.Cast(Rpred.CastPosition))
                        {
                            MainCombo(target);
                        }
                    }
                    else if (ComboDamage(target) < target.Health &&
                            (ComboDamage(target) + myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health))
                    {
                        var Rpred = DemSpells.R.GetPrediction(target);

                        if (Rpred.HitChancePercent >= slider(pred, "Rpred") && !Rpred.Collision && DemSpells.R.Cast(Rpred.CastPosition))
                        {
                            MainCombo(target);
                        }
                        if (check(combo, "CIGNT") && ignt.IsReady() && target.IsValidTarget(ignt.Range)) ignt.Cast(target);
                    }
                    return;*/
                }
                else
                {
                    if (check(combo, "CQ") && check(combo, "CW") && check(combo, "CE") && target.IsValidTarget(-5 + DemSpells.E.Range * 2) &&
                            DemSpells.Q.IsReady() && DemSpells.W.IsReady() && DemSpells.E.IsReady() && GetManaCost('Q', 'W', 'E') <= myhero.Mana)
                    {
                        MainCombo(target);
                    }
                    else if (check(combo, "CQ") && check(combo, "CE") && target.IsValidTarget(-5 + DemSpells.E.Range * 2) && !target.HasUndyingBuff() &&
                            DemSpells.Q.IsReady() && DemSpells.E.IsReady() && GetManaCost('Q', 'E') <= myhero.Mana)
                    {
                        switch (DemSpells.Q.IsInRange(target.Position))
                        {
                            case true:
                                if (DemSpells.Q.Cast(target))
                                {
                                    if (DemSpells.E.IsInRange(target.Position))
                                    {
                                        DemSpells.E.Cast(target.Position);
                                    }
                                    else if (target.Distance(myhero.Position) > 399 && target.Distance(myhero.Position) < 799)
                                    {
                                        if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, (target.Distance(myhero.Position) / 2)).To3D()))
                                        {
                                            DemSpells.E.Cast(target.Position);
                                        }                                                                     
                                    }
                                }
                                break;
                            case false:
                                if (target.Distance(myhero.Position) < (DemSpells.E.Range * 2) + 148 + DemSpells.Q.Range)
                                {
                                    switch (target.Distance(myhero.Position) < 400)
                                    {
                                        case true:
                                            if (DemSpells.E.Cast(target.Position))
                                            {
                                                if (DemSpells.E.Cast(target.Position))
                                                {
                                                    DemSpells.Q.Cast(target);
                                                }
                                            }
                                            break;
                                        case false:
                                            if (target.Distance(myhero.Position) > 400 && target.Distance(myhero.Position) < 799)
                                            {
                                                if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, (target.Distance(myhero.Position) / 2)).To3D()))
                                                {
                                                    if (DemSpells.E.Cast(target.Position))
                                                    {
                                                        DemSpells.Q.Cast(target);
                                                    }
                                                }
                                            }
                                            else if ((target.Distance(myhero.Position) > 799 && target.Distance(myhero.Position) < 799 + 150) ||
                                                     (target.Distance(myhero.Position) > (-1 + DemSpells.E.Range * 2) + 260 && target.Distance(myhero.Position) < ((-1 + DemSpells.E.Range * 2) + DemSpells.Q.Range - 1)))
                                            {
                                                if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, DemSpells.E.Range).To3D()))
                                                {
                                                    if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, DemSpells.E.Range).To3D()))
                                                    {
                                                        DemSpells.Q.Cast(target);
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {                      
                        if (check(combo, "CW") && DemSpells.W.CanCast(target))
                        {
                            DemSpells.W.Cast();
                        }

                        if (check(combo, "CQ") && DemSpells.Q.CanCast(target))
                        {
                            DemSpells.Q.Cast(target);
                        }

                        if (check(combo, "CE") && DemSpells.E.IsReady() && target.IsValidTarget(-10 + DemSpells.E.Range * 2))
                        {
                           /* switch(target.Distance(myhero.Position) < 400)
                            {
                                case true:
                                    if (DemSpells.E.Cast(target.Position))
                                    {
                                        return;
                                    }
                                    break;
                                case false:
                                    if (DemSpells.E.Cast(myhero.Position.Extend(target.Position, target.Distance(myhero.Position) / 2).To3D()))
                                    {
                                        if (DemSpells.E.Cast(target.Position))
                                        {
                                            return;
                                        }
                                    }
                                    break;
                            }*/
                            CastE(target);
                        }

                        if (check(combo, "CR") && DemSpells.R.IsReady() && target.IsValidTarget(DemSpells.R.Range - 30) && !target.IsInvulnerable && !target.HasUndyingBuff()
                            && RDamage(target) > target.TotalShieldHealth())
                        {
                            var Rpred = DemSpells.R.GetPrediction(target);

                            if ((ComboDamage(target) - RDamage(target) > target.Health && DemSpells.Q.IsInRange(target.Position)) || (target.Health < myhero.Health && DemSpells.W.IsInRange(target)))
                            {
                                return;
                            }

                            if (Rpred.CollisionObjects.Where(x => x is AIHeroClient || x.Name.ToLower().Contains("yasuo")).Count() == 0 &&
                                Rpred.HitChancePercent >= slider(pred, "RPred"))
                            {
                                DemSpells.R.Cast(Rpred.CastPosition);
                            }
                        }
                    }                           
                }
            }         
        }

        private static void CastE(AIHeroClient target)
        {
            var Epred = DemSpells.E.GetPrediction(target);

            if (Epred.HitChancePercent >= slider(pred, "EPred"))
            {
                switch (target.Distance(myhero.Position) < 400)
                {
                    case true:
                        if (DemSpells.E.Cast(Epred.CastPosition))
                        {
                            return;
                        }
                        break;
                    case false:

                        if (DemSpells.E.Cast(myhero.Position.Extend(Epred.CastPosition, DemSpells.E.Range - 1).To3D()))
                        {
                            if (DemSpells.E.Cast(Epred.CastPosition))
                            {
                                return;
                            }
                        }
                        break;
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);
            
            if (target != null && !target.IsInvulnerable)
            {
                if (check(harass, "HQ") && DemSpells.Q.CanCast(target))
                {
                    DemSpells.Q.Cast(target);
                }
                if (check(harass, "HE") && DemSpells.E.IsReady() && target.IsValidTarget(-5 + DemSpells.E.Range * 2))
                {
                    CastE(target);
                }
               /* if (check(harass, "HEQ") && DemSpells.Q.IsReady() && DemSpells.E.IsReady()/* && target.IsValidTarget(-1 + DemSpells.E.Range * 2) &&
                    GetManaCost('Q','E') <= myhero.Mana)
                {
                    if (target.IsValidTarget(DemSpells.Q.Range))
                    {
                        if (DemSpells.Q.Cast(target))
                        {
                            if (LastHarassPos == null)
                            {
                                LastHarassPos = myhero.ServerPosition;
                            }

                            if (JumpBack)
                            {
                                DemSpells.E.Cast((Vector3)LastHarassPos);
                            }
                        }
                    } 
                   /* switch(comb(harass, "HEQMODES"))
                    {
                        case 0:
                            switch (pred.Distance(myhero.Position) <= 380)
                            {
                                case true:
                                    if (DemSpells.E.Cast(myhero.Position.Extend(pred.To3D(), DemSpells.E.Range - 50).To3D()))
                                    {
                                        if (DemSpells.E.Cast(myhero.Position.Extend(pred.To3D(), pred.Distance(myhero.Position) + 20).To3D()))
                                        {
                                            DemSpells.Q.Cast(target);
                                        }
                                    }
                                    break;
                                case false:
                                    if (pred.Distance(myhero.Position) <= 780)
                                    {
                                        if (DemSpells.E.Cast(myhero.Position.Extend(pred.To3D(), DemSpells.E.Range - 1).To3D()))
                                        {
                                            if (DemSpells.E.Cast(myhero.Position.Extend(pred.To3D(), DemSpells.E.Range - 1).To3D()))
                                            {
                                                DemSpells.Q.Cast(target);
                                            }
                                        }
                                    }
                                    break;
                            } 
                            break;
                        case 1:
                            if (target.IsValidTarget(DemSpells.Q.Range))
                            {
                                if (DemSpells.Q.Cast(target))
                                {
                                    if (LastHarassPos == null)
                                    {
                                        LastHarassPos = myhero.ServerPosition;
                                    }

                                    if (JumpBack)
                                    {
                                        DemSpells.E.Cast((Vector3)LastHarassPos);
                                    }
                                }
                            }                            
                            break;
                    }  */ 
                if (check(harass, "HW") && DemSpells.W.IsReady() && myhero.CountEnemiesInRange(DemSpells.W.Range) >= slider(harass, "HWMIN"))
                {
                    DemSpells.W.Cast();
                }                                                    
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range).ToList();

            

            if (minions != null)
            {
                if (check(laneclear, "LQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
                {
                    foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 20))
                    {
                        DemSpells.Q.Cast(minion);
                    }
                }

                if (check(laneclear, "LW") && DemSpells.W.IsLearned && DemSpells.W.IsReady())
                {
                    int count = minions.Where(x => !x.IsDead && x.IsValidTarget(myhero.GetAutoAttackRange()))
                                        .Count();

                    if (count >= slider(jungleclear, "JWMIN")) DemSpells.W.Cast();
                }

                if (check(laneclear, "LE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    foreach(var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range * 2)))
                    {
                        var Radius = minion.Distance(myhero.Position) > 400 ? 270 : 330;

                        if (minion.CountEnemyMinionsInRange(Radius) >= slider(laneclear, "LEMIN"))
                        {
                            switch(DemSpells.E.IsInRange(minion.Position))
                            {
                                case true:
                                    DemSpells.E.Cast(minion.Position);
                                    break;
                                case false:
                                    if (minion.Distance(myhero.Position) < 800)
                                    {
                                        if (DemSpells.E.Cast(myhero.Position.Extend(minion.Position, DemSpells.E.Range - 1).To3D()))
                                        {
                                            DemSpells.E.Cast(minion.Position);
                                        }
                                    }                                                                      
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1000f);       
              
            if (Monsters != null)
            {
                if (check(jungleclear, "JQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
                {
                    switch (comb(jungleclear, "JQMODE"))
                    {
                        case 0:
                            foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 20))
                            {
                                DemSpells.Q.Cast(monster);
                            }
                            break;
                        case 1:
                            foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 30 &&
                                                                        !x.Name.ToLower().Contains("mini")))
                            {
                                DemSpells.Q.Cast(monster);
                            }
                            break;
                    }
                }

                if (check(jungleclear, "JW") && DemSpells.W.IsLearned && DemSpells.W.IsReady())
                {
                    int count = Monsters.Where(x => x.IsValidTarget(myhero.GetAutoAttackRange()))
                                        .Count();

                    if (count >= slider(jungleclear, "JWMIN")) DemSpells.W.Cast();
                }

                if (check(jungleclear, "JE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    var List = comb(jungleclear, "JEMODE") == 0 ? Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range * 2)) :
                                                                  Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range * 2) && !x.Name.ToLower().Contains("mini"));
                    foreach(var monster in List)
                    {
                        if (monster.Distance(myhero.Position) < 400)
                        {
                            DemSpells.E.Cast(monster.Position);
                        }
                        else
                        {
                            DemSpells.E.Cast(myhero.Position.Extend(monster.Position, DemSpells.E.Range).To3D());
                            DemSpells.E.Cast(monster.Position);
                        }
                    }
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (check(misc, "skinhax")) myhero.SetSkinId((int)comb(misc, "skinID"));

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") || !myhero.HasBuff("ItemMiniRegenPotion")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(potion.Id) && Item.CanUseItem(potion.Id)) potion.Cast();

                else if (Item.HasItem(biscuit.Id) && Item.CanUseItem(biscuit.Id)) biscuit.Cast();
            }

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {

                if (check(misc, "ksQ") && DemSpells.Q.CanCast(target) && QDamage(target) > Prediction.Health.GetPrediction(target, DemSpells.Q.CastDelay) &&
                    !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    DemSpells.Q.Cast(target);
                }

                if (check(misc, "ksE") && EDamage(target) > Prediction.Health.GetPrediction(target, DemSpells.E.CastDelay + 50) &&
                    !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && target.Distance(myhero.Position) <= 800)
                {
                    switch (DemSpells.E.IsInRange(target.Position))
                    {
                        case true:
                            DemSpells.E.Cast(myhero.Position);
                            DemSpells.E.Cast(target.Position);
                            break;
                        case false:
                            DemSpells.E.Cast(myhero.Position.Extend(target.Position, DemSpells.E.Range).To3D());
                            DemSpells.E.Cast(target.Position);
                            break;
                    }
                }

                if (check(misc, "autoign") && ignt.IsReady() && target.IsValidTarget(ignt.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }
        }

        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            /*
               =====================
                Credits => JokerArt
               =====================                           
            */

            var unit = sender as AIHeroClient;

            if (unit == null || !unit.IsValid)
            {
                return;
            }

            if (!unit.IsEnemy || !check(blocking, "BLOCK") || !DemSpells.E.IsReady())
            {
                return;
            }

            if (Evade.SpellDatabase.GetByName(args.SData.Name) != null && !check(blocking, "evade"))
                return;

            if (!SpellBlock.Contains(unit, args))
                return;

            if (args.End.Distance(Player.Instance) == 0)
                return;

            var castUnit = unit;
            var type = args.SData.TargettingType;

            if (!unit.IsValidTarget())
            {
                var target = TargetSelector.GetTarget(DemSpells.E.Range, DamageType.Mixed);
                if (target == null || !target.IsValidTarget(DemSpells.E.Range))
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && target.IsValidTarget(DemSpells.E.Range))
                {
                    castUnit = target;
                }
            }

            if (unit.ChampionName.Equals("Darius") && args.Slot == SpellSlot.Q && unit.Distance(myhero.Position) < 420)
            {
                Core.DelayAction(() => BlockE(), 700);
            }
          /*  if (unit.ChampionName.Equals("Blitzcrank") && args.Slot == SpellSlot.Q)
            {
                Core.DelayAction(() => BlockE(), 250);
            }*/
            if (unit.ChampionName.Equals("Malphite") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 300)
            {
                Core.DelayAction(() => BlockE(),
                    ((int)(args.Start.Distance(Player.Instance) / 700 * 1000) -
                    (int)(args.End.Distance(Player.Instance) / 700) - 250) + 100);
            }
            if (unit.ChampionName.Equals("Morgana") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 300)
            {
                Core.DelayAction(() => BlockE(), 3000);
            }
            if (unit.ChampionName.Equals("KogMaw") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 240)
            {
                Core.DelayAction(() => BlockE(), 1200);
            }
            if (unit.ChampionName.Equals("Ziggs") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 550)
            {
                Core.DelayAction(() => BlockE(),
                    ((int)(args.Start.Distance(Player.Instance) / 2800 * 1000) -
                    (int)(args.End.Distance(Player.Instance) / 2800) - 250) + 900);
            }
            if (unit.ChampionName.Equals("Karthus"))
            {
                if (args.Slot == SpellSlot.R)
                {
                    Core.DelayAction(() => BlockE(), 2500);
                }
                else if (args.Slot == SpellSlot.Q && Prediction.Position.PredictUnitPosition(myhero, 499).Distance(args.End) < 100)
                {
                    Core.DelayAction(() => BlockE(), 450);
                }
            }
            if (unit.ChampionName.Equals("Shen") && args.Slot == SpellSlot.E && args.End.Distance(myhero.Position) < 100)
            {
                Core.DelayAction(() => BlockE(),
                    ((int)(args.Start.Distance(Player.Instance) / 1600 * 1000) -
                    (int)(args.End.Distance(Player.Instance) / 1600) - 250) + 250);
            }
            if (unit.ChampionName.Equals("Zyra"))
            {
                Core.DelayAction(() => BlockE(),
                    (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                    (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 250);
            }
            if (unit.ChampionName.Equals("Amumu") && args.Slot == SpellSlot.R && unit.Distance(myhero.Position) <= 550)
            {
                BlockE();
            }
            if (args.End.Distance(Player.Instance) < 250)
            {
                if (unit.ChampionName.Equals("Bard") && args.End.Distance(Player.Instance) < 300)
                {
                    Core.DelayAction(() => BlockE(), (int)(unit.Distance(Player.Instance) / 7f) + 400);
                }
                else if (unit.ChampionName.Equals("Ashe"))
                {
                    Core.DelayAction(() => BlockE(),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)args.End.Distance(Player.Instance));
                    return;
                }
                else if (unit.ChampionName.Equals("Varus") || unit.ChampionName.Equals("TahmKench") ||
                         unit.ChampionName.Equals("Lux"))
                {
                    if (unit.ChampionName.Equals("Lux") && args.Slot == SpellSlot.R)
                    {
                        Core.DelayAction(() => BlockE(), 400);
                    }
                    Core.DelayAction(() => BlockE(),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 250);

                }
                else if (unit.ChampionName.Equals("Amumu"))
                {
                    if (sender.Distance(Player.Instance) < 1100)
                        Core.DelayAction(() => BlockE(),
                            (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                            (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 250);
                }
            }

            if (args.Target != null && type.Equals(SpellDataTargetType.Unit))
            {
                if (!args.Target.IsMe ||
                    (args.Target.Name.Equals("Barrel") && args.Target.Distance(Player.Instance) > 200 &&
                     args.Target.Distance(Player.Instance) < 400))
                {
                    return;
                }

                if (unit.ChampionName.Equals("Nautilus") ||
                    (unit.ChampionName.Equals("Caitlyn") && args.Slot.Equals(SpellSlot.R)))
                {
                    var d = unit.Distance(Player.Instance);
                    var travelTime = d / 3200;
                    var delay = Math.Floor(travelTime * 1000) + 900;
                    Core.DelayAction(() => BlockE(), (int)delay);
                    return;
                }
                BlockE();
            }

            if (type.Equals(SpellDataTargetType.Unit))
            {
                if (unit.ChampionName.Equals("Bard") && args.End.Distance(Player.Instance) < 300)
                {
                    Core.DelayAction(() => BlockE(), 400 + (int)(unit.Distance(Player.Instance) / 7f));
                }
                else if (unit.ChampionName.Equals("Riven") && args.End.Distance(Player.Instance) < 260)
                {
                    BlockE();
                }
                else
                {
                    BlockE();
                }
            }
            else if (type.Equals(SpellDataTargetType.LocationAoe) &&
                     args.End.Distance(Player.Instance) < args.SData.CastRadius)
            {
                if (unit.ChampionName.Equals("Annie") && args.Slot.Equals(SpellSlot.R))
                {
                    return;
                }
                BlockE();
            }
            else if (type.Equals(SpellDataTargetType.Cone) &&
                     args.End.Distance(Player.Instance) < args.SData.CastRadius)
            {
                BlockE();
            }
            else if (type.Equals(SpellDataTargetType.SelfAoe) || type.Equals(SpellDataTargetType.Self))
            {
                var d = args.End.Distance(Player.Instance.ServerPosition);
                var p = args.SData.CastRadius > 5000 ? args.SData.CastRange : args.SData.CastRadius;
                if (d < p)
                    BlockE();
            }
        }

        public static void BlockE()
        {
            switch (comb(blocking, "BLOCKMODE"))
            {
                case 0:
                    DemSpells.E.Cast(Player.Instance.Position);
                    break;
                case 1:
                    if (Game.CursorPos.Distance(myhero.Position) > 399)
                    {
                        DemSpells.E.Cast(myhero.Position.Extend(Game.CursorPos, DemSpells.R.Range - 1).To3D());
                    }
                    else
                    {
                        DemSpells.E.Cast(Game.CursorPos);
                    }                   
                    break;
            }
        }

     /*   private static void WallJump()
        {
            var DragonJumpPos = new Vector3(9355, 4500, -71);

            EloBuddy.SDK.Geometry.Polygon.Rectangle JumpingPoint1 = new EloBuddy.SDK.Geometry.Polygon.Rectangle(new Vector2(9083, 4718), new Vector2(9030, 4508), 50);

            var JumpingPoint = new Vector3(9002, 4404, 53);

            if (DemSpells.E.IsReady() && key(misc, "WALLJUMPKEY") && DemSpells.E.IsInRange(JumpingPoint))
            {
                if (DemSpells.E.Cast(JumpingPoint))
                {
                    if(DemSpells.E.Cast(DragonJumpPos))
                    {
                        return;
                    }
                }
              /*  if(DemSpells.E.Cast(JumpingPoint1.CenterOfPolygon().Extend(DragonJumpPos, 20).To3D()))
                {
                    DemSpells.E.Cast(JumpingPoint1.CenterOfPolygon().Extend(DragonJumpPos, (int)DemSpells.E.Range * 0.8f).To3D());
                }
                
               /* switch(JumpingPoint1.IsInside(myhero.Position))
                {
                    case true:
                        DemSpells.E.Cast(JumpingPoint1.CenterOfPolygon().Extend(DragonJumpPos, 20).To3D());
                        DemSpells.E.Cast(DragonJumpPos);
                        break;
                    case false:
                        if (JumpingPoint1.IsOutside(myhero.Position.To2D()) && JumpingPoint1.CenterOfPolygon().Extend(DragonJumpPos, 20).Distance(myhero.Position) < 400)
                        {
                            DemSpells.E.Cast(JumpingPoint1.CenterOfPolygon().Extend(DragonJumpPos, 20).To3D());
                            DemSpells.E.Cast(DragonJumpPos);
                            if (DemSpells.E.Cast(JumpingPoint1.CenterOfPolygon().To3D()))
                            {
                                DemSpells.E.Cast(DragonJumpPos);
                            }
                        }                       
                        break;
                }
                
            }
         /*   else if (DemSpells.E.IsReady() && key(misc, "WALLJUMPKEY") &&  JumpingPoint1.CenterOfPolygon().Extend(DragonJumpPos, 20).Distance(myhero.Position) > 405)
            {
                Orbwalker.MoveTo(JumpingPoint1.CenterOfPolygon().To3D());
            }
            
        }*/


        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;

          //  EloBuddy.SDK.Geometry.Polygon.Rectangle JumpingPoint1 = new EloBuddy.SDK.Geometry.Polygon.Rectangle(new Vector2(9083, 4718), new Vector2(9030, 4508), 50);
         //   Drawing.DrawCircle(JumpingPoint1.CenterOfPolygon().Extend(new Vector2(9342, 4552), 30).To3D(),20,Color.White);

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.LightBlue, DemSpells.Q.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.LightBlue, DemSpells.Q.Range, myhero.Position); }

            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.LightBlue, comb(draw, "EDRAWMODE") == 0 ? DemSpells.E.Range : DemSpells.E.Range * 2, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.LightBlue, comb(draw, "EDRAWMODE") == 0 ? DemSpells.E.Range : DemSpells.E.Range * 2, myhero.Position); }

            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.LightBlue, DemSpells.R.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.LightBlue, DemSpells.R.Range, myhero.Position); }

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
                         enemy.IsHPBarRendered && !enemy.IsDead &&
                         ComboDamage(enemy) + myhero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, Color.Green, "Combo + Ignite");
                }
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
            flee = menu.AddSubMenu("Flee", "fleee");
            blocking = menu.AddSubMenu("Spellblocking", "blocks");
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date);
            menu.AddLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
            combo.Add("CW", new CheckBox("Use W", true));
            combo.Add("CE", new CheckBox("Use E", true));
            combo.Add("CR", new CheckBox("Use R", true));
            combo.AddSeparator();
            combo.Add("CIGNT", new CheckBox("Use Ignite", false));
       //     combo.Add("EGAP", new CheckBox("Use E To Gapclose if enemy out of range", true));

            harass.AddLabel("Spells");
      //      harass.Add("HEQ", new CheckBox("Use Q + E Combo", true));
      //      harass.Add("HMODE", new ComboBox("Harass Mode", 0, "Use All Spells Separately", "Only Q + E Combo"));
     //       harass.Add("HEQMODES", new ComboBox("Select EQ Combo", 1, "Attack With W + Q To Escape", "Attack With Q + W To Escape"));
            harass.Add("HQ", new CheckBox("Use W", true));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use W", true));
            harass.Add("HWMIN", new Slider("Min Enemies In Range To Cast W", 1, 1, 5));
            harass.AddSeparator();
            harass.Add("HE", new CheckBox("Use E", true));           
            harass.AddSeparator();
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", true));            
            laneclear.AddSeparator();
            laneclear.Add("LW", new CheckBox("Use W", true));
            laneclear.Add("LWMIN", new Slider("Min Minions In Range To Cast W", 3, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", true));
            laneclear.Add("LEMIN", new Slider("Min Minions To Hit With E", 4, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", true));
            jungleclear.Add("JQMODE", new ComboBox("Q Mode", 1, "All Monsters", "Big Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W", true));
            jungleclear.Add("JWMIN", new Slider("Min Monsters Nearby To Cast W", 1, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E", true));
            jungleclear.Add("JEMODE", new ComboBox("E Mode", 1, "All Monsters", "Big Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawE", new CheckBox("Draw E Range", true));
            draw.Add("drawR", new CheckBox("Draw R Range", true));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies", true));
            draw.Add("EDRAWMODE", new ComboBox("E Drawing Mode", 0, "Single Jump Range", "Both Jumps Range"));

            blocking.Add("BLOCK", new CheckBox("Auto Block Spells With E", true));
            blocking.Add("evade", new CheckBox("Evade Integration", true));
            blocking.Add("BLOCKMODE", new ComboBox("Jump To", 1, "Player Position", "Mouse Position"));
            blocking.AddSeparator();

            flee.AddGroupLabel("Flee");
            flee.Add("EFLEE", new CheckBox("Use E To Flee", true));
            flee.AddLabel("(Casts To Mouse Position)");
            flee.AddSeparator();           

            misc.AddLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            misc.Add("ksE", new CheckBox("Killsteal with E", false));
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", false));
       //     misc.AddSeparator();
       //     misc.AddLabel("WallJump");
       //     misc.Add("WALLJUMPKEY", new KeyBind("WallJump Hotkey", false, KeyBind.BindTypes.HoldActive, 'T'));
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
            misc.Add("skinID", new ComboBox("Skin Hack", 9, "Default", "Atlantean", "Tundra", "Fisherman", "Void", "Chroma Gold", "Chroma Blue", "Chroma Red", "Cottontail", "Super Galaxy"));

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("E :");
            pred.Add("EPred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("R :");
            pred.Add("RPred", new Slider("Select % Hitchance", 90, 1, 100));
        }

    }

    public static class DemSpells
    {
        public static Spell.Targeted Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Skillshot R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Targeted(SpellSlot.Q, 550);
            W = new Spell.Active(SpellSlot.W, (uint)Player.Instance.GetAutoAttackRange());
            E = new Spell.Skillshot(SpellSlot.E, 400, EloBuddy.SDK.Enumerations.SkillShotType.Circular, 250, int.MaxValue, 330);
            R = new Spell.Skillshot(SpellSlot.R, 1300, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 250, 1200, 80);
        }
    }
}
