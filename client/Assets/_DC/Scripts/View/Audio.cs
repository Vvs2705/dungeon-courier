using System.Collections.Generic;
using UnityEngine;

namespace DC
{
    /// <summary>
    /// Audio do jogo (CONTRATO_V1 §12, docs/AUDIO.md). Clipes sintetizados por client/tools/audio_gen.py:
    /// Resources/Audio/sfx/&lt;nome&gt;_&lt;n&gt;.wav (variacoes do mesmo som) e Resources/Audio/musica/&lt;faixa&gt;.wav (lacos).
    /// Play: escolhe uma variacao (nunca a mesma 2x seguidas), varia o pitch +-4%, no maximo MaxPorSom vozes do mesmo nome e
    /// Vozes no total (rouba a mais antiga). Music: crossfade de Fade s entre 2 AudioSource; null ou "" esvazia.
    /// Volumes 0..1 no PlayerPrefs (dc.som, dc.musica). Clipe ausente: nao toca nada (o jogo nao quebra). Sem alocacao por quadro.
    /// </summary>
    public static class Audio
    {
        const int Vozes = 12, MaxPorSom = 3;
        const float Fade = 1.2f, PitchVar = 0.04f;
        const string SomKey = "dc.som", MusicaKey = "dc.musica";

        sealed class Banco { public AudioClip[] Clips; public int Ultimo = -1; public float Quando = -99f; }

        static readonly Dictionary<string, Banco> Bancos = new Dictionary<string, Banco>();
        static AudioSource[] _vozes;
        static readonly string[] Dono = new string[Vozes];
        static readonly float[] Inicio = new float[Vozes];
        static AudioSource _mAtual, _mVelha;
        static Fader _fader;
        static string _faixa;
        static float _som = -1f, _musica = -1f;

        public static float Som { get { if (_som < 0f) _som = PlayerPrefs.GetFloat(SomKey, 1f); return _som; } set => SetVolumes(value, Musica); }
        public static float Musica { get { if (_musica < 0f) _musica = PlayerPrefs.GetFloat(MusicaKey, 0.8f); return _musica; } set => SetVolumes(Som, value); }

        public static void Init(GameObject host)
        {
            if (_vozes != null && _vozes[0] != null) return;   // ja iniciado (e o host ainda existe)
            var grupos = new Dictionary<string, List<AudioClip>>();
            foreach (AudioClip c in Resources.LoadAll<AudioClip>("Audio/sfx"))
            {
                string n = Nome(c.name);
                if (!grupos.TryGetValue(n, out List<AudioClip> l)) grupos[n] = l = new List<AudioClip>();
                l.Add(c);
            }
            Bancos.Clear();
            foreach (var g in grupos) Bancos[g.Key] = new Banco { Clips = g.Value.ToArray() };

            _vozes = new AudioSource[Vozes];
            for (int i = 0; i < Vozes; i++) { _vozes[i] = Fonte(host, false); Dono[i] = null; }
            _mAtual = Fonte(host, true);
            _mVelha = Fonte(host, true);
            _fader = host.AddComponent<Fader>();
            _fader.enabled = false;
            _faixa = null;
        }

        /// <summary>Toca o som `nome` (no maximo uma vez por `minGap` s: varios golpes no mesmo quadro viram um).</summary>
        public static void Play(string nome, float pitch = 1f, float minGap = 0.05f)
        {
            if (_vozes == null || Som <= 0f || nome == null || !Bancos.TryGetValue(nome, out Banco b)) return;
            float agora = Time.unscaledTime;
            if (agora - b.Quando < minGap) return;
            b.Quando = agora;

            int n = b.Clips.Length, k = 0;
            if (n > 1) { k = Random.Range(0, b.Ultimo < 0 ? n : n - 1); if (b.Ultimo >= 0 && k >= b.Ultimo) k++; }
            b.Ultimo = k;

            int livre = -1, velha = -1, velhaMesmo = -1, mesmo = 0;
            for (int i = 0; i < Vozes; i++)
            {
                if (!_vozes[i].isPlaying) { if (livre < 0) livre = i; continue; }
                if (velha < 0 || Inicio[i] < Inicio[velha]) velha = i;
                if (Dono[i] != nome) continue;
                mesmo++;
                if (velhaMesmo < 0 || Inicio[i] < Inicio[velhaMesmo]) velhaMesmo = i;
            }
            int v = mesmo >= MaxPorSom ? velhaMesmo : livre >= 0 ? livre : velha;
            AudioSource s = _vozes[v];
            s.Stop();
            s.clip = b.Clips[k];
            s.pitch = pitch * (1f + Random.Range(-PitchVar, PitchVar));
            s.volume = Som;
            s.Play();
            Dono[v] = nome;
            Inicio[v] = agora;
        }

        /// <summary>Troca a musica com crossfade ("menu", "masmorra", "chefe"). A mesma faixa nao reinicia; null/"" faz fade para o silencio.</summary>
        public static void Music(string faixa)
        {
            if (_mAtual == null || faixa == _faixa || (string.IsNullOrEmpty(faixa) && string.IsNullOrEmpty(_faixa))) return;
            AudioClip c = string.IsNullOrEmpty(faixa) ? null : Resources.Load<AudioClip>("Audio/musica/" + faixa);
            if (c == null && !string.IsNullOrEmpty(faixa)) return;   // faixa ausente: fica como esta
            _faixa = faixa;
            AudioSource t = _mVelha; _mVelha = _mAtual; _mAtual = t;
            _mAtual.Stop();
            _mAtual.clip = c;
            _mAtual.volume = 0f;
            if (c != null) _mAtual.Play();
            _fader.T = 0f;
            _fader.enabled = true;
        }

        /// <summary>Volumes 0..1 (Configuracoes). Sem PlayerPrefs.Save aqui: o slider chama a cada quadro; o Game grava no Save/pausa/saida.</summary>
        public static void SetVolumes(float som, float musica)
        {
            _som = Mathf.Clamp01(som);
            _musica = Mathf.Clamp01(musica);
            PlayerPrefs.SetFloat(SomKey, _som);
            PlayerPrefs.SetFloat(MusicaKey, _musica);
            if (_mAtual != null && !_fader.enabled) _mAtual.volume = _musica;
        }

        static AudioSource Fonte(GameObject host, bool musica)
        {
            AudioSource s = host.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.loop = musica;
            s.priority = musica ? 0 : 128;   // musica nunca e cortada pelo limite de vozes do Unity
            return s;
        }

        /// <summary>"moedas_2" -> "moedas"; "entrega_rachada_1" -> "entrega_rachada".</summary>
        static string Nome(string s)
        {
            int i = s.LastIndexOf('_');
            if (i <= 0 || i == s.Length - 1) return s;
            for (int k = i + 1; k < s.Length; k++) if (!char.IsDigit(s[k])) return s;
            return s.Substring(0, i);
        }

        /// <summary>Rampa do crossfade em tempo real (vale na pausa); desliga sozinho no fim (zero custo parado).</summary>
        sealed class Fader : MonoBehaviour
        {
            public float T;

            void Update()
            {
                T = Mathf.Min(1f, T + Time.unscaledDeltaTime / Fade);
                float m = Musica;
                _mAtual.volume = T * m;
                _mVelha.volume = Mathf.Min(_mVelha.volume, (1f - T) * m);
                if (T < 1f) return;
                _mVelha.Stop();
                _mVelha.clip = null;
                enabled = false;
            }
        }
    }
}
