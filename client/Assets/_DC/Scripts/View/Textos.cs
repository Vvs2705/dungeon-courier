using System;
using System.Globalization;
using DC.Core;

namespace DC
{
    /// <summary>
    /// Textos do jogador (pt-BR), sem UnityEngine: um lugar so para revisar, medir e traduzir. As dicas do contrato vem do Core
    /// (Hints.Text). Nada de emoji: icones sao desenhados pelo Art.
    /// </summary>
    public static class Textos
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public const string Quadro = "Quadro de contratos", Guilda = "Guilda", Voltar = "VOLTAR", Continuar = "CONTINUAR",
            Jogar = "JOGAR", Repetir = "REPETIR",
            Resgatar = "RESGATAR", Recusar = "RECUSAR", Comprar = "COMPRAR", Maximo = "MÁX",
            Pausado = "Pausado", Retomado = "Contrato em curso", Desistir = "DESISTIR DO CONTRATO",
            DesistirTitulo = "Desistir do contrato?", DesistirCorpo = "O contrato falha e a carga NÃO fica na masmorra para resgate. As moedas dos baús ficam com você.",
            DesistirSim = "SIM, DESISTIR", Pular = "toque para pular",
            SaveRecusado = "Este save é de uma versão mais nova do jogo. Atualize: nada será jogado nem gravado aqui.",
            OfertaTitulo = "Carga perdida", AvisoPendente = "A carga ficou na masmorra: o próximo contrato oferece o resgate.",
            Rachou = "rachou!", Quebrou = "quebrou!", Ladrao = "ladrão!", Fugiu = "fugiu!", Chocou = "chocou!";

        public static string Som(bool on) => on ? "Som: ligado" : "Som: desligado";
        public static string Vibra(bool on) => on ? "Vibração: ligada" : "Vibração: desligada";

        public static string Carga(CargoType t) => CargoDef.All[(int)t].Name;

        /// <summary>"2 salas + entrega" / "3 salas + chefe + entrega".</summary>
        public static string Salas(ContractDef c)
        {
            int n = 0; bool boss = false;
            foreach (int[] layer in c.Layers)
            {
                RoomDef r = Content.Rooms[layer[0]];
                if (r.Boss) boss = true; else if (!r.Delivery) n++;
            }
            return n + (n == 1 ? " sala" : " salas") + (boss ? " + chefe" : "") + " + entrega";
        }

        /// <summary>O verbo que a carga muda, numa linha (linha 2 do quadro).</summary>
        public static string Verbo(ContractDef c)
        {
            switch (c.Cargo)
            {
                case CargoType.Relic: return "frágil: 3 rachaduras = quebra";
                case CargoType.Egg: return "choca em " + c.EggTarget.ToString("0", Inv) + " s: o filhote tem de chegar";
                default: return c.Index == 0 ? "primeiro contrato" : "entregue inteira: bônus";
            }
        }

        public static string Pendente(LostCargo p) => "Carga perdida: " + Carga(p.Type) + " (C" + (p.Contract + 1) + "). O próximo contrato oferece o resgate.";

        public static string Oferta(LostCargo p, int bonus) =>
            Carga(p.Type) + " do C" + (p.Contract + 1) + " ficou na masmorra" + (p.Cracks > 0 ? " com " + Rachaduras(p.Cracks) : "") +
            ".\nAceite e ela aparece no chão da 1ª sala. Entregue as duas e ganhe +" + bonus + " moedas.\nRecusou, ela se perde.";

        public static string Rachaduras(int n) => n == 1 ? "1 rachadura" : n + " rachaduras";

        /// <summary>Linha da dica quando o Core nao tem dica (Hint.None): o objetivo da sala.</summary>
        public static string Objetivo(Run r) => r.Room.Delivery ? "leve a carga até o cliente" : "escolha uma porta no topo";

        public static string Titulo(Outcome o)
        {
            switch (o)
            {
                case Outcome.Delivered: return "Entregue!";
                case Outcome.Died: return "Você caiu";
                case Outcome.CargoStolen: return "Roubaram a carga";
                case Outcome.CargoBroken: return "A relíquia quebrou";
                case Outcome.PetDied: return "O filhote não resistiu";
                default: return "Contrato abandonado";
            }
        }

        public static string Motivo(ContractResult r)
        {
            string sala = r.DeathRoom >= 0 ? Content.Rooms[r.DeathRoom].Name : "";
            switch (r.Outcome)
            {
                case Outcome.Died: return "O courier caiu" + (sala != "" ? " na " + sala : "") + ".";
                case Outcome.CargoStolen: return "O ladrão escapou pela borda da sala com a carga.";
                case Outcome.CargoBroken: return "A 3ª rachadura quebrou a relíquia.";
                case Outcome.PetDied: return "O filhote foi atingido duas vezes.";
                default: return "Você desistiu do contrato.";
            }
        }

        /// <summary>Reacao do cliente ao abrir a caixa (cena da entrega).</summary>
        public static string Reacao(ContractResult r)
        {
            if (r.Hatched) return "Chocou no caminho, mas o filhote chegou!";
            if (r.Cracks <= 0) return "Perfeito! Nenhuma rachadura.";
            if (r.Cracks == 1) return "Bom! Só 1 rachadura.";
            return "Hmm... " + r.Cracks + " rachaduras.";
        }

        public static string Mul(float m) => "×" + m.ToString("0.0#", Inv).Replace('.', ',');
        public static string Tempo(float s) => ((int)(s / 60f)) + ":" + ((int)(s % 60f)).ToString("00", Inv);

        /// <summary>Pagamento detalhado (rotulo | valor) do ContractResult: base x carga x integridade x bolsa = pagamento; + baus + resgate.</summary>
        public static string[][] Pagamento(ContractResult r, ContractDef c)
        {
            if (!r.Delivered)
                return new[] { new[] { "Tempo", Tempo(r.Time) }, new[] { "Moedas dos baús", "+" + r.ChestCoins }, new[] { "Total", "+" + r.Total } };
            return new[]
            {
                new[] { "Pagamento base", r.Base.ToString(Inv) },
                new[] { "Carga: " + Carga(c.Cargo) + (r.Hatched ? " (chocou)" : ""), Mul(r.CargoMul) },
                new[] { "Integridade: " + Rachaduras(r.Cracks), Mul(r.IntegrityMul) },
                new[] { "Bolsa da guilda", Mul(r.BagMul) },
                new[] { "Pagamento", r.Payment.ToString(Inv) },
                new[] { "Baús", "+" + r.ChestCoins },
                new[] { "Resgate" + (r.RescueAccepted && !r.RescueDelivered ? " (não entregue)" : ""), "+" + r.RescueBonus },
                new[] { "Tempo", Tempo(r.Time) },
                new[] { "Total", "+" + r.Total },
            };
        }

        public static string UpNome(Upgrade u) => u == Upgrade.Life ? "Vida" : u == Upgrade.Dash ? "Dash" : "Bolsa";
        public static string UpEfeito(Upgrade u) => u == Upgrade.Life ? "+1 de vida máxima" : u == Upgrade.Dash ? "-10% na recarga do dash" : "+10% de moedas";
        public static string Nivel(int lv) => "nível " + lv + "/" + Balance.UpgradeMax;

        public static string Num(int v) => v.ToString(Inv);
        public static string Segundos(float s) => Math.Max(0, (int)Math.Ceiling(s)).ToString(Inv) + " s";
    }
}
