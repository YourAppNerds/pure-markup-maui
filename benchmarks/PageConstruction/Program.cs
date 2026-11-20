using BenchmarkDotNet.Running;

namespace PageConstruction.Bench;

public static class Program
{
    // dotnet run -c Release --project benchmarks/PageConstruction -- --filter *
    // dotnet run -c Release --project benchmarks/PageConstruction -- --check   (equivalence only)
    public static void Main(string[] args)
    {
        if (args is ["--check"])
        {
            Equivalence.Check();
            Console.WriteLine("All variants build equivalent pages.");
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
