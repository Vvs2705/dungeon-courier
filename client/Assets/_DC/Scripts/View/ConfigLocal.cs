using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace DC
{
    /// <summary>
    /// Remote config sem backend (CONTRATO_LANCAMENTO §6.9): Resources/config.json traz os padroes do build; a chave "config_url" (vazia de
    /// fabrica) aponta uma URL https opcional cujo JSON plano sobrescreve as chaves locais. Sem URL nada sai do aparelho e vale o local.
    /// O Core so recebe o texto (sim.Config = Config.Parse) e ignora chave desconhecida ou valor invalido.
    /// </summary>
    public static class ConfigLocal
    {
        const int TimeoutS = 5;

        /// <summary>Texto do Resources/config.json ("" se faltar: o Core fica nos padroes).</summary>
        public static string Texto()
        {
            var t = Resources.Load<TextAsset>("config");
            return t != null ? t.text : "";
        }

        /// <summary>URL https do "config_url" do JSON local; null = nao configurada (nada de rede).</summary>
        public static string Url(string json)
        {
            Match m = Regex.Match(json ?? "", "\"config_url\"\\s*:\\s*\"([^\"]*)\"");
            string u = m.Success ? m.Groups[1].Value.Trim() : "";
            return u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? u : null;
        }

        /// <summary>Baixa a URL (timeout de 5 s); deu certo: entrega o JSON local com as chaves remotas por cima (a ultima chave vence no
        /// Config.Parse). Falhou: nao chama nada e o local continua valendo.</summary>
        public static IEnumerator Baixar(string url, string local, Action<string> ok)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = TimeoutS;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { Debug.Log("DC config remota: " + req.error); yield break; }
                ok(Juntar(local, req.downloadHandler.text));
            }
        }

        /// <summary>{local..., remoto...} em JSON plano (ponytail: o Config.Parse so le objeto plano).</summary>
        public static string Juntar(string local, string remoto) =>
            "{" + (local ?? "").Trim().Trim('{', '}') + "," + (remoto ?? "").Trim().Trim('{', '}') + "}";
    }
}
