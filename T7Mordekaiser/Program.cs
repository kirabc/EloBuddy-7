using System;
using System.Globalization;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7_Morde
{
    class Program
    {
        private static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear,jungleclear, misc, draw, pred, sequence1, sequence2, sequence3;
        private static float RAttackDelay = 1200;       

        private static Spell.Targeted Ignite { get; set; }

        static readonly string ChampionName = "Mordekaiser";
        public const string Version = "1.3";
        static readonly string Date = "7/9/16";

        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }

        public static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Mordekaiser") return; 

            Chat.Print("<font color='#0040FF'>T7</font><font color='#1F1F1F'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#7752FF'>By </font><font color='#0FA348'>Toyota</font><font color='#7752FF'>7</font><font color='#FF0000'> <3 </font>");

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGapcloser;
            Game.OnTick += OnTick;

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

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo) && (myhero.Health - TotalHealthLoss()) > 100) Combo();
            
            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.HealthPercent > harass["hminhealth"].Cast<Slider>().CurrentValue) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.HealthPercent > laneclear["lminhealth"].Cast<Slider>().CurrentValue) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.HealthPercent > jungleclear["jminhealth"].Cast<Slider>().CurrentValue) Jungleclear();
           
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
                            Player.LevelSpell(SpellSlot.E);
                            break;
                        case 3:
                            Player.LevelSpell(SpellSlot.W);
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

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float totaldamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady() ) { totaldamage += TotalQDamage(target); }
                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { totaldamage += TotalWDamage(target); }
                if (DemSpells.E.IsLearned && DemSpells.E.IsReady() ) { totaldamage += EDamage(target); }
                if (DemSpells.R.IsLearned && DemSpells.R.IsReady() ) { totaldamage += TotalRDamage(target); }

                return totaldamage;
            }
            return 0;
        }

        private static float TotalHealthLoss()
        {
            var HealthLoss = new[] { 0, 20, 23, 26, 29, 32 }[DemSpells.Q.Level] + 
                             new[] { 0, 25, 35, 45, 55, 65 }[DemSpells.W.Level] + 
                             new[] { 0, 24, 36, 48, 60, 72 }[DemSpells.E.Level];
            return HealthLoss;
        }

        private static Obj_AI_Base Ghost
        {
            get
            {
                if (DemSpells.R.Name.ToLower() != "mordekaisercotgguide") return null;

                return ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(m => m.Distance(myhero.Position) < 10000 && m.IsAlly && m.HasBuff("mordekaisercotgpetbuff2"));
            }                       
        }

        private static float TotalQDamage(AIHeroClient target)
        {   
            var qdamage = (new[] { 0, 10, 20, 30, 40, 50 }[DemSpells.Q.Level] +
                          (new[] { 0, 0.5, 0.6, 0.7, 0.8, 0.9 }[DemSpells.Q.Level] * myhero.TotalAttackDamage) +
                          (0.6 * myhero.FlatMagicDamageMod)) +
                          (new[] { 0, 20, 40, 60, 80, 100 }[DemSpells.Q.Level] +                                //T7's Maths OP Kappa pepo :P
                          (new[] { 0, 1, 1.2, 1.4, 1.6, 1.8 }[DemSpells.Q.Level] * myhero.TotalAttackDamage) +
                          (1.2 * myhero.FlatMagicDamageMod));
            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)qdamage);
        }

        private static float TotalWDamage(AIHeroClient target, bool SecondStage = false)
        {
            var TotalWDamage = (new[] { 0, 140, 180, 220, 260, 300 }[DemSpells.W.Level] + (0.9 * myhero.FlatMagicDamageMod)) + 
                               (new[] { 0, 50, 85, 120, 155, 190 }[DemSpells.W.Level] + (0.3 * myhero.FlatMagicDamageMod));

            var SecondStageDamage = new[] { 0, 50, 85, 120, 155, 190 }[DemSpells.W.Level] + (0.3 * myhero.FlatMagicDamageMod);

            if (!SecondStage) return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)TotalWDamage); 

            else return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)SecondStageDamage);
        }

        private static float EDamage(AIHeroClient target)
        {
            var edamage = new[] { 0, 35, 65, 95, 125, 155 }[DemSpells.Q.Level] + 
                          (0.6 * myhero.TotalAttackDamage) + 
                          (0.6 * myhero.FlatMagicDamageMod);
            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)edamage);
        }

        private static float TotalRDamage(AIHeroClient target)
        {
            var TotalRDamage = (new[] { 0, 0.25, 0.3, 0.35 }[DemSpells.R.Level] + 
                               (0.04 * (myhero.FlatMagicDamageMod / 100))) * 
                               target.MaxHealth;
            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)TotalRDamage);
        }

        private static bool CastW()
        {
            var allies = EntityManager.Heroes.Allies.Where(x => x.Distance(myhero.Position) < 999);

            if (DemSpells.W.IsReady() && check(combo, "CW") && DemSpells.W.Name.ToLower() != "mordekaisercreepingdeath2")
            {
                foreach (var ally in allies.Where(a => !a.IsMe && !a.IsDead && a.CountEnemiesInRange(350) > 0))
                {
                    if (DemSpells.W.Cast(ally)) return true;
                }

                if (myhero.CountEnemiesInRange(400) > 0 && DemSpells.W.Cast(myhero)) return true; 
            }

            return false;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (myhero.IsDead) return;

            if (check(combo, "CGhost"))
            {
                var target = TargetSelector.GetTarget(4500, DamageType.Physical, Player.Instance.Position);

                if (target != null && target.ValidTarget(4500) && Ghost != null)
                {
                    if (!(Environment.TickCount >= RAttackDelay))
                        return;

                    if (check(combo, "GHOSTCOMBO") && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                        return;

                    if (comb(combo, "GHOSTMODE") == 0)
                    {
                        target = TargetSelector.GetTarget(1000, DamageType.Physical, Player.Instance.Position);
                        DemSpells.R.Cast(target);
                    }
                    else
                    {
                        target = TargetSelector.GetTarget(4500, DamageType.Physical, Player.Instance.Position);
                        DemSpells.R.Cast(target);
                    }
                       
                    RAttackDelay = Environment.TickCount + Ghost.AttackDelay * 1000;                 
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget(1000))
            {
                if (check(combo, "CQ") && DemSpells.Q.CanCast(target) && DemSpells.Q.Cast()) return;

                CastW();

                if (check(combo, "CE") && DemSpells.W.CanCast(target))
                {
                    if (comb(combo, "EMode") == 0)
                    {
                        DemSpells.E.CastMinimumHitchance(target, slider(misc, "EPred"));
                    }
                    else DemSpells.E.Cast(target.Position);
                }

                if (check(combo, "CR") && DemSpells.R.CanCast(target) && TotalRDamage(target) > (target.Health + (target.FlatHPRegenMod * 10)))
                {
                    if (DemSpells.E.IsReady() && EDamage(target) > target.Health) return;
                                      
                    DemSpells.R.Cast(target); 
                }

                if (check(combo, "Cignt") && Ignite.CanCast(target) && target.Health > ComboDamage(target) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health && (TotalRDamage(target) < target.Health &&
                    !target.HasBuff("mordekaisercotgpetbuff2")))
                {
                    if (target.Distance(myhero.Position) < (DemSpells.E.Range / 2))
                        return;
                    
                    else Ignite.Cast(target);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget((int)DemSpells.E.Range))
            {
                if (check(harass, "HQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && target.IsValidTarget(myhero.AttackRange) && DemSpells.Q.Cast())
                    return;

                if (check(harass, "HE") && DemSpells.E.IsLearned && DemSpells.E.IsReady() && target.IsValidTarget(DemSpells.E.Range) &&
                    DemSpells.E.CastIfItWillHit(slider(harass, "HEMIN"), slider(misc, "EPred")))
                    return;
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range);

            if (minions != null)
            {
                if (check(laneclear, "LQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() &&
                    minions.Where(x => x.Distance(myhero) < myhero.AttackRange).Count() >= slider(laneclear, "lminmin1") && DemSpells.Q.Cast())
                    return;

                if (check(laneclear, "LW") && DemSpells.W.IsReady() && DemSpells.W.Name.ToLower() != "mordekaisercreepingdeath2" &&
                    EntityManager.MinionsAndMonsters.AlliedMinions.Any(x => x.Distance(myhero.Position) < 400) && 
                    DemSpells.W.Cast(EntityManager.MinionsAndMonsters.AlliedMinions.Where(x => x.Distance(myhero.Position) < 400)
                                                                                   .OrderByDescending(x => x.CountEnemiesInRange(175))
                                                                                   .FirstOrDefault()))
                    return;

                if (check(laneclear, "LE") && DemSpells.E.IsReady())
                {
                    foreach (var minion in minions.Where(x => DemSpells.E.CanCast(x)))
                    {
                        var pred = DemSpells.E.GetPrediction(minion);

                        if (pred.GetCollisionObjects<Obj_AI_Minion>().Count() >= slider(laneclear, "lminmin2") && DemSpells.E.Cast(pred.CastPosition)) return;
                    }
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget((int)DemSpells.W.Range))
            {
                if (check(misc, "ksE") && DemSpells.E.CanCast(target) && EDamage(target) > target.Health && DemSpells.E.Cast(target.Position))
                    return;

                if (check(misc, "ksW") && DemSpells.W.IsReady() && DemSpells.W.Name.ToLower() == "mordekaisercreepingdeath2" &&
                    TotalWDamage(target, true) > target.Health && DemSpells.W.Cast())
                    return;

                if (check(misc, "autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health && Ignite.Cast(target))
                    return;
            }

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            if (check(misc, "skinhack")) myhero.SetSkinId((int)comb(misc, "skinID"));
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1000f);

            if (DemSpells.Q.IsReady() && check(jungleclear, "JQ") && Monsters.Count(x => DemSpells.Q.IsInRange(x.Position)) >= slider(jungleclear, "JQMIN") && DemSpells.Q.Cast())
                return;

            foreach (var monster in Monsters.Where(x => x.IsValidTarget(DemSpells.E.Range)))
            {
                if (DemSpells.W.IsReady() && check(jungleclear, "JW") && DemSpells.W.Cast(myhero)) return;

                if (DemSpells.E.IsReady() && check(jungleclear, "JE") && DemSpells.E.Cast(monster)) return; 
            }
        }

        public static void OnGapcloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (sender != null && sender.IsEnemy && check(misc, "EGAP") && DemSpells.E.CanCast(sender) && !sender.HasBuffOfType(BuffType.SpellImmunity) && !sender.IsInvulnerable)
            {
                var epred = DemSpells.E.GetPrediction(sender);
                DemSpells.E.Cast(epred.CastPosition);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawW") && DemSpells.W.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Gray) : SharpDX.Color.Gray,
                    DemSpells.W.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Gray) : SharpDX.Color.Gray,
                    DemSpells.E.Range,
                    myhero.Position
                );
            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0)
            {
                Circle.Draw
                (
                    check(draw, "nodrawc") ? (DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Gray) : SharpDX.Color.Gray,
                    DemSpells.R.Range,
                    myhero.Position
                );
            }

            if (check(draw, "DRAWGHOSTAA") && Ghost != null && !myhero.IsDead && !check(draw, "nodraw"))
            {
                Circle.Draw(SharpDX.Color.SkyBlue, Ghost.AttackRange, Ghost.Position); 
            }

            if (check(draw, "DRAWGHOSTTIME") && Ghost != null && !myhero.IsDead && !check(draw, "nodraw"))
            {
                foreach( var buff in Ghost.Buffs.Where(x => x.IsValid() && x.Name.Contains("mordekaisercotgpetbuff2")))
                {
                    var endTime = Math.Max(0, buff.EndTime - Game.Time);
                    Drawing.DrawText(Drawing.WorldToScreen(Ghost.Position).X,                   
                                         Drawing.WorldToScreen(Ghost.Position).Y - 30,
                                         Color.Green, "Time: " + Convert.ToString(endTime, CultureInfo.InvariantCulture));
                }
            }
            
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead))
            {
                if (check(draw, "drawkillable") && enemy.VisibleOnScreen && ComboDamage(enemy) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X,
                                     Drawing.WorldToScreen(enemy.Position).Y - 30,
                                     Color.Green, "Killable With Combo");
                }
                else if (check(draw, "drawkillable") && enemy.VisibleOnScreen &&
                         ComboDamage(enemy) + myhero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, Color.Green, "Combo + Ignite");
                }
            }
        }

        private static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 Morde", "mordy");
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            jungleclear = menu.AddSubMenu("Jungleclear", "jclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");

            menu.AddGroupLabel("Welcome to T7 Mordekaiser And Thank You For Using!");
            menu.AddGroupLabel("Version " + Version + " " + Date);
            menu.AddGroupLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddGroupLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddGroupLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q in Combo", true));
            combo.Add("CW", new CheckBox("Use W in Combo", true));
            combo.Add("CE", new CheckBox("Use E in Combo", true));
            combo.Add("CR", new CheckBox("Use R in Combo", true));
            combo.Add("Cignt", new CheckBox("Use Ignite", true));
            combo.AddSeparator();
            combo.AddGroupLabel("E Mode");
            combo.Add("EMode", new ComboBox("Select Mode", 1, "With Prediction", "Without Prediciton"));
            combo.AddSeparator();
            combo.AddLabel("Ghost Settings");
            combo.Add("CGhost", new CheckBox("Auto Control Ghost", true));
            combo.Add("GHOSTMODE", new ComboBox("Select Ghost Mode     =>",0,"Fight My Target","Go Attack Enemies"));
            combo.Add("GHOSTCOMBO", new CheckBox("Only Control Drag While In Combo Mode", true));
       //     combo.Add("GHOSTMIN", new Slider("Dont Harass If More Than X Enemies:",3,1,5));

            harass.AddGroupLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", true));
            harass.AddSeparator(10);
            harass.Add("HE", new CheckBox("Use E", false));
            harass.Add("HEMIN", new Slider("Min Enemies For E", 1, 1, 5));
            harass.AddSeparator();
            harass.AddLabel("Min Mana To Harass");
            harass.Add("hminhealth", new Slider("Stop Harass At % Health", 65, 1, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", true));
            laneclear.Add("LW", new CheckBox("Use W", false));
            laneclear.Add("LE", new CheckBox("Use E", false));
            laneclear.AddSeparator();
            laneclear.AddLabel("Spell Options");
            laneclear.Add("lminmin1", new Slider("Min Minions To Cast Q", 2, 1, 6));
            laneclear.Add("lminmin2", new Slider("Min Minions To Cast E", 1, 1, 6));
            laneclear.AddSeparator();
            laneclear.AddLabel("Stop Laneclear At % Health");
            laneclear.Add("lminhealth", new Slider("%", 65, 1, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", true));
            jungleclear.Add("JQMIN", new Slider("Min Monsters For Q", 1, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("JW", new CheckBox("Use W", true));
            jungleclear.Add("JE", new CheckBox("Use E", true));
            jungleclear.Add("jminhealth", new Slider("Stop Jungleclear At % Health",50,1,100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawW", new CheckBox("Draw W Range", true));
            draw.Add("drawE", new CheckBox("Draw E Range", true));
            draw.Add("drawR", new CheckBox("Draw R Range", true));
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies", false));
            draw.Add("nodrawc", new CheckBox("Draw Only Ready Spells", false));
            draw.AddSeparator();
            draw.Add("DRAWGHOSTAA", new CheckBox("Draw Ghost's AA Range", true));
            draw.Add("DRAWGHOSTTIME", new CheckBox("Draw Ghost's Remaining Time", true));

            misc.AddLabel("Killsteal");
            misc.Add("ksW", new CheckBox("Killsteal with W", false));
            misc.Add("ksE", new CheckBox("Killsteal with E", true));
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", false));
            misc.Add("EGAP", new CheckBox("Use E On Gapcloser", false));
            misc.AddSeparator();
            misc.AddLabel("Prediction");
            misc.AddLabel("E :");
            misc.Add("EPred", new Slider("Select % Hitchance", 80,0,100));
            misc.AddSeparator();
            misc.Add("AUTOPOT", new CheckBox("Auto Potion", true));
            misc.Add("POTMIN", new Slider("Min Health % To Activate Pot", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.Add("LEVELMODE", new ComboBox("Select Sequence                    =>", 0, "Q>W>E", "Q>E>W"));
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhack", new CheckBox("Activate Skin hack"));
            misc.Add("skinID", new ComboBox("Skin Hack", 4, "Default", "Dragon Knight", "Infernal", "Pentakill", "Lord", "King Of Clubs"));

        }
    }

    public static class DemSpells
    {
        public static Spell.Active Q { get; private set; }
        public static Spell.Targeted W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Active(SpellSlot.Q, (uint)Player.Instance.GetAutoAttackRange());
            W = new Spell.Targeted(SpellSlot.W , 1000);
            E = new Spell.Skillshot(SpellSlot.E, 675, SkillShotType.Cone, 250, 2000, 12 * 2 * (int)Math.PI / 180); 
            R = new Spell.Targeted(SpellSlot.R, 650);
        }
    }

    public static class Extensions
    {
        public static bool ValidTarget(this AIHeroClient hero, int range)
        {
            return !hero.HasBuff("UndyingRage") && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("ChronoShift") && !hero.HasBuff("kindredrnodeathbuff") &&
                   !hero.IsInvulnerable && !hero.IsDead && hero.IsValidTarget(range) && !hero.IsZombie &&
                   !hero.HasBuffOfType(BuffType.Invulnerability) && !hero.HasBuffOfType(BuffType.SpellImmunity) && !hero.HasBuffOfType(BuffType.SpellShield);

        }
    }
}
