using System;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        int n = 8192;
        int runs = 3;
        int[] a = new int[n * n];

        for (int i = 0; i < a.Length; i++)
        {
            a[i] = (i % 10) + 1;
        }

        double totalRowMs = 0;
        double totalColMs = 0;

        for (int r = 1; r <= runs; r++)
        {
            Stopwatch sw = Stopwatch.StartNew();
            long rowSum = SumByRows(a, n);
            sw.Stop();
            double rowMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            long colSum = SumByCols(a, n);
            sw.Stop();
            double colMs = sw.Elapsed.TotalMilliseconds;

            totalRowMs += rowMs;
            totalColMs += colMs;

            Console.WriteLine($"Замер {r}: по строкам = {rowMs:F2} мс (сумма {rowSum}) | по столбцам = {colMs:F2} мс (сумма {colSum})");
        }

        double avgRow = totalRowMs / runs;
        double avgCol = totalColMs / runs;

        Console.WriteLine();
        Console.WriteLine($"Среднее по строкам:       {avgRow:F2} мс");
        Console.WriteLine($"Среднее по столбцам:      {avgCol:F2} мс");
        Console.WriteLine($"Во сколько раз медленнее: {avgCol / avgRow:F2}");
    }

    static long SumByRows(int[] a, int n)
    {
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                sum += a[i * n + j];
            }
        }
        return sum;
    }

    static long SumByCols(int[] a, int n)
    {
        long sum = 0;
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                sum += a[i * n + j];
            }
        }
        return sum;
    }
}
