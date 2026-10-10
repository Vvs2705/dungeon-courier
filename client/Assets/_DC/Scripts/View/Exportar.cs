using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace DC
{
    /// <summary>
    /// Tirar algo do aparelho SO quando o jogador manda (CONTRATO_V1 §10): o diario CSV (Configuracoes > Exportar diario) e o codigo do
    /// contrato (Resultado > Compartilhar). Sem plugin: Intent ACTION_SEND por AndroidJavaObject.
    ///   Android 10+ (API 29): o CSV e copiado para Downloads/DungeonCourier pelo MediaStore (content:// legivel pelo app escolhido,
    ///                         FLAG_GRANT_READ_URI_PERMISSION) e vai como EXTRA_STREAM text/csv.
    ///   Android 8-9 (API 26-28): sem MediaStore.Downloads e sem FileProvider (exigiria manifesto/plugin), vai o TEXTO do CSV como
    ///                         EXTRA_TEXT (so o fim se passar de 400 KB, com o cabecalho).
    ///   Windows: abre o Explorer com o arquivo selecionado. Outros: abre a pasta.
    /// O codigo Android compila em todas as plataformas (as classes JNI existem no UnityEngine) e so roda no Android: escolha em tempo
    /// de execucao, para o viewcheck pegar erro de compilacao.
    /// </summary>
    public static class Exportar
    {
        public enum Status { Vazio, Pasta, Escolha, Erro }
        const int MaxTexto = 400 * 1024;
        static bool Android => Application.platform == RuntimePlatform.Android;

        public static Status Diario(string path, string tituloEscolha)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path) || new FileInfo(path).Length == 0) return Status.Vazio;
            try
            {
                if (Android) { CompartilharCsv(path, tituloEscolha); return Status.Escolha; }
                AbrirPasta(path);
                return Status.Pasta;
            }
            catch (Exception e) { Debug.LogWarning("DC Exportar diario: " + e.Message); return Status.Erro; }
        }

        /// <summary>Texto curto (convite com o codigo). Android: escolha do app (true). PC: vai para a area de transferencia (false).</summary>
        public static bool Texto(string texto, string tituloEscolha)
        {
            try
            {
                if (Android)
                {
                    using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
                    {
                        intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
                        intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", texto).Dispose();
                        Escolher(intent, tituloEscolha);
                    }
                    return true;
                }
            }
            catch (Exception e) { Debug.LogWarning("DC Exportar texto: " + e.Message); }
            GUIUtility.systemCopyBuffer = texto;
            return false;
        }

        static void AbrirPasta(string path)
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + path.Replace('/', '\\') + "\"");
            else Application.OpenURL(new Uri(Path.GetDirectoryName(path)).AbsoluteUri);
        }

        // ------------------------------------------------------------------ Android (JNI, sem plugin)

        static void CompartilharCsv(string path, string titulo)
        {
            string nome = "DungeonCourier_diario_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv";
            int sdk;
            using (var ver = new AndroidJavaClass("android.os.Build$VERSION")) sdk = ver.GetStatic<int>("SDK_INT");
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            {
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", nome).Dispose();
                if (sdk >= 29)
                {
                    using (AndroidJavaObject uri = CopiarParaDownloads(path, nome))
                    {
                        intent.Call<AndroidJavaObject>("setType", "text/csv").Dispose();
                        intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri).Dispose();
                        intent.Call<AndroidJavaObject>("addFlags", 1).Dispose();   // FLAG_GRANT_READ_URI_PERMISSION
                        Escolher(intent, titulo);
                    }
                }
                else
                {
                    intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", Cauda(path)).Dispose();
                    Escolher(intent, titulo);
                }
            }
        }

        /// <summary>Copia o CSV para Downloads/DungeonCourier (MediaStore, API 29+, sem permissao) e devolve o content:// dele.</summary>
        static AndroidJavaObject CopiarParaDownloads(string path, string nome)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var sb = new sbyte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, sb, 0, bytes.Length);   // o JNI do Unity mapeia sbyte[] para byte[] do Java
            using (AndroidJavaObject act = Atividade())
            using (AndroidJavaObject resolver = act.Call<AndroidJavaObject>("getContentResolver"))
            using (var values = new AndroidJavaObject("android.content.ContentValues"))
            using (var downloads = new AndroidJavaClass("android.provider.MediaStore$Downloads"))
            using (AndroidJavaObject colecao = downloads.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
            {
                values.Call("put", "_display_name", nome);                   // MediaStore.MediaColumns.DISPLAY_NAME
                values.Call("put", "mime_type", "text/csv");                 // MediaStore.MediaColumns.MIME_TYPE
                values.Call("put", "relative_path", "Download/DungeonCourier"); // MediaStore.MediaColumns.RELATIVE_PATH
                AndroidJavaObject uri = resolver.Call<AndroidJavaObject>("insert", colecao, values);
                if (uri == null) throw new IOException("MediaStore recusou o arquivo");
                using (AndroidJavaObject os = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                {
                    os.Call("write", sb);
                    os.Call("close");
                }
                return uri;
            }
        }

        static string Cauda(string path)
        {
            string all = File.ReadAllText(path, Encoding.UTF8);
            if (all.Length <= MaxTexto) return all;
            int nl = all.IndexOf('\n');
            string header = nl > 0 ? all.Substring(0, nl + 1) : "";
            string tail = all.Substring(all.Length - MaxTexto);
            int cut = tail.IndexOf('\n');
            return header + (cut >= 0 ? tail.Substring(cut + 1) : tail);
        }

        static AndroidJavaObject Atividade()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) return player.GetStatic<AndroidJavaObject>("currentActivity");
        }

        static void Escolher(AndroidJavaObject intent, string titulo)
        {
            using (AndroidJavaObject act = Atividade())
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, titulo))
                act.Call("startActivity", chooser);
        }
    }
}
