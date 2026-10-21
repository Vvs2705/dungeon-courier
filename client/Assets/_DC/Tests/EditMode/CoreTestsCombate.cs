using System.Collections.Generic;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Revisao de combate e movimento (2026-10-21, feedback do Vinicius no celular: o Bruto preso na quina da sala e o Guarda parado).
    /// Cada teste falha se o problema voltar.</summary>
    public class CoreTestsCombate
    {
        const float Dt = Balance.Step;

        /// <summary>Andadores grandes (raio maior que o do courier) em todas as salas: partindo do ponto de nascimento, chegam ao alcance do
        /// courier parado em cada canto livre da sala (antes o Nav do courier mandava o Bruto pela fresta de 1 m da R5 e ele ficava na quina).</summary>
        [Test]
        public void AndadorGrande_ChegaAoCourierEmTodoCanto()
        {
            var falhas = new List<string>();
            foreach (RoomDef room in Content.Rooms)
            {
                var corpos = new List<Spawn>(room.Spawns);
                if (room.Wave != null) corpos.AddRange(room.Wave);
                var courierNav = new Nav(room);
                foreach (Spawn s in corpos)
                {
                    EnemyDef d = EnemyDef.All[(int)s.Type];
                    if (d.Speed <= 0f || d.Radius <= Nav.BaseR || !Andador(s.Type)) continue;
                    var nav = new Nav(room, d.Radius);
                    foreach (V2 canto in new[] { new V2(0f, 0f), new V2(Balance.RoomW, 0f), new V2(0f, Balance.RoomH), new V2(Balance.RoomW, Balance.RoomH), new V2(Balance.RoomW / 2f, Balance.RoomH / 2f) })
                    {
                        V2 alvo = Run.FreeNear(room, canto, Balance.CourierR);
                        if (Run.Blocked(room, alvo, Balance.CourierR) || courierNav.Steps(Balance.Entrance, alvo) == int.MaxValue) continue;
                        V2 p = Run.FreeNear(room, s.Pos, d.Radius);
                        float melhor = float.MaxValue;
                        for (int i = 0; i < (int)(40f / Dt) && melhor > d.Reach; i++)
                        {
                            p = p + nav.Dir(p, alvo) * (d.Speed * Dt);
                            Run.Collide(room, ref p, d.Radius);
                            melhor = System.Math.Min(melhor, V2.Dist(p, alvo) - d.Radius - Balance.CourierR);
                        }
                        // o canto pode ser apertado demais para o corpo: basta chegar a ate 0,6 m alem do alcance (o courier sai de la para andar)
                        if (melhor > System.Math.Max(d.Reach, 0.3f) + 0.6f) falhas.Add(room.Code + " " + s.Type + " -> " + alvo + ": " + melhor.ToString("0.0") + " m (parou em " + p + ")");
                    }
                }
            }
            Assert.IsEmpty(falhas, string.Join("\n", falhas));
        }

        /// <summary>O caso da foto: R5, Bruto abaixo da ponta do bloco direito, courier acima, perto da parede direita. Antes ficava na quina.</summary>
        [Test]
        public void Bruto_NaoTravaNaFrestaDe1m_ContornaPeloMeio()
        {
            var sim = new Sim(7) { Reputation = Sim.Contracts.Length };
            Run r = sim.DevStartAt(-1, "R5");
            Assert.IsNotNull(r);
            r.Enemies.Clear(); r.Courier.Hp = 99;
            Enemy b = r.Spawn(EnemyType.Brute, new V2(8.45f, 5.8f)); b.Hp = 999;
            V2 alvo = new V2(8.3f, 9f);
            float melhor = float.MaxValue;
            for (int i = 0; i < StepsFor(25f) && melhor > b.Def.Reach; i++)
            {
                r.Courier.Pos = alvo;
                sim.Tick(Dt, V2.Zero, false);
                melhor = System.Math.Min(melhor, V2.Dist(b.Pos, alvo) - Balance.CourierR);
            }
            Assert.LessOrEqual(melhor, b.Def.Reach, "Bruto preso em " + b.Pos);
        }

        // ------------------------------------------------------------------ Guarda sentinela

        /// <summary>Contrato com a sala `room`, so com o Guarda dela (vida de sobra dos dois lados), o courier ja tendo andado.</summary>
        static Enemy SoGuarda(Sim sim, int contrato, string room, out Run r)
        {
            r = sim.DevStartAt(contrato, room);
            Assert.IsNotNull(r, room);
            Enemy g = r.Enemies.Find(e => e.Type == EnemyType.Guard);
            Assert.IsNotNull(g);
            r.Enemies.RemoveAll(e => e != g);
            g.Hp = 999; r.Courier.Hp = 99;
            Assert.AreEqual(Balance.GuardLeash, g.Leash, "o guarda da sala tem coleira");
            sim.Tick(Dt, V2.Up, false);   // o pouso so conta depois de mexer na sala (FTUE); o alerta pede o courier parado
            return g;
        }

        [Test]
        public void Guarda_NoTutorial_AndaNaColeiraESemInvestida()
        {
            var sim = new Sim(7) { Reputation = Sim.Contracts.Length };
            Enemy g = SoGuarda(sim, 0, "R1", out Run r);   // C1: tutorial (sem reforco, sem investida)
            Assert.IsFalse(r.Contract.Waves);
            V2 parado = g.Home + new V2(0f, -4f);
            float longe = 0f;
            for (int i = 0; i < StepsFor(6f); i++)
            {
                r.Courier.Pos = parado;
                sim.Tick(Dt, V2.Zero, false);
                Assert.Less(g.Aim.Len, 0.5f, "sem investida no tutorial");
                longe = System.Math.Max(longe, V2.Dist(g.Pos, g.Home));
            }
            Assert.Greater(longe, 2f, "a sentinela sai do posto atras do courier");
            Assert.LessOrEqual(longe, Balance.GuardLeash + 0.1f, "mas so ate a coleira");
            Assert.Less(r.Courier.Hp, 99, "e golpeia quem fica parado perto dela");
        }

        [Test]
        public void Guarda_Investida_AcertaQuemFicaParado_ErraEAtordoaQuemSai()
        {
            var sim = new Sim(7) { Reputation = Sim.Contracts.Length };
            Enemy g = SoGuarda(sim, -1, "R6", out Run r);
            Assert.IsTrue(r.Contract.Waves, "R6 fica num contrato fora do tutorial");
            V2 parado = g.Home + new V2(0f, -5f);
            int mira = 0;
            for (int i = 0; i < StepsFor(8f) && mira == 0; i++)
            {
                r.Courier.Pos = parado;
                sim.Tick(Dt, V2.Zero, false);
                if (g.State == EnemyState.Telegraph && g.Aim.Len > 0.5f) mira = i;
            }
            Assert.Greater(mira, 0, "o guarda mira a investida em quem esta a media distancia");
            int hp = r.Courier.Hp;
            for (int i = 0; i < StepsFor(2f); i++) sim.Tick(Dt, V2.Zero, false);   // parado: leva a investida
            Assert.AreEqual(hp - g.Def.Damage, r.Courier.Hp, "a investida acerta quem fica parado");

            // de novo, mas saindo da linha durante a mira: ele erra e fica atordoado (tiro livre)
            mira = 0;
            for (int i = 0; i < StepsFor(10f) && mira == 0; i++)
            {
                if (g.State != EnemyState.Telegraph) r.Courier.Pos = parado;
                sim.Tick(Dt, V2.Zero, false);
                if (g.State == EnemyState.Telegraph && g.Aim.Len > 0.5f) mira = i;
            }
            Assert.Greater(mira, 0, "segunda investida");
            hp = r.Courier.Hp;
            r.Courier.Pos = r.Courier.Pos + g.Aim.Right * 2f;   // passo para o lado
            float stun = 0f;
            for (int i = 0; i < StepsFor(2f); i++) { sim.Tick(Dt, V2.Zero, false); stun = System.Math.Max(stun, g.Stun); }
            Assert.AreEqual(hp, r.Courier.Hp, "saiu da linha: nao apanha");
            Assert.Greater(stun, 0.5f, "errou: atordoado");
        }

        [Test]
        public void Guarda_CourierAndando_NaoSaiDoPosto()
        {
            var sim = new Sim(7) { Reputation = Sim.Contracts.Length };
            Enemy g = SoGuarda(sim, -1, "R6", out Run r);
            for (int i = 0; i < StepsFor(4f); i++)   // passa andando de um lado para o outro a 3,5 m do posto (com a carga nas maos)
            {
                sim.Tick(Dt, i % 40 < 20 ? new V2(1f, 0f) : new V2(-1f, 0f), false);
                r.Courier.Pos = new V2(r.Courier.Pos.X, g.Home.Y - 3.5f);
                Assert.Less(V2.Dist(g.Pos, g.Home), 0.2f, "so quem para para atirar tira o guarda do posto");
            }
        }

        [Test]
        public void Guarda_CourierLonge_VoltaAoPosto()
        {
            var sim = new Sim(7) { Reputation = Sim.Contracts.Length };
            Enemy g = SoGuarda(sim, -1, "R6", out Run r);
            g.Pos = g.Home + new V2(-2f, -2f);
            for (int i = 0; i < StepsFor(5f); i++) { r.Courier.Pos = Balance.Entrance; sim.Tick(Dt, V2.Zero, false); }
            Assert.Less(V2.Dist(g.Pos, g.Home), 0.3f, "longe do posto, volta para a porta");
        }

        static int StepsFor(float s) => (int)System.Math.Round(s / Dt);

        /// <summary>Quem anda pelo MoveWalker (os outros voam, cavam ou seguem o trilho).</summary>
        static bool Andador(EnemyType t) => t != EnemyType.Bat && t != EnemyType.Ghost && t != EnemyType.Gargoyle && t != EnemyType.Mole
                                            && t != EnemyType.Minecart;
    }
}
