using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    const int PROT_READ = 1;
    const int PROT_WRITE = 2;
    const int MAP_PRIVATE = 2;
    const int MAP_ANONYMOUS = 32;

    [DllImport("libc", SetLastError = true)]
    static extern IntPtr mmap(IntPtr addr, nuint length, int prot, int flags, int fd, nint offset);

    [DllImport("libc", SetLastError = true)]
    static extern int munmap(IntPtr addr, nuint length);

    static long prevFaults;

    static unsafe void Main()
    {
        int pageSize = Environment.SystemPageSize;
        long size = 256L * 1024 * 1024;

        Console.WriteLine($"Размер страницы: {pageSize} байт");
        Console.WriteLine($"Размер блока: {size / (1024 * 1024)} МиБ ({size / pageSize} страниц)");
        Console.WriteLine();

        PrintMem("Начало", isStart: true);

        IntPtr addr = mmap(IntPtr.Zero, (nuint)size, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
        if (addr == new IntPtr(-1))
        {
            Console.WriteLine($"Ошибка mmap: {Marshal.GetLastWin32Error()}");
            return;
        }
        PrintMem("После выделения");

        byte* ptr = (byte*)addr;

        long sum = 0;
        Stopwatch sw = Stopwatch.StartNew();
        for (long i = 0; i < size; i += pageSize)
        {
            sum += Volatile.Read(ref ptr[i]);
        }
        sw.Stop();
        PrintMem("После первого чтения", sw.Elapsed.TotalMilliseconds);

        sw.Restart();
        for (long i = 0; i < size; i += pageSize)
        {
            Volatile.Write(ref ptr[i], 1);
        }
        sw.Stop();
        PrintMem("После первой записи", sw.Elapsed.TotalMilliseconds);

        sw.Restart();
        for (long i = 0; i < size; i += pageSize)
        {
            Volatile.Write(ref ptr[i], 2);
        }
        sw.Stop();
        PrintMem("После повторной записи", sw.Elapsed.TotalMilliseconds);

        long lastPos = ((size - 1) / pageSize) * pageSize;
        byte v0 = Volatile.Read(ref ptr[0]);
        byte v1 = Volatile.Read(ref ptr[pageSize]);
        byte v2 = Volatile.Read(ref ptr[2 * pageSize]);
        byte vLast = Volatile.Read(ref ptr[lastPos]);

        if (munmap(addr, (nuint)size) != 0)
        {
            Console.WriteLine($"Ошибка munmap: {Marshal.GetLastWin32Error()}");
            return;
        }
        PrintMem("После освобождения");

        Console.WriteLine();
        Console.WriteLine($"Контрольная сумма первого чтения: {sum}");
        Console.WriteLine($"Проверка записи (ожидается 2): [0]={v0}, [page]={v1}, [2*page]={v2}, [last]={vLast}");
    }

    static void PrintMem(string stage, double? timeMs = null, bool isStart = false)
    {
        (double vmSize, double rss) = ReadStatus();
        long currentFaults = ReadMinorFaults();
        long deltaFaults = currentFaults - prevFaults;
        prevFaults = currentFaults;

        string faultsText = isStart ? "—" : deltaFaults.ToString();
        string timeText = timeMs.HasValue ? $"{timeMs.Value:F2}" : "—";

        Console.WriteLine($"{stage,-25} | VmSize: {vmSize,8:F2} МиБ | RSS: {rss,7:F2} МиБ | +minor: {faultsText,7} | Время: {timeText} мс");
    }

    static (double vmSizeMb, double rssMb) ReadStatus()
    {
        double vmSize = 0;
        double rss = 0;

        foreach (string line in File.ReadLines("/proc/self/status"))
        {
            if (line.StartsWith("VmSize:"))
            {
                vmSize = ParseKb(line) / 1024.0;
            }
            if (line.StartsWith("VmRSS:"))
            {
                rss = ParseKb(line) / 1024.0;
            }
        }

        return (vmSize, rss);
    }

    static long ParseKb(string line)
{
    string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    return long.Parse(parts[1]);
}

    static long ReadMinorFaults()
    {
        string text = File.ReadAllText("/proc/self/stat");
        int idx = text.LastIndexOf(')');
        string tail = text.Substring(idx + 1);
        string[] fields = tail.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return long.Parse(fields[7]);
    }
}
