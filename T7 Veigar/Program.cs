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

namespace Veigarino
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
            Chat.Print("<font color='#0040FF'>T7</font><font color='#A901DB'> Veigar</font> : Loaded!(v1.3)");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            Gapcloser.OnGapcloser += OnGapcloser;
            DatMenu();
            Game.OnTick += OnTick;
            
        } 
        private static void OnLvlUp(Obj_AI_Base guy ,Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!guy.IsMe) return;
/*Q>W>E*/   SpellSlot[] sequence1 = { SpellSlot.Unknown, SpellSlot.E, SpellSlot.W, SpellSlot.Q, SpellSlot.Q, SpellSlot.R, SpellSlot.Q, SpellSlot.W, SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.W, SpellSlot.E, SpellSlot.W, SpellSlot.W, SpellSlot.R, SpellSlot.E, SpellSlot.E };
/*Q>E>W*/   SpellSlot[] sequence2 = { SpellSlot.Unknown, SpellSlot.E, SpellSlot.Q, SpellSlot.W, SpellSlot.Q, SpellSlot.R, SpellSlot.Q, SpellSlot.E, SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.E, SpellSlot.E, SpellSlot.W, SpellSlot.W, SpellSlot.R, SpellSlot.W, SpellSlot.W };
/*E>Q>W*/   SpellSlot[] sequence3 = { SpellSlot.Unknown, SpellSlot.Q, SpellSlot.E, SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.E, SpellSlot.R, SpellSlot.Q, SpellSlot.Q, SpellSlot.W, SpellSlot.W, SpellSlot.R, SpellSlot.W, SpellSlot.W };

            if(misc["autoS"].Cast<CheckBox>().CurrentValue) Player.LevelSpell(sequence1[myhero.Level]);
        } 
        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            Misc();

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) || harass["autoH"].Cast<CheckBox>().CurrentValue && myhero.ManaPercent > harass["minMH"].Cast<Slider>().CurrentValue)
            {
                Harass();
            }
            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) || laneclear["AutoL"].Cast<CheckBox>().CurrentValue && laneclear["LcM"].Cast<Slider>().CurrentValue <= myhero.ManaPercent)
            {
                Laneclear();
            }

            if (flags.HasFlag(Orbwalker.ActiveModes.JungleClear) || laneclear["AutoL"].Cast<CheckBox>().CurrentValue && laneclear["LcM"].Cast<Slider>().CurrentValue <= myhero.ManaPercent)
            {
                Laneclear();
            }

            if (laneclear["Qlk"].Cast<KeyBind>().CurrentValue && laneclear["LcM"].Cast<Slider>().CurrentValue <= myhero.ManaPercent) QStack();              
        }
        private static float ComboDMG(Obj_AI_Base target)
        {
            if (target != null)
            {
                float cdmg = 0;

                if (DemSpells.Q.IsReady() && combo["useQ"].Cast<CheckBox>().CurrentValue) { cdmg += myhero.GetSpellDamage(target, SpellSlot.Q); }
                if (DemSpells.W.IsReady() && combo["useW"].Cast<CheckBox>().CurrentValue) { cdmg += myhero.GetSpellDamage(target, SpellSlot.W); }
                if (DemSpells.R.IsReady() && combo["useR"].Cast<CheckBox>().CurrentValue) { cdmg += UltDamage(target); }

           //     if (ignt.Slot != SpellSlot.Unknown && ignt.IsReady() && combo["igntC"].Cast<CheckBox>().CurrentValue) { cdmg += myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite); }
                return cdmg;
            }
            return 0;
        } 
        private static float UltDamage(Obj_AI_Base target)
        {
            var level = DemSpells.R.Level - 1;           

            var damage = new float[] { 175, 250, 325 }[level] + (((100 - target.HealthPercent) * 1.5) / 100) * new float[] { 175, 250, 325 }[level] +
                0.75 * myhero.FlatMagicDamageMod;
            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, (float)damage);                  
        }
        public static void Harass() 
        {        
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);

            if (target != null)
            {
                if (harass["hQ"].Cast<CheckBox>().CurrentValue && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target))
                {
                    var Qpred = DemSpells.Q.GetPrediction(target);
                    switch (pred["Qhit"].Cast<ComboBox>().CurrentValue)
                    {
                        case 0:
                            if (Qpred.HitChance >= HitChance.Low) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 1:
                            if (Qpred.HitChance >= HitChance.Medium) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 2:
                            if (Qpred.HitChance >= HitChance.High) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                    }
                }

                if (harass["hW"].Cast<CheckBox>().CurrentValue && DemSpells.W.IsReady() && DemSpells.W.IsInRange(target))
                {
                    var Wpred = DemSpells.W.GetPrediction(target);
                    switch (harass["hWm"].Cast<ComboBox>().CurrentValue)
                    {
                        case 0:
                            switch (pred["Whit"].Cast<ComboBox>().CurrentValue)
                            {
                                case 0:
                                    if (Wpred.HitChance == HitChance.Low) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 1:
                                    if (Wpred.HitChance == HitChance.Medium) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 2:
                                    if (Wpred.HitChance == HitChance.High) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                            }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;
                        case 2:
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
                    switch (pred["Qhit"].Cast<ComboBox>().CurrentValue)
                    {
                        case 0:
                            if (Qpred.HitChance >= HitChance.Low) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 1:
                            if (Qpred.HitChance >= HitChance.Medium) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                        case 2:
                            if (Qpred.HitChance >= HitChance.High) DemSpells.Q.Cast(Qpred.CastPosition);
                            break;
                    }
                }
                if (combo["useE"].Cast<CheckBox>().CurrentValue && DemSpells.E.IsReady() && target.Distance(myhero) < DemSpells.E.Range - 30)
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
                            switch (pred["Whit"].Cast<ComboBox>().CurrentValue)
                            {
                                case 0:
                                    if (Wpred.HitChance == HitChance.Low) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 1:
                                    if (Wpred.HitChance == HitChance.Medium) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                                case 2:
                                    if (Wpred.HitChance == HitChance.High) DemSpells.W.Cast(Wpred.CastPosition);
                                    break;
                            }
                            break;
                    }
                }
                if (combo["useR"].Cast<CheckBox>().CurrentValue && DemSpells.R.IsReady() && DemSpells.R.IsInRange(target) && ComboDMG(target) > target.Health && UltDamage(target) > target.Health) DemSpells.R.Cast(target);

                if (combo["igntC"].Cast<CheckBox>().CurrentValue && ignt.IsReady() && ComboDMG(target) > target.Health && ignt.IsInRange(target) && myhero.GetSummonerSpellDamage(target,DamageLibrary.SummonerSpells.Ignite) > target.Health) ignt.Cast(target);
            }
        } 
        private static void QStack()
        {
            var farm = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range).Where(x => Prediction.Health.GetPrediction(x, DemSpells.Q.CastDelay ) < (myhero.GetSpellDamage(x, SpellSlot.Q) - 15) && x.IsValidTarget() && x.Distance(myhero) < DemSpells.Q.Range - 10);
                                                                                                                                                                                                                              
            var FarmPred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(farm, DemSpells.Q.Width, (int)DemSpells.Q.Range);

            switch (laneclear["Qlm"].Cast<ComboBox>().CurrentValue)
            {
                case 0:
                    if (FarmPred.HitNumber >= 1 && !Orbwalker.IsAutoAttacking) DemSpells.Q.Cast(FarmPred.CastPosition);
                    break;
                case 1:
                    if (FarmPred.HitNumber == 2 && !Orbwalker.IsAutoAttacking) DemSpells.Q.Cast(FarmPred.CastPosition);
                    break;
                case 2:
                    var BigMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range).Where(x => x.BaseSkinName.Contains("Siege") || x.BaseSkinName.Contains("Super") && x.Health <= myhero.GetSpellDamage(x, SpellSlot.Q)- 20);
                    var BMpred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(BigMinions, DemSpells.Q.Width, (int)DemSpells.Q.Range);
                    if (BMpred.HitNumber == 1 && !Orbwalker.IsAutoAttacking) DemSpells.Q.Cast(BMpred.CastPosition);
                    break;
            }
        }
        private static void Laneclear()
        {
            var minion = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range);

            if (laneclear["LQ"].Cast<CheckBox>().CurrentValue  && DemSpells.Q.IsReady() && !laneclear["Qlk"].Cast<KeyBind>().CurrentValue)
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
                        switch (pred["Qhit"].Cast<ComboBox>().CurrentValue)
                        {
                            case 0:
                                if (Qpred.HitChance >= HitChance.Low) DemSpells.Q.Cast(Qpred.CastPosition);
                                break;
                            case 1:
                                if (Qpred.HitChance >= HitChance.Medium) DemSpells.Q.Cast(Qpred.CastPosition);
                                break;
                            case 2:
                                if (Qpred.HitChance >= HitChance.High) DemSpells.Q.Cast(Qpred.CastPosition);
                                break;
                        }
                    }
                }
                if (misc["ksW"].Cast<CheckBox>().CurrentValue && myhero.GetSpellDamage(target, SpellSlot.W) > target.Health && target.Distance(myhero) < DemSpells.W.Range && DemSpells.W.IsReady() && !target.IsInvulnerable)
                {

                    if (Wpred.HitChance == HitChance.Immobile || Wpred.HitChance >= HitChance.Medium) DemSpells.W.Cast(Wpred.CastPosition);
                }

                if (misc["ksR"].Cast<CheckBox>().CurrentValue && UltDamage(target) > target.Health && DemSpells.R.IsInRange(target) && DemSpells.R.IsReady() && !target.IsInvulnerable) DemSpells.R.Cast(target);

                if (misc["autoing"].Cast<CheckBox>().CurrentValue && ignt.IsReady() && ignt.IsInRange(target) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health) ignt.Cast(target);
            }
                        
        }
        private static void OnDraw(EventArgs args)
        {

            var color = new ColorBGRA(48, 123, 243, 1);
            var colorc = new ColorBGRA(48, 123, 243, 0);
            if (draw["drawQ"].Cast<CheckBox>().CurrentValue && DemSpells.Q.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(DemSpells.Q.IsOnCooldown ?  SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.Q.Range, myhero.Position); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.Q.Range, myhero.Position); }
                // Drawing.DrawCircle(myhero.Position, DemSpells.Q.Range, DemSpells.Q.IsOnCooldown ? Color.Transparent : Color.SkyBlue);
            }

            if (draw["drawW"].Cast<CheckBox>().CurrentValue && DemSpells.W.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(DemSpells.W.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

            }

            if (draw["drawE"].Cast<CheckBox>().CurrentValue && DemSpells.E.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(DemSpells.E.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.E.Range, myhero.Position); }

            }

            if (draw["drawR"].Cast<CheckBox>().CurrentValue && DemSpells.R.Level > 0 && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue)
            {

                if (draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

                else if (!draw["nodrawc"].Cast<CheckBox>().CurrentValue) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

            }

            if (draw["drawAA"].Cast<CheckBox>().CurrentValue && !myhero.IsDead && !draw["nodraw"].Cast<CheckBox>().CurrentValue) Circle.Draw(SharpDX.Color.LightYellow, myhero.AttackRange, myhero.Position); 

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (draw["drawk"].Cast<CheckBox>().CurrentValue && !draw["nodraw"].Cast<CheckBox>().CurrentValue && enemy.IsVisible && enemy.IsHPBarRendered && !enemy.IsDead && ComboDMG(enemy) > enemy.Health) Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, Color.Green, "Killable With Combo");
                else if (draw["drawk"].Cast<CheckBox>().CurrentValue && !draw["nodraw"].Cast<CheckBox>().CurrentValue && enemy.IsVisible && enemy.IsHPBarRendered && !enemy.IsDead && ComboDMG(enemy) + myhero.GetSummonerSpellDamage(enemy,DamageLibrary.SummonerSpells.Ignite) > enemy.Health) Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, Color.Green, "Combo + Ignite");
            }

            if (draw["drawStacks"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50, Drawing.WorldToScreen(myhero.Position).Y + 10, Color.Red, laneclear["Qlk"].Cast<KeyBind>().CurrentValue ? "Auto Stacking: ON" : "Auto Stacking: OFF");
            }

            if(draw["drawStackCount"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 25, Drawing.WorldToScreen(myhero.Position).Y + 25, Color.Red, "Count: " + myhero.GetBuffCount("veigarphenomenalevilpower").ToString());
            }

        }

        private static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (sender != null && sender.IsEnemy && DemSpells.E.IsInRange(sender) && DemSpells.E.IsReady() && sender.IsValidTarget() && misc["gapmode"].Cast<ComboBox>().CurrentValue != 0)
            {
                var gpred = DemSpells.E.GetPrediction(sender);
                switch (misc["gapmode"].Cast<ComboBox>().CurrentValue)
                {
                    case 1:
                        if(!sender.IsFleeing && sender.IsFacing(myhero))
                        DemSpells.E.Cast(myhero.Position);
                        break;
                    case 2:
                        if (gpred != null && gpred.HitChancePercent >= pred["Ehit"].Cast<Slider>().CurrentValue)
                        {
                            DemSpells.E.Cast(gpred.CastPosition);
                        }
                        break;
                }
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
            menu.AddGroupLabel("Version 1.3");
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
            harass.Add("hWm", new ComboBox("Select Mode",2,"With Prediciton","Without Prediction(Not Recommended)","Only On Stunned Enemies"));
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
            laneclear.AddGroupLabel("Q Stacking");
            laneclear.Add("Qlk", new KeyBind("Auto Stacking",true,KeyBind.BindTypes.PressToggle,'F'));
            laneclear.Add("Qlm",new ComboBox("Select Mode",1,"LastHit 1 Minion","LastHit 2 Minions","LastHit Only Big Minions"));
            laneclear.AddSeparator();
            laneclear.AddGroupLabel("Min W Minions");
            laneclear.Add("Wmm", new Slider("Min minions to use W", 2, 1, 6));
            laneclear.AddSeparator();
            laneclear.AddGroupLabel("Stop Laneclear At % Mana");
            laneclear.Add("LcM", new Slider("%", 50, 0, 100));
            laneclear.AddSeparator();
            laneclear.AddGroupLabel("Auto Laneclear");
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
            draw.Add("drawStacks", new CheckBox("Draw Auto Stack Mode", true));
            draw.Add("drawStackCount", new CheckBox("Draw Stack Count", false));

            misc.AddGroupLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q",false));
            misc.Add("ksW", new CheckBox("Killsteal with W(With Prediction)",false));
            misc.Add("ksR", new CheckBox("Killsteal with R",false));
            misc.Add("autoing", new CheckBox("Auto Ignite If Killable", false));
            misc.AddGroupLabel("Gapcloser");
            misc.Add("gapmode", new ComboBox("Use E On Gapcloser                                               Mode:", 2, "Off","Self","Enemy(Pred)"));
            misc.AddSeparator();
            misc.AddGroupLabel("Auto Level Up Spells");
            misc.Add("autoS",new CheckBox("Activate Auto Level Up Spells",true));
            misc.AddSeparator();
            misc.AddGroupLabel("Skin Hack");
            misc.Add("sh", new CheckBox("Activate Skin hack"));
            misc.Add("sID", new ComboBox("Skin Hack", 0, "Default", "White Mage", "Curling", "Veigar Greybeard", "Leprechaun", "Baron Von", "Superb Villain", "Bad Santa", "Final Boss"));


            pred.AddGroupLabel("Q HitChance");
            pred.Add("Qhit",new ComboBox("Selecte Hitchance",1,"Low","Medium","High"));
            pred.AddSeparator();
            pred.AddGroupLabel("W HitChance");
            pred.Add("Whit", new ComboBox("Selecte Hitchance", 1, "Low", "Medium", "High"));
            pred.AddSeparator();
            pred.AddGroupLabel("E HitChance");
            pred.Add("Ehit", new Slider("% Hitchance",85,1,100));
                
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
