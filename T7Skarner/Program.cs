using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace T7_Skarner
{
    class Program
    {

        #region Declarations
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;
        private static bool Pinged = false;

        private static Spell.Targeted Ignite { get; set; }
        private static Spell.Targeted Smite { get; set; }

        private static readonly string ChampionName = "Skarner";
        private static readonly string Version = "1.0";
        private static readonly string Date = "29/8/16";

        private static List<Obj_AI_Minion> Crystals = new List<Obj_AI_Minion>();

        public static Item tiamat { get; private set; }
        public static Item rhydra { get; private set; }
        public static Item thydra { get; private set; }
        public static Item yomus { get; private set; }
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }
        #endregion

        #region Events
        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }

            Chat.Print("<font color='#0040FF'>T7</font><font color='#9707F7'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#3737E6'>Toyota</font><font color='#D61EBE'>7</font><font color='#FF0000'> <3 </font>");

            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Game.OnTick += OnTick;

            tiamat = new Item(3077, 400f);
            rhydra = new Item(3074, 400f);
            thydra = new Item(3748);
            yomus = new Item(3142);
            Potion = new Item((int)ItemId.Health_Potion);
            Biscuit = new Item((int)ItemId.Total_Biscuit_of_Rejuvenation);
            RPotion = new Item((int)ItemId.Refillable_Potion);

            Player.LevelSpell(SpellSlot.Q);

            if (EloBuddy.SDK.Spells.SummonerSpells.PlayerHas(EloBuddy.SDK.Spells.SummonerSpellsEnum.Ignite))
            {
                Ignite = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 600);
            }
            if (EloBuddy.SDK.Spells.SummonerSpells.PlayerHas(EloBuddy.SDK.Spells.SummonerSpellsEnum.Smite))
            {
                Smite = new Spell.Targeted(myhero.GetSpellSlotFromName("summonersmite"), 500);
            }

            ObjectManager.Get<Obj_AI_Minion>().Where(x => x.BaseSkinName == "SkarnerPassiveCrystal").ToList().ForEach(x => Crystals.Add(x));

            DatMenu();
            CheckPrediction();
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            Orbwalker.ActiveModes flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();

            if ((flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > slider(harass, "HMIN")) ||
                (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > slider(laneclear, "LMIN")) ||
                (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) && myhero.ManaPercent > slider(jungleclear, "JMIN")))
            {
                AIO_Logic();
            }

            if (check(misc, "WFLEE") && DemSpells.W.IsReady() && flags.HasFlag(Orbwalker.ActiveModes.Flee)) DemSpells.W.Cast();

            Misc();
            
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            SpellSlot U = SpellSlot.Unknown;
            SpellSlot Q = SpellSlot.Q;
            SpellSlot W = SpellSlot.W;
            SpellSlot E = SpellSlot.E;
            SpellSlot R = SpellSlot.R;

            /*>>*/
            SpellSlot[] sequence1 = new SpellSlot[] { U, W, E, Q, Q, R, Q, W, Q, W, R, W, W, E, E, R, E, E, U };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0 || DemSpells.R.Level > 0)
            {
                Circle.Draw(check(draw, "drawonlyrdy") ? (DemSpells.Q.IsOnCooldown ? Color.Transparent : Color.Fuchsia) : Color.Fuchsia, DemSpells.Q.Range, myhero.Position);
            }

            if (check(draw, "drawE") && DemSpells.E.Level > 0)
            {
                Circle.Draw(check(draw, "drawonlyrdy") ? (DemSpells.Q.IsOnCooldown ? Color.Transparent : Color.Fuchsia) : Color.Fuchsia, DemSpells.E.Range, myhero.Position);
            }

            if (check(draw, "DRAWSHIELD") && DemSpells.W.IsReady())
            {
                Drawing.DrawText(
                    Drawing.WorldToScreen(myhero.Position).X - 50f,
                    Drawing.WorldToScreen(myhero.Position).Y + 50f,
                    System.Drawing.Color.White,
                    "Auto Shield: ");

                Drawing.DrawText(
                    Drawing.WorldToScreen(myhero.Position).X + 28f,
                    Drawing.WorldToScreen(myhero.Position).Y + 50f,
                    key(misc, "SHIELDKEY") ? System.Drawing.Color.Green : System.Drawing.Color.Red,
                    key(misc, "SHIELDKEY") ? "ON" : "OFF");
            }
        }
        #endregion

        #region Modes
        private static void Combo()
        {
            AIHeroClient target = TargetSelector.GetTarget(1200, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget(1100))
            {
             /*   if (target.HasEBuff() && target.Distance(myhero.Position) < myhero.GetAutoAttackRange() && Orbwalker.IsAutoAttacking)
                {
                    return;
                }*/

                if (check(combo, "CE") && DemSpells.E.CanCast(target))
                {
                    Prediction.Manager.PredictionOutput epred = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        Delay = DemSpells.E.CastDelay,
                        Radius = DemSpells.E.Radius,
                        Range = DemSpells.E.Range,
                        Speed = DemSpells.E.Speed,
                        Type = SkillShotType.Linear,
                        CollisionTypes = { EloBuddy.SDK.Spells.CollisionType.YasuoWall },
                        From = myhero.Position,
                        Target = target
                    });

                    if (epred.RealHitChancePercent >= slider(pred, "EPred") && DemSpells.E.Cast(epred.CastPosition))
                    {
                        return;
                    }
                }

                ItemManager(target);
            }

            if (check(combo, "CQ") && DemSpells.Q.IsReady())
            {
                CastQ(target);
            }
        }

        private static void AIO_Logic()
        {
            IEnumerable<Obj_AI_Base> Targets = Enumerable.Empty<Obj_AI_Base>();

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Harass:
                    Targets = EntityManager.Heroes.Enemies.Where(x => x.ValidTarget(1100)).AsEnumerable<AIHeroClient>();
                    break;
                case Orbwalker.ActiveModes.JungleClear:
                    Targets = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1100f).AsEnumerable<Obj_AI_Minion>();
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    Targets = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, 1100f).AsEnumerable<Obj_AI_Minion>();
                    break;
            }

            if (Targets != null)
            {
                Menu MenuCheck = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) ? harass :
                                (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ? laneclear : jungleclear);

                string QCheck = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) ? "HQMIN" : (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ? "LQMIN" : "JQMIN");
              //  string ECheck = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) ? "HEMIN" : (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ? "LEMIN" : "JEMIN");

                bool Cast = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) ? CastE((AIHeroClient)Targets.OrderBy(x => x.Distance(myhero.Position)).FirstOrDefault()) :
                           (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ? DemSpells.E.Cast(Targets.FirstOrDefault().Position) : CastE());

                if (check(MenuCheck, "E") && DemSpells.E.IsReady() && Cast)
                {
                    return;
                }

                if (check(MenuCheck, "Q") && DemSpells.Q.IsReady() && Targets.Where(x => DemSpells.Q.IsInRange(x)).Count() >= slider(MenuCheck, QCheck) && DemSpells.Q.Cast())
                {
                    return;
                }

            }
        }

        private static void Misc()
        {
            AIHeroClient target = TargetSelector.GetTarget(1100, DamageType.Magical, Player.Instance.Position);

            if (target != null && target.ValidTarget(1100))
            {
                if (check(misc, "KSQ") && DemSpells.Q.CanCast(target) && Prediction.Health.GetPrediction(target, DemSpells.Q.CastDelay) > 0f &&
                    target.Health < myhero.GetSpellDamage(target, SpellSlot.Q, DamageLibrary.SpellStages.Default) && DemSpells.Q.Cast())
                {
                    return;
                }
                if (check(misc, "KSE") && DemSpells.E.CanCast(target) && target.Health < myhero.GetSpellDamage(target, SpellSlot.E, DamageLibrary.SpellStages.Default) &&
                    Prediction.Health.GetPrediction(target, 1000 * (int)(target.Distance(myhero.Position, false) / DemSpells.E.Speed) + DemSpells.E.CastDelay) > 0f &&
                    DemSpells.E.GetPrediction(target).HitChancePercent >= slider(pred, "EPred") && DemSpells.E.Cast(DemSpells.E.GetPrediction(target).CastPosition))
                {
                    return;
                }

                if (Ignite != null && check(misc, "autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health && Ignite.Cast(target))
                {
                    return;
                }
            }

            if (Smite != null && Smite.IsReady() && key(jungleclear, "SMITEKEY"))
            {
                IEnumerable<Obj_AI_Minion> Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 500).Where(x => x.IsValidTarget(500) && !x.IsDead &&
                                                                                                                   !x.Name.ToLower().Contains("mini"));

                if (Monsters != null)
                {
                    using (IEnumerator<Obj_AI_Minion> list = Monsters.GetEnumerator())
                    {
                        Obj_AI_Minion monster = list.Current;

                        if (Smite.CanCast(monster) && monster.Health < myhero.GetSummonerSpellDamage(monster, DamageLibrary.SummonerSpells.Smite) && Smite.Cast(monster))
                        { return; }
                    }
                    return;
                }
            }

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            IEnumerable<Obj_AI_Minion> ValidCrystals = Crystals.Where(x => EntityManager.Heroes.Enemies.Where(y => y.Distance(x) < 230 &&
                                                                                                                y.HasBuff("skarnerpassivecrystalbuffcooldown"))
                                                                                                         .Any());


            if (check(misc, "PING") && !Pinged && ValidCrystals.Any())
            {
                using (IEnumerator<Obj_AI_Minion> list = ValidCrystals.GetEnumerator())
                {
                    if (list.MoveNext())
                    {
                        Obj_AI_Minion crystal = list.Current;
                        TacticalMap.SendPing(PingCategory.Fallback, crystal.Position);
                        Pinged = true;
                        Core.DelayAction(() => { Pinged = false; }, slider(misc, "PINGDELAY") * 1000);
                        return;
                    }
                }
                return;
            }


            if (key(misc, "SHIELDKEY") && DemSpells.W.IsReady() && myhero.HealthPercent <= slider(misc, "SMINH") && myhero.ManaPercent >= slider(misc, "SMINM"))
            {
                IEnumerable<AIHeroClient> list = EntityManager.Heroes.Enemies.Where(x => x.Distance(myhero.Position) < 400);

                if (list.Any() && list.Count() >= slider(misc, "SMINE") && DemSpells.W.Cast()) return;
            }

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);
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
            combo.Add("CQ", new CheckBox("Use Q"));
            combo.Add("CW", new CheckBox("Use W"));
            combo.Add("CE", new CheckBox("Use E"));
            combo.Add("CR", new CheckBox("Use R"));
            combo.Add("ITEMS", new CheckBox("Use Items"));

            harass.AddLabel("Spells");
            harass.Add("Q", new CheckBox("Use Q"));
            harass.Add("HQMIN", new Slider("Min Enemies For Q", 1, 1, 5));
            harass.AddSeparator();
            harass.Add("E", new CheckBox("Use E", false));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 1, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("Q", new CheckBox("Use Q", false));
            laneclear.Add("LQMIN", new Slider("Min Monsters For Q", 3, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("E", new CheckBox("Use E", false));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 1, 100));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("Q", new CheckBox("Use Q", false));
            jungleclear.Add("JQMIN", new Slider("Min Monsters For Q", 2, 1, 4));
            jungleclear.AddSeparator();
            jungleclear.Add("E", new CheckBox("Use E", false));
            jungleclear.Add("JEMODE", new ComboBox("Select E Targets", 0, "Big Monsters", "All Monsters"));
            jungleclear.AddSeparator();
            jungleclear.Add("JMIN", new Slider("Min Mana % To Jungleclear", 50, 1, 100));
            if (Smite != null)
            {
                jungleclear.AddSeparator();
                jungleclear.AddLabel("Smite");
                jungleclear.Add("SMITEKEY", new KeyBind("Auto-Smite Key", false, KeyBind.BindTypes.HoldActive, 83u, 27u));
                jungleclear.AddLabel("(Auto-Smite Will Only Kill Big Monsters)");
            }

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q/R Range"));
            draw.Add("drawE", new CheckBox("Draw E Range"));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("DRAWSHIELD", new CheckBox("Draw Auto-Shield's Status"));

            misc.AddLabel("Killsteal");
            misc.Add("KSQ", new CheckBox("Killsteal with Q"));
            misc.Add("KSE", new CheckBox("Killsteal with E", false));
            if (Ignite != null)
            {
                misc.Add("autoign", new CheckBox("Auto Ignite If Killable"));
            }
            misc.AddSeparator();
            misc.Add("PING", new CheckBox("Ping If Enemy Is Capturing A Crystal", false));
            misc.Add("PINGDELAY", new Slider("Delay Between Pings (In Seconds)", 15, 1, 30));
            misc.AddSeparator();
            misc.Add("SHIELDKEY", new KeyBind("Auto-Shield Hotkey", false, KeyBind.BindTypes.PressToggle, 'S'));
            misc.AddLabel("If");
            misc.Add("SMINH", new Slider("My Health Is Lower Than X%", 70, 1, 100));
            misc.AddLabel("And");
            misc.Add("SMINE", new Slider("There Are X Enemies Near Me", 1, 1, 5));
            misc.AddSeparator(12);
            misc.Add("SMINM", new Slider("Min Mana % To Auto-Shield", 30, 1, 100));
            misc.AddSeparator(50);
            misc.Add("WFLEE", new CheckBox("Use W To Flee", false));
            misc.AddSeparator();
            misc.AddLabel("Auto Potion");
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion"));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 60, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells"));
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack"));
            misc.Add("skinID", new ComboBox("Skin Hack", 3, new string[]
            {
                "Default",
                "Sandscourge",
                "Earthrune",
                "Battlecast Alpha",
                "Guardian Of The Sands"
            }));

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("E :");
            pred.Add("EPred", new Slider("Select % Hitchance", 90, 1, 100));
        }
        #endregion

        #region Methods
        private static void ItemManager(AIHeroClient target)
        {
            if (target != null && target.ValidTarget(1100) && check(combo, "ITEMS"))
            {
                if (tiamat.IsOwned(null) && tiamat.IsReady() && tiamat.IsInRange(target.Position))
                {
                    tiamat.Cast();
                }
                if (rhydra.IsOwned(null) && rhydra.IsReady() && rhydra.IsInRange(target.Position))
                {
                    rhydra.Cast();
                }
                if (thydra.IsOwned(null) && thydra.IsReady() && target.Distance(myhero.Position, false) < Player.Instance.GetAutoAttackRange(null) && !Orbwalker.CanAutoAttack)
                {
                    thydra.Cast();
                }
                if (yomus.IsOwned(null) && yomus.IsReady() && target.Distance(myhero.Position, false) < 1000f)
                {
                    yomus.Cast();
                }
            }
        }

        private static void CheckPrediction()
        {
            //  string CorrectPrediction = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ? "SDK Beta Prediction" : "SDK Prediction";

            string CorrectPrediction = "SDK Beta Prediction";

            if (Prediction.Manager.PredictionSelected == CorrectPrediction)
            {
                return;
            }
            else
            {
                Prediction.Manager.PredictionSelected = CorrectPrediction;
                Chat.Print("<font color='#00D118'>T7 Skarner: Prediction Has Been Automatically Changed!</font>");
                return;
            }
        }

        private static bool CastE()
        {
            var Monsters = EntityManager.MinionsAndMonsters.Monsters.Where(x => x.IsValidTarget(1100,false,myhero.Position));

            if (Monsters != null && DemSpells.E.IsReady())
            {
                foreach (Obj_AI_Minion monster in Monsters)
                {
                    if (comb(jungleclear, "JEMODE") == 0 && monster.BaseSkinName.Contains("Mini")) continue;

                    PredictionResult epred = DemSpells.E.GetPrediction(monster);

                    if (epred.HitChancePercent >= slider(pred, "EPred") && DemSpells.E.Cast(epred.CastPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CastE(AIHeroClient target)
        {
            var Enemies = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1100, false, myhero.Position));

            if (Enemies != null && DemSpells.E.IsReady())
            {
                foreach (AIHeroClient enemy in Enemies)
                {
                    PredictionResult epred = DemSpells.E.GetPrediction(enemy);

                    if (epred.HitChancePercent >= slider(pred, "EPred") && DemSpells.E.Cast(epred.CastPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CastQ(AIHeroClient target)
        {
            var Enemies = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(DemSpells.Q.Range + 100, false, myhero.Position) && x.ValidTarget((int)DemSpells.Q.Range + 100));

            if (Enemies != null && Enemies.Where(x => x.Distance(myhero.Position) < DemSpells.Q.Range && x.Equals(target)).Any() && DemSpells.Q.Cast())
            {
                return true;
            }

            return false;
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
        public static Spell.Active Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Active(SpellSlot.Q, 350);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1000, SkillShotType.Linear, 1500, 70)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Targeted(SpellSlot.R, 350);
        }
    }

    public static class Extensions
    {
        public static bool HasEBuff(this AIHeroClient hero)
        {
            return hero.HasBuff("skarnerpassivebuff");
        }

        public static bool ValidTarget(this AIHeroClient hero, int range)
        {
            return !hero.HasBuff("UndyingRage") && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("ChronoShift") && !hero.HasBuff("kindredrnodeathbuff") &&
                   !hero.IsInvulnerable && !hero.IsDead && hero.IsValidTarget(range) && !hero.IsZombie &&
                   !hero.HasBuffOfType(BuffType.Invulnerability) && !hero.HasBuffOfType(BuffType.SpellImmunity) && !hero.HasBuffOfType(BuffType.SpellShield);

        }
    }
}
