namespace Evelynn
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

    internal class Evelynn
    { 
        public static Menu Menu = new Menu("pEvelynn", "pEvelynn", true);

        public static Orbwalker Orbwalker = new Orbwalker();

        public static Obj_AI_Hero Player = ObjectManager.GetLocalPlayer();

        public static Spell Q, W, E, R;
        public void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 500);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, 290);
            R = new Spell(SpellSlot.R, 650);

            R.SetSkillshot(0.25f, 250f, float.MaxValue, false, SkillshotType.Circle);
        }

        public Evelynn()
        {
            Orbwalker.Attach(Menu);
            var WSet = new Menu("wset", "W Settings");
            {
                WSet.Add(new MenuBool("usew", "Use W"));
                WSet.Add(new MenuBool("usewslow", "^ Only W slows"));
                WSet.Add(new MenuBool("usewslowcombo", "^ Only W slows when in combo/flee"));
                WSet.Add(new MenuBool("usewengage", "Use W at end of stealth range in combo"));
            }
            var ComboMenu = new Menu("combo", "Combo");
            {

                ComboMenu.Add(new MenuBool("useq", "Use Q"));
                ComboMenu.Add(WSet);
                ComboMenu.Add(new MenuBool("usee", "Use E "));
                ComboMenu.Add(new MenuBool("user", "Use R"));
                ComboMenu.Add(new MenuSlider("enemies", "Use R if enemies >= X ", 1, 1, 5));
            }
          
            Menu.Add(ComboMenu);
            var HarassMenu = new Menu("harass", "Harass");
            {
                HarassMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                HarassMenu.Add(new MenuBool("useq", "Use Q"));
                HarassMenu.Add(new MenuBool("usee", "Use E "));

            }
            Menu.Add(HarassMenu);
            var FarmMenu = new Menu("farming", "Farming");
            {
                FarmMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                FarmMenu.Add(new MenuBool("useq", "Use Q"));
                FarmMenu.Add(new MenuBool("usee", "Use E"));
 
         
            }
            Menu.Add(FarmMenu);
            var KSMenu = new Menu("killsteal", "Killsteal");
            {
                KSMenu.Add(new MenuBool("ksq", "Killsteal with Q"));
                KSMenu.Add(new MenuBool("kse", "Killsteal with E"));
            }
            Menu.Add(KSMenu);
            var FleeMenu = new Menu("flee", "Flee");
            {
                FleeMenu.Add(new MenuBool("fleew", "Use W to Flee"));
                FleeMenu.Add(new MenuBool("fleewslow", "^ Only use W if slowed"));
                FleeMenu.Add(new MenuKeyBind("key", "Flee Key:", KeyCode.Z, KeybindType.Press));
            }
            Menu.Add(FleeMenu);
            var DrawMenu = new Menu("drawings", "Drawings");
            {
                DrawMenu.Add(new MenuBool("drawq", "Draw Q Range"));
                DrawMenu.Add(new MenuBool("drawe", "Draw E Range"));
                DrawMenu.Add(new MenuBool("drawr", "Draw R Range"));
               DrawMenu.Add(new MenuBool("drawDamage", "Draw Damage"));
            }
            Menu.Add(DrawMenu);
            var MiscMenu = new Menu("misc", "Misc.");
            {
                MiscMenu.Add(new MenuBool("smartw", "Auto W on slow"));
                MiscMenu.Add(new MenuBool("smartwflee", "Only auto W in flee mode"));
            }
            Menu.Add(MiscMenu);
 
            Menu.Attach();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            //Events.OnInterruptableTarget += OnInterruptableTarget;

      
            LoadSpells();
            Console.WriteLine("pEvelynn by Prickachu - Loaded");
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
                Render.Circle(Player.Position, Q.Range, 50, Color.Crimson);
            }

            if (Menu["drawings"]["drawe"].Enabled)
            {
                Render.Circle(Player.Position, E.Range, 50, Color.LightGreen);
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
            bool useQ = Menu["combo"]["useq"].Enabled;
            bool useW = Menu["combo"]["wset"]["usew"].Enabled;
            bool wengage = Menu["combo"]["wset"]["usewengage"].Enabled;
            bool useE = Menu["combo"]["usee"].Enabled;
            bool useR = Menu["combo"]["user"].Enabled;
            bool onlywslow = Menu["combo"]["wset"]["usewslow"].Enabled;
            bool onlywslowcombo = Menu["combo"]["wset"]["usewslowcombo"].Enabled;
            int renemies = Menu["combo"]["enemies"].As<MenuSlider>().Value;

            if (Player.IsDead || MenuGUI.IsChatOpen())
            {
                return;
            }

            if (Menu["misc"]["smartw"].Enabled && useW && !onlywslowcombo)
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
            if (Menu["flee"]["key"].Enabled)
            {
                Flee();
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
        private void Flee()
        {
            Player.IssueOrder(OrderType.MoveTo, Game.CursorPos);
            bool usew = Menu["flee"]["fleew"].Enabled;
            bool usewslow = Menu["flee"]["fleewslow"].Enabled;

            if (usew && !usewslow)
            {
                W.Cast();
            }

            if (usew && usewslow && Player.HasBuffOfType(BuffType.Slow))
            {
                W.Cast();
            }
        }
            private void Clearing()
        {
            bool useQ = Menu["farming"]["useq"].Enabled;
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
                            Q.Cast();
                        }
                    }
                }
                if (useE)
                {
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(E.Range))
                    {

                        if (minion.IsValidTarget(E.Range) && minion != null && !ImplementationClass.IOrbwalker.IsWindingUp)
                        {
                            E.CastOnUnit(minion);
                        }
                    }
                }


            }
        }
        public void autoW()
        {
            if (Player.HasBuffOfType(BuffType.Slow))
            {
                    W.Cast();
                
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
                bool useE = Menu["farming"]["usee"].Enabled;
                float manapercent = Menu["farming"]["mana"].As<MenuSlider>().Value;
                if (manapercent < Player.ManaPercent())
                {
                    if (useQ && jungleTarget.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(jungleTarget);
                    }
                    if (useE && jungleTarget.IsValidTarget(E.Range) && !ImplementationClass.IOrbwalker.IsWindingUp)
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
            if (Q.Ready &&
                Menu["killsteal"]["ksq"].Enabled)
            {
                var bestTarget = GetBestKillableHero(Q, DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.Q) >= bestTarget.Health && bestTarget.IsValidTarget(Q.Range))
                {
                    Q.Cast();
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
            bool useW = Menu["combo"]["wset"]["usew"].Enabled;
            bool wengage = Menu["combo"]["wset"]["usewengage"].Enabled;
            bool useE = Menu["combo"]["usee"].Enabled;
            bool useR = Menu["combo"]["user"].Enabled;
            bool onlywslow = Menu["combo"]["wset"]["usewslow"].Enabled;
            bool onlywslowcombo = Menu["combo"]["wset"]["usewslowcombo"].Enabled;
            int renemies = Menu["combo"]["enemies"].As<MenuSlider>().Value;

            var target = GetBestEnemyHeroTargetInRange(R.Range + 200);
            if (!target.IsValidTarget())
            {
                return;
            }
            if (W.Ready && useW && target.IsValidTarget(700) && !target.IsValidTarget(Player.GetFullAttackRange(target) + 100) && wengage && !onlywslow && !ImplementationClass.IOrbwalker.IsWindingUp)
            {
                W.Cast();
            }

            if (W.Ready && onlywslowcombo && Player.HasBuffOfType(BuffType.Slow) && !ImplementationClass.IOrbwalker.IsWindingUp)
            {
                W.Cast();
            }

            if (R.Ready && useR)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(t => t.IsEnemy && t.IsValidTarget(R.Range)))
                {
                    if (enemy.CountEnemyHeroesInRange(R.Width) >= renemies)
                    {
                        R.Cast(enemy);
                    }
                }
            }
            if (Q.Ready && useQ && target.IsValidTarget(Q.Range) )
            {
                if (target != null)
                {
                        Q.Cast();                }
            }
            if (E.Ready && useE && target.IsValidTarget(E.Range) && !ImplementationClass.IOrbwalker.IsWindingUp)
            {
                if (target != null)
                {
                    E.CastOnUnit(target);
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