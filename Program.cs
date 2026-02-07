using System.Diagnostics;
using EchoSolver.data;

namespace EchoSolver;


public class SimStats
{
    public long ExpConsumed;
    public long TunersConsumed;
    public int SuccessCount;
}

class Program
{
    static void Main(string[] args)
    {
        const long iterations = 100_000_000;
        var sw = Stopwatch.StartNew();

        long globalSuccesses = 0;
        long globalNetExp = 0;
        long globalNetTuners = 0;

        Parallel.For(
            0,
            iterations,

            // local init
            () => new { EchoObj = new Echo(), LocalStats = new SimStats() },

            // body
            (i, loopState, ctx) =>
            {
                var echo = ctx.EchoObj;
                echo.Reset();

                bool keep = ExecuteStrategy(echo);

                if (keep)
                {
                    ctx.LocalStats.SuccessCount++;
                    ctx.LocalStats.ExpConsumed += echo.ExpConsumed;
                    ctx.LocalStats.TunersConsumed += echo.TunersConsumed;
                }
                else
                {
                    ctx.LocalStats.ExpConsumed += (int)(echo.ExpConsumed * 0.25f);
                    ctx.LocalStats.TunersConsumed += (int)(echo.TunersConsumed * 0.7f);
                }

                return ctx;
            },

            // local finally
            (ctx) =>
            {
                Interlocked.Add(ref globalSuccesses, ctx.LocalStats.SuccessCount);
                Interlocked.Add(ref globalNetExp, ctx.LocalStats.ExpConsumed);
                Interlocked.Add(ref globalNetTuners, ctx.LocalStats.TunersConsumed);
            }
        );

        sw.Stop();

        Console.WriteLine($"Finished in {sw.Elapsed.TotalSeconds:F2} seconds.");
        Console.WriteLine($"------------------------------------------------");
        Console.WriteLine($"Valid={(double)globalSuccesses / iterations:P2}:");

        if (globalSuccesses > 0)
        {
            var avgExp = (double)globalNetExp / globalSuccesses;
            var avgTuners = (double)globalNetTuners / globalSuccesses;

            var avgTubes = avgExp / 5000;
            var TunerTubeRatio = avgTuners / avgTubes;
            var tubesCost = avgTubes / 4.6;
            var tunersCost = avgTuners / 20;

            Console.WriteLine($"| Exp    - {avgTubes:F1} x Premium");
            Console.WriteLine($"| Tuners - {avgTuners:F1}");
            Console.WriteLine($"| Bases  - {1 / ((double)globalSuccesses / iterations):N1}");
            Console.WriteLine($"Tuner/Tube Ratio: {TunerTubeRatio:F2}");
            Console.WriteLine($"Cost: {tubesCost:F1} + {tunersCost:F1} = {tubesCost + tunersCost:F1}");
        }
        else
        {
            Console.WriteLine("Warning: No valid Echoes were produced. Please check if the strategy or criteria are too strict.");
        }

        Console.WriteLine($"------------------------------------------------");
    }

    static bool ExecuteStrategy(Echo echo)
    {
        var (crit, good, okay) = (0, 0, 0);

        echo.Tune(); // +5
        (crit, good, okay) = EvaluateStats(echo);
        if (good < 1) return false;

        echo.Tune(); // +10
        echo.Tune(); // +15
        (crit, good, okay) = EvaluateStats(echo);
        if (crit < 1) return false;

        echo.Tune(); // +20
        (crit, good, okay) = EvaluateStats(echo);
        // if (good < 2) return false;
        if (okay < 3) return false;

        // final eval
        echo.Tune();
        (crit, good, okay) = EvaluateStats(echo);
        if (crit < 2) return false;
        if (good < 3) return false;
        if (okay < 4) return false;

        return true;
    }

    static (int crit, int good, int okay) EvaluateStats(Echo echo)
    {
        var crit = 0;
        if (echo.HasStat(SubStatType.CritRate, 6.9f)) crit++;
        if (echo.HasStat(SubStatType.CritDamage, 13.8f)) crit++;

        var good = crit;
        if (echo.HasStat(SubStatType.ATK_Percent, 7.9f)) good++;
        if (echo.HasStat(SubStatType.ResonanceLiberationBonus, 7.9f)) good++;
        if (echo.HasStat(SubStatType.EnergyRegen, 8.4f)) good++;

        var okay = good;
        if (echo.HasStat(SubStatType.ATK, 40f)) okay++;

        return (crit, good, okay);
    }

}
