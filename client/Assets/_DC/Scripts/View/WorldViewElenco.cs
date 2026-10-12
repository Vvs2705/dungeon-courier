using System.Collections.Generic;
using DC.Core;
using UnityEngine;

namespace DC
{
    /// <summary>
    /// Elenco das regioes 2-4 (CONTEUDO_REGIOES §5-§6, §12), parte do WorldView: inimigos, chefes e cargas novos, lidos do estado que as
    /// raias R2-R4 publicam (Run.Regiao2-4.cs): run.R2Of(e) (Act, Spot/Radius, Dir), run.Underground(e), run.R3Of(e) (Act), run.GrabbedBy,
    /// run.Forge (Marks, Flying, HeadDown), run.IceOf(carga), run.CageLeft, Cargo.Fuse do balao, Cargo.Out da lanterna.
    /// - Regiao 2 com as folhas prontas (RENDER_SPRITES): `goblin_mineiro` (Run persegue, Walk recua de frente, Throw com a soltura no
    ///   quadro 7 e a picareta em arco ate o circulo, Sleep/Wake, Hit), `toupeira` e `rainha_toupeira` (por baixo = sem corpo e rastro de
    ///   terra; Emerge no Strike; Swipe com impacto no 6; Call no Summon), `golem` (Slam com impacto no 5, Charge, Walk, Stun; brilho ciano
    ///   do ponto fraco desenhado NAS COSTAS pela frente do Core, porque o modelo tem a gema no peito), props `sapo`, `besouro`, `vagonete`
    ///   animados por codigo (o vagonete some no tunel).
    /// - Regiao 3: `esqueleto` (Attack, Rise no EnemyRevive, a pilha de ossos no ultimo quadro do Collapse), `aranha`/`viuva` com a pose
    ///   `_empinada` no aviso (cuspe, mordida, leque, fio, despencar), `fantasma`; `mumia` (Grab, Hold), `gargula` (Statue, Wake, Dive, Perch;
    ///   pedra tingida enquanto run.Untouchable), `necromante` (Cast, Summon; fumaca por alfa) pelos nomes do CONTEUDO_REGIOES §12.2.
    /// - Regiao 4 pelos nomes do §12.3: `diabrete` (Swap, Scratch), `kobold` (Toss, Walk recuando), `magma`/`brasa` (Smash, Punch),
    ///   `capataz` (Whip, Spin, Toss, Call), `salamandra` (prop), `dragao` (Fly no ar com a sombra em Forge.Shadow, Breath pelo aviso do
    ///   sopro, Land = cabeca baixa com o brilho e "dano em dobro"), `planta` (`planta_aberta` na mordida), `lanterna_apagada`.
    /// - Sem a folha, o desenho procedural de reserva legivel (forma + cor de papel) continua.
    /// - Avisos: regiao 2 pelo R2Foe (circulo da picareta e do monte, lingua, faixa da investida, pancada); regioes 3-4 pelas listas do Core
    ///   (DrawMarks); fora da luz, so olhos e sombra.
    /// </summary>
    public sealed partial class WorldView
    {
        const float WakeTime = 0.4f, RiseTime = 0.6f;
        readonly Dictionary<int, float> _rise = new Dictionary<int, float>();   // esqueleto remontando (EnemyRevive): id -> s
        int _biteId = -1; float _biteT;   // planta que acabou de morder (PlantBite B = carga): boca aberta por BiteTime
        const float BiteTime = 0.4f;

        /// <summary>Cor de papel das regioes 2-4 (ciano-petroleo = ataca de longe; lilas = mexe na carga; laranja-fogo = queima; verde =
        /// morto-vivo; CONTEUDO_REGIOES §12).</summary>
        static Color PapelRegiao(EnemyType t)
        {
            switch (t)
            {
                case EnemyType.GoblinMiner: return Art.Hex(0x2F7F78);
                case EnemyType.Mole: return Art.Hex(0x7A5A44);
                case EnemyType.BogFrog: return Art.Hex(0x5E7A3A);
                case EnemyType.BombBeetle: return Art.Hex(0x4A2A40);
                case EnemyType.Minecart: return Art.Hex(0x7A5A3A);
                case EnemyType.Ghost: return Art.Hex(0xBFF3FF);
                case EnemyType.Skeleton: return Art.Bone;
                case EnemyType.Spider: return Art.Hex(0x3A2A4A);
                case EnemyType.Mummy: return Art.Hex(0xCDBE9A);
                case EnemyType.Gargoyle: return Art.Hex(0x6E7A6A);
                case EnemyType.Imp: return Art.Hex(0x8A2E2E);
                case EnemyType.KoboldBomber: return Art.Hex(0x2F8F86);
                case EnemyType.MoleQueen: return Art.Hex(0x5A3E2E);
                case EnemyType.StoneGolem: return Art.Hex(0x6E7A8A);
                case EnemyType.SpiderWidow: return Art.Hex(0x1E1A24);
                case EnemyType.Necromancer: return Art.Hex(0x23232B);
                case EnemyType.KoboldForeman: return Art.Hex(0x2A7A70);
                case EnemyType.ElderDragon: return Art.Hex(0x1F5A44);
                default: return Art.Hex(0x2A2426);   // salamandra, elemental, brasa, ovo de salamandra: carvao com laranja
            }
        }

        static Color EyeC(EnemyType t)
        {
            switch (t)
            {
                case EnemyType.Skeleton: case EnemyType.Mummy: case EnemyType.Necromancer: return Undead;
                case EnemyType.Spider: case EnemyType.SpiderWidow: case EnemyType.Gargoyle: return Art.Bad;
                case EnemyType.Imp: return Lilac;
                case EnemyType.Salamander: case EnemyType.MagmaElemental: case EnemyType.MagmaShard: case EnemyType.ElderDragon: return LavaCore;
                case EnemyType.StoneGolem: case EnemyType.Ghost: return IceC;
                default: return Art.Hex(0xFFE9A0);
            }
        }

        /// <summary>Props sem rig (1 quadro `Static`) animados por codigo, como o morcego e o limo.</summary>
        static bool PropRegiao(EnemyType t) => t == EnemyType.BogFrog || t == EnemyType.BombBeetle || t == EnemyType.Minecart || t == EnemyType.Ghost
            || t == EnemyType.Spider || t == EnemyType.SpiderWidow || t == EnemyType.Salamander || t == EnemyType.SalamanderEgg;

        // ------------------------------------------------------------------ montagem

        void NewEntRegiao(Ent e, int order)
        {
            e.Glow = Art.NewSprite(e.Root, "OlhosNoEscuro", Art.Eyes(), Color.white, OOver + 3, Vector2.zero, Vector2.one * 0.3f);
            e.ZA = Art.NewSprite(e.Up, "Z", Zed(), Art.Ink, order + 7, Vector2.zero, Vector2.one * 0.22f);
            e.ZB = Art.NewSprite(e.Up, "Z", Zed(), Art.Ink, order + 7, Vector2.zero, Vector2.one * 0.15f);
            e.Weak = Art.NewSprite(e.Up, "PontoFraco", Art.Glow(), IceC, order - 1, Vector2.zero, Vector2.one * 0.8f);
            e.Lob = Art.NewSprite(e.Root, "Arremesso", Art.Triangle(), Art.Steel, OShot, Vector2.zero, Vector2.one * 0.3f);
            e.LobShadow = Art.NewSprite(e.Root, "SombraExtra", Art.Disc(), Art.A(Art.Dark, 0.4f), OActShadow, Vector2.zero, new Vector2(0.3f, 0.18f));
            e.Glow.enabled = e.ZA.enabled = e.ZB.enabled = e.Weak.enabled = e.Lob.enabled = e.LobShadow.enabled = false;
        }

        static void Look(SpriteRenderer s, Sprite sp, Color c, float x, float y, float sx, float sy, float rot = 0f)
        {
            s.enabled = true; s.sprite = sp; s.color = c;
            s.transform.localPosition = new Vector3(x, y, 0f); s.transform.localScale = new Vector3(sx, sy, 1f);
            s.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// <summary>Desenho procedural dos papeis das regioes 2-4 (forma, nao so a cor) e o sprite do arremesso. Com folha, o sprite (o
        /// procedural fica escondido no Dir). Chamado pelo PaintEnemy.</summary>
        static bool PaintRegiao(Ent v, EnemyType t, EnemyDef def)
        {
            if (t < EnemyType.GoblinMiner) return false;
            float r = def.Radius, d = 2f * r;
            bool proc = v.Anim == null;
            if (t == EnemyType.SalamanderEgg && proc && SpriteSheet.Get("ovo") is SpriteSheet ovo) { UseSprite(v, ovo, r); proc = false; }   // o `ovo` tingido
            v.Eyes.color = EyeC(t);
            v.Pupils.enabled = false;
            v.Eyes.enabled = proc;
            v.Eyes.transform.localScale = Vector3.one * (d * 0.45f);
            v.Deco.enabled = true;
            Color b = PapelRegiao(t);
            switch (t)
            {
                case EnemyType.GoblinMiner:
                    Look(v.Body, Art.Hood(), b, 0f, 0f, d * 1.05f, d * 1.05f);
                    Look(v.Deco, Art.Disc(), Art.Hex(0xB87333), 0f, 0.55f * r, d * 0.8f, d * 0.38f);   // capacete de cobre
                    Look(v.Extra, Art.Glow(), Art.Hex(0xFFD27A), 0f, 0.78f * r, d * 0.45f, d * 0.45f);   // lampada
                    break;
                case EnemyType.Mole:
                case EnemyType.MoleQueen:
                    Look(v.Body, Art.Disc(), b, 0f, 0f, d, d);
                    if (t == EnemyType.MoleQueen)
                    {
                        Look(v.Deco, Art.Triangle(), Lilac, 0f, -0.05f * r, d * 1.35f, d * 1.1f);   // capa lilas por tras
                        v.Deco.sortingOrder = v.Body.sortingOrder - 1;
                        Look(v.Extra, Art.Star(), Lilac, 0f, 0.9f * r, d * 0.5f, d * 0.5f);   // coroa de ametista
                    }
                    else
                    {
                        Look(v.Deco, Art.Triangle(), Art.Hex(0xF09AB0), 0f, -0.15f * r, d * 0.32f, d * 0.32f, 180f);   // focinho
                        Look(v.Extra, Art.Fists(), Art.Hex(0xE0C8A8), 0f, -0.1f * r, d * 1.35f, d * 1.35f);   // garras
                    }
                    break;
                case EnemyType.BogFrog:
                    Look(v.Body, Art.Disc(), b, 0f, 0f, d * 1.3f, d * 0.95f);
                    Look(v.Deco, Art.Disc(), Lilac, 0.35f * r, 0.3f * r, d * 0.25f, d * 0.25f);
                    Look(v.Extra, Art.Disc(), Lilac, -0.3f * r, 0.15f * r, d * 0.2f, d * 0.2f);
                    v.Eyes.color = Art.Gold; v.Eyes.transform.localScale = Vector3.one * (d * 0.7f);
                    break;
                case EnemyType.BombBeetle:
                    Look(v.Body, Art.Disc(), b, 0f, 0f, d, d);
                    Look(v.Deco, Art.Bars(), Art.A(VentHot, 0.85f), 0f, 0f, d * 0.8f, d * 0.8f, 90f);   // listras de aviso
                    Look(v.Extra, Art.Disc(), VentHot, 0f, 0.95f * r, d * 0.22f, d * 0.22f);   // pavio
                    break;
                case EnemyType.Minecart:
                    Look(v.Body, Art.Rounded(), b, 0f, 0f, d * 1.9f, d * 1.15f);
                    Look(v.Deco, Art.Square(), Art.SteelDark, 0f, 0.1f * r, d * 1.9f, d * 0.18f);
                    Look(v.Extra, Art.Disc(), Art.Gold, 0.8f * d, 0.6f * r, d * 0.3f, d * 0.3f);   // sino de latao
                    v.Eyes.enabled = false;
                    break;
                case EnemyType.Ghost:
                    Look(v.Body, GhostShape(), Art.A(b, 0.8f), 0f, 0f, d * 1.25f, d * 1.25f);
                    v.Deco.enabled = false;
                    v.Eyes.color = Art.Hex(0x3A3F55);
                    break;
                case EnemyType.Skeleton:
                    Look(v.Body, Art.Rounded(), b, 0f, 0f, d * 0.75f, d);
                    Look(v.Deco, Art.Skull(), b, 0f, 0.5f * r, d * 0.85f, d * 0.85f);
                    Look(v.Extra, Art.Bars(), Art.A(Art.Tinta, 0.45f), 0f, -0.25f * r, d * 0.55f, d * 0.45f, 90f);   // costelas
                    v.Eyes.transform.localScale = Vector3.one * (d * 0.35f);
                    break;
                case EnemyType.Spider:
                case EnemyType.SpiderWidow:
                    Look(v.Body, Art.Disc(), b, 0f, 0f, d, d);
                    Look(v.Deco, Legs(), Color.Lerp(b, Color.black, 0.3f), 0f, 0f, d * 1.95f, d * 1.95f);
                    v.Deco.sortingOrder = v.Body.sortingOrder - 1;
                    if (t == EnemyType.SpiderWidow) Look(v.Extra, Art.Diamond(), Lilac, 0f, -0.15f * r, d * 0.3f, d * 0.4f);   // ampulheta
                    break;
                case EnemyType.Mummy:
                    Look(v.Body, Art.Rounded(), b, 0f, 0f, d * 0.95f, d * 1.1f);
                    Look(v.Deco, Art.Bars(), Art.A(Art.Tinta, 0.3f), 0f, 0f, d * 0.95f, d * 1.05f, 90f);   // ataduras
                    break;
                case EnemyType.Gargoyle:
                    Look(v.Body, Art.Rounded(), b, 0f, 0f, d, d);
                    Look(v.Deco, Art.Horns(), Color.Lerp(b, Color.black, 0.35f), 0f, 0f, d * 1.1f, d * 1.1f);
                    Look(v.Extra, Art.Bat(), Color.Lerp(b, Color.black, 0.25f), 0f, 0.1f * r, d * 1.9f, d * 1.9f);   // asas de pedra
                    v.Extra.sortingOrder = v.Body.sortingOrder - 1;
                    break;
                case EnemyType.Salamander:
                    Look(v.Body, Art.Egg(), b, 0f, 0f, d * 0.95f, d * 1.7f, 90f);
                    Look(v.Deco, Art.Bars(), Art.A(LavaC, 0.9f), 0f, 0f, d * 1.2f, d * 0.55f);   // listras de brasa
                    break;
                case EnemyType.Imp:
                    Look(v.Body, Art.Disc(), b, 0f, 0f, d, d);
                    Look(v.Deco, Art.Horns(), Art.Hex(0x2A2426), 0f, 0f, d * 1.3f, d * 1.3f);
                    break;
                case EnemyType.KoboldBomber:
                case EnemyType.KoboldForeman:
                    Look(v.Body, Art.Hood(), b, 0f, 0f, d * (t == EnemyType.KoboldForeman ? 1.1f : 1.05f), d * 1.05f);
                    Look(v.Deco, Art.Disc(), VentHot, 0f, -0.25f * r, d * 0.5f, d * 0.5f);   // barriga laranja
                    if (t == EnemyType.KoboldForeman) Look(v.Extra, Art.Triangle(), Art.Hex(0xB07A3A), 0f, 0.95f * r, d * 1.1f, d * 0.5f);   // capacete com chifre
                    else Look(v.Extra, Art.Square(), Art.Hex(0xB07A3A), 0f, 0.45f * r, d * 0.7f, d * 0.18f);   // oculos de latao
                    break;
                case EnemyType.MagmaElemental:
                case EnemyType.MagmaShard:
                    Look(v.Body, Art.Rounded(), b, 0f, 0f, d * 1.1f, d * 1.1f);
                    Look(v.Deco, Art.Cracks(t == EnemyType.MagmaShard ? 2 : 3), LavaC, 0f, 0f, d, d);   // rachaduras de lava
                    if (t == EnemyType.MagmaElemental) Look(v.Extra, Art.Fists(), Art.Hex(0x3A3036), 0f, 0f, d * 1.45f, d * 1.45f);
                    break;
                case EnemyType.StoneGolem:
                    Look(v.Body, Art.Rounded(), b, 0f, 0f, d * 1.15f, d * 0.95f);
                    Look(v.Deco, Art.Fists(), Art.SteelDark, 0f, 0f, d * 1.5f, d * 1.5f);
                    Look(v.Extra, Art.Disc(), Art.Hex(0x4E6A4A), 0.3f * r, 0.4f * r, d * 0.25f, d * 0.2f);   // musgo
                    v.Eyes.transform.localScale = Vector3.one * (d * 0.25f);
                    break;
                case EnemyType.Necromancer:
                    Look(v.Body, Art.Hood(), b, 0f, 0f, d * 1.15f, d * 1.15f);
                    Look(v.Deco, Art.ThinRing(), Art.A(Undead, 0.7f), 0f, -0.3f * r, d * 0.9f, d * 0.3f);   // runas da barra
                    break;
                case EnemyType.ElderDragon:
                    Look(v.Body, Art.Disc(), b, 0f, 0f, d * 1.25f, d);
                    Look(v.Deco, Art.Horns(), Art.Hex(0xB07A3A), 0f, 0f, d * 1.35f, d * 1.35f);
                    Look(v.Extra, Art.Bat(), Art.Hex(0x17402F), 0f, 0.1f * r, d * 2.1f, d * 2.1f);
                    v.Extra.sortingOrder = v.Body.sortingOrder - 1;
                    break;
                case EnemyType.SalamanderEgg:
                    Look(v.Body, Art.Egg(), b, 0f, 0f, d * 1.1f, d * 1.1f);
                    Look(v.Deco, Art.Disc(), VentHot, 0.2f * r, 0.2f * r, d * 0.3f, d * 0.3f);
                    v.Eyes.enabled = false;
                    break;
                default:
                    v.Deco.enabled = false;
                    break;
            }
            v.BodyC = v.Body.color;
            // arremesso: picareta do mineiro, frasco do kobold/Capataz (sprite girando por codigo; sem folha, a forma procedural)
            SpriteSheet lob = t == EnemyType.GoblinMiner ? SpriteSheet.Get("picareta") : t == EnemyType.KoboldBomber || t == EnemyType.KoboldForeman ? SpriteSheet.Get("frasco") : null;
            v.Lob.sprite = lob != null ? lob.Frame(lob.Pick("Static"), 0, 0) : t == EnemyType.GoblinMiner ? Art.Triangle() : Art.Disc();
            v.Lob.color = lob != null ? Color.white : t == EnemyType.GoblinMiner ? Art.Steel : VentHot;
            v.Lob.transform.localScale = Vector3.one * (lob != null ? 1f : 0.3f);
            return true;
        }

        // ------------------------------------------------------------------ relogio do aviso e estado do Core

        /// <summary>Relogio proprio do aviso (as raias de regiao contam o aviso no proprio estado, nao no Enemy.Tele): comeca na entrada em
        /// Telegraph com a duracao de Enemy.Tele naquele passo (ou Def.Tele).</summary>
        void TeleClock(Run run, Ent v, Enemy e)
        {
            bool tele = e.State == EnemyState.Telegraph;
            if (tele && !v.WasTele) { v.TeleT0 = run.Time; v.TeleDur = Mathf.Max(0.1f, e.Tele > 0f ? e.Tele : e.Def.Tele); }
            v.WasTele = tele;
        }

        /// <summary>Progresso 0..1 do aviso: pelo relogio proprio nas regioes 2-4 (TeleClock); o TeleProgress do Core nas outras.</summary>
        static float TeleK(Run run, Ent v, Enemy e)
        {
            if (e.State != EnemyState.Telegraph) return 0f;
            if (e.Type < EnemyType.GoblinMiner) return e.TeleProgress;
            return Mathf.Clamp01((run.Time - v.TeleT0) / Mathf.Max(0.1f, v.TeleDur));
        }

        static ForgeMark MarkOf(Run run, int id)
        {
            foreach (ForgeMark m in run.Forge.Marks) if (m.Owner == id) return m;
            return null;
        }

        static bool OwnsWarn(Run run, int id)
        {
            foreach (Warn w in run.Warns) if (w.Owner == id) return true;
            return MarkOf(run, id) != null;
        }

        static R3Act Act3(Run run, Enemy e) { R3Foe f = run.R3Of(e); return f != null ? f.Act : R3Act.None; }
        static R2Act Act2(Run run, Enemy e) { R2Foe f = run.R2Of(e); return f != null ? f.Act : R2Act.None; }

        // ------------------------------------------------------------------ animacao

        /// <summary>Procedural (sem folha): flutuar, respirar, balancar, asas.</summary>
        void AnimProcRegiao(Ent v, Enemy e, Vector2 vel, bool tele)
        {
            float d = 2f * e.Def.Radius, w = _time + e.Phase * 6f;
            switch (e.Type)
            {
                case EnemyType.Ghost: v.Up.localPosition = new Vector3(0f, (Lift * 0.6f + 0.08f * Mathf.Sin(w * 9.42f)) / Tilt, 0f); break;
                case EnemyType.BogFrog:
                {
                    float k = Mathf.Sin(w * 4f) * 0.05f, st = tele ? 0.08f : 0f;
                    v.Body.transform.localScale = new Vector3(d * 1.3f * (1f + k + st), d * 0.95f * (1f - k - st), 1f);
                    break;
                }
                case EnemyType.BombBeetle: v.Body.transform.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(w * 37.7f)); break;
                case EnemyType.Minecart: v.Dir.localPosition = new Vector3(0f, vel.sqrMagnitude > 1e-6f ? 0.03f * Mathf.Abs(Mathf.Sin(w * 20f)) : 0f, 0f); break;
                case EnemyType.Spider: case EnemyType.SpiderWidow:
                    v.Deco.transform.localScale = Vector3.one * (d * 1.95f * (1f + 0.04f * Mathf.Sin(w * 14f)));
                    v.Body.transform.localScale = new Vector3(d, d * (tele ? 1.15f : 1f), 1f);   // empina no aviso
                    break;
                case EnemyType.Salamander: v.Body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f + 8f * Mathf.Sin(w * 25f)); break;
                case EnemyType.Gargoyle: case EnemyType.ElderDragon:
                    v.Extra.transform.localScale = new Vector3(d * (e.Type == EnemyType.Gargoyle ? 1.9f : 2.1f) * (0.85f + 0.15f * Mathf.Abs(Mathf.Sin(w * 3f))), v.Extra.transform.localScale.y, 1f);
                    break;
                case EnemyType.SalamanderEgg: v.Body.transform.localRotation = Quaternion.Euler(0f, 0f, (3f + 12f * e.TeleProgress) * Mathf.Sin(w * 20f)); break;
            }
        }

        /// <summary>Estado do Core -> clipe das folhas das regioes 2-4 (chamado no default do AnimEnemy). false = nao e papel das regioes.
        /// Nomes: CONTEUDO_REGIOES §12 / RENDER_SPRITES (clipe que a folha nao tem cai no Pick, nunca em erro).</summary>
        bool AnimRegiao(Ent v, Enemy e, ref SpriteSheet s, Vector2 p, Vector2 vel, Vector2 cp, bool tele, float impact, ref Vector2 face, ref float speed,
                        out int clip, out int frame)
        {
            clip = -1; frame = -1;
            EnemyType t = e.Type;
            if (t < EnemyType.GoblinMiner) return false;
            Run run = _run;
            int k;
            float tk = TeleK(run, v, e);
            bool hit = v.HitT > 0f && !tele && v.StrikeT <= 0f;
            if (PropRegiao(t))
            {
                // aranha e Viuva: a pose `_empinada` no aviso (cuspe, mordida, leque, fio, despencar); sem ela, o corpo estica no PropAnim
                SpriteSheet rear = Empinada(run, e, tele), b = rear ?? SheetAt(EnemySheet, (int)t);
                if (b != null) s = b;
                clip = s.Pick("Static");
                PropAnim(v, e, vel, tele && rear == null);
                return true;
            }
            if (t == EnemyType.Skeleton && _rise.TryGetValue(e.Id, out float rt) && (k = s.Clip("Rise")) >= 0) { clip = k; frame = Frac(s, k, 1f - rt / RiseTime); return true; }
            if (hit && (k = s.Clip("Hit")) >= 0) { clip = k; frame = Frac(s, k, 1f - v.HitT / 0.3f); return true; }
            if (v.RoarT > 0f && (k = First(s, SummonClip)) >= 0) { clip = k; face = cp - p; return true; }   // Summon/Call/Roar uma vez
            R2Foe f2 = run.R2Of(e);
            switch (t)
            {
                case EnemyType.GoblinMiner:
                case EnemyType.KoboldBomber:
                    if (e.Asleep && (k = s.Clip("Sleep")) >= 0) { clip = k; return true; }
                    if (v.WakeT > 0f && (k = s.Clip("Wake")) >= 0) { clip = k; frame = Frac(s, k, 1f - v.WakeT / WakeTime); return true; }
                    if ((tele || v.StrikeT > 0f) && (k = First(s, t == EnemyType.GoblinMiner ? MinerThrow : KoboldToss)) >= 0)
                    {
                        clip = k; frame = LobFrame(s, k, impact, tele, tk);
                        V2 at = f2 != null && f2.Act == R2Act.Throw ? f2.Spot : MarkOf(run, e.Id) is ForgeMark m ? m.Pos : run.Courier.Pos;
                        face = V(at) - p;
                        return true;
                    }
                    if (e.State == EnemyState.Flee) { clip = s.Pick("Walk", "Run"); face = cp - p; return true; }   // recua de frente para o courier
                    if (e.State == EnemyState.Chase) { clip = s.Pick("Run", "Walk"); return true; }
                    break;
                case EnemyType.Mole:
                case EnemyType.MoleQueen:
                    if (run.Underground(e)) { clip = s.Pick("Burrow"); frame = s.Frames(clip) - 1; return true; }   // escondido (DrawEnemyRegiao)
                    if (f2 != null && f2.Act == R2Act.Swipe && (k = s.Clip("Swipe")) >= 0)
                    { clip = k; frame = StrikeFrame(s, k, impact, tele, tk, v.StrikeT); face = cp - p; return true; }
                    if (v.StrikeT > 0f && (k = s.Clip("Emerge")) >= 0) { clip = k; frame = Frac(s, k, 1f - v.StrikeT / StrikeHold); return true; }
                    clip = s.Pick("Look", "Idle"); face = cp - p;
                    return true;
                case EnemyType.StoneGolem:
                    if (e.Stun > 0f && (k = s.Clip("Stun")) >= 0) { clip = k; return true; }
                    if (f2 != null && (f2.Act == R2Act.ChargeAim || f2.Act == R2Act.Charge) && (k = s.Clip("Charge")) >= 0) { clip = k; face = V(f2.Dir); return true; }
                    if ((tele || v.StrikeT > 0f) && (k = s.Clip("Slam")) >= 0)
                    { clip = k; frame = StrikeFrame(s, k, impact, tele, tk, v.StrikeT); face = f2 != null ? V(f2.Dir) : cp - p; return true; }
                    if (e.State == EnemyState.Chase) { clip = s.Pick("Walk"); if (f2 != null) face = V(f2.Dir); return true; }
                    clip = s.Pick("Idle"); face = f2 != null ? V(f2.Dir) : cp - p;   // o Golem gira devagar: a frente vem do Core
                    return true;
                case EnemyType.Gargoyle:
                    switch (Act3(run, e))
                    {
                        case R3Act.Statue: clip = s.Pick("Statue", "Idle"); return true;
                        case R3Act.Wake: if ((k = s.Clip("Wake")) >= 0) { clip = k; frame = Frac(s, k, tk); return true; } break;
                        case R3Act.Dive: case R3Act.Fly: clip = s.Pick("Dive", "Wake"); return true;
                        case R3Act.Perch: clip = s.Pick("Perch", "Idle"); return true;
                    }
                    break;
                case EnemyType.Mummy:
                    if (Act3(run, e) == R3Act.Hold && (k = s.Clip("Hold")) >= 0) { clip = k; return true; }
                    if (Act3(run, e) == R3Act.Grab && (k = s.Clip("Grab")) >= 0) { clip = k; frame = Frac(s, k, tk); face = cp - p; return true; }
                    break;
                case EnemyType.Necromancer:
                    if (Act3(run, e) == R3Act.Raise && (k = First(s, SummonClip)) >= 0) { clip = k; return true; }
                    if (Act3(run, e) == R3Act.Cast && (k = s.Clip("Cast")) >= 0) { clip = k; face = cp - p; return true; }   // o raio e o Warn Beam
                    break;
                case EnemyType.ElderDragon:
                    if (run.Forge.Flying && (k = s.Clip("Fly")) >= 0) { clip = k; return true; }
                    if (MarkOf(run, e.Id) is ForgeMark bm && bm.Kind == ForgeMarkKind.Breath && (k = s.Clip("Breath")) >= 0)
                    {   // pescoco esticando pelo aviso; boca aberta (ultimo quadro) enquanto o sopro sai (Active)
                        clip = k; frame = bm.Active > 0f ? s.Frames(k) - 1 : Frac(s, k, bm.Total > 0f ? 1f - bm.Left / bm.Total : 1f);
                        face = V(bm.Dir); return true;
                    }
                    if (run.Forge.HeadDown > 0f && (k = s.Clip("Land")) >= 0) { clip = k; return true; }   // cabeca baixa: a janela de dano
                    break;
                case EnemyType.Imp:
                    if (tele && MarkOf(run, e.Id) is ForgeMark im && im.Kind == ForgeMarkKind.Swap && (k = s.Clip("Swap")) >= 0) { clip = k; frame = Frac(s, k, tk); return true; }
                    break;
                case EnemyType.KoboldForeman:
                    if (MarkOf(run, e.Id) is ForgeMark fm && fm.Kind == ForgeMarkKind.Spin && (k = s.Clip("Spin")) >= 0) { clip = k; frame = StrikeFrame(s, k, impact, tele, tk, v.StrikeT); return true; }
                    if (MarkOf(run, e.Id) is ForgeMark fl && fl.Kind == ForgeMarkKind.Flask && (k = First(s, KoboldToss)) >= 0) { clip = k; frame = LobFrame(s, k, impact, tele, tk); face = V(fl.Pos) - p; return true; }
                    break;
            }
            // golpe do papel pelo nome (regioes 3-4) com o quadro de impacto no fim do aviso
            string[] atk = AttackOf(t);
            if ((tele || v.StrikeT > 0f) && atk != null && (k = First(s, atk)) >= 0)
            { clip = k; frame = StrikeFrame(s, k, impact, tele, tk, v.StrikeT); face = cp - p; return true; }
            if (e.State == EnemyState.Chase) { clip = s.Pick("Walk", "Run"); return true; }
            clip = s.Pick("Idle"); face = cp - p;
            return true;
        }

        /// <summary>Pose empinada da aranha/Viuva no aviso (folha `aranha_empinada` / `viuva_empinada`), ou null.</summary>
        static SpriteSheet Empinada(Run run, Enemy e, bool tele)
        {
            R3Act a = Act3(run, e);
            if (e.Type == EnemyType.Spider && (tele || a == R3Act.Spit)) return SpriteSheet.Get("aranha_empinada");
            if (e.Type == EnemyType.SpiderWidow && (tele || a == R3Act.Fan || a == R3Act.Thread || a == R3Act.Pull || a == R3Act.Drop))
                return SpriteSheet.Get("viuva_empinada");
            return null;
        }

        static readonly string[] MinerThrow = { "Throw" }, KoboldToss = { "Toss", "Throw" }, SummonClip = { "Summon", "Call", "Roar" },
            AtkSkeleton = { "Attack" }, AtkMummy = { "Grab", "Attack" }, AtkGargoyle = { "Dive", "Wake" }, AtkNecro = { "Cast" },
            AtkImp = { "Scratch", "Swap" }, AtkMagma = { "Smash" }, AtkShard = { "Punch", "Smash" }, AtkForeman = { "Whip", "Spin" },
            AtkDragon = { "Breath" };

        static string[] AttackOf(EnemyType t)
        {
            switch (t)
            {
                case EnemyType.Skeleton: return AtkSkeleton;
                case EnemyType.Mummy: return AtkMummy;
                case EnemyType.Gargoyle: return AtkGargoyle;
                case EnemyType.Necromancer: return AtkNecro;
                case EnemyType.Imp: return AtkImp;
                case EnemyType.MagmaElemental: return AtkMagma;
                case EnemyType.MagmaShard: return AtkShard;
                case EnemyType.KoboldForeman: return AtkForeman;
                case EnemyType.ElderDragon: return AtkDragon;
                default: return null;
            }
        }

        /// <summary>Quadro na fracao f (0..1) do clipe.</summary>
        static int Frac(SpriteSheet s, int clip, float f) { int n = s.Frames(clip); return Mathf.Clamp((int)(Mathf.Clamp01(f) * n), 0, n - 1); }

        /// <summary>Arremesso com o projetil em voo na 2a metade do aviso (cai no Strike): [0, soltura) na 1a metade, [soltura, fim) na 2a.</summary>
        static int LobFrame(SpriteSheet s, int clip, float impact, bool tele, float prog)
        {
            int n = s.Frames(clip), k = Mathf.Clamp(Mathf.RoundToInt(impact * n), 1, n - 1);
            if (!tele) return n - 1;
            return prog < 0.5f ? Mathf.Min(k - 1, (int)(prog / 0.5f * k)) : Mathf.Min(n - 1, k + (int)((prog - 0.5f) / 0.5f * (n - k)));
        }

        /// <summary>Props das regioes com folha (1 quadro): respirar, balancar, flutuar, sacolejar; flipX pelo movimento (como o morcego).</summary>
        void PropAnim(Ent v, Enemy e, Vector2 vel, bool tele)
        {
            float w = _time + e.Phase * 6f;
            if (Mathf.Abs(vel.x) > 1e-4f) v.FlipX = vel.x > 0f;
            Transform sp = v.Spr.transform;
            switch (e.Type)
            {
                case EnemyType.BogFrog: { float k = 0.05f * Mathf.Sin(w * 4f), st = tele ? 0.08f : 0f; sp.localScale = new Vector3(1f + k + st, 1f - k - st, 1f); break; }
                case EnemyType.BombBeetle: sp.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(w * 37.7f)); break;
                case EnemyType.Minecart:
                    v.Up.localPosition = new Vector3(0f, vel.sqrMagnitude > 1e-6f ? 0.03f * Mathf.Abs(Mathf.Sin(w * 20f)) / Tilt : 0f, 0f);
                    sp.localRotation = Quaternion.Euler(0f, 0f, vel.sqrMagnitude > 1e-6f ? 2f * Mathf.Sin(w * 17f) : 0f);
                    break;
                case EnemyType.Ghost: v.Up.localPosition = new Vector3(0f, (Lift * 0.6f + 0.08f * Mathf.Sin(w * 9.42f)) / Tilt, 0f); break;
                case EnemyType.Spider: case EnemyType.SpiderWidow:
                { float k = 0.04f * Mathf.Sin(w * 14f); sp.localScale = new Vector3(1f + k, (1f - k) * (tele ? 1.15f : 1f), 1f); break; }
                case EnemyType.Salamander: sp.localRotation = Quaternion.Euler(0f, 0f, 8f * Mathf.Sin(w * 25f)); break;
                case EnemyType.SalamanderEgg: sp.localRotation = Quaternion.Euler(0f, 0f, (3f + 12f * e.TeleProgress) * Mathf.Sin(w * 20f)); break;
            }
        }

        // ------------------------------------------------------------------ telegraph

        /// <summary>Aviso das regioes 2-4. Regiao 2 pelo R2Foe do Core: picareta e monte = circulo no Spot (raio do Core; monte em terra),
        /// lingua = linha lilas ate o Spot, investida = faixa ate o Spot, pancada = circulo do Radius; vagonete, toupeira por baixo e o
        /// tunel = sem circulo (o trilho acende). Regioes 3-4: se o Core publicou o aviso (run.Warns / run.Forge.Marks), ele e desenhado no
        /// DrawMarks e aqui nada. true = tratou (o DrawEnemies nao desenha o circulo do Reach).</summary>
        bool TeleRegiao(Run run, Ent v, Enemy e, Vector2 p, bool tele)
        {
            if (e.Type < EnemyType.GoblinMiner) return false;
            R2Foe f = run.R2Of(e);
            float k = Mathf.Max(0.05f, TeleK(run, v, e));
            bool last = e.State == EnemyState.Telegraph && k > 0.88f;
            if (e.Def.Hazard || (f != null && (f.Act == R2Act.Under || f.Act == R2Act.Tunnel || f.Act == R2Act.Bell || f.Act == R2Act.Pass || f.Act == R2Act.Charge)))
            { HideTele(v); return true; }
            if (f != null && (f.Act == R2Act.Throw || f.Act == R2Act.Mound || f.Act == R2Act.Slam))
            {
                bool mound = f.Act == R2Act.Mound;
                Vector2 at = f.Act == R2Act.Slam ? Vector2.zero : V(f.Spot) - p;
                float r = f.Radius > 0f ? f.Radius : e.Def.Reach;
                v.AimLine.enabled = v.AimFill.enabled = false;
                v.Ring.enabled = v.Fill.enabled = true;
                v.Ring.sprite = Art.ThinRing(); v.Fill.sprite = Art.Disc();
                v.Ring.transform.localPosition = v.Fill.transform.localPosition = at;
                v.Ring.transform.localRotation = v.Fill.transform.localRotation = Quaternion.identity;
                v.Ring.transform.localScale = Vector3.one * (2f * r);
                v.Fill.transform.localScale = Vector3.one * (2f * r * k);
                v.Ring.color = last ? Color.white : Art.A(Art.Tele, 0.9f);
                v.Fill.color = mound ? Art.A(Art.Hex(0x6A4A30), 0.75f) : Art.A(Art.Tele, 0.25f);   // o monte de terra cresce sob o alvo
                if (mound && Random.value < 0.25f) Puff(Art.Disc(), V(f.Spot) + Random.insideUnitCircle * r * 0.6f, new Vector2(0f, 0.6f), Art.Hex(0x6A4A30), 0.12f, 0.04f, 0.4f);
                return true;
            }
            if (f != null && (f.Act == R2Act.Tongue || f.Act == R2Act.ChargeAim))
            {
                Vector2 to = V(f.Spot) - p;
                float dist = to.magnitude, ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                Vector2 dir = dist > 1e-4f ? to / dist : Vector2.down;
                bool tongue = f.Act == R2Act.Tongue;
                float len = Mathf.Max(0.1f, dist - e.Def.Radius), w = tongue ? 0.18f : 2f * Mathf.Max(0.3f, f.Radius);
                v.Ring.enabled = v.Fill.enabled = false;
                v.AimLine.enabled = v.AimFill.enabled = true;
                Line(v.AimLine, dir, e.Def.Radius, len, tongue ? 0.06f : w, ang);
                Line(v.AimFill, dir, e.Def.Radius, len * k, w, ang);
                Color c = tongue ? Lilac : Art.Tele;
                v.AimLine.color = last ? Color.white : Art.A(c, tongue ? 0.9f : 0.2f);
                v.AimFill.color = Art.A(c, tongue ? 0.35f : 0.4f);
                return true;
            }
            if (OwnsWarn(run, e.Id)) { HideTele(v); return true; }   // regioes 3-4: o Core desenha o aviso (DrawMarks)
            return false;
        }

        static void HideTele(Ent v) => v.Ring.enabled = v.Fill.enabled = v.AimLine.enabled = v.AimFill.enabled = false;

        /// <summary>A carga no chao em q leva o golpe que esta no aviso (regioes 2-4)? Regiao 2 pelo R2Foe; regioes 3-4 pelos avisos do Core;
        /// no circulo do Reach, so quem racha a carga no chao (Golem, elemental, besouro, Dragao).</summary>
        bool InTeleRegiao(Run run, V2 q)
        {
            float cr = Balance.CargoR;
            foreach (Enemy e in run.Enemies)
            {
                if (!e.Alive || e.Type < EnemyType.GoblinMiner) continue;
                R2Foe f = run.R2Of(e);
                if (f != null)
                {
                    if ((f.Act == R2Act.Throw || f.Act == R2Act.Mound) && V2.Dist(q, f.Spot) <= f.Radius + cr) return true;
                    if (f.Act == R2Act.Slam && V2.Dist(q, e.Pos) <= f.Radius + cr) return true;
                    if (f.Act == R2Act.Tongue && V2.Dist(q, f.Spot) <= 0.4f + cr) return true;
                    if (f.Act == R2Act.ChargeAim && V2.SegDist(q, e.Pos, f.Spot) <= f.Radius + cr) return true;
                }
                if (e.State == EnemyState.Telegraph && !OwnsWarn(run, e.Id) && (f == null || f.Act == R2Act.None || f.Act == R2Act.Fuse))
                {
                    bool smash = e.Type == EnemyType.StoneGolem || e.Type == EnemyType.MagmaElemental || e.Type == EnemyType.BombBeetle || e.Type == EnemyType.ElderDragon;
                    if (smash && V2.Dist(e.Pos, q) <= e.Def.Reach + cr) return true;
                }
            }
            foreach (Warn w in run.Warns)
            {
                if (w.Kind == WarnKind.Circle && V2.Dist(q, w.From) <= w.Radius + cr) return true;
                if ((w.Kind == WarnKind.Strip || w.Kind == WarnKind.Beam) && V2.SegDist(q, w.From, w.To) <= w.Width * 0.5f + cr) return true;
                if (w.Kind == WarnKind.Thread && V2.Dist(q, w.To) <= 0.4f + cr) return true;
            }
            foreach (ForgeMark m in run.Forge.Marks)
            {
                switch (m.Kind)
                {
                    case ForgeMarkKind.Flask: case ForgeMarkKind.Spin: case ForgeMarkKind.Land: case ForgeMarkKind.Swap:
                        if (V2.Dist(q, m.Pos) <= m.Radius + cr) return true; break;
                    case ForgeMarkKind.Whip: if (V2.SegDist(q, m.Pos, m.Pos + m.Dir * m.Length) <= m.Radius + cr) return true; break;
                    case ForgeMarkKind.Breath:
                    {
                        V2 w = q - m.Pos; float l = w.Len;
                        if (l <= m.Length + cr && (l < 1e-4f || V2.Dot(w, m.Dir) / l >= Balance.BreathCos)) return true;
                        break;
                    }
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ por quadro, depois do DrawEnemies de cada inimigo

        /// <summary>Escuro (fora da luz: so olhos e sombra), corpo escondido (toupeira por baixo, vagonete no tunel, Viuva no teto), zZ e o anel
        /// de 2 m, Wake, Rise, ponto fraco do Golem (nas costas), Dragao no ar e de cabeca baixa, arremesso em arco, mumia segurando o courier,
        /// besouro piscando, gargula de pedra, fantasma translucido, ondas na agua.</summary>
        void DrawEnemyRegiao(Run run, Ent v, Enemy e, Vector2 p, Vector2 vel, float headY, float dt)
        {
            EnemyType t = e.Type;
            bool tele = e.State == EnemyState.Telegraph;
            v.HitT = Mathf.Max(0f, v.HitT - dt); v.WakeT = Mathf.Max(0f, v.WakeT - dt);
            if (v.WasAsleep && !e.Asleep) v.WakeT = WakeTime;
            v.WasAsleep = e.Asleep;
            R2Act a2 = t >= EnemyType.GoblinMiner ? Act2(run, e) : R2Act.None;
            R3Act a3 = t >= EnemyType.GoblinMiner ? Act3(run, e) : R3Act.None;
            bool under = t >= EnemyType.GoblinMiner && run.Underground(e), tunnel = a2 == R2Act.Tunnel, ceiling = a3 == R3Act.Ceiling;
            // escuro: fora da luz do Core (run.InLight) o corpo some e ficam os olhos e a sombra; o vagonete e projetil (sempre visivel)
            bool lit = !run.Lit || e.Def.Hazard || run.InLight(e.Pos), hidden = under || tunnel || ceiling;
            bool show = lit && !hidden;
            if (v.Shown != show) { v.Shown = show; v.Up.gameObject.SetActive(show); }
            v.Glow.enabled = !lit && !hidden;
            if (!lit && !hidden)
            {
                v.Glow.transform.localPosition = new Vector3(0f, headY * 0.8f / Tilt, 0f);
                v.Glow.transform.localScale = Vector3.one * Mathf.Max(0.25f, e.Def.Radius * 0.9f);
                v.Glow.color = Art.A(EyeC(t), 0.75f + 0.25f * Mathf.Sin(_time * 3f + e.Phase * 6f));
            }
            if (v.Anim != null) v.Shadow.enabled = !hidden;
            if (t < EnemyType.GoblinMiner) return;
            // por baixo da terra: rastro de terra e o som de cavar
            if (under && vel.sqrMagnitude > 1e-6f && (v.StepT -= dt) <= 0f)
            {
                v.StepT = 0.12f;
                Puff(Art.Disc(), p + Random.insideUnitCircle * 0.2f, Vector2.zero, Art.A(Art.Hex(0x6A4A30), 0.85f), t == EnemyType.MoleQueen ? 0.55f : 0.38f, 0.2f, 0.9f, OOver);
                Audio.Play("toupeira_cava", 1f, 0.6f);
            }
            // dormindo: zZ subindo e, com o courier a 3 m, o anel de 2 m em que ele acorda
            v.ZA.enabled = v.ZB.enabled = e.Asleep;
            if (e.Asleep)
            {
                float u = Mathf.Repeat(_time * 0.6f + e.Phase, 1f), u2 = Mathf.Repeat(u + 0.5f, 1f);
                v.ZA.transform.localPosition = new Vector3(0.2f + 0.15f * u, headY + 0.15f + 0.45f * u, 0f); v.ZA.color = Art.A(Art.Ink, 1f - u);
                v.ZB.transform.localPosition = new Vector3(0.25f + 0.15f * u2, headY + 0.15f + 0.45f * u2, 0f); v.ZB.color = Art.A(Art.Ink, 1f - u2);
                bool near = V2.Dist(e.Pos, run.Courier.Pos) <= Balance.WakeRange + 1f;
                v.Ring.enabled = near;
                if (near)
                {
                    v.Ring.sprite = Art.ThinRing();
                    v.Ring.transform.localPosition = Vector3.zero; v.Ring.transform.localRotation = Quaternion.identity;
                    v.Ring.transform.localScale = Vector3.one * (2f * Balance.WakeRange);
                    v.Ring.color = Art.A(Art.Ink, 0.35f);
                }
            }
            // esqueleto remontando (EnemyRevive): pop verde
            if (_rise.TryGetValue(e.Id, out float rt))
            {
                rt -= dt;
                if (rt <= 0f) _rise.Remove(e.Id); else _rise[e.Id] = rt;
            }
            // Golem: ponto fraco (cristais ciano) NAS COSTAS = atras da frente do Core (R2Foe.Dir); de costas para a camera fica na frente
            // do corpo, de frente fica atras (aparece em volta). Dragao de cabeca baixa: brilho dourado na cabeca (dano x2)
            v.Weak.enabled = t == EnemyType.StoneGolem || (t == EnemyType.ElderDragon && run.Forge.HeadDown > 0f);
            if (t == EnemyType.StoneGolem)
            {
                R2Foe f = run.R2Of(e);
                V2 front = f != null ? f.Dir : new V2(0f, -1f);
                float h = (v.Anim != null ? Lift * At(EnemyH, (int)t, 2.6f) : e.Def.Radius) * 0.62f;
                v.Weak.transform.localPosition = new Vector3(-front.X * 0.45f, h - front.Y * 0.25f, 0f);
                v.Weak.sortingOrder = front.Y < 0f ? OEnemy - 1 : OEnemy + 1;
                v.Weak.color = Art.A(IceC, 0.55f + 0.35f * Mathf.Sin(_time * 4f));
                v.Weak.transform.localScale = Vector3.one * (0.7f + 0.1f * Mathf.Sin(_time * 4f));
            }
            else if (v.Weak.enabled)
            {
                v.Weak.transform.localPosition = new Vector3(0f, headY * 0.8f, 0f);
                v.Weak.sortingOrder = OEnemy + 1;
                v.Weak.color = Art.A(Art.Gold, 0.5f + 0.4f * Mathf.Sin(_time * 10f));
                v.Weak.transform.localScale = Vector3.one * 1.2f;
            }
            // Dragao no ar (intocavel; Enemy.Pos = a sombra): o corpo sobe 2 m e a sombra grande segue o courier
            bool flying = t == EnemyType.ElderDragon && run.Forge.Flying;
            if (t == EnemyType.ElderDragon)
            {
                v.Up.localPosition = new Vector3(0f, flying ? Lift * 2f / Tilt : 0f, 0f);
                bool head = run.Forge.HeadDown > 0f;   // pousou: 3 s de dano x2
                if (head && !v.WasHead) Float(p, Textos.DanoEmDobro, Art.Gold, 48);
                v.WasHead = head;
            }
            // sombra extra: Viuva no teto (sombra pulsando onde vai despencar) e Dragao no ar
            bool arco = (t == EnemyType.GoblinMiner && a2 == R2Act.Throw) || ((t == EnemyType.KoboldBomber || t == EnemyType.KoboldForeman) && MarkOf(run, e.Id) is ForgeMark fm && fm.Kind == ForgeMarkKind.Flask);
            float lk = TeleK(run, v, e);
            if ((t == EnemyType.KoboldBomber || t == EnemyType.KoboldForeman) && MarkOf(run, e.Id) is ForgeMark fk && fk.Kind == ForgeMarkKind.Flask && fk.Total > 0f)
                lk = 1f - fk.Left / fk.Total;   // o frasco pelo relogio do aviso do Core
            bool lob = arco && lk >= 0.5f;
            v.Lob.enabled = lob;
            v.LobShadow.enabled = lob || ceiling || flying;
            if (lob)
            {
                V2 at = a2 == R2Act.Throw ? run.R2Of(e).Spot : MarkOf(run, e.Id).Pos;
                float k = (lk - 0.5f) / 0.5f;
                Vector2 g = (V(at) - p) * k;
                v.Lob.transform.localPosition = g + new Vector2(0f, Lift * (0.9f * (1f - k) + 4f * 1.2f * k * (1f - k)) / Tilt);
                v.Lob.transform.localRotation = Quaternion.Euler(0f, 0f, -720f * k);
                v.LobShadow.transform.localPosition = g;
                v.LobShadow.transform.localScale = new Vector3(0.25f + 0.15f * k, 0.15f + 0.08f * k, 1f);
                v.LobShadow.color = Art.A(Art.Dark, 0.4f);
            }
            else if (ceiling || flying)
            {
                float s = ceiling ? 1.6f : 3.2f, pulse = 1f + 0.08f * Mathf.Sin(_time * 6f);
                v.LobShadow.transform.localPosition = flying ? V(run.Forge.Shadow) - p : Vector2.zero;   // no ar, Enemy.Pos = a sombra
                v.LobShadow.transform.localScale = new Vector3(s * pulse, s * 0.6f * pulse, 1f);
                v.LobShadow.color = Art.A(Art.Dark, 0.5f);
            }
            // mumia segurando o courier: atadura do corpo dela ate ele
            if (t == EnemyType.Mummy && run.GrabbedBy == e)
            {
                Vector2 to = V(run.Courier.Pos) - p;
                v.Line.enabled = true;
                v.Line.color = Art.A(Art.Hex(0xCDBE9A), 0.9f);
                v.Line.transform.localPosition = to * 0.5f;
                v.Line.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
                v.Line.transform.localScale = new Vector3(to.magnitude, 0.12f, 1f);
            }
            else if (v.Line.enabled && t != EnemyType.Thief && t != EnemyType.Captain) v.Line.color = Art.A(Art.Role(EnemyType.Thief), 0.6f);
            // tinta pelo estado
            Color tint = Color.white;
            if (t == EnemyType.BombBeetle && (tele || a2 == R2Act.Fuse)) tint = Color.Lerp(Color.white, Art.Bad, 0.5f + 0.5f * Mathf.Sin(_time * 30f));   // pavio
            else if (t == EnemyType.Gargoyle && run.Untouchable(e))   // intocavel: de pedra no pedestal, ou translucida voltando a ele
                tint = a3 == R3Act.Fly ? new Color(1f, 1f, 1f, 0.55f) : new Color(0.62f, 0.68f, 0.62f);
            else if (t == EnemyType.Necromancer && a3 == R3Act.Smoke) tint = new Color(1f, 1f, 1f, 0.4f);   // fumaca do salto
            else if (t == EnemyType.Ghost) tint = new Color(1f, 1f, 1f, 0.78f);
            else if (t == EnemyType.SalamanderEgg && v.Anim != null) tint = new Color(0.3f, 0.24f, 0.24f);   // o `ovo` preto
            if (tint != Color.white)
            {
                if (v.Anim != null) v.Spr.color = Mul(v.Spr.color, tint);
                else v.Body.color = Mul(v.Body.color, tint);
            }
            // agua: ondas no pe de quem anda nela (voadores e quem esta por baixo nao)
            if (!under && run.Room.Water.Length > 0 && t != EnemyType.Ghost && vel.sqrMagnitude > 1e-6f && run.InWater(e.Pos) && (v.StepT -= dt) <= 0f)
            {
                v.StepT = 0.35f;
                Puff(Art.ThinRing(), p, Vector2.zero, Art.A(WaterHi, 0.7f), 0.3f, 1f, 0.45f, ORing);
            }
        }

        // ------------------------------------------------------------------ cargas novas

        static Color CargoCor(CargoType t)
        {
            switch (t)
            {
                case CargoType.ArcaneHeart: return Art.Hex(0xC2187A);
                case CargoType.IceCrystal: return IceC;
                case CargoType.GriffinCage: return Art.Hex(0xC9A040);
                case CargoType.Lantern: return Art.Hex(0xB08D57);
                case CargoType.Mirror: return Art.Gold;
                case CargoType.Balloon: return Art.Hex(0xFFD54A);
                case CargoType.Flytrap: return Art.Hex(0x5FA83A);
                case CargoType.TreasureMap: return Art.Hex(0xEADFC6);
                default: return Art.CargoColor(t);
            }
        }

        void NewCargoRegiao(CargoV v)
        {
            v.Aura = Art.NewSprite(v.Root, "Ima", Art.DashedRing(), Art.A(CargoCor(CargoType.ArcaneHeart), 0.3f), ORing + 1, Vector2.zero, Vector2.one);
            v.Lamp = Art.NewSprite(v.Up, "Luz", Art.Glow(), Flame, -1, Vector2.zero, Vector2.one);
            v.Str = Art.NewSprite(v.Up, "Fio", Art.Square(), Art.A(Art.Ink, 0.85f), 0, Vector2.zero, Vector2.one);
            v.IceBg = Art.NewSprite(v.Up, "GeloFundo", Art.Square(), Art.A(Art.Tinta, 0.85f), 6, Vector2.zero, new Vector2(0.62f, 0.13f));
            v.IceFill = Art.NewSprite(v.Up, "Gelo", Art.Square(), IceC, 7, Vector2.zero, new Vector2(0.56f, 0.07f));
            v.Aura.enabled = v.Lamp.enabled = v.Str.enabled = v.IceBg.enabled = v.IceFill.enabled = false;
        }

        /// <summary>Forma procedural das 8 cargas novas (com folha, o sprite): coracao, losango de gelo, gaiola com grade, lanterna estreita
        /// com chama, espelho oval em moldura dourada, balao listrado, planta com boca, mapa com o X.</summary>
        static void PaintCargoRegiao(CargoV v, CargoType t)
        {
            Transform dt = v.Deco.transform;
            dt.localPosition = Vector3.zero; dt.localScale = Vector3.one * 0.4f;
            switch (t)
            {
                case CargoType.ArcaneHeart: v.Body.sprite = Art.Heart(); v.Deco.sprite = Art.Disc(); v.Deco.color = Art.A(Color.white, 0.5f); dt.localPosition = new Vector3(-0.15f, 0.15f, 0f); dt.localScale = Vector3.one * 0.2f; break;
                case CargoType.IceCrystal: v.Body.sprite = Art.Diamond(); v.Deco.sprite = Art.Diamond(); v.Deco.color = Art.A(Color.white, 0.55f); dt.localScale = Vector3.one * 0.45f; break;
                case CargoType.GriffinCage: v.Body.sprite = Art.Rounded(); v.Deco.sprite = Art.Bars(); v.Deco.color = Art.SteelDark; dt.localScale = Vector3.one * 0.8f; break;
                case CargoType.Lantern: v.Body.sprite = Art.Rounded(); v.Body.transform.localScale = new Vector3(0.7f, 1f, 1f); v.Deco.sprite = Art.Disc(); v.Deco.color = Art.Hex(0xFFD27A); dt.localScale = Vector3.one * 0.42f; break;
                case CargoType.Mirror: v.Body.sprite = Art.Egg(); v.Deco.sprite = Art.Egg(); v.Deco.color = Art.Hex(0x9FE6F0); dt.localScale = Vector3.one * 0.72f; break;
                case CargoType.Balloon: v.Body.sprite = Art.Disc(); v.Deco.sprite = Art.Bars(); v.Deco.color = Art.A(Art.Cream, 0.55f); dt.localScale = Vector3.one * 0.85f; break;
                case CargoType.Flytrap: v.Body.sprite = Art.Disc(); v.Deco.sprite = Art.Triangle(); v.Deco.color = Art.Hex(0xD8344A); dt.localPosition = new Vector3(0f, -0.1f, 0f); dt.localScale = Vector3.one * 0.45f; dt.localRotation = Quaternion.Euler(0f, 0f, 180f); break;
                case CargoType.TreasureMap: v.Body.sprite = Art.Rounded(); v.Body.transform.localScale = new Vector3(1f, 0.7f, 1f); v.Deco.sprite = Art.Diamond(); v.Deco.color = Art.Hex(0xB3262E); dt.localPosition = new Vector3(0.15f, -0.08f, 0f); dt.localScale = Vector3.one * 0.25f; break;
                default: return;   // carga sem desenho: a forma de reserva do Art.CargoSprite
            }
            if (v.Sheet == null) v.Outline.sprite = v.Body.sprite;
        }

        /// <summary>Estado das cargas novas no mundo (depois do DrawCargoV1). pos = pe da carga (m da sala).</summary>
        void DrawCargoRegiao(Run run, CargoV v, Cargo g, bool ground, Vector2 pos, float dt)
        {
            CargoType t = g.Type;
            bool spr = v.Sheet != null, held = g.State == CargoState.Held, on = g.State != CargoState.Gone;
            if (!spr && t >= CargoType.ArcaneHeart && v.HitT <= 0.24f) v.Body.color = CargoCor(t);   // o Art.CargoColor ainda nao tem as cores novas
            v.Up.localPosition = Vector3.zero;
            float top = v.Vis.localPosition.y + 0.45f * (spr ? At(CargoSize, (int)t, 0.5f) : ground ? 0.62f : 0.5f);
            // coracao: anel do ima (Balance.HeartRing) e pulso
            v.Aura.enabled = t == CargoType.ArcaneHeart && on;
            if (v.Aura.enabled)
            {
                v.Aura.transform.localScale = Vector3.one * (2f * Balance.HeartRing);
                v.Aura.transform.localRotation = Quaternion.Euler(0f, 0f, _time * 12f);
                v.Aura.color = Art.A(CargoCor(CargoType.ArcaneHeart), 0.28f + 0.1f * Mathf.Sin(_time * 3f));
                float k = 1f + 0.07f * Mathf.Sin(_time * 7.5f);
                v.Vis.localScale *= k; v.Spr.transform.localScale *= k;
            }
            // lanterna: luz quente acesa (o veu abre o buraco pelo Core); apagada solta fumaca. Espelho: brilho correndo
            v.Lamp.enabled = (t == CargoType.Lantern && !g.Out) || t == CargoType.Mirror;
            if (t == CargoType.Lantern && !g.Out)
            {
                v.Lamp.transform.localPosition = v.Vis.localPosition;
                v.Lamp.transform.localScale = Vector3.one * (1.6f + 0.15f * Mathf.Sin(_time * 9f));
                v.Lamp.color = Art.A(Flame, 0.55f);
            }
            else if (t == CargoType.Mirror)
            {
                float u = Mathf.Repeat(_time * 0.5f, 1f);
                v.Lamp.transform.localPosition = v.Vis.localPosition + new Vector3(-0.15f + 0.3f * u, 0.1f, 0f);
                v.Lamp.transform.localScale = Vector3.one * 0.35f;
                v.Lamp.color = Art.A(Color.white, 0.7f * Mathf.Sin(u * Mathf.PI));
            }
            if (spr && t == CargoType.Lantern) v.Spr.sprite = Static(g.Out ? SpriteSheet.Get("lanterna_apagada") ?? v.Sheet : v.Sheet);
            if (spr && t == CargoType.Flytrap) v.Spr.sprite = Static(_biteT > 0f && g.Id == _biteId ? SpriteSheet.Get("planta_aberta") ?? v.Sheet : v.Sheet);
            if (t == CargoType.Lantern && g.Out && on && (_lampT -= dt) <= 0f)
            {
                _lampT = 0.5f;
                Puff(Art.Disc(), pos + new Vector2(0f, 0.5f / Tilt), new Vector2(Random.Range(-0.2f, 0.2f), 0.8f), Art.A(Art.Dim, 0.6f), 0.1f, 0.3f, 0.8f);
            }
            // balao: preso pelo fio 1 m acima das maos; no chao sobe (Cargo.Fuse = s no chao) e escapa em Balance.BalloonRise: anel e 4-3-2-1
            v.Str.enabled = t == CargoType.Balloon && (held || (ground && g.Fuse > 0f));
            if (t == CargoType.Balloon)
            {
                if (held) { v.Str.transform.localPosition = new Vector3(0f, -BalloonLift / 2f - 0.1f, 0f); v.Str.transform.localScale = new Vector3(0.025f, BalloonLift, 1f); }
                else if (ground)
                {
                    float k = Mathf.Clamp01(g.Fuse / Balance.BalloonRise), lift = 0.9f * k + 0.05f * Mathf.Sin(_time * 3f);
                    v.Up.localPosition = new Vector3(0f, lift / Tilt, 0f);
                    v.Str.transform.localPosition = new Vector3(0f, -lift / 2f, 0f); v.Str.transform.localScale = new Vector3(0.025f, Mathf.Max(0.01f, lift), 1f);
                }
                if (ground && g.Fuse > 0f && run.Running) Countdown(v, Balance.BalloonRise - g.Fuse, Mathf.Clamp01(g.Fuse / Balance.BalloonRise), pos, top + 0.9f, dt);
                else if (v.Num != null && v.Num.enabled) { v.Num.enabled = false; v.NumShown = -1; }
            }
            // cristal: barra do gelo (run.IceOf: >= 70 sem rachadura, >= 40 uma); gotas; na agua brilha recongelando; perto do fogo pinga
            bool ice = t == CargoType.IceCrystal && on;
            v.IceBg.enabled = v.IceFill.enabled = ice;
            if (ice)
            {
                float lv = Mathf.Clamp01(run.IceOf(g) / 100f), y = top + 0.22f;
                v.IceBg.transform.localPosition = new Vector3(0f, y, 0f);
                v.IceFill.transform.localPosition = new Vector3(-0.28f + 0.28f * lv, y, 0f);
                v.IceFill.transform.localScale = new Vector3(0.56f * lv, 0.07f, 1f);
                v.IceFill.color = lv * 100f >= Balance.IceIntact ? Color.Lerp(IceC, Color.white, 0.4f) : lv * 100f >= Balance.IceCracked ? IceC : Art.Bad;
                bool water = run.Room.Water.Length > 0 && (ground ? run.InWater(g.Pos) : held && run.InWater(run.Courier.Pos));
                bool hot = run.NearFire(held ? run.Courier.Pos : g.Pos);
                if (water && !_wasRefreeze) Audio.Play("gelo_congela");
                _wasRefreeze = water;
                if ((_dropT -= dt) <= 0f)
                {
                    _dropT = water ? 0.15f : hot ? 0.12f : 0.45f;
                    if (water) Puff(Art.Star(), pos + Random.insideUnitCircle * 0.3f + new Vector2(0f, 0.3f / Tilt), Vector2.zero, Art.A(Color.white, 0.9f), 0.12f, 0.02f, 0.4f);
                    else Puff(Art.Disc(), pos + new Vector2(Random.Range(-0.15f, 0.15f), 0.2f / Tilt), new Vector2(0f, -1.2f), Art.A(IceC, 0.9f), 0.07f, 0.04f, 0.4f);
                }
                if (hot) Audio.Play("gelo_derrete", 1f, 0.9f);
            }
            // gaiola: no ultimo 1 s antes do grito (run.CageLeft) treme e o anel amarelo cresce ate Balance.CageRadius
            if (t == CargoType.GriffinCage && on)
            {
                float left = run.CageLeft;
                bool warn = left >= 0f && left <= Balance.CageWarn;
                v.Aura.enabled = warn;
                if (warn)
                {
                    float k = 1f - left / Balance.CageWarn;
                    v.Aura.transform.localScale = Vector3.one * (2f * Balance.CageRadius * k);
                    v.Aura.transform.localRotation = Quaternion.identity;
                    v.Aura.color = Art.A(Art.Cargo, 0.5f + 0.4f * k);
                    float sh = 10f * k * Mathf.Sin(_time * 60f);
                    v.Vis.localRotation = v.Spr.transform.localRotation = Quaternion.Euler(0f, 0f, sh);
                }
            }
            // planta: boca abrindo e fechando no chao; mapa: brilho dourado
            if (t == CargoType.Flytrap && ground)
            {
                float k = 1f + 0.08f * Mathf.Max(0f, Mathf.Sin(_time * 5f)) + (g.Id == _biteId ? 0.5f * _biteT : 0f);   // mordida: pulo
                v.Vis.localScale *= k; v.Spr.transform.localScale *= k;
            }
            if (t == CargoType.TreasureMap && on && (_sparkleT -= dt) <= 0f)
            {
                _sparkleT = 0.6f;
                Puff(Art.Star(), pos + Random.insideUnitCircle * 0.3f + new Vector2(0f, 0.3f / Tilt), Vector2.zero, Art.Gold, 0.14f, 0.02f, 0.5f);
            }
        }

        static Sprite Static(SpriteSheet s) => s.Frame(s.Pick("Static"), 0, 0);

        /// <summary>Numero grande piscando sobre a carga (o 3-2-1 do barril, o 4-3-2-1 do balao).</summary>
        void Countdown(CargoV v, float left, float k, Vector2 pos, float top, float dt)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(left - 1e-3f), 0, Digits.Length - 1);
            if (v.Num == null) v.Num = NewLabel(72);
            if (n != v.NumShown) { v.NumShown = n; v.Num.text = Digits[n]; v.NumPop = 0.25f; }
            v.NumPop = Mathf.Max(0f, v.NumPop - dt);
            v.Num.enabled = Mathf.Repeat(_time * (2f + 6f * k), 1f) < 0.72f;
            v.Num.color = n <= 1 ? Art.Bad : Color.Lerp(Art.Ink, Art.Bad, k);
            v.Num.rectTransform.localScale = Vector3.one * (1f + 2.4f * v.NumPop);
            v.Num.rectTransform.position = ToScreen(new Vector3(pos.x, pos.y + (top + 0.55f) / Tilt, 0f));
        }
    }
}
