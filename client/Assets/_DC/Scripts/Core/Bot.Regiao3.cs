using System;

namespace DC.Core
{
    /// <summary>Bot na regiao 3 (CONTEUDO_REGIOES §11) e a medicao da campanha da regiao (docs/BALANCE_REGIAO3.md). Mesmo handicap do bot
    /// da regiao 1 (reacao 0,25 s): sai das faixas e circulos de aviso; com o espelho (ate 1 rachadura) encara o arqueiro, o cuspe e o raio;
    /// leva a lanterna apagada a tocha; pega o balao antes de ~2,3 s no chao; leva a carga ate a pilha de ossos (a 3 m o esqueleto nao volta) e
    /// espera o esqueleto chegar perto antes de pousar; agarrado pela mumia pousa e luta (dash com pouca vida); no escuro chega perto do
    /// Guarda/chefe fora da luz ate ilumina-lo. ponytail: regras, nao busca (nao desvia de teia nem de gargula no caminho).</summary>
    public sealed partial class Bot
    {
        /// <summary>Estado estimado do fim da regiao 2 (a raia R2 mede a regiao 2 em paralelo; estimativa do CONTEUDO_REGIOES §8/§10 + BALANCE.md):
        /// guilda 8/8/8/8, armas no nivel 3, 97 estrelas (3 por contrato em ordem, so nas regioes 1-2), Botas de lama destravadas (3 projetos da
        /// regiao 2) no nivel 5 e equipadas, 1 projeto da Bomba de fumaca (R2-7) e Region3StartCoins moedas paradas: 1.089 do fim da regiao 1
        /// + ~4 x 3.768 de pagamento da regiao 2 - 4.237 da guilda - 2.100 das armas - 6.800 das Botas 2-5 = ~3.000.</summary>
        public static string Regiao3StartSave(int seed, int coins = Region3StartCoins)
        {
            RegionDef reg = RegionDef.Of(Regiao3.Id);
            var st = new char[reg.First]; int left = Math.Min(Region3StartStars, 3 * reg.First);
            for (int i = 0; i < st.Length; i++) { int k = Math.Min(3, left); st[i] = (char)('0' + k); left -= k; }
            return "v=3\nseed=" + seed + "\ncoins=" + coins + "\nrep=" + reg.First + "\nlv=8,8,8,8\nstars=" + new string(st) + "\nwlv=3,3,3\nwpn=0\n"
                   + "gear=" + (int)Gear.Boots + ",0,1,0,0,3,0,0,0,0," + Balance.GearMaxLevel + "\n";
        }
        public const int Region3StartCoins = 3000, Region3StartStars = 97;

        /// <summary>`n` campanhas da regiao 3 (sementes base..) partindo de Regiao3StartSave, com compras como o PlayCampaign.</summary>
        public static Campaign[] PlayRegion3(int n, int baseSeed = 6100)
        {
            var a = new Campaign[n];
            for (int s = 0; s < n; s++) a[s] = PlayCampaign(baseSeed + s, region: Regiao3.Id, from: Regiao3StartSave(baseSeed + s), maxAttempts: 80);
            return a;
        }

        /// <summary>Tabela realista da regiao 3: cada contrato medido `seeds` vezes a partir do save de antes da 1a tentativa dele em cada campanha.</summary>
        public static Stats[] MeasureRegion3Table(Campaign[] camps, int seeds)
        {
            RegionDef reg = RegionDef.Of(Regiao3.Id);
            var t = new Stats[reg.Contracts.Length];
            for (int c = 0; c < t.Length; c++) t[c] = new Stats { Contract = reg.First + c };
            for (int k = 0; k < camps.Length; k++)
                for (int c = 0; c < t.Length; c++)
                {
                    string b = camps[k].Before[reg.First + c];
                    if (b != null) MeasureFrom(b, reg.First + c, seeds, 1000 + 100 * k, 0.5f, t[c]);
                }
            return t;
        }

        static Enemy ByIdR3(Run r, int id) { foreach (Enemy e in r.Enemies) if (e.Id == id && e.Alive) return e; return null; }
        /// <summary>Espelho nas maos com ate 1 rachadura: vale encarar (cada reflexo racha 1).</summary>
        static bool MirrorOk(Run r) { foreach (Cargo g in r.Cargos) if (g.State == CargoState.Held && g.Type == CargoType.Mirror && g.Cracks <= 1) return true; return false; }

        partial void PlanR3(Run r, ref int act)
        {
            Courier c = r.Courier;
            // a) agarrado pela mumia: pousa e luta (2 golpes soltam); com pouca vida, dash para longe (larga a carga)
            if (r.GrabbedBy != null)
            {
                if (c.Hp <= 2 && c.DashReady && !r.Holds(CargoType.Cake)) { Go(c.Pos + (c.Pos - r.GrabbedBy.Pos).Norm * 3f); act = 1; return; }
                stop = true; act = 0; return;
            }
            // b) balao no chao: pegar antes de escapar
            foreach (Cargo g in r.Cargos) if (g.State == CargoState.Ground && g.Type == CargoType.Balloon && g.Fuse >= BalloonGrab) { Go(g.Pos); act = 0; return; }
            // c) avisos no chao: sair da faixa (gargula, cuspe, leque, raio) e do circulo da Viuva; com o espelho, encarar o cuspe e o raio
            foreach (Warn w in r.Warns)
            {
                if (w.Kind == WarnKind.Circle)
                {
                    if (V2.Dist(c.Pos, w.From) > w.Radius + Balance.CourierR + 0.3f) continue;
                    V2 away = (c.Pos - w.From).Norm; if (away.Len < 0.5f) away = new V2(0f, -1f);
                    Go(Run.FreeNear(r.Room, w.From + away * (w.Radius + 1.3f), Balance.CourierR)); act = 0; return;
                }
                if (w.Kind != WarnKind.Strip || V2.SegDist(c.Pos, w.From, w.To) > w.Width * 0.5f + Balance.CourierR + 0.3f) continue;
                Enemy o = ByIdR3(r, w.Owner);
                if (MirrorOk(r) && o != null && o.Type != EnemyType.Gargoyle) { Go(w.From); act = 0; return; }   // de frente: o espelho devolve
                V2 side = (w.To - w.From).Norm.Right; if (V2.Dot(c.Pos - w.From, side) < 0f) side = side * -1f;
                Go(Run.FreeNear(r.Room, c.Pos + side * (w.Width * 0.5f + 1.4f), Balance.CourierR)); act = 0; return;
            }
            // c2) espelho ou lanterna (no escuro) nas maos e golpe corpo a corpo chegando: sair andando (o dash larga: racha / apaga)
            if (r.Holds(CargoType.Mirror) || (r.Dark && r.Holds(CargoType.Lantern)))
                foreach (Enemy e in r.Enemies)
                {
                    if (!e.Alive || e.State != EnemyState.Telegraph || e.Def.Reach >= 3f || e.Aim.Len > 0.5f) continue;
                    if (V2.Dist(e.Pos, c.Pos) - Balance.CourierR > e.Def.Reach + 0.4f) continue;
                    Go(Escape(r, c.Pos, e.Pos)); act = 0; return;
                }
            // d) arqueiro mirando e o espelho nas maos: encarar (a flecha volta nele)
            if (MirrorOk(r))
                foreach (Enemy e in r.Enemies)
                    if (e.Alive && e.Type == EnemyType.Archer && e.State == EnemyState.Telegraph && V2.SegDist(c.Pos, e.Pos, e.Pos + e.Aim * Balance.ArrowRange) <= Balance.CourierR + 0.35f)
                    { Go(e.Pos); act = 0; return; }
            // e) lanterna apagada nas maos no escuro: ate a tocha mais perto
            if (r.Dark)
                foreach (Cargo g in r.Cargos)
                {
                    if (g.Type != CargoType.Lantern || !g.Out || g.State != CargoState.Held) continue;
                    V2 best = c.Pos; float bd = float.MaxValue;
                    foreach (V2 t in r.Torches) { float d = V2.Dist(t, c.Pos); if (d < bd) { bd = d; best = t; } }
                    if (bd < float.MaxValue) { Go(best); act = 0; return; }
                }
            if (bored) return;
            // e2) reliquia nas maos e fantasma chegando (atravessa parede; o toque racha): pousar e abater na luz
            if (r.Holds(CargoType.Relic))
                foreach (Enemy e in r.Enemies)
                    if (e.Alive && e.Type == EnemyType.Ghost && e.Carry == null && V2.Dist(e.Pos, c.Pos) <= 3.2f) { stop = true; act = 0; return; }
            // f) pilha de ossos longe da carga: levar a carga ate ela (a 3 m o esqueleto nao volta)
            BonePile pile = null; float pd = float.MaxValue;
            foreach (BonePile b in r.Bones) { float d = V2.Dist(b.Pos, c.Pos); if (d < pd) { pd = d; pile = b; } }
            if (pile != null && r.Main.State == CargoState.Held && pd > Balance.BonesNear - 0.5f && pd <= 7f) { Go(pile.Pos); act = 0; return; }
            if (pile != null && r.Main.State == CargoState.Ground && V2.Dist(r.Main.Pos, pile.Pos) > Balance.BonesNear && pile.Left > 1.5f
                && V2.Dist(r.Main.Pos, c.Pos) <= 4f) { Go(r.Main.Pos); act = 0; return; }
            // g) luta obrigatoria no escuro com o alvo fora da luz: chegar perto ate ilumina-lo (com a lanterna acesa nas maos, para a ~3,7 m:
            //    pousada ela ilumina 4 m e o Necromante so salta a 3,4 m)
            Enemy locker = null;
            foreach (Enemy e in r.Enemies) if (e.Alive && e.Def.Locks && !r.Untouchable(e)) locker = e;
            if (r.Lit && locker != null && !r.InLight(locker.Pos) && !(r.Holds(CargoType.Message) && !locker.Def.Boss))
            {
                bool lamp = false; foreach (Cargo g in r.Cargos) if (g.Type == CargoType.Lantern && !g.Out && g.State != CargoState.Gone) lamp = true;
                float want = lamp ? 3.7f : Math.Max(1.6f, Balance.LightCourier * r.LightMul - r.LightCut - 0.5f);
                if (V2.Dist(c.Pos, locker.Pos) > want) { Go(Run.FreeNear(r.Room, locker.Pos + (c.Pos - locker.Pos).Norm * want, Balance.CourierR)); act = 0; return; }
            }
            // h) esqueleto vindo: com a carga nas maos, chegar a ~2,6 m dele antes de pousar (morre perto da carga e nao volta)
            if ((fightRoom || locker != null) && r.Main.State == CargoState.Held)
            {
                Enemy s = null; float sd = float.MaxValue;
                foreach (Enemy e in r.Enemies) if (e.Alive && e.Type == EnemyType.Skeleton) { float d = V2.Dist(e.Pos, c.Pos); if (d < sd) { sd = d; s = e; } }
                if (s != null && sd > 2.6f && sd <= Balance.ShotRange) { Go(s.Pos); act = 0; }
            }
        }

        /// <summary>O bot pega o balao do chao com ~1,7 s de folga (a reacao e o caminho comem o resto).</summary>
        const float BalloonGrab = 2.3f;
    }
}
