namespace Brand
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

    internal class Brand
    {
        public static Menu Menu = new Menu("pBrand", "pBrand", true);

        public static Orbwalker Orbwalker = new Orbwalker();

        public static Obj_AI_Hero Player = ObjectManager.GetLocalPlayer();

        public static Spell Q, W, E, R;
        public void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 1050);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R, 750);

            Q.SetSkillshot(0.25f, 0.60f, 1550f, true, SkillshotType.Line);
            W.SetSkillshot(0.25f, 0.50f, float.MaxValue, false, SkillshotType.Circle);
        }

        public Brand()
        {
            Orbwalker.Attach(Menu);
            var ComboMenu = new Menu("combo", "Combo");
            {
                ComboMenu.Add(new MenuBool("usee", "Use E in combo"));
                ComboMenu.Add(new MenuBool("usew", "Use W in combo"));
                ComboMenu.Add(new MenuList("comboselect", "Combo order: ", new[] {"Dynamic"}, 0));
            }
            Menu.Add(ComboMenu);
            var QSet = new Menu("qset", "Q Settings");
            {
                QSet.Add(new MenuBool("useq", "Use Q in combo"));
                QSet.Add(new MenuBool("useqablaze", "Use Q only to stun", false));
                QSet.Add(new MenuSlider("maxrange", "Q Max range: ", 1000, 0, 1050));
            }
            ComboMenu.Add(QSet);
            var RSet = new Menu("rset", "R Settings");
            {
                RSet.Add(new MenuBool("user", "Use R in combo"));
                RSet.Add(new MenuBool("rkill", "Only R when killable by combo (Will cast on only 1 target if killable)"));
                RSet.Add(new MenuSlider("rmin", "Only R when >= X champions", 1, 1, 5));
                RSet.Add(new MenuSlider("rhealth", "Only enemy HP % >", 40, 1, 100));
                RSet.Add(new MenuKeyBind("teamfight", "Teamfight mode (Will always R if > X champions set above, regardless of HP)", KeyCode.T, KeybindType.Toggle));
            }
            ComboMenu.Add(RSet);
            var HarassMenu = new Menu("harass", "Harass");
            {
                HarassMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                HarassMenu.Add(new MenuBool("useq", "Use Q"));
                HarassMenu.Add(new MenuBool("usew", "Use W"));
                HarassMenu.Add(new MenuBool("usee", "Use E "));
            }
            Menu.Add(HarassMenu);
            var FarmMenu = new Menu("farming", "Lane Clear");
            {
                FarmMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                FarmMenu.Add(new MenuBool("useq", "Use Q", false));
                FarmMenu.Add(new MenuBool("usew", "Use W"));
                FarmMenu.Add(new MenuBool("usee", "Use E"));
            }
            Menu.Add(FarmMenu);
            var Jungle = new Menu("jungle", "Jungle Clear");
            {
                Jungle.Add(new MenuSlider("mana", "Mana Manager", 50));
                Jungle.Add(new MenuBool("useq", "Use Q"));
                Jungle.Add(new MenuBool("usew", "Use W"));
                Jungle.Add(new MenuBool("usee", "Use E"));
                
            }
            Menu.Add(Jungle);
            var KSMenu = new Menu("killsteal", "Killsteal");
            {
                KSMenu.Add(new MenuBool("ksq", "Killsteal with Q"));
                KSMenu.Add(new MenuBool("kse", "Killsteal with E"));
                KSMenu.Add(new MenuBool("ksr", "Killsteal with R", false));
            }
            Menu.Add(KSMenu);
            var DrawMenu = new Menu("drawings", "Drawings");
            {
                DrawMenu.Add(new MenuBool("drawq", "Draw Q Range"));
                DrawMenu.Add(new MenuBool("draww", "Draw W Range"));
                DrawMenu.Add(new MenuBool("drawe", "Draw E Range"));
                DrawMenu.Add(new MenuBool("drawr", "Draw R Range"));
                DrawMenu.Add(new MenuBool("drawDamage", "Draw Damage"));
                DrawMenu.Add(new MenuBool("drawautostun", "Draw Auto-Stun Range"));
                DrawMenu.Add(new MenuBool("drawtf", "Draw Teamfight Mode"));
            }
            Menu.Add(DrawMenu);
            var MiscMenu = new Menu("misc", "Misc.");
            {
                MiscMenu.Add(new MenuBool("autostun", "Auto-Stun"));
                MiscMenu.Add(new MenuSlider("autostunrange", "^ Auto-Stun closest enemy within X range", 300, 0, 600));
            }
            Menu.Add(MiscMenu);

            Menu.Attach();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            //Events.OnInterruptableTarget += OnInterruptableTarget;


            LoadSpells();
            Console.WriteLine("pBrand by Prickachu - Loaded");
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
            Vector2 maybeworks;
            var heropos = Render.WorldToScreen(Player.Position, out maybeworks);
            var xaOffset = (int)maybeworks.X;
            var yaOffset = (int)maybeworks.Y;

            if (Menu["drawings"]["drawq"].Enabled)
            {
                Render.Circle(Player.Position, Menu["combo"]["qset"]["maxrange"].As<MenuSlider>().Value, 50, Color.White);
            }

            if (Menu["drawings"]["drawe"].Enabled)
            {
                Render.Circle(Player.Position, E.Range, 50, Color.Violet);
            }
            if (Menu["drawings"]["draww"].Enabled)
            {
                Render.Circle(Player.Position, W.Range, 50, Color.Black);
            }
            if (Menu["drawings"]["drawr"].Enabled)
            {
                Render.Circle(Player.Position, R.Range, 50, Color.Yellow);
            }
            if (Menu["drawings"]["drawautostun"].Enabled)
            {
                Render.Circle(Player.Position, Menu["misc"]["autostunrange"].As<MenuSlider>().Value, 50, Color.Red);
            }
            if (Menu["drawings"]["drawtf"].Enabled)
            {
                if (Menu["combo"]["rset"]["teamfight"].Enabled)
                {
                    Render.Text(xaOffset - 50, yaOffset + 10, Color.Lime, "Teamfight mode: ON",
                        RenderTextFlags.VerticalCenter);
                }
                if (!Menu["combo"]["rset"]["teamfight"].Enabled)
                {
                    Render.Text(xaOffset - 50, yaOffset + 10, Color.Red, "Teamfight mode: OFF",
                        RenderTextFlags.VerticalCenter);
                }
            }
            if (Menu["drawings"]["drawDamage"].Enabled)
            {

                ObjectManager.Get<Obj_AI_Base>()
                    .Where(h => h is Obj_AI_Hero && h.IsValidTarget() && h.IsValidTarget(Q.Range + R.Range))
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
                            double Qdamage = Player.GetSpellDamage(unit, SpellSlot.Q);
                            double Wdamage = Player.GetSpellDamage(unit, SpellSlot.W);
                            double Edamage = Player.GetSpellDamage(unit, SpellSlot.E);
                            double Rdamage = Player.GetSpellDamage(unit, SpellSlot.R);
                            double totalDmg = Qdamage + Wdamage + Edamage + Rdamage;
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

            public void autoStun()
        {
            float range = Menu["misc"]["autostunrange"].As<MenuSlider>().Value;
            var bestTarget = TargetSelector.Implementation.GetOrderedTargets(range).FirstOrDefault(t => t.IsValidTarget());
            if (bestTarget.IsValidTarget(range) && Q.Ready && E.Ready)
            {
                if (E.Ready)
                {
                    E.CastOnUnit(bestTarget);
                }
                if (Q.Ready)
                {
                    Q.Cast(bestTarget);
                }
            }
            
        }
        private void Game_OnUpdate()
        {
            if (Player.IsDead || MenuGUI.IsChatOpen())
            {
                return;
            }

            if (Menu["misc"]["autostun"].Enabled)
            {
                autoStun();
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
            return GameObjects.EnemyMinions.Where(m => m.IsValidTarget(range) && m.UnitSkinName.Contains("Minion") && !m.UnitSkinName.Contains("Odin")).ToList();
        }

        private void Clearing()
        {
            bool useQ = Menu["farming"]["useq"].Enabled;
            bool useW = Menu["farming"]["usew"].Enabled;
            bool useE = Menu["farming"]["usee"].Enabled;
            float manapercent = Menu["farming"]["mana"].As<MenuSlider>().Value;
            if (manapercent < Player.ManaPercent())
            {
                if (useW)
                {

                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(W.Range))
                    {

                        if (minion.IsValidTarget(W.Range) && minion != null)
                        {

                            W.Cast(minion);
                        }
                    }
                }

                if (useE)
                {
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(E.Range))
                    {
                        if (minion.IsValidTarget(E.Range) && minion != null && minion.HasBuff("BrandAblaze"))
                        {
                            E.CastOnUnit(minion);
                        }
                    }
                }

                if (useQ)
                {
                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                    {
                        if (minion.IsValidTarget(Q.Range) && minion != null)
                        {
                            Q.Cast(minion);
                        }
                    }
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
                    !GetGenericJungleMinionsTargets().Contains(jungleTarget) || ImplementationClass.IOrbwalker.IsWindingUp || !jungleTarget.IsValidSpellTarget())
                {
                    return;
                }
                bool useQ = Menu["jungle"]["useq"].Enabled;
                bool useW = Menu["jungle"]["usew"].Enabled;
                bool useE = Menu["jungle"]["usee"].Enabled;
                float manapercent = Menu["jungle"]["mana"].As<MenuSlider>().Value;
                if (manapercent < Player.ManaPercent())
                {
                    if (useW && jungleTarget.IsValidTarget(W.Range))
                    {
                        W.Cast(jungleTarget);
                    }
                    if (useE && jungleTarget.IsValidTarget(E.Range) && jungleTarget.HasBuff("BrandAblaze"))
                    {
                        E.CastOnUnit(jungleTarget);
                    }
                    if (useQ && jungleTarget.IsValidTarget(Q.Range))
                    {
                        Q.Cast(jungleTarget);
                    }
                }
            }
        }





        public static List<Obj_AI_Minion> GetAllGenericMinionsTargets()
        {
            return GetAllGenericMinionsTargetsInRange(float.MaxValue);
        }

        public static List<Obj_AI_Minion> GetAllGenericMinionsTargetsInRange(float range)
        {
            return GetEnemyLaneMinionsTargetsInRange(range).Concat(GetGenericJungleMinionsTargetsInRange(range)).ToList();
        }

        public static List<Obj_AI_Base> GetAllGenericUnitTargets()
        {
            return GetAllGenericUnitTargetsInRange(float.MaxValue);
        }

        public static List<Obj_AI_Base> GetAllGenericUnitTargetsInRange(float range)
        {
            return GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(range)).Concat<Obj_AI_Base>(GetAllGenericMinionsTargetsInRange(range)).ToList();
        }




        public static Obj_AI_Hero GetBestKillableHero(Spell spell, DamageType damageType = DamageType.True, bool ignoreShields = false)
        {
            return TargetSelector.Implementation.GetOrderedTargets(spell.Range).FirstOrDefault(t => t.IsValidTarget());
        }
 
        private void Killsteal()
            
        {
            float maxq = Menu["combo"]["qset"]["maxrange"].As<MenuSlider>().Value;
            if (Q.Ready &&
                Menu["killsteal"]["ksq"].Enabled)
            {
                var bestTarget = GetBestKillableHero(Q, DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.Q) >= bestTarget.Health && bestTarget.IsValidTarget(maxq))
                {
                    Q.Cast(bestTarget);
                }
            }
            if (E.Ready &&
                Menu["killsteal"]["kse"].Enabled)
            {
                var bestTarget = GetBestKillableHero(E, DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.E) >= bestTarget.Health && bestTarget.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(bestTarget);
                }
            }
            if (R.Ready &&
                Menu["killsteal"]["ksr"].Enabled)
            {
                var bestTarget = GetBestKillableHero(R, DamageType.Magical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.R) >= bestTarget.Health && bestTarget.IsValidTarget(R.Range))
                {
                    R.CastOnUnit(bestTarget);
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
            bool useQ = Menu["combo"]["qset"]["useq"].Enabled;
            bool useW = Menu["combo"]["usew"].Enabled;
            bool useE = Menu["combo"]["usee"].Enabled;
            bool useR = Menu["combo"]["rset"]["user"].Enabled;
            bool teamfight = Menu["combo"]["rset"]["teamfight"].Enabled;
            bool useRKillable = Menu["combo"]["rset"]["rkill"].Enabled;
            bool onlystun = Menu["combo"]["qset"]["useqablaze"].Enabled;
            int renemies = Menu["combo"]["rset"]["rmin"].As<MenuSlider>().Value;
            int rminhealth = Menu["combo"]["rset"]["rmin"].As<MenuSlider>().Value;
            float maxq = Menu["combo"]["qset"]["maxrange"].As<MenuSlider>().Value;


            var target = GetBestEnemyHeroTargetInRange(Q.Range);

            if (!target.IsValidTarget())
            {
                return;
            }


            if (target == null)
            {
                return;
            }
            double Qdamage = Player.GetSpellDamage(target, SpellSlot.Q);
            double Wdamage = Player.GetSpellDamage(target, SpellSlot.W);
            double Edamage = Player.GetSpellDamage(target, SpellSlot.E);
            double Rdamage = Player.GetSpellDamage(target, SpellSlot.R);
            double totalDmg = Qdamage + Wdamage + Edamage + Rdamage;

            //Dynamic combo
            //If enemy in range of e
            if (target.IsValidTarget(E.Range) && useE)
            {

                if (R.Ready && //r ready
                    useR &&  //use r in combo
                    useRKillable && //only r if killable
                    target.Health < totalDmg && // if killable
                    Q.Ready && //q ready
                    E.Ready && //e ready
                    W.Ready && //w ready
                    target.HealthPercent() > rminhealth // make sure hp > min health slider
                   || //OR if teamfight is enabled
                    R.Ready && //r ready
                    teamfight && //teamfight enabled
                    target.CountEnemyHeroesInRange(750) >= renemies) //and > renemies
                {
                    R.CastOnUnit(target);
                }

                if (E.Ready)
                {
                    E.CastOnUnit(target);
                }

                var meow = Q.GetPrediction(target);
                var collisions = (IList<Obj_AI_Base>)meow.CollisionObjects;

                if (!Q.Ready || !onlystun ||
                        collisions.Any()) //if onlystun is off and or q is down and or q collision exists
                {
                   

                    if (R.Ready && //r ready    
                        useR && //r option on
                        useRKillable && //r killable option on
                        target.Health < totalDmg && //and is actually killable
                        Q.Ready && //spell checks
                        E.Ready &&
                        W.Ready &&
                        target.HealthPercent() > rminhealth && //min health check
                        target.CountEnemyHeroesInRange(750) >= renemies //has to be this many enemies in area
                        ||
                        R.Ready && //r check
                        teamfight && //teamfight mode
                        target.CountEnemyHeroesInRange(750) >= renemies) //if > x enemies
                    {
                        R.CastOnUnit(target);
                    }
                    if (Q.Ready &&
                        target.IsValidTarget(maxq))
                    {
                        if (onlystun && !target.HasBuff("BrandAblaze"))
                        {
                            return;
                        }
                        else
                            Q.Cast(target);
                    }
                   
                } 

                if (E.Ready)
                {
                    E.CastOnUnit(target);
                }

                if (target.HasBuff("BrandAblaze"))
                {
                    Q.Cast(target);
                }

                if (W.Ready &&
                            target.IsValidTarget(W.Range) && useW)
                {
                    W.Cast(target);
                }

                if (R.Ready && useR && useRKillable && target.Health < totalDmg && Q.Ready && E.Ready && W.Ready && target.HealthPercent() > rminhealth && target.CountEnemyHeroesInRange(750) >= renemies || R.Ready && teamfight && target.CountEnemyHeroesInRange(750) >= renemies)
                {
                    R.CastOnUnit(target);
                }
                //If it does ->
                else
                {
                    if (W.Ready &&
                        target.IsValidTarget(W.Range) && useW)
                    {
                        W.Cast(target);
                    }

                    if (Q.Ready &&
                        target.IsValidTarget(maxq))
                    {
                        if (onlystun && !target.HasBuff("BrandAblaze"))
                        {
                            return;
                        }
                        else
                            Q.Cast(target);
                    }
                    if (R.Ready && useR && useRKillable && target.Health < totalDmg && Q.Ready && E.Ready && W.Ready && target.HealthPercent() > rminhealth && target.CountEnemyHeroesInRange(750) >= renemies || R.Ready && teamfight && target.CountEnemyHeroesInRange(750) >= renemies)
                    {
                        R.CastOnUnit(target);
                    }
                }

            }
            if (target.IsValidTarget(W.Range) && W.Ready)
            {
                W.Cast(target);
            }
            if (onlystun && target.HasBuff("BrandAblaze"))
            {
                if (target.IsValidTarget(Q.Range) && Q.Ready)
                Q.Cast(target);
            }
            else
                 if (target.IsValidTarget(Q.Range) && Q.Ready && !onlystun)
                Q.Cast(target);
            if (R.Ready && useR && target.IsValidTarget(R.Range) && useRKillable && target.Health < totalDmg && Q.Ready && E.Ready && W.Ready && target.HealthPercent() > rminhealth && target.CountEnemyHeroesInRange(750) >= renemies || R.Ready && teamfight && target.CountEnemyHeroesInRange(750) >= renemies)
            {
                R.CastOnUnit(target);
            }

        }
            

        


        private void OnHarass()
        {
            bool useQ = Menu["harass"]["useq"].Enabled;
            bool useW = Menu["harass"]["usew"].Enabled;
            bool useE = Menu["harass"]["usee"].Enabled;
            bool onlystun = Menu["combo"]["qset"]["useqablaze"].Enabled;
            float manapercent = Menu["harass"]["mana"].As<MenuSlider>().Value;
            var target = GetBestEnemyHeroTargetInRange(Q.Range);
            float maxq = Menu["combo"]["qset"]["maxrange"].As<MenuSlider>().Value;
            if (manapercent < Player.ManaPercent())
            {
                if (!target.IsValidTarget())
                {
                    return;
                }
                //combo when enemy in range of e
                if (target == null)
                {
                    return;
                }

                var meow = Q.GetPrediction(target);
                var collisions = (IList<Obj_AI_Base>)meow.CollisionObjects;
                //Dynamic combo
                //If enemy in range of e
                if (target.IsValidTarget(E.Range) && useE)
                {
                    if (E.Ready)
                    {
                        E.CastOnUnit(target);
                    }

                    if (!Q.Ready || !onlystun ||
                        collisions.Any())
                    {
                        return;
                    }

                    if (E.Ready)
                    {
                        E.CastOnUnit(target);
                    }

                    if (target.HasBuff("BrandAblaze"))
                    {
                        Q.Cast(target);
                    }

                    if (W.Ready &&
                        target.IsValidTarget(W.Range))
                    {
                        W.Cast(target);
                    }
                    
                }   //If it does ->
                else
                {
                    if (W.Ready &&
                        target.IsValidTarget(W.Range))
                    {
                        W.Cast(target);
                    }

                    if (Q.Ready &&
                        target.IsValidTarget(maxq))
                    {
                        if (onlystun && !target.HasBuff("BrandAblaze"))
                        {
                            return;
                        }
                        else
                            Q.Cast(target);
                    }
                    
                }
            }
        }
    }
}
