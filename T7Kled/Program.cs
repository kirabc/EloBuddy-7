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

namespace T7_Kled
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }

        #region Declarations
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred;
        private static Spell.Targeted Ignite { get; set; }
        static readonly string ChampionName = "Kled";
        static readonly string Version = "1.0";
        static readonly string Date = "11/8/16";
        public static Item tiamat { get; private set; }
        public static Item rhydra { get; private set; }
        public static Item thydra { get; private set; }
        public static Item cutl { get; private set; }
        public static Item blade { get; private set; }
        public static Item yomus { get; private set; }
        public static Item Potion { get; private set; }
        public static Item Biscuit { get; private set; }
        public static Item RPotion { get; private set; }
        #endregion

        #region Events
        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != ChampionName) { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#DEBC23'> " + ChampionName + "</font> : Loaded!(v" + Version + ")");
            Chat.Print("<font color='#04B404'>By </font><font color='#3737E6'>Toyota</font><font color='#D61EBE'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Gapcloser.OnGapcloser += OnGapcloser;
            Game.OnTick += OnTick;
            Potion = new Item((int)ItemId.Health_Potion);
            tiamat = new Item((int)ItemId.Tiamat_Melee_Only, 400);
            cutl = new Item((int)ItemId.Bilgewater_Cutlass, 550);
            thydra = new Item((int)ItemId.Titanic_Hydra);
            blade = new Item((int)ItemId.Blade_of_the_Ruined_King, 550);
            rhydra = new Item((int)ItemId.Ravenous_Hydra_Melee_Only, 400);
            yomus = new Item((int)ItemId.Youmuus_Ghostblade);
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

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) { Combo(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) || key(harass, "AUTOHARASS")) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) || key(laneclear, "AUTOLANECLEAR")) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear)) Jungleclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.Flee)) Flee();

            Misc();

        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            var U = SpellSlot.Unknown;
            var Q = SpellSlot.Q;
            var W = SpellSlot.W;
            var E = SpellSlot.E;
            var R = SpellSlot.R;

            /*Q>W>E*/
            SpellSlot[] sequence1 = { U, W, E, Q, Q, R, Q, W, Q, W, R, W, W, E, E, R, E, E, U };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static void OnGapcloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (!check(misc, "QGAP")) return;

            var spell = HasMount() ? DemSpells.Q1 : DemSpells.Q2;

            if (sender.IsEnemy && DemSpells.Q1.IsInRange(args.End))
            {
                    var qpred = spell.GetPrediction(sender);

                    if ((HasMount() ? qpred.CollisionObjects.Where(x => x is AIHeroClient).Count() == 0 : !qpred.Collision) &&
                        qpred.HitChance == HitChance.Dashing && spell.Cast(qpred.CastPosition)) return;
            }
        }
        #endregion

        #region Methods
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

        private static float QDamage(AIHeroClient target)
        {
            int index = myhero.Spellbook.GetSpell(SpellSlot.Q).Level - 1;

            var Q1Damage = (new[] { 25, 50, 75, 100, 125 }[index] * (0.6f * myhero.TotalAttackDamage)) +
                           (new[] { 50, 100, 150, 200, 250 }[index] * (1.2f * myhero.TotalAttackDamage));

            var Q2Damage = new[] { 30, 45, 60, 75, 90 }[index] * (0.8f * myhero.TotalAttackDamage);

            return myhero.CalculateDamageOnUnit(target, DamageType.Physical,HasMount() ? Q1Damage : Q2Damage);
        }

        private static bool HasMount()
        {
            return myhero.GetAutoAttackRange() > 150;
        }

        private static void ItemManager(AIHeroClient target)
        {
            if (target != null && target.IsValidTarget() && check(combo, "ITEMS"))
            {
                if (tiamat.IsOwned() && tiamat.IsReady() && tiamat.IsInRange(target.Position) && tiamat.Cast())
                { return; }

                if (rhydra.IsOwned() && rhydra.IsReady() && rhydra.IsInRange(target.Position) && rhydra.Cast())
                { return; }

                if (thydra.IsOwned() && thydra.IsReady() && target.Distance(myhero.Position) < Player.Instance.GetAutoAttackRange() && !Orbwalker.CanAutoAttack &&
                    thydra.Cast())
                { return; }

                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position) && cutl.Cast(target))
                { return; }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position) && blade.Cast(target))
                { return; }

                if (yomus.IsOwned() && yomus.IsReady() && target.Distance(myhero.Position) < 1000 && yomus.Cast())
                { return; }
            }
        }
        #endregion

        #region Modes
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Physical, Player.Instance.Position);

            if (target != null && !target.IsInvulnerable)
            {
                if (check(combo, "CQ") && DemSpells.Q1.CanCast(target))
                {
                    if (HasMount())
                    {
                        var qpred = DemSpells.Q1.GetPrediction(target);

                        if (qpred.CollisionObjects.Where(x => x is AIHeroClient).Count() == 0 && qpred.HitChancePercent >= slider(pred, "Q1Pred") &&
                            DemSpells.Q1.Cast(qpred.CastPosition))
                        { return; }
                    }
                    else
                    {
                        var qpred = DemSpells.Q2.GetPrediction(target);

                        if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "Q2Pred") &&
                            DemSpells.Q2.Cast(qpred.CastPosition))
                        { return; }
                    }
                }

                if (HasMount() && check(combo, "CE") && DemSpells.E.CanCast(target))
                {
                    var epred = DemSpells.E.GetPrediction(target);

                    if (epred.CastPosition.IsUnderTurret() || (target.HasBuff("klede2target") && myhero.Position.Extend(target.Position, 450).IsUnderTurret()))
                    { return; }


                    if (!target.HasBuff("klede2target") && epred.HitChancePercent >= slider(pred, "EPred") && DemSpells.E.Cast(epred.CastPosition))
                    { return; }

                    if (target.HasBuff("klede2target") && DemSpells.E.Cast(target))
                    { return; }
                }

                ItemManager(target);
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && !target.IsInvulnerable)
            {
                if (check(harass, "HQ") && DemSpells.Q1.CanCast(target))
                {
                    if (HasMount())
                    {
                        var qpred = DemSpells.Q1.GetPrediction(target);

                        if (qpred.CollisionObjects.Where(x => x is AIHeroClient).Count() == 0 && qpred.HitChancePercent >= slider(pred, "Q1Pred") &&
                            DemSpells.Q1.Cast(qpred.CastPosition))
                        { return; }
                    }
                    else
                    {
                        var qpred = DemSpells.Q2.GetPrediction(target);

                        if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "Q2Pred") &&
                            DemSpells.Q2.Cast(qpred.CastPosition))
                        { return; }
                    }
                }
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, 1000);

            if (minions != null)
            {
                if (check(laneclear, "LQ") && DemSpells.Q1.IsReady())
                {
                    if (HasMount() && DemSpells.Q1.CastOnBestFarmPosition(slider(laneclear, "LQMIN")))
                    { return; }
                    else
                    {
                        foreach (var minion in minions.Where(x => x.Health > 30 && x.IsValid))
                        {
                            var qpred = DemSpells.Q2.GetPrediction(minion);

                            if (comb(laneclear, "LQMODE") == 0 && !minion.Name.ToLower().Contains("siege") && !minion.Name.ToLower().Contains("super")) continue;

                            if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "Q2Pred") && DemSpells.Q2.Cast(qpred.CastPosition)) return;
                        }
                    }
                }

                if (HasMount() && check(laneclear, "LE") && DemSpells.E.IsReady() && DemSpells.E.CastOnBestFarmPosition(slider(laneclear, "LEMIN")))
                { return; }
            }
        }

        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(myhero.Position, 1000).Where(x => x.IsValidTarget(DemSpells.Q1.Range));
            var spell = HasMount() ? DemSpells.Q1 : DemSpells.Q2;
            var hit = HasMount() ? slider(pred, "Q1Pred") : slider(pred, "Q2Pred");

            if (Monsters != null)
            {
                if (Monsters.Where(x => x.HasBuff("klede2target")).Any() && DemSpells.E.Cast(Game.CursorPos)) return;

                if (check(jungleclear, "JQ") && DemSpells.Q1.IsReady())
                {
                    if (Monsters.Where(x => !x.Name.ToLower().Contains("mini")).Any())
                    {
                        foreach (var monster in Monsters.Where(x => x.Health > 30 && !x.Name.ToLower().Contains("mini")))
                        {
                            var qpred = spell.GetPrediction(monster);

                            if (!qpred.Collision && qpred.HitChancePercent >= hit && spell.Cast(qpred.CastPosition)) return;                            
                        }
                    }
                    else
                    {
                        foreach (var monster in Monsters.Where(x => x.Health > 30))
                        {
                            var qpred = spell.GetPrediction(monster);

                            if (!qpred.Collision && qpred.HitChancePercent >= slider(pred, "Q1Pred") && spell.Cast(qpred.CastPosition)) return;
                        }
                    }
                }

                if (HasMount() && check(jungleclear, "JE") && DemSpells.E.IsReady())
                {
                    foreach (var monster in Monsters.Where(x => x.Health > 30 && !x.Name.Contains("Mini")))
                    {
                        var epred = DemSpells.E.GetPrediction(monster);

                        if (epred.HitChancePercent >= slider(pred, "EPred") && DemSpells.E.Cast(epred.CastPosition)) return;
                    }
                }
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (check(misc, "AUTOPOT") && (!myhero.HasBuff("RegenerationPotion") && !myhero.HasBuff("ItemMiniRegenPotion") && !myhero.HasBuff("ItemCrystalFlask")) &&
                myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Item.HasItem(Potion.Id) && Item.CanUseItem(Potion.Id)) Potion.Cast();

                else if (Item.HasItem(Biscuit.Id) && Item.CanUseItem(Biscuit.Id)) Biscuit.Cast();

                else if (Item.HasItem(RPotion.Id) && Item.CanUseItem(RPotion.Id)) RPotion.Cast();
            }

            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);

            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                if (check(misc, "ksQ") && myhero.Spellbook.GetSpell(SpellSlot.Q).IsReady && Prediction.Health.GetPrediction(target, 250) > 0 &&
                    Prediction.Health.GetPrediction(target, 250) <= QDamage(target) && HasMount() ? DemSpells.Q1.CanCast(target) : DemSpells.Q2.CanCast(target))
                {
                    var qpred = HasMount() ? DemSpells.Q1.GetPrediction(target) : DemSpells.Q2.GetPrediction(target);

                    if (HasMount() && qpred.CollisionObjects.Where(x => x.IsEnemy).Count() > 0) return;

                    else if (qpred.HitChancePercent >= (HasMount() ? slider(pred, "Q1Pred") : slider(pred, "Q2Pred")) && DemSpells.Q1.Cast(qpred.CastPosition))
                    { return; }
                }

                if (Ignite != null && check(misc, "autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) &&
                    myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health && Ignite.Cast(target))
                { return; }
            }
        }

        private static void Flee()
        {
            if (check(misc, "EFLEE") && DemSpells.E.IsReady() && DemSpells.E.Cast(myhero.Position.Extend(Game.CursorPos, DemSpells.E.Range).To3D()))
            { return; }
        }
        #endregion

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || check(draw, "nodraw")) return;

            if (check(draw, "drawQ") && DemSpells.Q1.IsLearned)
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw((HasMount() ? DemSpells.Q1.IsOnCooldown : DemSpells.Q2.IsOnCooldown) ? SharpDX.Color.Transparent : SharpDX.Color.Gold, HasMount() ? DemSpells.Q1.Range : DemSpells.Q2.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Gold, HasMount() ? DemSpells.Q1.Range : DemSpells.Q2.Range, myhero.Position); }

            }

            if (check(draw, "drawE") && DemSpells.E.IsLearned  && HasMount())
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Gold, DemSpells.E.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Gold, DemSpells.E.Range, myhero.Position); }

            }

            if (check(draw, "drawauto"))
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50,
                                     Drawing.WorldToScreen(myhero.Position).Y + 10,
                                     Color.White,
                                     "Auto Laneclear: ");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X + 52,
                                 Drawing.WorldToScreen(myhero.Position).Y + 10,
                                 key(laneclear, "AUTOLANECLEAR") ? Color.Green : Color.Red,
                                 key(laneclear, "AUTOLANECLEAR") ? "ON" : "OFF");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50,
                                 Drawing.WorldToScreen(myhero.Position).Y + 25,
                                 Color.White,
                                 "Auto Harass: ");
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X + 34,
                                 Drawing.WorldToScreen(myhero.Position).Y + 25,
                                 key(harass, "AUTOHARASS") ? Color.Green : Color.Red,
                                 key(harass, "AUTOHARASS") ? "ON" : "OFF");
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
            combo.Add("CQ", new CheckBox("Use Q"));
            combo.Add("CE", new CheckBox("Use E"));
            combo.AddSeparator();
            combo.Add("ITEMS", new CheckBox("Use Items"));

            harass.AddLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", false));
            harass.AddSeparator();
            harass.Add("AUTOHARASS", new KeyBind("Auto Harass", false, KeyBind.BindTypes.PressToggle, 'H'));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", false));
            laneclear.Add("LQMIN", new Slider("Min Minions For Mounted Q", 3, 1, 10));
            laneclear.Add("LQMODE", new ComboBox("Dismounted Q Targets", 0,"Big Minions", "All Minions"));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", false));
            laneclear.Add("LEMIN", new Slider("Min Minions For E", 3, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("AUTOLANECLEAR", new KeyBind("Auto Laneclear", false, KeyBind.BindTypes.PressToggle, 'L'));

            jungleclear.AddGroupLabel("Spells");
            jungleclear.Add("JQ", new CheckBox("Use Q", false));            
            jungleclear.Add("JE", new CheckBox("Use E", false));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range"));
            draw.Add("drawE", new CheckBox("Draw E Range"));
            draw.AddSeparator();
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawauto", new CheckBox("Draw Automatic Mode's Status"));

            misc.AddLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            if (Ignite != null) misc.Add("autoign", new CheckBox("Auto Ignite If Killable"));
            misc.AddSeparator();
            misc.AddLabel("Anti-Gapcloser");
            misc.Add("QGAP", new CheckBox("Use Q On Gapclosers", false));
            misc.AddSeparator();
            misc.AddLabel("Flee");
            misc.Add("EFLEE", new CheckBox("Use E To Flee"));
            misc.AddSeparator();
            misc.AddLabel("Auto Potion");
            misc.Add("AUTOPOT", new CheckBox("Activate Auto Potion"));
            misc.Add("POTMIN", new Slider("Min Health % To Active Potion", 25, 1, 100));
            misc.AddSeparator();
            misc.AddLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells"));
            misc.AddSeparator();
            misc.AddLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack"));
            misc.Add("skinID", new ComboBox("Skin Hack", 1, "Default", "Sir"));

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("Mounted Q :");
            pred.Add("Q1Pred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("Dismounted Q :");
            pred.Add("Q2Pred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("E :");
            pred.Add("EPred", new Slider("Select % Hitchance", 90, 1, 100));
        }

    }

    public static class DemSpells
    {
        public static Spell.Skillshot Q1 { get; private set; }
        public static Spell.Skillshot Q2 { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Active E2 { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q1 = new Spell.Skillshot(SpellSlot.Q, 800, SkillShotType.Linear, 250, 3000, 40);
            Q2 = new Spell.Skillshot(SpellSlot.Q, 700, SkillShotType.Cone, 250, 1600, 90);
            E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, 250, 950, 100);
        }
    }
}
