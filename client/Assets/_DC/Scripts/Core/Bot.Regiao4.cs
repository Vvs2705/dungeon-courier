using System;

namespace DC.Core
{
    /// <summary>Bot na regiao 4 (CONTEUDO_REGIOES §11): sai dos avisos no chao (frasco, chicote, giro, sopro, pouso, diabrete), espera o fogo do
    /// respiro do lado de fora, sai do fogo no chao, empurra inimigos na lava com o dash, pousa a planta e "da o passo" com o dash (luta de
    /// longe dela) e, com o Mapa ou o Faro de bau, vai aos baus enterrados pela ganancia. Medicao em docs/BALANCE_REGIAO4.md.</summary>
    public sealed partial class Bot
    {
        bool wantBuried4;

        /// <summary>Estrelas, moedas e equipamento estimados no fim da regiao 3 (CONTEUDO_REGIOES §10; HIPOTESE ate as raias R2/R3 medirem).</summary>
        public const int EndR3Stars = 132, EndR3Coins = 13900;

        /// <summary>Save do fim estimado da regiao 3, para medir a regiao 4 sem jogar as regioes 2-3: 132 estrelas (bot ~2,2 por contrato nas
        /// novas) preenchidas do C1 em diante (sem as regioes 2-3 no build, sobram para os contratos da 4, so para ela abrir); guilda 8/8/8/8 e
        /// armas no nivel 3 (as regioes 2-3 esgotam os ~4.200 + ~2.100 que a regiao 1 deixa); Fumaca e Botas destravadas no nivel 5 (13.600)
        /// e o Gancho com 2 projetos (R3-2, R3-9); 13.900 moedas (~33.800 ganhas nas regioes 2-3 menos esses ralos).</summary>
        public static string Regiao3EndSave(int seed)
        {
            var st = new char[Sim.Contracts.Length]; int left = EndR3Stars;
            for (int i = 0; i < st.Length; i++) { int k = Math.Max(0, Math.Min(3, left)); st[i] = (char)('0' + k); left -= k; }
            return "v=3\nseed=" + seed + "\nrep=" + RegionDef.Of(4).First + "\ncoins=" + EndR3Coins + "\nlv=8,8,8,8\nwlv=3,3,3\nstars=" + new string(st)
                   + "\ngear=" + (int)Gear.Smoke + ",0,3,2,0,3,0,5,0,0,5\n";
        }

        partial void OnRoomR4(Run r)
        {
            wantBuried4 = false;
            if (!r.Revealed || r.Contract.Cargo == CargoType.Message) return;
            foreach (Chest b in r.Buried) if (!b.Opened) { wantBuried4 = rng.Float() < Greed + 0.3f; break; }   // ponytail: so sorteia em sala com bau enterrado (regiao 1 nao muda)
        }

        /// <summary>Inimigo da regiao 4 cujo golpe tem aviso no chao (run.Forge.Marks): o PlanR4 sai do aviso, nao o passo 1 do plano geral.</summary>
        static bool MarkedR4(Enemy e) => e.Type == EnemyType.KoboldBomber || e.Type == EnemyType.KoboldForeman || e.Type == EnemyType.ElderDragon || e.Type == EnemyType.Imp;

        static bool Holding(Run r) { foreach (Cargo g in r.Cargos) if (g.State == CargoState.Held) return true; return false; }

        /// <summary>O aviso pega o ponto p (com folga `pad`)?</summary>
        static bool Threat4(ForgeMark m, V2 p, float pad)
        {
            switch (m.Kind)
            {
                case ForgeMarkKind.Whip: return V2.SegDist(p, m.Pos, m.Pos + m.Dir * m.Length) <= m.Radius + pad;
                case ForgeMarkKind.Breath:
                {
                    V2 v = p - m.Pos; float d = v.Len;
                    return d <= m.Length + pad && (d < 0.05f || V2.Dot(v * (1f / d), m.Dir) >= Balance.BreathCos - (m.Sweep ? 0.75f : 0.12f));   // varredura: foge do arco todo
                }
                case ForgeMarkKind.Swap: return false;   // o redemoinho e na carga: a mira atira no diabrete
                default: return V2.Dist(p, m.Pos) <= m.Radius + pad;
            }
        }

        /// <summary>Ponto seguro a ~1 m fora do aviso: circulo = para longe do centro; faixa e cone = para o lado (ou atras de um pilar).</summary>
        static V2 Out4(Run r, ForgeMark m, V2 p)
        {
            V2 best = p; float bs = float.MinValue;
            foreach (V2 dir in V2.Dirs8)
                for (float k = 1f; k <= 4f; k += 1f)
                {
                    V2 q = Run.FreeNear(r.Room, p + dir * k, Balance.CourierR);
                    if (Threat4(m, q, Balance.CourierR + 0.3f)) continue;
                    float s = -V2.Dist(q, p) - (r.NearFire(q) ? 2f : 0f) + 0.3f * Math.Min(Math.Min(q.X, Balance.RoomW - q.X), 2f);
                    if (s > bs) { bs = s; best = q; }
                    break;
                }
            return best;
        }

        /// <summary>Perigo visivel em p: fogo no chao, respiro que solta fogo em menos de 1 s ou aviso no chao (menos o redemoinho).</summary>
        static bool Hot4(Run r, V2 p)
        {
            const float pad = Balance.CourierR * 0.6f;
            foreach (FirePatch f in r.Forge.Fires) if (V2.Dist(p, f.Pos) <= f.Radius + pad) return true;
            for (int i = 0; i < r.Room.Vents.Length; i++) if (r.VentFireIn(i) < 1f && V2.Dist(p, r.Room.Vents[i].Pos) <= r.Room.Vents[i].Radius + pad) return true;
            foreach (ForgeMark m in r.Forge.Marks) if ((m.Left > 0f || m.Active > 0f) && Threat4(m, p, pad)) return true;
            return false;
        }

        /// <summary>Lugar ruim para pousar: fogo no chao, respiro (qualquer fase) ou aviso no chao.</summary>
        static bool BadSpot4(Run r, V2 p)
        {
            if (Hot4(r, p)) return true;
            foreach (FirePatch f in r.Forge.Fires) if (V2.Dist(p, f.Pos) <= f.Radius + 0.35f) return true;
            for (int i = 0; i < r.Room.Vents.Length; i++) if (r.VentFireIn(i) < 2.5f && V2.Dist(p, r.Room.Vents[i].Pos) <= r.Room.Vents[i].Radius + 0.3f) return true;
            return false;
        }

        /// <summary>Ponto livre e sem perigo mais perto de p (8 direcoes, 0,9-2,7 m), puxando para `pref`; sem nenhum, 1,5 m para longe de `from`.</summary>
        static V2 Safe4(Run r, V2 p, V2 pref, V2 from)
        {
            V2 away = (p - from).Norm; if (away.Len < 0.5f) away = V2.Up * -1f;
            V2 best = Run.FreeNear(r.Room, p + away * 1.5f, Balance.CourierR); float bs = float.MinValue;
            foreach (V2 d in V2.Dirs8)
                for (float k = 0.9f; k <= 2.8f; k += 0.6f)
                {
                    V2 q = p + d * k;
                    if (Run.Blocked(r.Room, q, Balance.CourierR) || q.X < 0.4f || q.Y < 0.4f || q.X > Balance.RoomW - 0.4f || q.Y > Balance.RoomH - 0.4f || Hot4(r, q)) continue;
                    float sc = -k + 0.4f * V2.Dot(d, pref);
                    if (sc > bs) { bs = sc; best = q; }
                    break;
                }
            return best;
        }

        /// <summary>A cada passo (como quem ve o chao): se o proximo meio metro entra em fogo, respiro prestes a soltar fogo ou aviso, desvia para a
        /// direcao livre mais parecida; sem nenhuma, espera parado. Indo pegar a carga que esta no fogo, entra.</summary>
        partial void SteerR4(Run r, ref V2 move)
        {
            float l = move.Len;
            if (r.Forge.Fires.Count == 0 && r.Forge.Marks.Count == 0 && r.Room.Vents.Length == 0) return;
            V2 c = r.Courier.Pos;
            if (l < 0.05f)   // vai parar (pousar): nao no fogo, em respiro nem em aviso; anda devagar ate um lugar seguro
            {
                if (!Holding(r) || r.AwaitingMove) return;
                V2 spot = c + r.Courier.Facing.Right * Balance.PlantOffset;
                if (!BadSpot4(r, spot) && !BadSpot4(r, c)) return;
                V2 safe = Safe4(r, c, V2.Zero, spot);
                if (V2.Dist(safe, c) > 0.1f) move = (safe - c).Norm * 0.6f;
                return;
            }
            V2 dir = move * (1f / l);
            if (V2.Dist(c, goal) < 1.0f || Hot4(r, c)) return;   // ja dentro: o plano tira
            foreach (Cargo g in r.Cargos) if (g.State == CargoState.Ground && g.Type == CargoType.Barrel && g.Fuse >= 1.4f) return;   // barril quase explodindo: vai direto
            if (!Hot4(r, c + dir * 0.45f) && !Hot4(r, c + dir * 0.9f)) return;
            V2 best = V2.Zero; float bs = -0.3f;
            foreach (V2 d in V2.Dirs8)
            {
                float sc = V2.Dot(d, dir);
                if (sc <= bs || Run.Blocked(r.Room, c + d * 0.5f, Balance.CourierR) || Hot4(r, c + d * 0.45f) || Hot4(r, c + d * 0.9f)) continue;
                bs = sc; best = d;
            }
            move = best * l;
        }

        partial void PlanR4(Run r, ref int act)
        {
            Courier c = r.Courier;
            ForgeState f = r.Forge;
            foreach (Cargo g in r.Cargos) if (g.State == CargoState.Ground && g.Type == CargoType.Barrel && g.Fuse >= 1.4f) return;   // pavio adiantado: o plano geral vai buscar o barril
            // a) aviso no chao que pega o courier: sair (dash so em cima da hora e sem carga nas maos: o dash a largaria dentro do aviso)
            foreach (ForgeMark m in f.Marks)
            {
                if ((m.Left <= 0f && m.Active <= 0f) || !Threat4(m, c.Pos, Balance.CourierR + 0.25f)) continue;
                Go(Out4(r, m, c.Pos));
                act = m.Left > 0f && m.Left < 0.35f && c.DashReady && !Holding(r) ? 1 : 0;
                return;
            }
            // b) dentro de um respiro que vai soltar fogo: sair (esperar do lado de fora e o SteerR4, que nao deixa entrar)
            for (int i = 0; i < r.Room.Vents.Length; i++)
            {
                Vent v = r.Room.Vents[i];
                if (r.VentFireIn(i) > 1.2f || V2.Dist(c.Pos, v.Pos) > v.Radius + Balance.CourierR + 0.1f) continue;
                Go(Safe4(r, c.Pos, (goal - c.Pos).Norm, v.Pos)); act = 0; return;
            }
            // c) fogo no chao: sair
            foreach (FirePatch p in f.Fires)
                if (V2.Dist(c.Pos, p.Pos) <= p.Radius + 0.25f) { Go(Safe4(r, c.Pos, (goal - c.Pos).Norm, p.Pos)); act = 0; return; }
            // d) inimigo entre o courier e a lava: o dash empurra (3 de dano; a brasa morre). Sem carga nas maos (o dash a largaria).
            if (r.Room.Lava.Length > 0 && c.DashReady && !Holding(r))
                foreach (Enemy e in r.Enemies)
                {
                    if (!e.Alive || e.Def.Locks || e.Def.Hazard || e.Type == EnemyType.Bat || e.Type == EnemyType.Salamander || e.Type == EnemyType.MagmaElemental
                        || e.Type == EnemyType.SalamanderEgg) continue;
                    float d = V2.Dist(e.Pos, c.Pos);
                    if (d > 2.6f || d < 0.3f) continue;
                    V2 dir = (e.Pos - c.Pos) * (1f / d);
                    if (!r.InLava(e.Pos + dir * 1.2f, e.Def.Radius)) continue;
                    Go(e.Pos + dir * 0.5f); act = 1; return;
                }
            // e) planta no chao: pousar e "dar o passo" com o dash (a planta morde quem fica ao lado) e lutar de longe dela; sem luta, pega de volta
            Cargo plant = null;
            foreach (Cargo g in r.Cargos) if (g.Type == CargoType.Flytrap && g.State == CargoState.Ground) plant = g;
            if (plant != null)
            {
                bool busy = false;
                foreach (Enemy e in r.Enemies)
                    if (e.Alive && (e.Carry != null || (e.State == EnemyState.Telegraph && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach + 0.4f))) busy = true;
                Enemy near = Nearest(r, c.Pos), lockr = null;
                foreach (Enemy e in r.Enemies) if (e.Alive && e.Def.Locks) lockr = e;
                float nd = near != null ? V2.Dist(near.Pos, c.Pos) : float.MaxValue, pd = V2.Dist(c.Pos, plant.Pos);
                bool fight = near != null && nd <= Balance.ShotRange && (fightRoom || lockr != null || nd <= 2.5f) && !bored;
                if (!busy && fight)
                {
                    if (pd - Balance.CourierR <= Balance.PlantReach + 0.2f && c.State == CourierState.Planted && c.DashReady) { Go(Escape(r, c.Pos, plant.Pos)); act = 1; return; }
                    if (pd <= 3.6f && pd - Balance.CourierR > Balance.PlantReach) { stop = true; act = 0; return; }
                }
            }
            // f) baus enterrados revelados (Mapa / Faro de bau): ganancia, com a sala calma
            if (wantBuried4 && r.Revealed && !bored)
            {
                Chest best = null; float bd = float.MaxValue;
                foreach (Chest b in r.Buried) if (!b.Opened) { float d = V2.Dist(b.Pos, c.Pos); if (d < bd) { bd = d; best = b; } }
                if (best == null) { wantBuried4 = false; return; }
                foreach (Enemy e in r.Enemies) if (e.Alive && (e.Carry != null || e.Def.Locks || V2.Dist(e.Pos, c.Pos) <= 2.5f)) return;
                foreach (Cargo g in r.Cargos) if (g.State == CargoState.Ground && V2.Dist(g.Pos, c.Pos) > r.Sim.PickupRange) return;
                if (bd <= Balance.ChestRange - 0.3f) stop = true; else Go(best.Pos);
                act = 0;
            }
        }
    }
}
