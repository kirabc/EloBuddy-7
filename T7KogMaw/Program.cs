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


namespace T7_KogMaw
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        
        private static Menu menu, combo, harass, laneclear, misc, draw, pred, jungleclear;
        
        private static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 600);
        private static Spell.Targeted smite = new Spell.Targeted(myhero.GetSpellSlotFromName("summonersmite"), 500);
        
        public static int WRange = new[] { 0, 590, 620, 650, 680, 710 }[Player.Instance.Spellbook.GetSpell(SpellSlot.W).Level];
        public static int RRange = new[] { 0, 1200, 1500, 1800 }[Player.Instance.Spellbook.GetSpell(SpellSlot.R).Level];
        
        static readonly string ChampionName = "KogMaw";
        static readonly string Version = "1.1";
        static readonly string Date = "24/7/16";
        
        public static Item cutl { get; private set; }
        public static Item blade { get; private set; }
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != "KogMaw") { return; }

            Chat.Print("<font color='#0040FF'>T7</font><font color='#0B7D0B'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#3737E6'>Toyota</font><font color='#D61EBE'>7</font><font color='#FF0000'> <3 </font>");

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Gapcloser.OnGapcloser += OnGapcloser;
            Game.OnTick += OnTick; 
                    
            cutl = new Item((int)ItemId.Bilgewater_Cutlass, 550);
            blade = new Item((int)ItemId.Blade_of_the_Ruined_King, 550);
            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);

            Player.LevelSpell(SpellSlot.W);
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

            if (check(misc, "AUTOPASSIVE")) PassiveLogic();         
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

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += QDamage(target); }

                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { TotalDamage += myhero.GetAutoAttackDamage(target) * 2; }

                if (DemSpells.E.IsLearned && DemSpells.E.IsReady()) { TotalDamage += EDamage(target); }

                if (DemSpells.R.IsLearned && DemSpells.R.IsReady()) { TotalDamage += (RDamage(target) * slider(combo, "CRMAX")); }

                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, cutl.Id); }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, blade.Id); }

                return TotalDamage;
            }
            return 0;
        }

        private static float PassiveDamage(AIHeroClient target)
        {
            if (!myhero.HasBuff("KogMawIcathianSurprise")) return 0;

            return myhero.CalculateDamageOnUnit(target, DamageType.True, (100 + (25 * myhero.Level)));
        }

        private static float QDamage(AIHeroClient target)
        {
            int index = DemSpells.Q.Level - 1;

            var QDamage = new[] { 80, 130, 180, 230, 280 }[index] + (0.5f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, QDamage);
        }

        private static float EDamage(AIHeroClient target)
        {
            int index = DemSpells.E.Level - 1;

            var EDamage = new[] { 60, 110, 160, 210, 260 }[index] + (0.7f * myhero.FlatMagicDamageMod);

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, EDamage);

        }

        private static float RDamage(Obj_AI_Base target)
        {
            int index = DemSpells.R.Level - 1;

            float RDamage = 0;

            if (target.HealthPercent >= 50)
            {
                RDamage = new[] { 70, 110, 150 }[index] +
                          (0.65f * myhero.TotalAttackDamage) +
                          (0.25f * myhero.FlatMagicDamageMod);
            }
            else if (target.HealthPercent <= 49 && target.HealthPercent >= 25)
            {
                RDamage = new[] { 140, 220, 300 }[index] +
                          (1.3f * myhero.TotalAttackDamage) +
                          (0.5f * myhero.FlatMagicDamageMod);
            }
            else if (target.HealthPercent <= 24)
            {
                RDamage = new[] { 210, 330, 450 }[index] +
                          (1.95f * myhero.TotalAttackDamage) +
                          (0.75f * myhero.FlatMagicDamageMod);
            }

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, RDamage);
        }

        private static float ReachTime(AIHeroClient target)
        {
            float reachtime = ((target.Distance(myhero.Position) - 50) / myhero.MoveSpeed) * 1000;
            return reachtime;
        }

        private static void ItemManager(AIHeroClient target)
        {
            if (target != null && target.IsValidTarget() && check(combo, "ITEMS"))
            {
                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position))
                {
                    cutl.Cast(target);
                }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position))
                {
                    blade.Cast(target);
                }
            }
        }

        private static void PassiveLogic()
        {
            float EndTime = 0;

            if (myhero.HasBuff("KogMawIcathianSurprise"))
            {
                EndTime = Math.Max(0, myhero.Buffs.Where(x => x.Name.Contains("IcathianSurprise") && x.IsValid()).FirstOrDefault().EndTime - Game.Time) * 1000;
            }
            else return;

            var CloseEnemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValid && x.Distance(myhero.Position) < 1500 && (EndTime - ReachTime(x)) > 0.1f).ToList();
            if (CloseEnemies.Count() == 0) return;

            if ((EndTime - ReachTime(CloseEnemies.OrderBy(x => x.Health).FirstOrDefault()) > 0.1f))
            {
                Orbwalker.MoveTo(CloseEnemies.OrderBy(x => x.Health).FirstOrDefault().Position);
            }
            else { Orbwalker.MoveTo(CloseEnemies.OrderBy(x => x.Distance(myhero.Position)).FirstOrDefault().Position); }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1900, DamageType.Physical, myhero.Position);

            if (target != null && !target.IsInvulnerable)
            {          
                var qpred = DemSpells.Q.GetPrediction(target);
                var epred = DemSpells.E.GetPrediction(target);
                var rpred = DemSpells.R.GetPrediction(target);
                
                ItemManager(target);

                if (check(combo, "CQ") && DemSpells.Q.CanCast(target) &&
                    qpred.HitChancePercent >= slider(pred, "QPred") && !qpred.Collision)
                {
                    DemSpells.Q.Cast(qpred.CastPosition);
                }

                if (check(combo, "CW") && target.Distance(myhero.Position) < WRange)
                {
                    DemSpells.W.Cast();
                }

                if (check(combo, "CE") && DemSpells.E.CanCast(target) &&
                    epred.HitChancePercent >= slider(pred, "EPred"))
                {
                    DemSpells.E.Cast(epred.CastPosition);
                }

                if (check(combo, "CR") && DemSpells.R.CanCast(target) && rpred.HitChancePercent >= slider(pred, "RPred") &&
                     myhero.GetBuffCount("kogmawlivingartillerycost") <= slider(combo, "CRMAX"))
                {
                    switch(comb(combo, "CRMIN"))
                    {
                        case 0:
                            if (target.HealthPercent <= 100.00f) DemSpells.R.Cast(rpred.CastPosition);
                            break;
                        case 1:
                            if (target.HealthPercent <= 49.99f) DemSpells.R.Cast(rpred.CastPosition);
                            break;                          
                        case 2:
                            if (target.HealthPercent <= 24.99f) DemSpells.R.Cast(rpred.CastPosition);
                            break;
                    }
                }
                
                if (check(combo, "Cignt") && ignt.CanCast(target))
                {
                    if (target.Health > ComboDamage(target) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health &&
                        !check(misc, "autoign"))
                    {
                        ignt.Cast(target);
                    }
                    else if (target.Health > ComboDamage(target) &&
                            (ComboDamage(target) + (myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) - 5)) > target.Health)
                    {
                        ignt.Cast(target); 
                    }
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                var qpred = DemSpells.Q.GetPrediction(target);
                var epred = DemSpells.E.GetPrediction(target);

                if (check(harass, "HQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && target.IsValidTarget(DemSpells.Q.Range - 10) &&
                    qpred.HitChancePercent >= slider(pred, "QPred"))
                {
                    DemSpells.Q.Cast(qpred.CastPosition);
                }

                if (check(harass, "HW") && DemSpells.W.IsLearned && DemSpells.W.IsReady() &&
                    myhero.CountEnemiesInRange(WRange) >= slider(harass, "HWMIN"))
                {
                    DemSpells.W.Cast();
                }

                if (check(harass, "HE") && DemSpells.E.IsLearned && DemSpells.E.IsReady() &&
                    epred.CollisionObjects.Where(x => !x.IsDead && !x.IsAlly && x is AIHeroClient).Count() >= slider(harass, "HEMIN"))
                {
                    DemSpells.E.Cast(epred.CastPosition);
                }
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, WRange).ToArray();

            if (minions != null)
            {
                var epred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, DemSpells.E.Width, 1200);
                var rpred = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, 120, 1750);
   

                if (check(laneclear, "LQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
                {
                    foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && myhero.GetSpellDamage(x, SpellSlot.Q) > x.Health + 10 &&
                                                         Prediction.Health.GetPrediction(x, (int)((x.Distance(myhero.Position) / DemSpells.Q.Speed) * 1000)) > 30))
                    {
                        var qpred = DemSpells.Q.GetPrediction(minion);

                        if (!qpred.Collision) DemSpells.Q.Cast(qpred.CastPosition);
                    }
                }

                if (check(laneclear, "LW") && DemSpells.W.IsLearned && DemSpells.W.IsReady())
                {
                    int count = minions.Where(x => !x.IsDead && x.IsValidTarget(myhero.AttackRange) && x.Health > 20).Count();

                    if (count >= slider(laneclear, "LWMIN")) DemSpells.W.Cast();
                }

                if (check(laneclear, "LE") && DemSpells.E.IsLearned && DemSpells.E.IsReady() && epred.HitNumber >= slider(laneclear, "LEMIN"))
                {
                    DemSpells.E.Cast(epred.CastPosition);
                }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1800f);

            if (check(jungleclear, "JQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
            {
                switch (comb(jungleclear, "JQMODE"))
                {
                    case 0:
                        foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 30))
                        {
                            DemSpells.Q.Cast(monster.Position);
                        }
                        break;
                    case 1:
                        foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.Q.Range) && x.Health > 30 &&
                                                                    !x.Name.ToLower().Contains("mini")))
                        {
                            var pred = DemSpells.Q.GetPrediction(monster);
                            if (!pred.Collision) DemSpells.Q.Cast(pred.CastPosition);
                        }
                        break;
                }
            }

            if (check(jungleclear, "JW") && DemSpells.W.IsLearned && DemSpells.W.IsReady())
            {
                int count = Monsters.Where(x => x.Distance(myhero.Position) < WRange)
                                    .Count();

                if (count >= slider(jungleclear, "JWMIN")) DemSpells.W.Cast();
            }

            if (check(jungleclear, "JE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
            {
                switch (comb(jungleclear, "JEMODE"))
                {
                    case 0:
                        foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range) && x.Health > 30))
                        {
                            DemSpells.E.Cast(monster.Position);
                        }
                        break;
                    case 1:
                        foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(DemSpells.E.Range) && x.Health > 30 &&
                                                                    !x.Name.ToLower().Contains("mini")))
                        {
                            var pred = DemSpells.E.GetPrediction(monster);
                            if (pred.HitChancePercent >= 80) DemSpells.E.Cast(pred.CastPosition);
                        }
                        break;
                }
            }

            if (check(jungleclear, "JR") && DemSpells.R.IsLearned && DemSpells.R.IsReady())
            {
                if (myhero.HasBuff("kogmawlivingartillerycost") &&
                    myhero.GetBuffCount("kogmawlivingartillerycost") == 3) return;

                foreach (var monster in EntityManager.MinionsAndMonsters.GetJungleMonsters().Where(x => !x.Name.ToLower().Contains("mini") && !x.IsDead &&
                                                                                            x.Health > 50 && x.IsValidTarget(DemSpells.R.Range) && RDamage(x) > x.Health))
                {
                    var pred = DemSpells.R.GetPrediction(monster);
                    if (pred.HitChancePercent >= 80) DemSpells.R.Cast(monster);
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") || !myhero.HasBuff("ItemMiniRegenPotion")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();
            }

            if (target != null && !target.IsZombie && !target.IsInvulnerable && !target.HasUndyingBuff())
            {
                var qpred = DemSpells.Q.GetPrediction(target);
                var rpred = DemSpells.R.GetPrediction(target);

                if (check(misc, "ksQ") && DemSpells.Q.CanCast(target) && !qpred.Collision &&
                    QDamage(target) > target.Health && slider(pred, "QPred") >= qpred.HitChancePercent)
                {
                    DemSpells.Q.Cast(qpred.CastPosition);
                }

                if (check(misc, "ksR") && DemSpells.R.CanCast(target) &&
                    RDamage(target) > target.Health)
                {
                    if (rpred.HitChancePercent >= slider(pred, "RPred") ||
                        rpred.HitChance == HitChance.Immobile ||
                        target.HasBuffOfType(BuffType.Stun))
                    {
                        DemSpells.R.Cast(rpred.CastPosition);
                    }
                }             
                if (check(misc, "autoign") && ignt.IsReady() && target.IsValidTarget(ignt.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
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
                { Circle.Draw(DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.LimeGreen, DemSpells.Q.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.LimeGreen, DemSpells.Q.Range, myhero.Position); }

            }

            if (check(draw, "drawW") && DemSpells.W.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.W.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.LimeGreen, WRange, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.LimeGreen, WRange, myhero.Position); }

            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.LimeGreen, DemSpells.E.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.LimeGreen, DemSpells.E.Range, myhero.Position); }

            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.LimeGreen, new[] { 0, 1200, 1500, 1800 }[DemSpells.R.Level], myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.LimeGreen, new[] { 0, 1200, 1500, 1800 }[DemSpells.R.Level], myhero.Position); }

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

        public static void OnGapcloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (sender != null && !sender.IsMe && sender.IsEnemy && check(misc, "EGAP") && DemSpells.E.IsReady() &&
                sender.IsValidTarget(DemSpells.E.Range - 30))
            {
                var epred = DemSpells.E.GetPrediction(sender);
                DemSpells.E.Cast(epred.CastPosition);
            }
        }

        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 KogMaw", "kogmaw");
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            jungleclear = menu.AddSubMenu("Jungleclear", "jclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 KogMaw And Thank You For Using!");
            menu.AddLabel("Version 1.0 30/6/2016");
            menu.AddLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
            combo.Add("CW", new CheckBox("Use W", true));
            combo.Add("CE", new CheckBox("Use E", true));
            combo.AddSeparator();
            combo.Add("CR", new CheckBox("Use R", true));
            combo.Add("CRMIN", new ComboBox("Min Enemy Health To Cast R", 1, "100%", "50%", "25%"));
            combo.Add("CRMAX", new Slider("Max R Stacks", 5, 1, 10));
            combo.AddSeparator();
            combo.Add("Cignt", new CheckBox("Use Ignite", false));
            combo.Add("ITEMS", new CheckBox("Use Items", true));

            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", true));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use W", true));
            harass.Add("HWMIN", new Slider("Min Enemies In Range To Cast E", 1, 1, 5));
            harass.AddSeparator();
            harass.Add("HE", new CheckBox("Use E", true));
            harass.Add("HEMIN", new Slider("Min Enemies To Hit With E", 2, 1, 5));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", false));
            laneclear.AddSeparator();
            laneclear.Add("LW", new CheckBox("Use W", true));
            laneclear.Add("LWMIN", new Slider("Min Minions Nearby To Cast E", 3, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", false));
            laneclear.Add("LEMIN", new Slider("Min Minions To Hit With E", 2, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));
            jungleclear.Add("JQMODE", new ComboBox("Q Mode", 1, "All Monsters", "Big Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W", true));
            jungleclear.Add("JWMIN", new Slider("Min Monsters To Cast W", 1, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JE", new CheckBox("Use E", false));
            jungleclear.Add("JEMODE", new ComboBox("E Mode", 1, "All Monsters", "Big Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JR", new CheckBox("Use R To Finish Big Monsters", false));
            jungleclear.Add("JRSTACKS", new Slider("Max R Stacks", 3, 1, 10));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 10, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawW", new CheckBox("Draw W Extented Attack Range", true));
            draw.Add("drawE", new CheckBox("Draw E Range", true));
            draw.Add("drawR", new CheckBox("Draw R Range", true));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies", true));

            misc.AddLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            misc.Add("ksR", new CheckBox("Killsteal with R", true));
            misc.AddSeparator();
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));
            misc.Add("AUTOPASSIVE", new CheckBox("Auto Control Passive", true));
            misc.Add("EGAP", new CheckBox("Use E On Gapcloser", false));
            misc.AddSeparator();
            misc.Add("AUTOPOT", new CheckBox("Auto Potion", true));
            misc.Add("POTMIN", new Slider("Min Health % To Activate Pot", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack", true));
            misc.Add("skinID", new ComboBox("Skin Hack", 7, "Default", "Caterpillar", "Sonoran", "Monarch", "Reindeer", "Lion Dance", "Deep Sea", "Jurassic", "Battlecast"));

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("Q :");
            pred.Add("QPred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("E :");
            pred.Add("EPred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("R :");
            pred.Add("RPred", new Slider("Select % Hitchance", 90, 1, 100));
        }
    }
    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Skillshot R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1175, SkillShotType.Linear, 250, 1650, 70);
            W = new Spell.Active(SpellSlot.W, (uint)Program.WRange);
            E = new Spell.Skillshot(SpellSlot.E, 1280, SkillShotType.Linear, 500, 1200, 120);
            R = new Spell.Skillshot(SpellSlot.R, (uint)Program.RRange , SkillShotType.Circular, 1200, int.MaxValue, 240);
        }
    }
}
