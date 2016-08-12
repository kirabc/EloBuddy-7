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

namespace T7_Brand
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;
        private static Prediction.Position.PredictionData WData = new Prediction.Position.PredictionData(Prediction.Position.PredictionData.PredictionType.Circular,
                                                                                                         900, 250, 0, 650, int.MaxValue);
        private static Spell.Targeted Ignite { get; set; }
        static readonly string ChampionName = "Brand";
        static readonly string Version = "1.0";
        static readonly string Date = "11/8/16";
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#FF0505'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Game.OnTick += OnTick;
            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            RPotion = new Item((int)ItemId.Refillable_Potion);
            Player.LevelSpell(SpellSlot.W);

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

            if (check(misc, "AUTOEXPLODE")) AutoExplode();
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
            SpellSlot[] sequence1 = { U, Q, E, W, W, R, W, Q, W, Q, R, Q, Q, E, E, R, E, E, U };
            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
            /*  if (check(misc, "autolevel"))
              {
                  Core.DelayAction(delegate
                  {
                      Player.LevelSpell(sequence1[myhero.Level]);
                  }, slider(misc, "LVLDELAY"));
              }*/
        }

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += QDamage(target); }

                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { TotalDamage += WDamage(target); }

                if (DemSpells.E.IsLearned && DemSpells.E.IsReady()) { TotalDamage += EDamage(target); }

                if (DemSpells.R.IsLearned && DemSpells.R.IsReady()) { TotalDamage += RDamage(target); }

                return TotalDamage;
            }
            return 0;
        }

        private static float QDamage(AIHeroClient target)
        {
            int index = DemSpells.Q.Level - 1;

            var QDamage = new[] { 80, 110, 140, 170, 200 }[index] + (0.55f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, QDamage);
        }

        private static float WDamage(AIHeroClient target)
        {
            int index = DemSpells.W.Level - 1;

            var WDamage = new[] { 75, 120, 165, 210, 255 }[index] + 
                ((IsBlazed(target) ? 0.25f : 0) * new[] { 75, 120, 165, 210, 255 }[index]) + 
                ((IsBlazed(target) ? 0.6f : 0.75f) * myhero.FlatMagicDamageMod);                       

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, WDamage);
        }

        private static float EDamage(AIHeroClient target)
        {
            int index = DemSpells.E.Level - 1;

            var EDamage = new[] { 70, 90, 110, 130, 150 }[index] + (0.35f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, EDamage);
        }

        private static float RDamage(AIHeroClient target)
        {
            int index = DemSpells.R.Level - 1;

            var RDamage = new[] { 100, 200, 300 }[index] + (0.25f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, RDamage);
        }

        private static bool IsBlazed(AIHeroClient target)
        {
            return target.HasBuff("BrandAblaze");
        }

        private static int GetStacks(AIHeroClient target)
        {
            if (IsBlazed(target)) return target.GetBuffCount("BrandAblaze");

            return 0;
        }

        private static void Combo()
        {
            var wtarget = TargetSelector.GetTarget(DemSpells.E.Range + 200, DamageType.Magical, Player.Instance.Position);

            if (check(combo, "CW") && DemSpells.W.IsReady() && myhero.CountEnemiesInRange(DemSpells.E.Range) > 0)
            {
                var list = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(DemSpells.W.Range)).ToArray<Obj_AI_Base>();

                if (list != null)
                {                  
                    if (myhero.CountEnemiesInRange(DemSpells.E.Range) == 1)
                    {
                        var wpred = DemSpells.W.GetPrediction(wtarget);

                        if (wpred.HitChancePercent >= slider(pred, "WPred") && DemSpells.W.Cast(wpred.CastPosition)) return;
                    }
                    else
                    {
                        var AoEPRed = Prediction.Position.PredictCircularMissileAoe(list, DemSpells.W.Range, DemSpells.W.Radius, DemSpells.W.CastDelay, int.MaxValue)
                                                         .OrderByDescending(x => x.CollisionObjects.Where(y => y is AIHeroClient && y.IsEnemy)
                                                                                                   .Count())
                                                         .FirstOrDefault(x => x.CollisionObjects.Contains(wtarget) || x.CollisionObjects.Any());

                        if (AoEPRed != null && DemSpells.W.Cast(AoEPRed.CastPosition)) return;
                    }
                }
            }     
            
            var target = TargetSelector.GetTarget(1200, DamageType.Magical, Player.Instance.Position);

            if (target != null && !target.IsInvulnerable)
            {
                if (check(combo, "CQ") && DemSpells.Q.CanCast(target))
                {
                    if ( (!check(combo, "FOCUSSTUN")) ||
                         (check(combo, "FOCUSSTUN") && IsBlazed(target)) ||
                         (check(combo, "FOCUSSTUN") && !IsBlazed(target) && DemSpells.W.IsOnCooldown && DemSpells.E.IsOnCooldown))
                        
                    {
                        var qpred = DemSpells.Q.GetPrediction(target);

                        if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "QPred") && DemSpells.Q.Cast(qpred.CastPosition)) return;
                    }
                }          

                if (check(combo, "CE") && DemSpells.E.CanCast(target))
                {
                    DemSpells.E.Cast(target);
                }

                if (Ignite != null && check(combo, "CIgnite") && Ignite.IsReady() && ComboDamage(target) < target.Health &&
                (ComboDamage(target) + myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite)) > target.Health)
                {
                    Ignite.Cast(target);
                }
            }

            if (check(combo, "CR") && DemSpells.R.IsReady())
            {
                var AoeHit = Extensions.CountEnemiesInRange(target.Position, 400) >= slider(combo, "CRMIN");
                var bestaoe =
                    EntityManager.Heroes.Enemies.OrderByDescending(e => Extensions.CountEnemiesInRange(e.Position, 400))
                        .FirstOrDefault(e => e.IsValidTarget(DemSpells.R.Range) && Extensions.CountEnemiesInRange(e.Position, 400) >= slider(combo, "CRMIN"));
                if (AoeHit)
                {
                    DemSpells.R.Cast(target);
                }
                else
                {
                    if (bestaoe != null)
                    {
                        DemSpells.R.Cast(bestaoe);
                    }
                }
            }

            
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.IsValid && !target.IsInvulnerable)
            {
                if (check(harass, "HQ") && DemSpells.Q.CanCast(target))
                {
                    if (check(harass, "HQSTUN") && !IsBlazed(target)) return;

                    var qpred = DemSpells.Q.GetPrediction(target);

                    if (qpred.HitChancePercent >= slider(pred, "QPred") && DemSpells.Q.Cast(qpred.CastPosition)) return;
                }

                if (check(harass, "HW") && DemSpells.W.IsReady() && myhero.CountEnemiesInRange(DemSpells.W.Range) >= slider(harass, "HWMIN"))
                {
                    var Enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValid && DemSpells.W.CanCast(x));

                    if (Enemies != null)
                    {
                        var AoePred = Prediction.Position.GetPredictionAoe(Enemies.ToArray<Obj_AI_Base>(), WData);

                        foreach(var Result in AoePred)
                        {
                            if (Result.CollisionObjects.Count() >= slider(harass, "HWMIN") && DemSpells.W.Cast(Result.CastPosition))
                            { return; }
                        }
                    }
                }

                if (check(harass, "HE") && DemSpells.E.IsReady())
                {
                    foreach(var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValid && DemSpells.E.CanCast(x))
                                                                     .OrderByDescending(x => TargetSelector.GetPriority(x)))
                    {
                        if (check(harass, "HE" + enemy.ChampionName)) DemSpells.E.Cast(enemy);
                    }
                }
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.Q.Range);

            if (minions != null)
            {
                if (check(laneclear, "LQ") && DemSpells.Q.IsReady())
                {
                    foreach (var minion in minions.Where(x => DemSpells.Q.CanCast(x)))
                    {
                        if (comb(laneclear, "LQMODE") == 0 && !minion.Name.Contains("Siege") && !minion.Name.Contains("Super") ) return;

                        var qpred = DemSpells.Q.GetPrediction(minion);

                        if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q.Cast(qpred.CastPosition);
                    }
                }

                if (check(laneclear, "LW") && DemSpells.W.IsReady())
                {
                    DemSpells.W.CastOnBestFarmPosition(slider(laneclear, "LWMIN"));
                }

                if (check(laneclear, "LE") && DemSpells.E.IsReady())
                {
                    foreach (var minion in minions.Where(x => DemSpells.E.CanCast(x)))
                    {
                        if (comb(laneclear, "LEMODE") == 0 && !minion.Name.Contains("Siege") && !minion.Name.Contains("Super")) return;

                        DemSpells.E.Cast(minion);
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1100).Where(x => !x.IsDead && x.IsValidTarget(1100));

            if (Monsters != null)
            {
                if (check(jungleclear, "JQ") && DemSpells.Q.IsReady())
                {
                    foreach(var monster in Monsters.Where(x => DemSpells.Q.CanCast(x)))
                    {
                        if (monster.Name.ToLower().Contains("mini") && comb(jungleclear, "JQMODE") == 0) return;

                        var qpred = DemSpells.Q.GetPrediction(monster);

                        if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q.Cast(qpred.CastPosition);
                    }
                }

                if (check(jungleclear, "JW") && DemSpells.W.IsReady())
                {
                    var AoEPred = Prediction.Position.GetPredictionAoe(Monsters.ToArray<Obj_AI_Base>(), WData);

                    foreach (var Result in AoEPred)
                    {
                        if (Result.CollisionObjects.Count() >= slider(jungleclear, "JWMIN")) DemSpells.W.Cast(Result.CastPosition);
                    }
                }

                if (check(jungleclear, "JE") && DemSpells.E.IsReady())
                {
                    foreach (var monster in Monsters.Where(x => DemSpells.E.CanCast(x)))
                    {
                        if (monster.Name.ToLower().Contains("mini") && comb(jungleclear, "JEMODE") == 0) return;

                        DemSpells.E.Cast(monster);
                    }
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1100, DamageType.Magical, Player.Instance.Position);

            if (target != null && !target.IsInvulnerable)
            {
                if (check(misc, "KSQ") && DemSpells.Q.CanCast(target) && Prediction.Health.GetPrediction(target, DemSpells.Q.CastDelay) > 0 &&
                    target.Health < QDamage(target))
                {
                    var qpred = DemSpells.Q.GetPrediction(target);

                    if (qpred.HitChancePercent >= slider(pred, "QPred")) DemSpells.Q.Cast(qpred.CastPosition);
                }

                if (check(misc, "KSW") && DemSpells.W.CanCast(target) && Prediction.Health.GetPrediction(target, DemSpells.W.CastDelay) > 0 &&
                    target.Health < WDamage(target))
                {
                    var wpred = DemSpells.W.GetPrediction(target);

                    if (wpred.HitChancePercent >= slider(pred, "WPred")) DemSpells.W.Cast(wpred.CastPosition);
                }

                if (check(misc, "KSE"))
                {
                   foreach(var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValid && DemSpells.E.CanCast(x) && target.Health < EDamage(target) &&
                                                                                 Prediction.Health.GetPrediction(target, DemSpells.E.CastDelay) > 0 ))
                    {
                        DemSpells.E.Cast(enemy);
                    }
                }

                if (check(misc, "KSR") && DemSpells.R.CanCast(target) && Prediction.Health.GetPrediction(target, DemSpells.R.CastDelay) > 0 &&
                    target.Health < RDamage(target) && (ComboDamage(target) - RDamage(target)) < target.Health)
                {
                    DemSpells.R.Cast(target);
                }

                if (check(misc, "autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) && ComboDamage(target) < target.Health &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
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

          //  if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);
        }

        private static void AutoExplode()
        {
            var Enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValid && x.Distance(myhero.Position) < DemSpells.Q.Range && GetStacks(x) == 2);

            if (Enemies != null && check(misc, "AUTOEXPLODE"))
            {
                if (check(misc, "AUTOQ"))
                {
                    foreach(var target in Enemies.Where(x => DemSpells.Q.CanCast(x)).OrderByDescending(x => TargetSelector.GetPriority(x)))
                    {
                        var qpred = DemSpells.Q.GetPrediction(target);

                        if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "QPred") && DemSpells.Q.Cast(qpred.CastPosition))
                        {
                            Core.DelayAction(() => { return; }, DemSpells.Q.CastDelay + (int)(target.Distance(myhero.Position) / DemSpells.Q.Speed) * 1000);
                        }
                    }
                }

                else if (check(misc, "AUTOW"))
                {
                    foreach(var target in Enemies.Where(x => DemSpells.W.CanCast(x)).OrderByDescending(x => TargetSelector.GetPriority(x)))
                    {
                        var wpred = DemSpells.W.GetPrediction(target);

                        if (wpred.HitChancePercent >= slider(pred, "WPred") && DemSpells.W.Cast(wpred.CastPosition))
                        {
                            Core.DelayAction(() => { return; }, DemSpells.W.CastDelay);
                        }
                    }
                }

                else if (check(misc, "AUTOE"))
                {
                    foreach (var target in Enemies.Where(x => DemSpells.E.CanCast(x)).OrderByDescending(x => TargetSelector.GetPriority(x)))
                    {
                        DemSpells.E.Cast(target);

                        Core.DelayAction(() => { return; }, DemSpells.E.CastDelay);
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0 && !myhero.IsDead)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.Q.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.Q.Range, myhero.Position); }

            }

            if (check(draw, "drawW") && DemSpells.W.Level > 0 && !myhero.IsDead)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.W.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0 && !myhero.IsDead)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position); }

            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0 && !myhero.IsDead)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

            }

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (check(draw, "drawkillable") && enemy.IsVisible &&
                    enemy.IsHPBarRendered && !enemy.IsDead && ComboDamage(enemy) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X,
                                     Drawing.WorldToScreen(enemy.Position).Y - 30,
                                     Color.Green, "Killable With Combo");
                }
                else if (check(draw, "drawkillable") && enemy.IsVisible &&
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
            combo.Add("CRMIN", new Slider("Min Enemies To Hit With R", 2, 1, 5));
            combo.AddSeparator();
            combo.Add("FOCUSSTUN", new CheckBox("Focus On Stunning Target"));
            if (Ignite != null) combo.Add("CIgnite", new CheckBox("Use Ignite", false));


            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", false));
            harass.Add("HQSTUN", new CheckBox("Use Q On To Stun"));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use W", false));
            harass.Add("HWMIN", new Slider("Min Champs For W", 2, 1, 5));
            harass.AddSeparator();
            harass.Add("HE", new CheckBox("Use E", false));
            harass.AddLabel("Use E On:");
            foreach(AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                harass.Add("HE" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", false));
            laneclear.Add("LQMODE", new ComboBox("Q Targets", 0, "Big Minions", "All Minions"));
            laneclear.AddSeparator();
            laneclear.Add("LW", new CheckBox("Use W", false));
            laneclear.Add("LWMIN", new Slider("Min Minions For W", 2, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", false));
            laneclear.Add("LEMODE", new ComboBox("Q Targets", 0, "Big Minions", "All Minions"));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));
            jungleclear.Add("JQMODE", new ComboBox("Q Targets", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W", false));
            jungleclear.Add("JWMIN", new Slider("Min Minions For W", 2, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E", false));
            jungleclear.Add("JEMODE", new ComboBox("Q Targets", 0, "Big Monsters", "All Monsters"));
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
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies", true));

            misc.AddLabel("Killsteal");
            misc.Add("KSQ", new CheckBox("Killsteal with Q", false));
            misc.Add("KSW", new CheckBox("Killsteal with W", false));
            misc.Add("KSE", new CheckBox("Killsteal with E"));
            misc.Add("KSR", new CheckBox("Killsteal with R", false));
            if (Ignite != null) misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));
            misc.AddSeparator();
            misc.Add("AUTOEXPLODE", new CheckBox("Auto-Explode Target", false));
            misc.AddLabel("With");
            misc.Add("AUTOQ", new CheckBox("Q", false));
            misc.Add("AUTOW", new CheckBox("W", false));
            misc.Add("AUTOE", new CheckBox("E", false));
            misc.AddSeparator();
            misc.AddLabel("Auto Potion");
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion", true));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.Add("LVLDELAY", new Slider("Auto Level Delay(ms)", 350, 0, 1000));
            misc.AddLabel("(Delays Are Randomized Based On Your Selection)");
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack", true));
            misc.Add("skinID", new ComboBox("Skin Hack", 5, "Default", "Apocalyptic", "Vandal", "Cryocore", "Zombie", "Spirit Fire"));

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
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Targeted E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1050, SkillShotType.Linear, 250, 1600, 120)
            {
                AllowedCollisionCount = 0                               
            };
            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 650, int.MaxValue, 250)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Targeted(SpellSlot.E, 625);
            R = new Spell.Targeted(SpellSlot.R, 750);
        }
    }
}
