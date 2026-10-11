using System;

namespace DC.Core
{
    /// <summary>Bot na regiao 2 (CONTEUDO_REGIOES §11), com o mesmo handicap de reacao: sai do circulo da picareta, do monte, do pavio, da
    /// patada/pancada e da faixa da investida; sai do trilho com o sino e espera para cruzar; passa longe de quem dorme (se nao escolheu lutar);
    /// pousa fora do anel do sapo; contra a toupeira pousa na agua; na Rainha luta de dentro da poca; leva o cristal pela agua; acalma a
    /// gaiola com o dash quando nao ha ladrao; pousa o coracao como isca com 3+ perseguidores. Medicao em docs/BALANCE_REGIAO2.md.</summary>
    public sealed partial class Bot
    {
        /// <summary>Antes do plano geral: perigos da regiao 2 e as salas/cargas com regra propria. act: -1 = segue o plano geral, 0 = plano feito,
        /// 1 = dash.</summary>
        partial void PlanR2(Run r, ref int act)
        {
            Courier c = r.Courier;
            bool fragile = HoldsFragile(r) || r.Holds(CargoType.Cake);
            // 1) avisos no chao que pegam o courier: sair (dash so em cima da hora e sem carga que sofre com ele)
            foreach (Enemy e in r.Enemies)
            {
                R2Foe f = r.R2Of(e);
                if (!e.Alive || f == null || e.Def.Hazard) continue;
                V2 from = e.Pos; bool hit = false, side = false; V2 lane = f.Spot;
                switch (f.Act)
                {
                    case R2Act.Throw: case R2Act.Mound: from = f.Spot; hit = V2.Dist(c.Pos, f.Spot) <= f.Radius + Balance.CourierR + 0.35f; break;
                    case R2Act.Swipe: case R2Act.Slam: hit = V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach + 0.4f; break;
                    case R2Act.Fuse: hit = V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach + 0.5f; break;
                    case R2Act.ChargeAim: case R2Act.Charge: side = true; hit = V2.SegDist(c.Pos, e.Pos, lane) <= e.Def.Radius + Balance.CourierR + 0.5f; break;
                    case R2Act.Tongue: side = true; lane = e.Pos + f.Dir * e.Def.Reach; hit = f.Target == null && V2.SegDist(c.Pos, e.Pos, lane) <= Balance.CourierR + 0.35f; break;   // lingua no courier
                }
                if (!hit) continue;
                if (side)
                {
                    V2 n = (lane - e.Pos).Norm.Right;
                    if (V2.Dot(c.Pos - e.Pos, n) < 0f) n = n * -1f;
                    Go(Run.FreeNear(r.Room, c.Pos + n * 2f, Balance.CourierR));
                }
                else Go(Escape(r, c.Pos, from));
                act = e.Tele < 0.3f && c.DashReady && !fragile && f.Act != R2Act.Throw ? 1 : 0;
                return;
            }
            foreach (Rock k in r.Rocks)
                if (k.Fall > 0f && V2.Dist(c.Pos, k.Pos) <= Balance.RockHalf * 1.42f + Balance.CourierR + 0.2f) { Go(Escape(r, c.Pos, k.Pos)); act = 0; return; }
            // 2) trilho com o sino tocando ou o vagonete passando: sair de cima
            foreach (Enemy e in r.Enemies)
            {
                R2Foe f = r.R2Of(e);
                if (f == null || e.Type != EnemyType.Minecart || f.Rail < 0 || !CartComing(e, f, c.Pos)) continue;
                Rail rail = r.Room.Rails[f.Rail];
                float d = V2.SegDist(c.Pos, rail.A, rail.B);
                if (d > e.Def.Radius + Balance.CourierR + 0.6f) continue;
                Go(Run.FreeNear(r.Room, c.Pos + Off(c.Pos, rail) * (e.Def.Radius + Balance.CourierR + 1.0f - d), Balance.CourierR));
                act = 0; return;
            }
            // 3) Rainha: luta de dentro de uma poca (ela nao sai na agua); carga fora da agua: pegar de volta
            foreach (Enemy e in r.Enemies)
            {
                if (!e.Alive || e.Type != EnemyType.MoleQueen || r.Room.Water.Length == 0) continue;
                foreach (Cargo g in r.Cargos)
                    if (g.State == CargoState.Ground && !r.InWater(g.Pos)) { Go(g.Pos); act = 0; return; }
                V2 pool = NearestWater(r, c.Pos);
                if (V2.Dist(c.Pos, pool) <= 0.45f || (r.InWater(c.Pos) && r.Main.State == CargoState.Ground)) stop = true; else Go(pool);
                act = 0; return;
            }
            // 4) gaiola: o dash acalma o grifinho antes do grito, se o grito acordaria ou atrairia alguem e nao ha ladrao para levar a gaiola
            if (r.Holds(CargoType.GriffinCage) && r.CageLeft >= 0f && r.CageLeft <= Balance.CageWarn + Reaction && c.DashReady && Nearest(r, c.Pos, true) == null)
            {
                bool matters = r.Waves == 0 && r.Room.WaveTrigger != RoomTrigger.None && r.Contract.Waves;
                foreach (Enemy e in r.Enemies) if (e.Alive && !e.Def.Hazard && !e.Def.Locks && V2.Dist(e.Pos, c.Pos) <= Balance.CageRadius) matters = true;
                if (matters) { act = 1; return; }
            }
            // 5) cristal: recongelar na agua quando a sala tem agua perto e ninguem encosta
            Cargo ice = r.Main.Type == CargoType.IceCrystal ? r.Main : null;
            if (ice != null && ice.State != CargoState.Gone && !bored && r.Room.Water.Length > 0 && (Nearest(r, c.Pos) == null || V2.Dist(Nearest(r, c.Pos).Pos, c.Pos) > 3f))
            {
                float v = r.IceOf(ice);
                V2 pool = NearestWater(r, c.Pos);
                if (ice.State == CargoState.Held && v < 85f && V2.Dist(c.Pos, pool) <= 6f)
                {
                    if (V2.Dist(c.Pos, pool) <= 0.45f) stop = true; else Go(pool);
                    act = 0; return;
                }
                if (ice.State == CargoState.Ground && r.InWater(ice.Pos) && v < 97f && V2.Dist(c.Pos, ice.Pos) <= r.Sim.PickupRange) { stop = true; act = 0; return; }
            }
        }

        /// <summary>Depois do plano geral: onde pousar (fora do anel do sapo; na agua contra a toupeira e com o cristal), coracao como isca,
        /// toupeira fora = alvo, passar longe de quem dorme e esperar o vagonete para cruzar o trilho.</summary>
        partial void AfterR2(Run r, ref bool dash)
        {
            Courier c = r.Courier;
            if (dash || c.State == CourierState.Dashing) return;
            bool held = r.Main.State == CargoState.Held;
            if (stop && held)
            {
                foreach (Enemy e in r.Enemies)   // pousar no anel do sapo = a lingua puxa a carga
                    if (e.Alive && e.Type == EnemyType.BogFrog && V2.Dist(e.Pos, c.Pos) <= e.Def.Reach + Balance.PlantOffset + 0.6f && r.LineClear(e.Pos, c.Pos, 0.1f))
                    { Go(Run.FreeNear(r.Room, c.Pos + (c.Pos - e.Pos).Norm * 1.5f, Balance.CourierR)); return; }
                bool mole = false;
                foreach (Enemy e in r.Enemies) if (e.Alive && e.Type == EnemyType.Mole) mole = true;
                if ((mole || r.Holds(CargoType.IceCrystal)) && r.Room.Water.Length > 0 && !r.InWater(c.Pos) && !bored)
                {
                    V2 pool = NearestWater(r, c.Pos);
                    if (V2.Dist(c.Pos, pool) <= 4f) { Go(pool); return; }
                }
            }
            if (!stop && !bored)
            {
                Cargo heart = r.Main.Type == CargoType.ArcaneHeart && r.Main.State != CargoState.Gone ? r.Main : null;
                if (heart != null)
                {
                    int chasers = 0; V2 hp = heart.State == CargoState.Held ? c.Pos : heart.Pos;
                    foreach (Enemy e in r.Enemies) if (e.Alive && (e.Type == EnemyType.Bat || e.Type == EnemyType.Slime || e.Type == EnemyType.BombBeetle) && V2.Dist(e.Pos, hp) <= 6f) chasers++;
                    if (chasers >= 3 && (held || V2.Dist(c.Pos, heart.Pos) <= r.Sim.PickupRange)) { stop = true; return; }
                }
                foreach (Enemy e in r.Enemies)   // toupeira fora da terra (2 s): a janela para abater
                {
                    R2Foe f = r.R2Of(e);
                    if (e.Alive && f != null && e.Type == EnemyType.Mole && f.Act == R2Act.Up && r.CanHit(e) && V2.Dist(e.Pos, c.Pos) <= Balance.ShotRange - 0.5f) { stop = true; return; }
                }
            }
            if (stop) return;
            if (!fightRoom)   // dormindo: desviar para passar a mais de 2 m
                foreach (Enemy e in r.Enemies)
                {
                    if (!e.Alive || !e.Asleep || V2.Dist(e.Pos, c.Pos) > Balance.WakeRange + 1.0f) continue;
                    V2 to = (goal - c.Pos).Norm, away = (c.Pos - e.Pos).Norm;
                    if (V2.Dot(to, e.Pos - c.Pos) <= 0f) continue;   // ja esta passando para longe
                    Go(Run.FreeNear(r.Room, c.Pos + (to + away * 1.3f).Norm * 1.5f, Balance.CourierR));
                    return;
                }
            foreach (Enemy e in r.Enemies)   // trilho perigoso entre o courier e o destino: esperar o vagonete passar
            {
                R2Foe f = r.R2Of(e);
                if (f == null || e.Type != EnemyType.Minecart || f.Rail < 0 || !CartComing(e, f, c.Pos)) continue;
                Rail rail = r.Room.Rails[f.Rail];
                V2 n = (rail.B - rail.A).Norm.Right;
                float a = V2.Dot(c.Pos - rail.A, n), b = V2.Dot(goal - rail.A, n);
                if (a * b < 0f && Math.Abs(a) <= e.Def.Radius + Balance.CourierR + 1.1f) { stop = true; return; }
            }
        }

        /// <summary>O vagonete vai passar por p: sino tocando, ou passando sem ter passado de p ainda.</summary>
        static bool CartComing(Enemy cart, R2Foe f, V2 p)
        {
            if (f.Act == R2Act.Bell) return true;
            return f.Act == R2Act.Pass && V2.Dot(p - cart.Pos, f.Dir) > -(cart.Def.Radius + Balance.CourierR + 0.3f);
        }

        /// <summary>Direcao para fora do trilho, do lado em que p esta.</summary>
        static V2 Off(V2 p, Rail rail)
        {
            V2 n = (rail.B - rail.A).Norm.Right;
            return V2.Dot(p - rail.A, n) >= 0f ? n : n * -1f;
        }

        /// <summary>Centro da poca mais perto (ou p se a sala nao tem agua).</summary>
        static V2 NearestWater(Run r, V2 p)
        {
            V2 best = p; float bd = float.MaxValue;
            foreach (Rect w in r.Room.Water) { float d = V2.Dist(p, w.C); if (d < bd) { bd = d; best = w.C; } }
            return best;
        }
    }
}
