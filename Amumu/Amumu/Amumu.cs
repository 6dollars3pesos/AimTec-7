namespace Amumu
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.Util;
    

    using Spell = Aimtec.SDK.Spell;

    internal class Amumu { 
        public static Menu Menu = new Menu("pAmumu", "pAmumu", true);

        public static Orbwalker Orbwalker = new Orbwalker();

        public static Obj_AI_Hero Player = ObjectManager.GetLocalPlayer();

        public static Spell Q, W, E, R;
        public void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 300);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R, 550);

            Q.SetSkillshot(0.25f, 0.50f, 2000f, true, SkillshotType.Line);
        }

        public Amumu()
        {
            Orbwalker.Attach(Menu);
            var ComboMenu = new Menu("combo", "Combo");
            {
                ComboMenu.Add(new MenuBool("useq", "Use Q"));
                ComboMenu.Add(new MenuSlider("maxrange", "Q Max range: ", 1100, 0, 1100));
                ComboMenu.Add(new MenuBool("usew", "Use W"));
                ComboMenu.Add(new MenuBool("usee", "Use E "));
                ComboMenu.Add(new MenuBool("user", "Use R"));
                ComboMenu.Add(new MenuSlider("enemies", "Use R if enemies >= X ", 1, 1, 5));
            }
            Menu.Add(ComboMenu);
            var HarassMenu = new Menu("harass", "Harass");
            {
                HarassMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                HarassMenu.Add(new MenuBool("useq", "Use Q"));
                ComboMenu.Add(new MenuSlider("maxrange", "Q Max range: ", 1100, 0, 1100));
                HarassMenu.Add(new MenuBool("usew", "Use W"));
                HarassMenu.Add(new MenuBool("usee", "Use E "));

            }
            Menu.Add(HarassMenu);
            var FarmMenu = new Menu("farming", "Farming");
            {
                FarmMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                FarmMenu.Add(new MenuBool("useq", "Use Q", false));
                FarmMenu.Add(new MenuBool("usew", "Use W"));
                FarmMenu.Add(new MenuBool("usee", "Use E"));
 
         
            }
            Menu.Add(FarmMenu);
            var KSMenu = new Menu("killsteal", "Killsteal");
            {
                KSMenu.Add(new MenuBool("ksq", "Killsteal with Q"));
                KSMenu.Add(new MenuBool("kse", "Killsteal with E"));
                KSMenu.Add(new MenuBool("ksr", "Killsteal with R", false));
                KSMenu.Add(new MenuBool("ksrgap", "Gapclose with Q for R", false));
            }
            Menu.Add(KSMenu);
            var DrawMenu = new Menu("drawings", "Drawings");
            {
                DrawMenu.Add(new MenuBool("drawq", "Draw Q Range"));
                DrawMenu.Add(new MenuBool("draww", "Draw W Range"));
                DrawMenu.Add(new MenuBool("drawe", "Draw E Range"));
                DrawMenu.Add(new MenuBool("drawr", "Draw R Range"));
               DrawMenu.Add(new MenuBool("drawDamage", "Draw Damage"));
            }
            Menu.Add(DrawMenu);
            var MiscMenu = new Menu("misc", "Misc.");
            {
                MiscMenu.Add(new MenuBool("InterruptQ", "Interrupt with Q (Not functional yet)", false));
                MiscMenu.Add(new MenuBool("smartw", "Smart W"));
            }
            Menu.Add(MiscMenu);
 
            Menu.Attach();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            //Events.OnInterruptableTarget += OnInterruptableTarget;

      
            LoadSpells();
            Console.WriteLine("pAmumu by Prickachu - Loaded");
        }
        public static readonly List<string> SpecialChampions = new List<string> { "Annie", "Jhin" };
        public static int SxOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 1 : 10;
        }
        public static int SyOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 3 : 20;
        }
        private void Render_OnPresent()
        {

            if (Menu["drawings"]["drawq"].Enabled)
            {
                Render.Circle(Player.Position, Menu["combo"]["maxrange"].As<MenuSlider>().Value, 50, Color.Crimson);
            }

            if (Menu["drawings"]["drawe"].Enabled)
            {
                Render.Circle(Player.Position, E.Range, 50, Color.LightGreen);
            }
            if (Menu["drawings"]["draww"].Enabled)
            {
                Render.Circle(Player.Position, W.Range, 50, Color.Black);
            }
            if (Menu["drawings"]["drawr"].Enabled)
            {
                Render.Circle(Player.Position, R.Range, 50, Color.Crimson);
            }
            if (Menu["drawings"]["drawDamage"].Enabled)
            {
                
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(h => h is Obj_AI_Hero && h.IsValidTarget() && h.IsValidTarget(Q.Range+R.Range))
                    .ToList()
                    .ForEach(
                        unit =>
                        {

                            var heroUnit = unit as Obj_AI_Hero;
                            int width = 103;
                            int height = 8;
                            int xOffset = SxOffset(heroUnit);
                            int yOffset = SyOffset(heroUnit);
                            var barPos = unit.FloatingHealthBarPosition;
                            barPos.X += xOffset;
                            barPos.Y += yOffset;

                            var drawEndXPos = barPos.X + width * (unit.HealthPercent() / 100);
                            var drawStartXPos = (float)(barPos.X + (unit.Health > Player.GetSpellDamage(unit, SpellSlot.Q) + Player.GetSpellDamage(unit, SpellSlot.E) + Player.GetSpellDamage(unit, SpellSlot.R) + Player.GetSpellDamage(unit, SpellSlot.W)
                                                            ? width * ((unit.Health - Player.GetSpellDamage(unit, SpellSlot.Q) + Player.GetSpellDamage(unit, SpellSlot.E) + Player.GetSpellDamage(unit, SpellSlot.R) + Player.GetSpellDamage(unit, SpellSlot.W)) / unit.MaxHealth * 100 / 100)
                                                            : 0));

                            Render.Line(drawStartXPos, barPos.Y, drawEndXPos, barPos.Y, height, true, unit.Health < Player.GetSpellDamage(unit, SpellSlot.Q) + Player.GetSpellDamage(unit, SpellSlot.E) + Player.GetSpellDamage(unit, SpellSlot.R) + Player.GetSpellDamage(unit, SpellSlot.W) ? Color.GreenYellow : Color.Orange);
                            
                        });
            }
        }

      /* public void OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs args)
        {
            if (Player.IsDead || !args.Sender.IsValidTarget())
            {
                return;
            }
            if (args.Sender.IsValidTarget(Q.Range)
                && Menu["misc"]["InterruptQ"].Enabled)
            {
                Q.CastOnUnit(args.Sender);
            }
        }*/

 
        private void Game_OnUpdate()
        {
            if (Player.IsDead || MenuGUI.IsChatOpen())
            {
                return;
            }

            if (Menu["misc"]["smartw"].Enabled)
            {
                autoW();
            }

            switch (Orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    OnCombo();
                    break;
                case OrbwalkingMode.Mixed:
                    OnHarass();
                    break;
                case OrbwalkingMode.Laneclear:
                    Clearing();
                    Jungle();
                    break;

            }
         
            
            Killsteal();
        }
        public static List<Obj_AI_Minion> GetEnemyLaneMinionsTargets()
        {
            return GetEnemyLaneMinionsTargetsInRange(float.MaxValue);
        }
        public static List<Obj_AI_Minion> GetEnemyLaneMinionsTargetsInRange(float range)
        {
            return GameObjects.EnemyMinions.Where(m => m.IsValidTarget(range)).ToList();
        }

        private void Clearing()
        {
            bool useQ = Menu["farming"]["useq"].Enabled;
            bool useW = Menu["farming"]["usew"].Enabled;
            bool useE = Menu["farming"]["usee"].Enabled;
            float manapercent = Menu["farming"]["mana"].As<MenuSlider>().Value;
            if (manapercent < Player.ManaPercent())
            {
                if (useQ)
                {
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                    {

                        if (minion.IsValidTarget(Q.Range) && minion != null)
                        {
                            Q.CastOnUnit(minion);
                        }
                    }
                }
                if (useW)
                {
                    
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(W.Range))
                    {

                        if (minion.IsValidTarget(W.Range) && minion != null && !Player.HasBuff("AuraofDespair"))
                        {
                            
                            W.Cast();
                        }
                    }
                }
                if (useE)
                {
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(E.Range))
                    {

                        if (minion.IsValidTarget(E.Range) && minion != null)
                        {
                            E.Cast(minion);
                        }
                    }
                }


            }
        }
        public void autoW()
        {
            if (Player.HasBuff("AuraofDespair"))
            {
                if (Player.CountEnemyHeroesInRange(W.Range + 100) == 0 && GetEnemyLaneMinionsTargetsInRange(W.Range + 100).Count == 0 && GetGenericJungleMinionsTargetsInRange(W.Range + 100).Count == 0)
                {
                    W.Cast();
                }
            }
        }
        public static List<Obj_AI_Minion> GetGenericJungleMinionsTargets()
        {
            return GetGenericJungleMinionsTargetsInRange(float.MaxValue);
        }
        public static List<Obj_AI_Minion> GetGenericJungleMinionsTargetsInRange(float range)
        {
            return GameObjects.Jungle.Concat(GameObjects.JungleSmall).Where(m => m.IsValidTarget(range)).ToList();
        }

        private void Jungle()
        {
            foreach (var jungleTarget in GameObjects.Jungle.Where(m => m.IsValidTarget(Q.Range)).ToList())
            { 
                if (!jungleTarget.IsValidTarget() ||
                    !GetGenericJungleMinionsTargets().Contains(jungleTarget))
                {
                    return;
                }
                bool useQ = Menu["farming"]["useq"].Enabled;
                bool useW = Menu["farming"]["usew"].Enabled;
                bool useE = Menu["farming"]["usee"].Enabled;
                float manapercent = Menu["farming"]["mana"].As<MenuSlider>().Value;
                if (manapercent < Player.ManaPercent())
                {
                    if (useQ && jungleTarget.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(jungleTarget);
                    }
                    if (useW && jungleTarget.IsValidTarget(W.Range) && !Player.HasBuff("AuraofDespair"))
                    {
                        W.Cast();
                    }
                    if (useE && jungleTarget.IsValidTarget(E.Range))
                    {
                        E.Cast(jungleTarget);
                    }
                }
            }
        }

    

       


     
       


        public static Obj_AI_Hero GetBestKillableHero(Spell spell, DamageType damageType = DamageType.True, bool ignoreShields = false)
        {
            return TargetSelector.Implementation.GetOrderedTargets(spell.Range).FirstOrDefault(t => t.IsValidTarget());
        }
        public static Obj_AI_Hero GetRGAP(DamageType damageType = DamageType.True, bool ignoreShields = false)
        {
            return TargetSelector.Implementation.GetOrderedTargets(Q.Range + R.Range).FirstOrDefault(t => t.IsValidTarget());
        }
        private void Killsteal()

        {
            float maxq = Menu["combo"]["maxrange"].As<MenuSlider>().Value;
            if (Q.Ready &&
                Menu["killsteal"]["ksq"].Enabled)
            {
                var bestTarget = GetBestKillableHero(Q, DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.Q) >= bestTarget.Health && bestTarget.IsValidTarget(maxq))
                {
                    Q.CastOnUnit(bestTarget);
                }
            }
            if (E.Ready && 
                Menu["killsteal"]["kse"].Enabled)
            {
                var bestTarget = GetBestKillableHero(E, DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.E) >= bestTarget.Health && bestTarget.IsValidTarget(E.Range))
                {
                    E.Cast(bestTarget);
                }
            }
            if (R.Ready &&
                Menu["killsteal"]["ksr"].Enabled)
            {
                var bestTarget = GetBestKillableHero(R, DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.R) >= bestTarget.Health && bestTarget.IsValidTarget(R.Range))
                {
                    R.Cast(bestTarget);
                }
            }
            if (Q.Ready &&
                Menu["killsteal"]["ksrgap"].Enabled)
            {
                var bestTarget = GetRGAP(DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.R) >= bestTarget.Health && bestTarget.Distance(Player) > R.Range)
                {
                    Q.Cast(bestTarget.Position);
                    
                }
                if (bestTarget != null && bestTarget.Distance(Player) <= R.Range && bestTarget != null && Player.GetSpellDamage(bestTarget, SpellSlot.R) >= bestTarget.Health)
                {
                    R.Cast();
                }
            }
        }

        public static Obj_AI_Hero GetBestEnemyHeroTarget()
        {
            return GetBestEnemyHeroTargetInRange(float.MaxValue);
        }
        
        public static Obj_AI_Hero GetBestEnemyHeroTargetInRange(float range)
        {
            var ts = TargetSelector.Implementation;
            var target = ts.GetTarget(range);
            if (target != null && target.IsValidTarget())
            {
                return target;
            }

            return ts.GetOrderedTargets(range).FirstOrDefault(t => target.IsValidTarget());
        }
        private void OnCombo()
        {
            bool useQ = Menu["combo"]["useq"].Enabled;
            bool useW = Menu["combo"]["usew"].Enabled;
            bool useE = Menu["combo"]["usee"].Enabled;
            bool useR = Menu["combo"]["user"].Enabled;
            int renemies = Menu["combo"]["enemies"].As<MenuSlider>().Value;
            float maxq = Menu["combo"]["maxrange"].As<MenuSlider>().Value;

            var target = GetBestEnemyHeroTargetInRange(Q.Range);
            if (!target.IsValidTarget())
            {
                return;
            }


            if (Q.Ready && useQ && target.IsValidTarget(maxq) )
            {
                if (target != null)
                {
                        Q.Cast(target);                }
            }
            if (W.Ready && useW && target.IsValidTarget(W.Range) && !Player.HasBuff("AuraofDespair"))
            {
                if (target != null)
                {
                    W.Cast();
                }
            }
            if (E.Ready && useE && target.IsValidTarget(E.Range))
            {
                if (target != null)
                {
                    E.Cast();
                }
            }
            if (R.Ready && useR && target.IsValidTarget(R.Range) && Player.CountEnemyHeroesInRange(R.Range) >= renemies)
            {
                if (target != null)
                {
                    R.Cast();
                }
            }
        }

        private void OnHarass()
        {
            bool useQ = Menu["harass"]["useq"].Enabled;
            bool useW = Menu["harass"]["usew"].Enabled;
            bool useE = Menu["harass"]["usee"].Enabled;
            float manapercent = Menu["harass"]["mana"].As<MenuSlider>().Value;
            var target = GetBestEnemyHeroTargetInRange(Q.Range);
            if (manapercent < Player.ManaPercent())
            {
                if (!target.IsValidTarget())
                {
                    return;
                }
                if (Q.Ready && useQ && target.IsValidTarget(Q.Range))
                {
                    if (target != null)
                    {
                        Q.CastOnUnit(target);
                    }
                }
                if (W.Ready && useW && target.IsValidTarget(W.Range) && !Player.HasBuff("AuraofDespair"))
                {
                    if (target != null)
                    {
                        W.Cast();
                    }
                }
                if (E.Ready && useE && target.IsValidTarget(E.Range))
                {
                    if (target != null)
                    {
                        E.Cast();
                    }
                }
            }
        }
    }
}