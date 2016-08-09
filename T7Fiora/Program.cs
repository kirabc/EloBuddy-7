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
using T7_Fiora.Evade;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7_Fiora
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        public static Menu menu, combo, harass, laneclear, misc, draw, pred, fleee, blocking, jungleclear;
        public static Spell.Targeted ignt { get; private set; }
        private static string Version = "1.3";
        private static string Date = "27/7/16";
        public static Item tiamat { get; private set; }
        public static Item rhydra { get; private set; }
        public static Item thydra { get; private set; }
        public static Item cutl { get; private set; }
        public static Item blade { get; private set; }
        public static Item yomus { get; private set; }
        public static Item potion { get; private set; }
        public static Item biscuit { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Fiora") { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#FF0505'> Fiora</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
           // Orbwalker.OnPostAttack += OnPostAttack;
            DatMenu();
            Game.OnTick += OnTick;
            tiamat = new Item((int)ItemId.Tiamat_Melee_Only, 400);
            rhydra = new Item((int)ItemId.Ravenous_Hydra_Melee_Only, 400);
            thydra = new Item((int)ItemId.Titanic_Hydra);
            cutl = new Item((int)ItemId.Bilgewater_Cutlass, 550);
            blade = new Item((int)ItemId.Blade_of_the_Ruined_King, 550);
            yomus = new Item((int)ItemId.Youmuus_Ghostblade);
            potion = new Item((int)ItemId.Health_Potion);
            biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 600);
            Player.LevelSpell(SpellSlot.Q);
            SpellBlock.Initialize();
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) { Combo(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > harass["HMIN"].Cast<Slider>().CurrentValue) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > laneclear["LMIN"].Cast<Slider>().CurrentValue) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")) { Jungleclear(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Flee)) Flee();

            Misc();
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

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            /*Q>E>W*/
            SpellSlot[] sequence1 = { SpellSlot.Unknown, SpellSlot.W, SpellSlot.E, SpellSlot.Q,
                                        SpellSlot.Q, SpellSlot.R, SpellSlot.Q, SpellSlot.E, 
                                        SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.E, 
                                        SpellSlot.E, SpellSlot.W, SpellSlot.W, SpellSlot.R, 
                                        SpellSlot.W , SpellSlot.W,SpellSlot.Unknown };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += QDamage(target); }

                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { TotalDamage += WDamage(target); }

                if (DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    TotalDamage += (float)((myhero.GetAutoAttackDamage(target) * (int)slider(combo, "AAMIN")) + (new float[] { 0, 1.4f, 1.55f, 1.7f, 1.85f, 2 }[DemSpells.E.Level] * myhero.TotalAttackDamage));
                }

                if (DemSpells.R.IsLearned && DemSpells.R.IsReady())
                { TotalDamage += (float)PassiveManager.GetPassiveDamage(target, 4); }
                else { TotalDamage += (float)PassiveManager.GetPassiveDamage(target, PassiveManager.GetPassiveCount(target)); }

                if (tiamat.IsOwned() && tiamat.IsReady() && tiamat.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, tiamat.Id); }

                if (rhydra.IsOwned() && rhydra.IsReady() && rhydra.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, rhydra.Id); }

                /*  if (thydra.IsOwned() && thydra.IsReady())
                  { TotalDamage += myhero.GetItemDamage(target, thydra.Id); }*/

                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, cutl.Id); }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, blade.Id); }

                return TotalDamage;
            }
            return 0;
        }

        private static float QDamage(AIHeroClient target)
        {
            int index = DemSpells.Q.Level - 1;

            var QDamage = new float[] { 65, 75, 85, 95, 105 }[index] +
                          (new float[] { 0.55f, 0.70f, 0.85f, 1, 1.15f }[index] * myhero.TotalAttackDamage);
            return myhero.CalculateDamageOnUnit(target, DamageType.Physical, QDamage);
        }

        private static float WDamage(AIHeroClient target)
        {
            int index = DemSpells.W.Level - 1;

            var WDamage = new float[] { 90, 130, 170, 210, 250 }[index] + myhero.FlatMagicDamageMod;

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, WDamage);
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

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                var WPred = DemSpells.W.GetPrediction(target);

                PassiveManager.castAutoAttack(target);

                ItemManager(target);

                if (check(combo, "CQ") && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target.Position))
                {
                    switch (comb(pred, "QPREDMODE"))
                    {
                        case 0:
                            PassiveManager.castQhelper(target);
                            break;
                        case 1:
                            DemSpells.Q.Cast(target.Position);
                            break;
                    }
                }

                if (check(combo, "CW") && DemSpells.W.IsReady() && DemSpells.W.IsInRange(target.Position))
                {
                    switch (comb(pred, "WPREDMODE"))
                    {
                        case 0:
                            if (slider(pred, "WPred") <= WPred.HitChancePercent)
                            { DemSpells.W.Cast(WPred.CastPosition); }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;
                    }
                }

                if (check(combo, "CE") && DemSpells.E.IsReady() && target.Distance(myhero.Position) < DemSpells.E.Range)
                {
                    DemSpells.E.Cast();
                }

                if (check(combo, "CR") && DemSpells.R.IsReady() && target.Distance(myhero.Position) < DemSpells.R.Range &&
                    myhero.HealthPercent >= slider(combo, "CRMIN") && ComboDamage(target) > target.Health)
                {
                    if ((ComboDamage(target) - PassiveManager.GetPassiveDamage(target, 4) > target.Health) ||
                       (ignt.IsReady() && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)) return;

                    if(check(combo, "CRTURRET"))
                    {
                        var ClosestTurret = EntityManager.Turrets.Enemies.Where(x => x.IsTargetable).OrderBy(x => x.Distance(myhero.Position)).FirstOrDefault();

                        if (ClosestTurret.Distance(target.Position) > 900) DemSpells.R.Cast(target);
                    }
                    else DemSpells.R.Cast(target); 

                }

                if (check(combo, "Cignt") && ignt.IsReady() && ignt.IsInRange(target.Position))
                {
                    if (target.Health > ComboDamage(target) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health &&
                        !check(misc, "autoign"))
                    {
                        ignt.Cast(target);
                    }
                    else if (target.Health > ComboDamage(target))
                    {
                        if ((ComboDamage(target) + (myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) - 5)) > target.Health)
                        { ignt.Cast(target); }
                    }
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.IsValidTarget())
            {
                var qpred = DemSpells.Q.GetPrediction(target);
                var wpred = DemSpells.W.GetPrediction(target);

                if (check(harass, "HQ") && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target.Position))
                {
                    if (Extensions.CountEnemiesInRange(target, 650) <= slider(harass, "HQMAX"))
                    {
                        switch (comb(pred, "QPREDMODE"))
                        {
                            case 0:
                                PassiveManager.castQhelper(target);
                                break;
                            case 1:
                                DemSpells.Q.Cast(target.Position);
                                break;
                        }
                    }
                }

                if (check(harass, "HW") && DemSpells.W.IsReady() && DemSpells.W.IsInRange(target.Position) &&
                   !target.IsZombie && !target.IsInvulnerable)
                {
                    switch (comb(pred, "WPREDMODE"))
                    {
                        case 0:
                            if (wpred.HitChancePercent >= slider(pred, "WPred")) { DemSpells.W.Cast(wpred.CastPosition); }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;

                    }
                }
            }

        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range).ToArray();

            if (minions != null)
            {

                var wpred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, DemSpells.W.Width, (int)DemSpells.W.Range);

                if (check(laneclear, "LQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
                {
                    foreach (var minion in minions.Where(x => x.IsValid() && !x.IsDead && x.Health > 15))
                    {
                        if (comb(pred, "QPREDMODE") == 0 &&
                            Prediction.Position.PredictUnitPosition(minion, DemSpells.Q.CastDelay).Distance(myhero.Position) <= (DemSpells.Q.Range - 50))
                        { DemSpells.Q.Cast(minion.Position); }

                        else { DemSpells.Q.Cast(minion.Position); }

                    }
                }

                if (check(laneclear, "LW") && DemSpells.W.IsLearned && DemSpells.W.IsReady())
                {
                    if (slider(laneclear, "LWMIN") == 1)
                    {
                        switch (comb(pred, "WPREDMODE"))
                        {
                            case 0:
                                if (wpred.HitNumber == slider(pred, "WPred")) { DemSpells.W.Cast(wpred.CastPosition); }
                                break;
                            case 1:
                                DemSpells.W.Cast(minions.Where(x => x.Distance(myhero.Position) < DemSpells.W.Range &&
                                                               !x.IsDead && x.Health > 25 && x.IsValid()).OrderBy(x => x.Distance(myhero.Position))
                                                                                                         .FirstOrDefault().Position);
                                break;
                        }
                    }
                    else
                    {
                        if (wpred.HitNumber >= slider(laneclear, "LWMIN"))
                        { DemSpells.W.Cast(wpred.CastPosition); }
                    }
                }

                if (check(laneclear, "LE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    int count = minions.Where(x => x.IsValid() && !x.IsDead && x.Distance(myhero.Position) <= 170).Count();

                    if (count >= slider(laneclear, "LEMIN"))
                    {
                        DemSpells.E.Cast();
                    }

                }

                if (rhydra.IsOwned() && rhydra.IsReady())
                {
                    int count = minions.Where(x => x.IsValid() && !x.IsDead && x.Distance(myhero.Position) <= rhydra.Range).Count();

                    if (count >= slider(laneclear, "HYDRAMIN")) rhydra.Cast();                                        
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1800f);

            var WPred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(Monsters, DemSpells.W.Width, (int)DemSpells.W.Range);

            if (check(jungleclear, "JQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
            {
                foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 100))
                {
                    DemSpells.Q.Cast(monster.Position);
                }
            }

            if (check(jungleclear, "JW") && DemSpells.W.IsLearned && DemSpells.W.IsReady())
            {
                if (WPred.HitNumber >= slider(jungleclear, "JWMIN")) DemSpells.W.Cast(WPred.CastPosition);
            }

            if (check(jungleclear, "JE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
            {
                int count = Monsters.Where(x => x.IsValid() && !x.IsDead && x.Distance(myhero.Position) <= Player.Instance.GetAutoAttackRange()).Count();

                if (count >= slider(jungleclear, "JEMIN")) DemSpells.E.Cast(); 
            }

            if (rhydra.IsOwned() && rhydra.IsReady())
            {
                int count = Monsters.Where(x => x.IsValid() && !x.IsDead && !x.Name.ToLower().Contains("mini") &&
                                           x.Distance(myhero.Position) <= rhydra.Range)
                                    .Count();

                if (count >= 1) rhydra.Cast();
            }
        }

        private static void Flee()
        {
            if (myhero.CountEnemiesInRange(1000) < slider(fleee, "FLEEMIN") && slider(fleee,"FLEEMIN") != 0) return;

            if (check(fleee, "QFLEE") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
            {
                DemSpells.Q.Cast(Game.CursorPos);
            }

            if (check(fleee, "YOMUSFLEE") && yomus.IsOwned() && yomus.IsReady())
            {
                yomus.Cast();
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);


            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") || !myhero.HasBuff("ItemMiniRegenPotion")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(potion.Id) && Item.CanUseItem(potion.Id)) potion.Cast();

                else if (Item.HasItem(biscuit.Id) && Item.CanUseItem(biscuit.Id)) biscuit.Cast();
            }

            if (target != null)
            {
                var qpred = DemSpells.Q.GetPrediction(target);
                var wpred = DemSpells.W.GetPrediction(target);

                if (check(misc, "ksQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && target.IsValidTarget(DemSpells.Q.Range) &&
                   !target.IsZombie && !target.IsInvulnerable && QDamage(target) > target.Health && slider(pred, "QPred") >= qpred.HitChancePercent)
                {
                    switch (comb(pred, "QPREDMODE"))
                    {
                        case 0:
                            PassiveManager.castQhelper(target);
                            break;
                        case 1:
                            DemSpells.Q.Cast(target.Position);
                            break;
                    }
                }

                if (check(misc, "ksW") && DemSpells.W.IsLearned && DemSpells.W.IsReady() && target.IsValidTarget(DemSpells.W.Range) &&
                   !target.IsZombie && !target.IsInvulnerable && WDamage(target) > target.Health)
                {
                    switch (comb(pred, "WPREDMODE"))
                    {
                        case 0:
                            if (wpred.HitChancePercent >= slider(pred, "WPred")) { DemSpells.W.Cast(wpred.CastPosition); }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;

                    }
                }

                if (check(misc, "autoign") && ignt.IsReady() &&
                    ignt.IsInRange(target) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }

        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, 750, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, 750, myhero.Position); }

            }

            if (check(draw, "drawW") && DemSpells.W.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.W.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

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

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var unit = sender as AIHeroClient;
            

            /*
                Credits => JokerArt
            */


            if (unit == null || !unit.IsValid)
            {
                return;
            }


            if (unit.IsMe && args.Slot.Equals(SpellSlot.E))
            {
                Orbwalker.ResetAutoAttack();
                return;
            }

            if (!unit.IsEnemy || !check(blocking, "BLOCK") || !DemSpells.W.IsReady())
            {
                return;
            }

            if (Evade.SpellDatabase.GetByName(args.SData.Name) != null && !check(blocking, "evade"))
                return;

            if (!SpellBlock.Contains(unit, args))
                return;

            if (args.End.Distance(Player.Instance) == 0)
                return;

            if (check(blocking, "RANGE") && unit.Distance(myhero.Position) > DemSpells.W.Range)
                return;
  

            var castUnit = unit;
            var type = args.SData.TargettingType;

            if (!unit.IsValidTarget())
            {
                var target = TargetSelector.GetTarget(DemSpells.W.Range, DamageType.Mixed);
                if (target == null || !target.IsValidTarget(DemSpells.W.Range))
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && target.IsValidTarget(DemSpells.W.Range))
                {
                    castUnit = target;
                }
            }

            if (unit.ChampionName.Equals("Caitlyn") && args.Slot == SpellSlot.Q)
            {
                Core.DelayAction(() => CastW(castUnit),
                    ((int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                    (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500) + 250);
            }
            if (unit.ChampionName.Equals("Cassiopeia"))
            {
                switch(args.Slot)
                {
                    case SpellSlot.Q:
                        if (Prediction.Position.PredictUnitPosition(myhero, 400).Distance(args.Target.Position) <= 75)
                        {
                            Core.DelayAction(() => CastW(castUnit), 300);
                        }
                        break;
                    case SpellSlot.E:
                        if (args.Target.IsMe)
                        {
                            Core.DelayAction(() => CastW(castUnit),
                                ((int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                                (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500));
                        }
                        break;
                    case SpellSlot.R:
                        if (Prediction.Position.PredictUnitPosition(myhero, 500).Distance(args.Target.Position) <= 875 && PassiveManager.AngleBetween(myhero.Position.To2D(),unit.Position.To2D(),unit.Direction.To2D()) <= 85)
                        {
                            Core.DelayAction(() => CastW(castUnit), 500);
                        }
                        break;
                }
            }
            if (unit.ChampionName.Equals("Blitzcrank") && args.Slot == SpellSlot.R && unit.Distance(myhero.Position) < 600)
            {
               // Core.DelayAction(() => CastW(castUnit), 100);
                CastW(castUnit);
            }
            if (unit.ChampionName.Equals("Darius") && args.Slot == SpellSlot.Q && unit.Distance(myhero.Position) < 420)
            {
                Core.DelayAction(() => CastW(castUnit), 700);
            }
            if (unit.ChampionName.Equals("Jax") && args.Slot == SpellSlot.E && unit.Distance(myhero.Position) < 187.5f)
            {
                Core.DelayAction(() => CastW(castUnit), 1000);
            }
            if (unit.ChampionName.Equals("Malphite") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 300)
            {
                Core.DelayAction(() => CastW(castUnit),
                    ((int)(args.Start.Distance(Player.Instance) / 700 * 1000) -
                    (int)(args.End.Distance(Player.Instance) / 700) - 500) + 100);
            }
            if (unit.ChampionName.Equals("Morgana") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 300)
            {
                Core.DelayAction(() => CastW(castUnit), 3000);
            }
            if (unit.ChampionName.Equals("KogMaw") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 240)
            {
                Core.DelayAction(() => CastW(castUnit), 1200);
            }
            if (unit.ChampionName.Equals("Ziggs") && args.Slot == SpellSlot.R && myhero.Position.Distance(args.End) < 550)
            {
                Core.DelayAction(() => CastW(castUnit),
                    ((int)(args.Start.Distance(Player.Instance) / 2800 * 1000) -
                    (int)(args.End.Distance(Player.Instance) / 2800) - 500) + 900);
            }
            if (unit.ChampionName.Equals("Karthus") && (args.Slot == SpellSlot.R || args.Slot == SpellSlot.Q))
            {
                switch (args.Slot)
                {
                    case SpellSlot.R:
                        Core.DelayAction(() => CastW(castUnit), 2900);
                        break;
                    case SpellSlot.Q:
                        if (Prediction.Position.PredictUnitPosition(myhero, 450).Distance(args.Target.Position) < 100)
                        {
                            Core.DelayAction(() => CastW(castUnit), 450);
                        }
                        break;
                }
            }
            if (unit.ChampionName.Equals("Shen") && args.Slot == SpellSlot.E && args.End.Distance(myhero.Position) < 100)
            {
                Core.DelayAction(() => CastW(castUnit),
                    ((int)(args.Start.Distance(Player.Instance) / 1600 * 1000) -
                    (int)(args.End.Distance(Player.Instance) / 1600) - 500) + 250);
            }
            if (unit.ChampionName.Equals("Zyra"))
            {
                    Core.DelayAction(() => CastW(castUnit),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500);
            }
            if (unit.ChampionName.Equals("Amumu") && args.Slot == SpellSlot.R && unit.Distance(myhero.Position) <= 550)
            {
                CastW(castUnit);
            } 
            if (args.End.Distance(Player.Instance) < 250)
            {
                if (unit.ChampionName.Equals("Bard") && args.End.Distance(Player.Instance) < 300)
                {
                    Core.DelayAction(() => CastW(castUnit), (int)(unit.Distance(Player.Instance) / 7f) + 400);
                }
                else if (unit.ChampionName.Equals("Ashe"))
                {
                    Core.DelayAction(() => CastW(castUnit),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)args.End.Distance(Player.Instance));
                    return;
                }
                else if (unit.ChampionName.Equals("Varus") || unit.ChampionName.Equals("TahmKench") ||
                         unit.ChampionName.Equals("Lux"))
                {
                    if (unit.ChampionName.Equals("Lux") && args.Slot == SpellSlot.R)
                    {
                        Core.DelayAction(() => CastW(castUnit), 400);
                    }
                    Core.DelayAction(() => CastW(castUnit),
                        (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                        (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500);

                }
                else if (unit.ChampionName.Equals("Amumu"))
                {
                    if (sender.Distance(Player.Instance) < 1100)
                        Core.DelayAction(() => CastW(castUnit),
                            (int)(args.Start.Distance(Player.Instance) / args.SData.MissileSpeed * 1000) -
                            (int)(args.End.Distance(Player.Instance) / args.SData.MissileSpeed) - 500);
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
                    Console.WriteLine("TT: " + travelTime + " Delay: " + delay);
                    Core.DelayAction(() => CastW(castUnit), (int)delay);
                    return;
                }
                CastW(castUnit);
            }

            if (type.Equals(SpellDataTargetType.Unit))
            {
                if (unit.ChampionName.Equals("Bard") && args.End.Distance(Player.Instance) < 300)
                {
                    Core.DelayAction(() => CastW(castUnit), 400 + (int)(unit.Distance(Player.Instance) / 7f));
                }
                else if (unit.ChampionName.Equals("Riven") && args.End.Distance(Player.Instance) < 260)
                {
                    CastW(castUnit);
                }
                else
                {
                    CastW(castUnit);
                }
            }
            else if (type.Equals(SpellDataTargetType.LocationAoe) &&
                     args.End.Distance(Player.Instance) < args.SData.CastRadius)
            {
                if (unit.ChampionName.Equals("Annie") && args.Slot.Equals(SpellSlot.R))
                {
                    return;
                }
                CastW(castUnit);
            }
            else if (type.Equals(SpellDataTargetType.Cone) &&
                     args.End.Distance(Player.Instance) < args.SData.CastRadius)
            {
                CastW(castUnit);
            }
            else if (type.Equals(SpellDataTargetType.SelfAoe) || type.Equals(SpellDataTargetType.Self))
            {
                var d = args.End.Distance(Player.Instance.ServerPosition);
                var p = args.SData.CastRadius > 5000 ? args.SData.CastRange : args.SData.CastRadius;
                if (d < p)
                    CastW(castUnit);
            }
        }

        public static bool CastW(Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget(DemSpells.W.Range))
                return DemSpells.W.Cast(Player.Instance.Position);

            var cast = DemSpells.W.GetPrediction(target);
            var castPos = DemSpells.W.IsInRange(cast.CastPosition) ? cast.CastPosition : target.ServerPosition;

            return DemSpells.W.Cast(castPos);
        }
    
        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 Fiora", "fiora");
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            jungleclear = menu.AddSubMenu("Jungleclear", "jclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");
            fleee = menu.AddSubMenu("Flee", "fleeee");
            blocking = menu.AddSubMenu("SpellBlock", "spellblok");
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 Fiora And Thank You For Using!");
            menu.AddGroupLabel("Version " + Version + " " + Date);
            menu.AddGroupLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddGroupLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddGroupLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
            //   combo.Add("CQGAP", new CheckBox("Use Q To Gapclose If Enemy Out Of Range", true));
            combo.Add("CW", new CheckBox("Use W", true));
            combo.Add("CE", new CheckBox("Use E", true));
       //     combo.Add("CERESET", new CheckBox("Only Use E After AA(Reset AA)", true));
            combo.AddSeparator();
            combo.Add("CR", new CheckBox("Use R", true));
            combo.Add("CRTURRET", new CheckBox("Dont Use R When Close To Enemy Turrets", false));
            combo.Add("CRMIN", new Slider("Min % Health To Use R", 35, 1, 99));
            combo.AddSeparator();
            combo.Add("AAMIN", new Slider("Min Auto Attacks For Combo Damage Calculation", 2, 1, 30));
            combo.Add("Cignt", new CheckBox("Use Ignite", false));
            combo.Add("ITEMS", new CheckBox("Use Items", true));

            harass.AddGroupLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", true));
            harass.Add("HQMAX", new Slider("Dont Use Q If More Than X Enemies Nearby", 1, 1, 5));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use W", true));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", true));
            laneclear.AddSeparator();
            laneclear.Add("LW", new CheckBox("Use W", true));
            laneclear.Add("LWMIN", new Slider("Min Minions To Hit With W", 2, 1, 6));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", true));
            laneclear.Add("LEMIN", new Slider("Min Minions To Hit With E", 2, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LHYDRA", new CheckBox("Use Hydra", true));
            laneclear.Add("HYDRAMIN", new Slider("Min Minions Nearby To Use Hydra", 5, 1, 20));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W", true));
            jungleclear.Add("JWMIN", new Slider("Min Monsters To Hit With W", 1, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E", false));
            jungleclear.Add("JEMIN", new Slider("Min Monsters To Cast W", 1, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JHYDRA", new CheckBox("Use Hydra", true));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 10, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawW", new CheckBox("Draw W Range", true));
            draw.Add("drawR", new CheckBox("Draw R Range", true));
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies", true));

            misc.AddGroupLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            misc.Add("ksW", new CheckBox("Killsteal with W", true));
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));           
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
            misc.Add("skinID", new ComboBox("Skin Hack", 3, "Default", "Royal Guard", "Nightraven", "Headmistress", "PROJECT:", "Pool Party"));

            blocking.Add("BLOCK", new CheckBox("Use W To Block Spells", true));        
            blocking.Add("evade", new CheckBox("Evade Integration", true));
            blocking.AddSeparator();
            blocking.Add("RANGE", new CheckBox("Only Block When Caster Is In W Range", false));
            blocking.AddSeparator();

            fleee.AddGroupLabel("Spells/Items To Use On Flee Mode");
            fleee.Add("QFLEE", new CheckBox("Use Q To Flee", true));
            fleee.AddLabel("(Casts To Mouse Position)", 1);
            fleee.AddSeparator();
            fleee.Add("YOMUSFLEE", new CheckBox("Use Youmuu's Ghostblade While In Flee Mode", false));
            fleee.AddSeparator();
            fleee.Add("FLEEMIN", new Slider("Min Enemies In Range To Flee", 0, 0, 5));

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("Q :");
            pred.Add("QPREDMODE", new ComboBox("Use Prediction For Q", 0, "On", "Off"));
            pred.Add("QPred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("W :");
            pred.Add("WPREDMODE", new ComboBox("Use Prediction For W", 0, "On", "Off"));
            pred.Add("WPred", new Slider("Select % Hitchance", 80, 1, 100));
        }
    }
    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 750, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 250, 500, 0);
            W = new Spell.Skillshot(SpellSlot.W, 750, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 500, 3200, 70);
            E = new Spell.Active(SpellSlot.E, 175);
            E.CastDelay = 0;

            R = new Spell.Targeted(SpellSlot.R, 500);
            R.CastDelay = (int).066f;
        }
    }
}
