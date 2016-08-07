using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace T7_Nidalee
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;        
        public static bool Q1Ready = true, Q2Ready = true, W1Ready = true, W2Ready = true, E2Ready = true;  
        static readonly string ChampionName = "Nidalee";
        static readonly string Version = "1.0";
        static readonly string Date = "7/8/16";
        private static Spell.Targeted ignt { get; set; }
        private static Spell.Targeted smite { get; set; }
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }

            Chat.Print("<font color='#0040FF'>T7</font><font color='#FF0505'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#3737E6'>Toyota</font><font color='#D61EBE'>7</font><font color='#FF0000'> <3 </font>");

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Obj_AI_Base.OnProcessSpellCast += OnProcess;        
            Game.OnTick += OnTick;

            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            RPotion = new Item((int)ItemId.Refillable_Potion);

            Player.LevelSpell(SpellSlot.Q);
            DemSpells.Q1.AllowedCollisionCount = 0;
            DatMenu();

            if (EloBuddy.SDK.Spells.SummonerSpells.PlayerHas(EloBuddy.SDK.Spells.SummonerSpellsEnum.Ignite))
            {
                ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 600);
            }
            else if (EloBuddy.SDK.Spells.SummonerSpells.PlayerHas(EloBuddy.SDK.Spells.SummonerSpellsEnum.Smite))
            {
                smite = new Spell.Targeted(myhero.GetSpellSlotFromName("summonersmite"), 500);
            }
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo(); 

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > slider(harass, "HMIN")) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > slider(laneclear, "LMIN")) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")) Jungleclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.Flee) && check(misc, "W2FLEE") && myhero.Spellbook.GetSpell(SpellSlot.W).IsLearned)
            {
                if (IsCougar())
                {
                    DemSpells.W2.Cast(myhero.Position.Extend(Game.CursorPos, DemSpells.W2.Range - 1.0f).To3D());
                }
                else
                {
                    if (W2Ready && DemSpells.R.Cast())
                    {
                        DemSpells.W2.Cast(myhero.Position.Extend(Game.CursorPos, DemSpells.W2.Range - 1.0f).To3D());
                        return;
                    }
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

            /*>>*/
            SpellSlot[] sequence1 = { U, E, W, Q, Q, R, Q, E, Q, E, R, E, E, W, W, R, W, W, U };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        /*  private static float ComboDamage(AIHeroClient target)
            {
                if (target != null)
                {
                    float TotalDamage = 0;

                    if (myhero.Spellbook.GetSpell(SpellSlot.Q).IsLearned && Q2Ready) { TotalDamage += Q2Damage(target); }  
                  
                    if (myhero.Spellbook.GetSpell(SpellSlot.Q).IsLearned && Q1Ready) { TotalDamage += Q1Damage(target); } 
 
                    if (myhero.Spellbook.GetSpell(SpellSlot.W).IsLearned && W2Ready) { TotalDamage += WDamage(target); }

                    if (myhero.Spellbook.GetSpell(SpellSlot.E).IsLearned && E2Ready) { TotalDamage += EDamage(target); }                              

                    return TotalDamage;
                }
                return 0;
            }*/

        private static float QDamage(AIHeroClient target)
        {
            int index = myhero.Spellbook.GetSpell(SpellSlot.Q).Level - 1;
            var dist = target.Distance(myhero.Position);
            var BaseDamage = new[] { 70, 85, 100, 115, 130 }[index];

            float QDamage = 0;

            if (dist <= 525)
            {
                QDamage = BaseDamage + (0.4f * myhero.FlatMagicDamageMod);
            } 
            else if (dist > 525 && dist < 1300)
            {
                QDamage = ((((dist - 525) / 3.875f) / 100) * BaseDamage) + (0.4f * myhero.FlatMagicDamageMod);
            }   
            else if (dist >= 1300)
            {
                QDamage = new[] { 210, 255, 300, 345, 390}[index] + (1.2f * myhero.FlatMagicDamageMod);
            }

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, QDamage);
        }

        private static bool IsCougar()
        {
            return myhero.GetAutoAttackRange() < 300;
        }

        private static bool IsHunted(AIHeroClient target)
        {
            return target.HasBuff("NidaleePassiveHunted");
        }

        private static void RLogic(AIHeroClient target)
        {
            if (IsCougar())
            {
                if (Q1Ready && (DemSpells.Q2.IsOnCooldown || myhero.CountEnemiesInRange(300) == 0) &&
                    (DemSpells.W2.IsOnCooldown || myhero.CountEnemiesInRange(DemSpells.W2.Range) == 0) &&
                    (DemSpells.E2.IsOnCooldown || myhero.CountEnemiesInRange(DemSpells.E2.Range + 100) == 0) && !myhero.IsFleeing)
                {
                    DemSpells.R.Cast();
                }
            }
            else if (!IsCougar())
            {
                if (DemSpells.Q1.IsOnCooldown && ((W2Ready && (IsHunted(target) ? myhero.CountEnemiesInRange(DemSpells.W2E.Range) : myhero.CountEnemiesInRange(DemSpells.W2.Range)) >= 1) ||
                                                  (Q2Ready && myhero.CountEnemiesInRange(DemSpells.Q2.Range) >= 1) ||
                                                  (E2Ready && myhero.CountEnemiesInRange(DemSpells.E2.Range) >= 1) ))
                {
                    DemSpells.R.Cast();
                }
            }
            return;
        }

        private static void CastW2(AIHeroClient target)
        {
            if (DemSpells.W2.IsReady())
            {
                switch(IsHunted(target))
                {
                    case true:
                        if (target.IsValidTarget(DemSpells.W2E.Range))
                        {
                            var wpred = DemSpells.W2E.GetPrediction(target);

                            if (wpred.HitChancePercent >= slider(pred, "W2Pred")) DemSpells.W2E.Cast(wpred.CastPosition);
                        }
                        break;
                    case false:
                        if (target.IsValidTarget(DemSpells.W2.Range))
                        {
                            var wpred = DemSpells.W2.GetPrediction(target);

                            if (wpred.HitChancePercent >= slider(pred, "W2Pred")) DemSpells.W2.Cast(wpred.CastPosition);
                        }
                        break;
                }
            }
            return;
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1600, DamageType.Physical, Player.Instance.Position);

            if (target != null)
            {
                if (IsCougar())
                {                    
                    if (check(combo, "CQ2") && DemSpells.Q2.CanCast(target) && DemSpells.Q2.Cast())
                    {                        
                        return;
                    }

                    if (check(combo, "CW2") && DemSpells.W2.CanCast(target))
                    {
                        CastW2(target);
                    }

                    if (check(combo, "CE2") && DemSpells.E2.CanCast(target))
                    {
                        if (Prediction.Position.PredictUnitPosition(target, DemSpells.E2.CastDelay).Distance(myhero.Position) <= DemSpells.E2.Range)
                        {
                            DemSpells.E2.Cast(target.Position);
                            return;
                        }
                    }
                }
                else if (!IsCougar())
                {
                    if (check(combo, "CQ1") && DemSpells.Q1.CanCast(target))
                    {
                        var qpred = DemSpells.Q1.GetPrediction(target);

                        if (qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q1.Cast(qpred.CastPosition); return;
                    }

                    if (check(combo, "CW1") && DemSpells.W1.CanCast(target))
                    {
                        var wpred = DemSpells.W1.GetPrediction(target);

                        if (wpred.HitChancePercent >= slider(pred, "W1Pred")) DemSpells.W1.Cast(wpred.CastPosition); return;
                    }
                }

                if (check(combo, "CR") && DemSpells.R.IsReady())
                {
                    RLogic(target);
                }
            }           
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1600, DamageType.Physical, Player.Instance.Position);

            if (target != null)
            {
                if (IsCougar())
                {
                    if (check(harass, "HQ2") && DemSpells.Q2.CanCast(target) && DemSpells.Q2.Cast())
                    {
                        return;
                    }

                    if (check(harass, "HW2") && DemSpells.W2.CanCast(target))
                    {
                        CastW2(target);
                    }

                    if (check(harass, "HE2") && DemSpells.E2.CanCast(target))
                    {
                        if (Prediction.Position.PredictUnitPosition(target, DemSpells.E2.CastDelay).Distance(myhero.Position) <= DemSpells.E2.Range && DemSpells.E2.Cast(target.Position))
                        {                            
                            return;
                        }
                    }
                }
                else if (!IsCougar())
                {
                    if (check(harass, "HQ1") && DemSpells.Q1.CanCast(target))
                    {
                        var qpred = DemSpells.Q1.GetPrediction(target);

                        if (qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q1.Cast(qpred.CastPosition); return;
                    }

                    if (check(harass, "HW1") && DemSpells.W1.CanCast(target))
                    {
                        var wpred = DemSpells.W1.GetPrediction(target);

                        if (wpred.HitChancePercent >= slider(pred, "W1Pred")) DemSpells.W1.Cast(wpred.CastPosition); return;
                    }
                }

                if (check(harass, "HR") && DemSpells.R.IsReady())
                {
                    RLogic(target);
                }
            }          
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.Q1.Range).ToList();

            if (minions != null)
            {
                if (!IsCougar())
                {
                    if (check(laneclear, "LQ1") && DemSpells.Q1.IsReady())
                    {
                        foreach (var minion in minions.Where(x => x.IsValidTarget(DemSpells.Q1.Range) && x.Health > 40).OrderBy(x => x.Distance(myhero.Position)))
                        {
                            var qpred = DemSpells.Q1.GetPrediction(minion);

                            if ((comb(laneclear, "LQ1MODE") == 0 && !minion.BaseSkinName.ToLower().Contains("siege") || minion.BaseSkinName.ToLower().Contains("super")) ||
                                Prediction.Health.GetPrediction(minion, DemSpells.Q1.CastDelay + (int)(((minion.Distance(myhero.Position) - minion.BoundingRadius) / DemSpells.Q1.Speed) * 1000)) < 5) continue;

                            if (qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q1.Cast(qpred.CastPosition);
                        }
                    }
                }
                else if (IsCougar())
                {
                    if (check(laneclear, "LQ2") && DemSpells.Q2.IsReady())
                    {
                        if (minions.Where(x => x.IsValidTarget(DemSpells.Q2.Range) && x.Health > 30).Count() >= slider(laneclear, "LQ2MIN")) DemSpells.Q2.Cast();
                    }

                    if (check(laneclear, "LW2") && DemSpells.W2.IsReady())
                    {
                        DemSpells.W2.CastOnBestFarmPosition(slider(laneclear, "LW2MIN"));
                    }

                    if (check(laneclear, "LE2") && DemSpells.E2.IsReady())
                    {
                        foreach (var minion in minions.Where(x => x.IsValidTarget(DemSpells.E2.Range) && x.Health > 40))
                        {
                            if (comb(laneclear, "LE2MODE") == 0 && !minion.BaseSkinName.ToLower().Contains("siege") || minion.BaseSkinName.ToLower().Contains("super")) continue;

                            var epred = DemSpells.E2.GetPrediction(minion);

                            if (epred.HitChancePercent >= slider(pred, "EPred")) DemSpells.E2.Cast(epred.CastPosition);
                        }
                    }
                }

                if (check(laneclear, "LAUTOR") && DemSpells.R.IsReady())
                {
                    if (IsCougar())
                    {
                        if (Q1Ready && (DemSpells.Q2.IsOnCooldown || minions.Where(x => x.IsValidTarget(DemSpells.Q2.Range)).Count() == 0) &&
                            (DemSpells.W2.IsOnCooldown || minions.Where(x => x.IsValidTarget(DemSpells.W2.Range)).Count() == 0) &&
                            (DemSpells.E2.IsOnCooldown || minions.Where(x => x.IsValidTarget(DemSpells.E2.Range)).Count() == 0) && !myhero.IsFleeing)
                        {
                            DemSpells.R.Cast();
                        }
                    }
                    if (!IsCougar())
                    {
                        if (!Q1Ready && ((W2Ready && minions.Where(x => x.IsValidTarget(DemSpells.W2.Range)).Count() >= 1) ||
                                         (Q2Ready && minions.Where(x => x.IsValidTarget(DemSpells.Q2.Range)).Count() >= 1) ||
                                         (E2Ready && minions.Where(x => x.IsValidTarget(DemSpells.E2.Range)).Count() >= 1)))
                        {
                            DemSpells.R.Cast();
                        }
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1600).Where(x => !x.IsDead && x.IsValid);
           
            if (Monsters != null)
            {
                if (!IsCougar())
                {
                    if (check(jungleclear, "JQ1") && DemSpells.Q1.IsReady())
                    {
                        foreach(var monster in Monsters.Where(x => x.IsValidTarget(DemSpells.Q1.Range) && x.Health > 40).OrderBy(x => x.Distance(myhero.Position)))
                        {
                            var qpred = DemSpells.Q1.GetPrediction(monster);

                            if (comb(jungleclear, "JQ1MODE") == 0 && monster.Name.ToLower().Contains("mini")) continue;

                            if (qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q1.Cast(qpred.CastPosition);
                        }
                    }
                }
                else if (IsCougar())
                {
                    if (check(jungleclear, "JQ2") && DemSpells.Q2.IsReady())
                    {
                        if (Monsters.Where(x => x.IsValidTarget(DemSpells.Q2.Range) && x.Health > 30).Count() >= slider(jungleclear, "JQ2MIN")) DemSpells.Q2.Cast();
                    }

                    if (check(jungleclear, "JW2") && DemSpells.W2.IsReady())
                    {
                        var pred = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(Monsters, 75, 375);

                        if (pred.HitNumber >= slider(jungleclear, "JW2MIN")) DemSpells.W2.Cast(pred.CastPosition);
                    }

                    if (check(jungleclear, "JE2") && DemSpells.E2.IsReady())
                    {
                        foreach (var monster in Monsters.Where(x => x.IsValidTarget(DemSpells.E2.Range) && x.Health > 40))
                        {                           
                            if (comb(jungleclear, "JE2MODE") == 0 && monster.Name.ToLower().Contains("mini")) continue;

                            var epred = DemSpells.E2.GetPrediction(monster);

                            if (epred.HitChancePercent >= slider(pred, "EPred")) DemSpells.E2.Cast(epred.CastPosition);
                        }
                    }                   
                }

                if (check(jungleclear, "JAUTOR") && DemSpells.R.IsReady())
                {
                    if (IsCougar())
                    {
                        if (Q1Ready && (DemSpells.Q2.IsOnCooldown || Monsters.Where(x => x.IsValidTarget(DemSpells.Q2.Range)).Count() == 0) &&
                            (DemSpells.W2.IsOnCooldown || Monsters.Where(x => x.IsValidTarget(DemSpells.W2.Range)).Count() == 0) &&
                            (DemSpells.E2.IsOnCooldown || Monsters.Where(x => x.IsValidTarget(DemSpells.E2.Range)).Count() == 0) && !myhero.IsFleeing)
                        {
                            DemSpells.R.Cast();
                        }
                    }
                    if (!IsCougar())
                    {
                        if (!Q1Ready && ((W2Ready && Monsters.Where(x => x.IsValidTarget(DemSpells.W2.Range)).Count() >= 1) ||
                                         (Q2Ready && Monsters.Where(x => x.IsValidTarget(DemSpells.Q2.Range)).Count() >= 1) || 
                                         (E2Ready && Monsters.Where(x => x.IsValidTarget(DemSpells.E2.Range)).Count() >= 1)) )
                        {
                            DemSpells.R.Cast();
                        }
                    }
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1600, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                if (check(misc, "ksQ") && !IsCougar() && DemSpells.Q1.IsReady() && target.IsValidTarget(DemSpells.Q1.Range) && QDamage(target) > target.Health)
                {
                    var qpred = DemSpells.Q1.GetPrediction(target);

                    if (qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q1.Cast(qpred.CastPosition);
                }

                if (ignt != null && check(misc, "autoign") && ignt.IsReady() && target.IsValidTarget(ignt.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }
            
            if (key(misc, "EKEY") && !IsCougar() && DemSpells.E1.IsReady() && myhero.ManaPercent >= slider(misc, "EMINM") && !myhero.IsRecalling())
            {              
                var ClosestAlly = EntityManager.Heroes.Allies.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E1.Range) && x.HealthPercent <= slider(misc, "EMINA"))
                                                                     .OrderBy(x => x.Health)
                                                                     .FirstOrDefault();
                switch (comb(misc, "EMODE"))
                {
                    case 0:
                        if (myhero.HealthPercent <= slider(misc, "EMINH"))
                        {
                            DemSpells.E1.Cast(myhero);
                        }                                                        
                        break;
                    case 1:                       
                        if (ClosestAlly != null) DemSpells.E1.Cast(ClosestAlly.Position);
                        break; 
                    case 2:
                        if (ClosestAlly != null)
                        {
                            switch (myhero.Health > ClosestAlly.Health)
                            {
                                case true:
                                    DemSpells.E1.Cast(ClosestAlly.Position);
                                    break;
                                case false:
                                    if (myhero.HealthPercent <= slider(misc, "EMINH") && myhero.CountEnemiesInRange(1000) >= slider(misc, "EMINE"))
                                    {
                                        DemSpells.E1.Cast(myhero.Position);
                                    }
                                    break;
                            }
                        }
                        else goto case 0;
                        break;
                }         
            }

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            if (smite != null && smite.IsReady() && key(jungleclear, "SMITEKEY"))
            {
                var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 500).Where(x => x.IsValidTarget(500) && !x.IsDead &&
                                                                                                                   !x.Name.ToLower().Contains("mini"));

                if (Monsters != null)
                {
                    foreach (var monster in Monsters)
                    {
                        var SmiteDamage = myhero.GetSummonerSpellDamage(monster, DamageLibrary.SummonerSpells.Smite);

                        if (smite.CanCast(monster) && monster.Health < SmiteDamage && smite.Cast(monster)) return;
                    }
                }
            }

        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && myhero.Spellbook.GetSpell(SpellSlot.Q).Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {
                if (check(draw, "drawonlyrdy"))
                {
                    Circle.Draw(
                        (IsCougar() ? DemSpells.Q2.IsOnCooldown : DemSpells.Q1.IsOnCooldown) ? SharpDX.Color.Transparent : SharpDX.Color.Gold,
                        IsCougar() ? DemSpells.Q2.Range : DemSpells.Q1.Range,
                        myhero.Position);
                }

                else if (!check(draw, "drawonlyrdy"))
                {
                    Circle.Draw(
                        SharpDX.Color.Gold,
                        IsCougar() ? DemSpells.Q2.Range : DemSpells.Q1.Range,
                        myhero.Position);
                }
            }

            if (check(draw, "drawW") && myhero.Spellbook.GetSpell(SpellSlot.W).Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {
                if (check(draw, "drawonlyrdy"))
                {
                    Circle.Draw(
                        (IsCougar() ? DemSpells.W2.IsOnCooldown : DemSpells.W1.IsOnCooldown) ? SharpDX.Color.Transparent : SharpDX.Color.Gold,
                        IsCougar() ? DemSpells.W2.Range : DemSpells.W1.Range,
                        myhero.Position);
                }
                else if (!check(draw, "drawonlyrdy"))
                {
                    Circle.Draw(
                        SharpDX.Color.Gold,
                        IsCougar() ? DemSpells.W2.Range : DemSpells.W1.Range,
                        myhero.Position);
                }
            }

            if (check(draw, "drawE") && myhero.Spellbook.GetSpell(SpellSlot.E).Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {
                if (check(draw, "drawonlyrdy"))
                {
                    Circle.Draw(
                        (IsCougar() ? DemSpells.E2.IsOnCooldown : DemSpells.E1.IsOnCooldown) ? SharpDX.Color.Transparent : SharpDX.Color.Gold,
                        IsCougar() ? DemSpells.E2.Range : DemSpells.E1.Range,
                        myhero.Position);
                }

                else if (!check(draw, "drawonlyrdy"))
                {
                    Circle.Draw(
                        SharpDX.Color.Gold,
                        IsCougar() ? DemSpells.E2.Range : DemSpells.E1.Range,
                        myhero.Position);
                }
            }

            if (check(draw, "DRAWHEAL"))
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50,
                                 Drawing.WorldToScreen(myhero.Position).Y + 10,
                                 Color.White,
                                 "Auto Healing: ");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X + 37,
                                 Drawing.WorldToScreen(myhero.Position).Y + 10,
                                 key(misc, "EKEY") ? Color.Green : Color.Red,
                                 key(misc, "EKEY") ? "ON" : "OFF");
            }

         /*   foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (check(draw, "drawkillable") && !check(draw, "nodraw") && enemy.IsVisible &&
                    enemy.IsHPBarRendered && !enemy.IsDead && ComboDamage(enemy) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X,
                                     Drawing.WorldToScreen(enemy.Position).Y - 30,
                                     Color.Green, "Killable With Combo");
                }
                else if (check(draw, "drawkillable") && !check(draw, "nodraw") && enemy.IsVisible &&
                         enemy.IsHPBarRendered && !enemy.IsDead && ignt != null
                         ComboDamage(enemy) + myhero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, Color.Green, "Combo + Ignite");
                }
            }*/
        }

        private static void OnProcess(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch(IsCougar())
                {
                    case false:
                        switch(args.Slot)
                        {
                            case SpellSlot.Q:
                                Q1Ready = false;
                                Core.DelayAction(() => { Q1Ready = true; }, (int)(myhero.Spellbook.GetSpell(SpellSlot.Q).Cooldown) * 1000);
                                break;
                            case SpellSlot.W:
                                W1Ready = false;
                                Core.DelayAction(() => { W1Ready = true; }, (int)(myhero.Spellbook.GetSpell(SpellSlot.W).Cooldown) * 1000);
                                break;
                        }
                        break;
                    case true:
                        switch(args.Slot)
                        {
                            case SpellSlot.Q:
                                Q2Ready = false;
                                Core.DelayAction(() => { Q2Ready = true; }, (int)(myhero.Spellbook.GetSpell(SpellSlot.Q).Cooldown) * 1000);
                                Orbwalker.ResetAutoAttack();
                                break;
                            case SpellSlot.W:
                                W2Ready = false;
                                Core.DelayAction(() => { W2Ready = true; }, (int)(myhero.Spellbook.GetSpell(SpellSlot.W).Cooldown) * 1000);
                                break;
                            case SpellSlot.E:
                                E2Ready = false;
                                Core.DelayAction(() => { E2Ready = true; }, (int)(myhero.Spellbook.GetSpell(SpellSlot.E).Cooldown) * 1000);
                                break;
                        }
                        break;
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
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 " + ChampionName + " And Thank You For Using!");
            menu.AddLabel("Version " + Version + " " + Date);
            menu.AddLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddLabel("Spells");
            combo.AddLabel("Human Form");
            combo.Add("CQ1", new CheckBox("Use Q"));
            combo.Add("CW1", new CheckBox("Use W", false));
            combo.AddSeparator();
            combo.AddLabel("Cougar Form");
            combo.Add("CQ2", new CheckBox("Use Q"));
            combo.Add("CW2", new CheckBox("Use W"));
            combo.Add("CE2", new CheckBox("Use E"));
            combo.AddSeparator();
            combo.Add("CR", new CheckBox("Auto Switch Forms (R)"));
            combo.AddSeparator();
            combo.Add("Cignt", new CheckBox("Use Ignite", false));

            harass.AddLabel("Spells");
            harass.AddLabel("Human Form");
            harass.Add("HQ1", new CheckBox("Use Q", false));;
            harass.AddSeparator();
            harass.AddLabel("Cougar Form");
            harass.Add("HQ2", new CheckBox("Use Q", false));
            harass.Add("HW2", new CheckBox("Use W", false));
            harass.Add("HE2", new CheckBox("Use E", false));
            harass.AddSeparator();
            harass.Add("HR", new CheckBox("Auto Switch Forms (R)", false));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.AddLabel("Human Form");
            laneclear.Add("LQ1", new CheckBox("Use Q", false));
            laneclear.Add("LQ1MODE", new ComboBox("Human Q Mode", 0, "Big Minions", "All Minions"));
            laneclear.AddSeparator();
            laneclear.AddLabel("Cougar Form");
            laneclear.Add("LQ2", new CheckBox("Use Q", false));
            laneclear.Add("LQ2MIN", new Slider("Min Minions For Q", 1, 1, 4));
            laneclear.AddSeparator();
            laneclear.Add("LW2", new CheckBox("Use W", false));
            laneclear.Add("LW2MIN", new Slider("Min Minions To Hit With W", 1, 1, 4));
            laneclear.AddSeparator();
            laneclear.Add("LE2", new CheckBox("Use E", false));
            laneclear.Add("LE2MODE", new ComboBox("Cougar E Mode", 0, "Big MInions", "All Minions"));
            laneclear.AddSeparator();
            laneclear.AddLabel("R Usage");
            laneclear.Add("LAUTOR", new CheckBox("Auto Switch Forms (R)", false));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.AddLabel("Human Form");
            jungleclear.Add("JQ1", new CheckBox("Use Q", false));
            jungleclear.Add("JQ1MODE", new ComboBox("Human Q Mode", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.AddLabel("Cougar Form");
            jungleclear.Add("JQ2", new CheckBox("Use Q", false));
            jungleclear.Add("JQ2MIN", new Slider("Min Monsters For Q", 1, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JW2", new CheckBox("Use W", false));
            jungleclear.Add("JW2MIN", new Slider("Min Monsters To Hit With W", 1, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JE2", new CheckBox("Use E", false));
            jungleclear.Add("JE2MODE", new ComboBox("Cougar E Mode", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.AddLabel("R Usage");
            jungleclear.Add("JAUTOR", new CheckBox("Auto Switch Forms (R)", false));
            jungleclear.AddSeparator();
            jungleclear.AddLabel("Smite");
            jungleclear.Add("SMITEKEY", new KeyBind("Auto-Smite Key", false, KeyBind.BindTypes.PressToggle, 'S'));
            jungleclear.AddLabel("(Smite Will Target On Big Monsters Like Blue, Red, Dragon etc.)");
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 50, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range"));
            draw.Add("drawW", new CheckBox("Draw W Range"));
            draw.Add("drawE", new CheckBox("Draw E Range"));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies"));
            draw.Add("DRAWHEAL", new CheckBox("Draw Auto Healing Status"));
            //   draw.AddSeparator();
            //  draw.Add("DRAWMODE", new ComboBox("Which Spells To Draw?", 2, "Human Spells Only", "Cougar Spells Only", "Both Form Spells"));

            misc.AddLabel("Auto Healing (E)");
            misc.Add("EKEY", new KeyBind("Auto Heal Hotkey", false, KeyBind.BindTypes.PressToggle, 'H'));
            misc.AddSeparator();
            misc.Add("EMODE", new ComboBox("Healing Mode", 2, "Only Self", "Only Ally", "Self And Ally"));
            misc.AddLabel("Self");
            misc.Add("EMINH", new Slider("Min Self Health %", 25, 1, 100));
            misc.AddLabel("Ally");
            misc.Add("EMINA", new Slider("Min Ally Health %", 25, 1, 100));
            misc.AddLabel("Mana");
            misc.Add("EMINM", new Slider("Min Mana % To Auto Health", 50, 1, 100));
            misc.AddLabel("_____________________________________________________________________________");
            misc.AddSeparator();
            misc.Add("W2FLEE", new CheckBox("Use Cougar W To Flee", false));
            misc.AddSeparator();
            misc.AddLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable"));
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
            pred.AddSeparator();
            pred.AddLabel("Human W :");
            pred.Add("W1Pred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("Cougar W :");
            pred.Add("W2Pred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("E :");
            pred.Add("EPred", new Slider("Select % Hitchance", 90, 1, 100));
        }

    }

    public static class DemSpells
    {
        public static Spell.Skillshot Q1 { get; private set; }
        public static Spell.Active Q2 { get; private set; }
        public static Spell.Skillshot W1 { get; private set; }
        public static Spell.Skillshot W2 { get; private set; }
        public static Spell.Skillshot W2E { get; private set; }
        public static Spell.Targeted E1 { get; private set; }
        public static Spell.Skillshot E2 { get; private set; }
        public static Spell.Active R { get; private set; }

        static DemSpells()
        {
            Q1 = new Spell.Skillshot(SpellSlot.Q, 1500, SkillShotType.Linear, 500, 1300, 80);
            Q2 = new Spell.Active(SpellSlot.Q, 200);
            W1 = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 500, 1450, 80);
            W2 = new Spell.Skillshot(SpellSlot.W, 375, SkillShotType.Circular, 500, int.MaxValue, 210);
            W2E = new Spell.Skillshot(SpellSlot.W, 750, SkillShotType.Circular, 500, int.MaxValue, 210);
            E1 = new Spell.Targeted(SpellSlot.E, 600);
            E2 = new Spell.Skillshot(SpellSlot.E, 300, SkillShotType.Cone, 500, int.MaxValue,
                (int)(15.00 * Math.PI / 180.00))
            {
                ConeAngleDegrees = 180
            };
            R = new Spell.Active(SpellSlot.R);
        }
    }
}
