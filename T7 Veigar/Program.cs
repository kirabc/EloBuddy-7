using System;
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


namespace HolaAtMe
{
    class ΤοΠιλλ
    {
        private static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu,combo,harass,laneclear,misc,draw,pred,sequence1,sequence2,sequence3;
        public static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 550);
          
        public static void OnLoad(EventArgs arg)
        {
            DemSpells.Q.AllowedCollisionCount = 1;
            if (Player.Instance.ChampionName != "Veigar") { return; }
            Chat.Print("<font color='#0000FF'>T7</font><font color='#A901DB'> Veigar</font> : Loaded!(v1.0)");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            DatMenu();
            Game.OnTick += OnTick;
            
        } 
        private static void OnLvlUp(Obj_AI_Base guy ,Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!guy.IsMe) return;
/*Q>W>E*/   SpellSlot[] sequence1 = { SpellSlot.Unknown, SpellSlot.E, SpellSlot.W, SpellSlot.Q, SpellSlot.Q, SpellSlot.R, SpellSlot.Q, SpellSlot.W, SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.W, SpellSlot.E, SpellSlot.W, SpellSlot.W, SpellSlot.R, SpellSlot.E, SpellSlot.E };
/*Q>E>W*/   SpellSlot[] sequence2 = { SpellSlot.Unknown, SpellSlot.E, SpellSlot.Q, SpellSlot.W, SpellSlot.Q, SpellSlot.R, SpellSlot.Q, SpellSlot.E, SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.E, SpellSlot.E, SpellSlot.W, SpellSlot.W, SpellSlot.R, SpellSlot.W, SpellSlot.W };
/*E>Q>W*/   SpellSlot[] sequence3 = { SpellSlot.Unknown, SpellSlot.Q, SpellSlot.E, SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.E, SpellSlot.R, SpellSlot.Q, SpellSlot.Q, SpellSlot.W, SpellSlot.W, SpellSlot.R, SpellSlot.W, SpellSlot.W };

            Player.LevelSpell(sequence1[myhero.Level]);
        } 
        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            Misc();
            
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();

            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LaneClear && laneclear["LcM"].Cast<Slider>().CurrentValue <= myhero.ManaPercent ) Laneclear();

            if (laneclear["AutoL"].Cast<CheckBox>().CurrentValue && laneclear["LcM"].Cast<Slider>().CurrentValue <= myhero.ManaPercent) Laneclear();

            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass && myhero.ManaPercent >= harass["minMH"].Cast<Slider>().CurrentValue) Harass();

            if (harass["autoH"].Cast<CheckBox>().CurrentValue && myhero.ManaPercent > harass["minMH"].Cast<Slider>().CurrentValue) Harass();

            
            
        } 
        private static float ComboDMG(Obj_AI_Base target)
        {
            if (target != null)
            {
                float cdmg = 0;

                if (DemSpells.Q.IsReady()) { cdmg = cdmg + myhero.GetSpellDamage(target, SpellSlot.Q); }
                if (DemSpells.W.IsReady()) { cdmg = cdmg + myhero.GetSpellDamage(target, SpellSlot.W); }
                if (DemSpells.R.IsReady()) { cdmg = cdmg + myhero.GetSpellDamage(target, SpellSlot.R); }

                if (ignt.Slot != SpellSlot.Unknown && ignt.IsReady()) { cdmg = cdmg + myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite); }
                return cdmg;
            }
            return 0;
        } 
        public static void Harass() 
        {        
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (target != null)
            {
                if (harass["hQ"].Cast<CheckBox>().CurrentValue && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target))
                {
                    var Qpred = DemSpells.Q.GetPrediction(target);
                    switch (pred["Qh"].Cast<Slider>().CurrentValue)
                    {
                        case 1:
                            if (Qpred.HitChance >= HitChance.Low) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 2:
                            if (Qpred.HitChance >= HitChance.Medium) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 3:
                            if (Qpred.HitChance >= HitChance.High) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                    }
                }

                if (harass["hW"].Cast<CheckBox>().CurrentValue && DemSpells.W.IsReady() && DemSpells.W.IsInRange(target))
                {
                    var Wpred = DemSpells.W.GetPrediction(target);
                    switch (harass["hWm"].Cast<Slider>().CurrentValue)
                    {
                        case 1:
                            switch (pred["Wh"].Cast<Slider>().CurrentValue)
                            {
                                case 1:
                                    if (Wpred.HitChance == HitChance.Low) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 2:
                                    if (Wpred.HitChance == HitChance.Medium) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 3:
                                    if (Wpred.HitChance == HitChance.High) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                            }
                            break;
                        case 2:
                            DemSpells.W.Cast(target.Position);
                            break;
                        case 3:
                            if (Wpred.HitChance == HitChance.Immobile || target.HasBuffOfType(BuffType.Stun)) DemSpells.W.Cast(Wpred.CastPosition);
                            break;

                    }
                }
            }                 
        } 
        private static void Combo() 
        {
           
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);
            
            if(target != null)
            { 
                var Qpred = DemSpells.Q.GetPrediction(target);
                var Wpred = DemSpells.W.GetPrediction(target);


                if (combo["useQ"].Cast<CheckBox>().CurrentValue && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target))
                {
                    switch (pred["Qh"].Cast<Slider>().CurrentValue)
                    {
                        case 1:
                            if (Qpred.HitChance >= HitChance.Low) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 2:
                            if (Qpred.HitChance >= HitChance.Medium) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 3:
                            if (Qpred.HitChance >= HitChance.High) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                    }
                }
                if (combo["useE"].Cast<CheckBox>().CurrentValue && DemSpells.E.IsReady() && DemSpells.E.IsInRange(target))
                {
                    switch (combo["Es"].Cast<CheckBox>().CurrentValue)
                    {
                        case false:
                            DemSpells.E.Cast(target.Position);
                            break;
                        case true:
                            if (!target.HasBuffOfType(BuffType.Stun)) DemSpells.E.Cast(target.Position);
                            break;
                    }
                }
                if (combo["useW"].Cast<CheckBox>().CurrentValue && DemSpells.W.IsReady() && DemSpells.W.IsInRange(target))
                {
                    switch (combo["useWs"].Cast<CheckBox>().CurrentValue)
                    {
                        case true:
                            if (target.HasBuffOfType(BuffType.Stun)) DemSpells.W.Cast(target.Position);
                            break;
                        case false:
                            switch (pred["Wh"].Cast<Slider>().CurrentValue)
                            {
                                case 1:
                                    if (Wpred.HitChance == HitChance.Low) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 2:
                                    if (Wpred.HitChance == HitChance.Medium) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 3:
                                    if (Wpred.HitChance == HitChance.High) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                            }
                            break;
                    }
                }
                if (combo["useR"].Cast<CheckBox>().CurrentValue && DemSpells.R.IsReady() && DemSpells.R.IsInRange(target) && ComboDMG(target) > target.Health) DemSpells.R.Cast(target);

                if (combo["igntC"].Cast<CheckBox>().CurrentValue && ignt.IsReady() && ComboDMG(target) > target.Health && ignt.IsInRange(target)) ignt.Cast(target);
            }


        } 
        private static void Laneclear()
        {
            var minion = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position,DemSpells.W.Range).OrderBy(a => a.Health);

            if (laneclear["LQ"].Cast<CheckBox>().CurrentValue  && DemSpells.Q.IsReady())
            {
               var Qpred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minion, DemSpells.Q.Width, (int)DemSpells.Q.Range);

               if (Qpred.HitNumber >= 1) DemSpells.Q.Cast(Qpred.CastPosition);

            }
            if (laneclear["LW"].Cast<CheckBox>().CurrentValue && minion != null && DemSpells.W.IsReady())
            {
                var Wpred = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minion, DemSpells.W.Width, (int)DemSpells.W.Range);

                if (Wpred.HitNumber >= laneclear["Wmm"].Cast<Slider>().CurrentValue) DemSpells.W.Cast(Wpred.CastPosition);

            }      
        } 
        private static void Misc()
        {
            if (misc["sh"].Cast<CheckBox>().CurrentValue)
            {
                myhero.SetSkinId((int)misc["sID"].Cast<ComboBox>().CurrentValue);
            }

            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (target != null)
            {
                var Qpred = DemSpells.Q.GetPrediction(target);
                var Wpred = DemSpells.W.GetPrediction(target);

                if (misc["ksQ"].Cast<CheckBox>().CurrentValue && myhero.GetSpellDamage(target, SpellSlot.Q) > target.Health && target.Distance(myhero) < DemSpells.W.Range && DemSpells.Q.IsReady() && !target.IsInvulnerable)
                {
                    if (target.HasBuffOfType(BuffType.Stun)) DemSpells.Q.Cast(target.Position);
                    else
                    {
                        switch (pred["Qh"].Cast<Slider>().CurrentValue)
                        {
                            case 1:
                                if (Qpred.HitChance >= HitChance.Low) DemSpells.Q.Cast(Qpred.CastPosition);
                                break;
                            case 2:
                                if (Qpred.HitChance >= HitChance.Medium) DemSpells.Q.Cast(Qpred.CastPosition);
                                break;
                            case 3:
                                if (Qpred.HitChance >= HitChance.High) DemSpells.Q.Cast(Qpred.CastPosition);
                                break;
                        }
                    }
                }
                if (misc["ksW"].Cast<CheckBox>().CurrentValue && myhero.GetSpellDamage(target, SpellSlot.W) > target.Health && target.Distance(myhero) < DemSpells.W.Range && DemSpells.W.IsReady() && !target.IsInvulnerable)
                {

                    if (Wpred.HitChance == HitChance.Immobile || Wpred.HitChance >= HitChance.Medium) DemSpells.W.Cast(Wpred.CastPosition);
                }
            }
                        
        }
        private static void OnDraw(EventArgs args)
        {
         
            if (draw["drawQ"].Cast<CheckBox>().CurrentValue && DemSpells.Q.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.Q.Range,DemSpells.Q.IsOnCooldown ? Color.Transparent:Color.SkyBlue); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.Q.Range, Color.SkyBlue); }

            }

            if (draw["drawW"].Cast<CheckBox>().CurrentValue && DemSpells.W.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.W.Range, DemSpells.W.IsOnCooldown ? Color.Transparent : Color.SkyBlue); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.W.Range, Color.SkyBlue); }

            }

            if (draw["drawE"].Cast<CheckBox>().CurrentValue && DemSpells.E.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.E.Range, DemSpells.E.IsOnCooldown ? Color.Transparent : Color.SkyBlue); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.E.Range, Color.SkyBlue); }

            }

            if (draw["drawR"].Cast<CheckBox>().CurrentValue && DemSpells.R.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.R.Range, DemSpells.R.IsOnCooldown ? Color.Transparent : Color.SkyBlue); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Drawing.DrawCircle(myhero.Position, DemSpells.R.Range, Color.SkyBlue); }

            }

            if (draw["drawAA"].Cast<CheckBox>().CurrentValue && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue) Drawing.DrawCircle(myhero.Position, myhero.AttackRange, Color.White); 

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (draw["drawk"].Cast<CheckBox>().CurrentValue && !draw["nodraw"].Cast<CheckBox>().CurrentValue) Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, ComboDMG(enemy) > enemy.Health ? Color.Green : Color.Transparent, "Killable");
            }
        } 
        private static void DatMenu()
        {

            menu = MainMenu.AddMenu("T7 Veigar", "veigarxd");
            combo = menu.AddSubMenu("Combo" ,"combo");
            harass = menu.AddSubMenu("Harass","harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc" ,"misc");
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 Veigar And Thank You For Using!");
            menu.AddGroupLabel("Version 1.0");
            menu.AddGroupLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddGroupLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddGroupLabel("Spells");
            combo.Add("useQ", new CheckBox("Use Q in Combo",true)); 
            combo.Add("useW", new CheckBox("Use W in Combo",true));
            combo.Add("useE", new CheckBox("Use E in Combo",true));
            combo.Add("useR", new CheckBox("Use R in Combo",true));
            combo.Add("igntC", new CheckBox("Use Ignite", false));
            combo.AddSeparator();
            combo.Add("Es", new CheckBox("Dont Use E On Immobile Enemies",true));
            combo.Add("useWs", new CheckBox("Use W Only On Stunned Enemies", true));
            
 
            harass.AddGroupLabel("Spells");
            harass.Add("hQ", new CheckBox("Use Q",true));
            harass.Add("hW", new CheckBox("Use W",false));
            harass.AddGroupLabel("W Mode:");
            var Wm = harass.Add("hWm", new Slider("Select Mode", 1, 1, 3));
            Wm.OnValueChange += (s, cargs) =>
            {
                switch (cargs.NewValue)
                {
                    case 1:
                        Wm.DisplayName = "With Prediction";
                        return;
                    case 2:
                        Wm.DisplayName = "Without Prediction(Not recommended)";
                        return;
                    case 3:
                        Wm.DisplayName = "Only On Stunned Enemies";
                        return;
                }
            };
            harass.AddSeparator();
            harass.AddGroupLabel("Min Mana To Harass");
            harass.Add("minMH", new Slider("Stop Harass At % Mana", 40, 0, 100));
            harass.AddSeparator();
            harass.AddGroupLabel("Auto Harass");
            harass.Add("autoH", new CheckBox(" Use Auto harass", false));


            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q",true)); 
            laneclear.Add("LW", new CheckBox("Use W",false));
            laneclear.AddSeparator();
            laneclear.AddGroupLabel("Min W Minions");
            laneclear.Add("Wmm", new Slider("Min minions to use W", 2, 1, 6));
            laneclear.AddSeparator();
            laneclear.AddGroupLabel("Stop Laneclear At % Mana");
            laneclear.Add("LcM", new Slider("%", 50, 0, 100));
            laneclear.AddSeparator();
            laneclear.AddGroupLabel("Auto Harass");
            laneclear.Add("AutoL", new CheckBox("Auto Laneclear", false));

            

            draw.Add("nodraw", new CheckBox("Disable All Drawings",false)); 
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range",true));
            draw.Add("drawW", new CheckBox("Draw W Range",true));
            draw.Add("drawE", new CheckBox("Draw E Range",true));
            draw.Add("drawR", new CheckBox("Draw R Range",true));
            draw.Add("drawAA", new CheckBox("Draw AA Range",false));
            draw.Add("drawk", new CheckBox("Draw Killable Enemies",false));
            draw.Add("nodrawc", new CheckBox("Draw Only Ready Spells",false));

            misc.Add("ksQ", new CheckBox("Killsteal with Q"));
            misc.Add("ksW", new CheckBox("Killsteal with W(With Prediction)"));
            misc.AddSeparator();
            misc.AddGroupLabel("Auto Level Up Spells");
            misc.Add("autoS",new CheckBox("Activate Auto Level Up Spells",true));
            misc.Add("lvlSpells", new ComboBox("Choose Sequence" , 0 , "Q>W>E"));
            misc.Add("lolKappa",new CheckBox("More Sequences Coming Soon!",false));
            misc.AddSeparator();
            misc.AddGroupLabel("Skin Hack");
            misc.Add("sh", new CheckBox("Activate Skin hack"));
            misc.Add("sID", new ComboBox("Skin Hack", 0, "Default", "White Mage", "Curling", "Veigar Greybeard", "Leprechaun", "Baron Von", "Superb Villain", "Bad Santa", "Final Boss"));


            pred.AddGroupLabel("Q HitChance");
            var Qh = pred.Add("Qh", new Slider("Select Hitchance", 2, 1, 3));
            Qh.OnValueChange += (s, cargs) =>
            {
                switch (cargs.NewValue)
                {
                    case 1:
                        Qh.DisplayName = "Low";
                        return;
                    case 2:
                        Qh.DisplayName = "Medium";
                        return;
                    case 3:
                        Qh.DisplayName = "High";
                        return;
                }
            };
            pred.AddSeparator();
            pred.AddGroupLabel("W HitChance");
            var Wh = pred.Add("Wh", new Slider("Select Hitchance", 2, 1, 3));
            Wh.OnValueChange += (s, cargs) =>
            {
                switch (cargs.NewValue)
                {
                    case 1:
                        Wh.DisplayName = "Low";
                        return;
                    case 2:
                        Wh.DisplayName = "Medium";
                        return;
                    case 3:
                        Wh.DisplayName = "High";
                        return;
                }
            };    
        }          
    }

    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 950, SkillShotType.Linear, 250, 2000, 70);
            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 1350, 0, 225);
            E = new Spell.Skillshot(SpellSlot.E, 700, SkillShotType.Circular, 500, 0, 425);
            R = new Spell.Targeted(SpellSlot.R, 650);
        }
    }
}
