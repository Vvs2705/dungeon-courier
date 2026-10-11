namespace DC.Core
{
    // Regiao 3 (Cripta Assombrada): em implementacao pela raia R3.
    public sealed partial class Run
    {
        /// <summary>Luz a menos (m): courier (Necromante fase 3, Neblina na cripta) e tochas (Necromante fase 3).</summary>
        public float LightCut => lightCut3; public float TorchCut => torchCut3;
        float lightCut3, torchCut3;
    }
}
