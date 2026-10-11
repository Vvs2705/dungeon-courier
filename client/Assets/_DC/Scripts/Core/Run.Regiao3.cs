namespace DC.Core
{
    // Regiao 3 (Cripta Assombrada): comportamentos novos (inimigos, chefes, cargas, perigos da sala). ESQUELETO da Leva A: a raia R3 implementa
    // aqui so os ganchos que precisar (declarados no fim de Run.cs; sem implementacao custam zero). Exemplo:
    //
    //   partial void EnemyR3(Enemy e, ref bool handled)
    //   {
    //       switch (e.Type)
    //       {
    //           case EnemyType.Ghost: /* ... */ handled = true; break;
    //       }
    //   }
    //
    // Regras: deterministico (so o rng da sala e Rng.Hash; nada de System.Random nem relogio); nada de Math.Sin/Cos (V2.Dirs8);
    // emita eventos com Emit(Ev.X, a, b) para a View e o diario; carga que falha chama Finish(Outcome.X); Sim.Dirty = true quando
    // mudar algo que o save grava. Testes em Tests/EditMode (so API publica) com prova de vermelho.
    public sealed partial class Run
    {
    }
}
