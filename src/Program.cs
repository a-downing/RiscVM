global using System.Diagnostics;
global using System.Text;

using RiscVM;

enum Constants {
    CUSTOM_SYSCALL_START = 0x40000000,
    PERIPH_BASE = 0x40000000
}

enum Syscalls {
    SYS_write = 64,
    OUTB = Constants.CUSTOM_SYSCALL_START + 0,
    OUTH = Constants.CUSTOM_SYSCALL_START + 1,
    OUTW = Constants.CUSTOM_SYSCALL_START + 2,
    SQRTF = Constants.CUSTOM_SYSCALL_START + 3,
    FTOSTR = Constants.CUSTOM_SYSCALL_START + 4
}

class Program {
    static string _writeBuffer = string.Empty;

    static int Write(RISCVSystem sys, int fd, int addr, int len) {
        for(int i = 0; i < len; i++) {
            char c = (char)sys.Memory[addr + i];

            if(c == '\n') {
                Console.WriteLine($"WRITE: {_writeBuffer}");
                _writeBuffer = string.Empty;
            } else {
                _writeBuffer += c;
            }
        }

        return len;
    }

    static int CopyStringToVM(RISCVSystem cpu, string str, int addr, int len) {
        byte[] data = Encoding.UTF8.GetBytes(str);

        int i;
        for(i = 0; i < data.Length; i++) {
            if(i < len) {
                cpu.Memory[addr + i] = data[i];
            }
        }

        if(i < len) {
            cpu.Memory[addr + i] = 0;
        }
        
        return i;
    }

    static string CopyStringFromVM(RISCVSystem cpu, int addr) {
        string result = "";

        for(byte b = cpu.Memory[addr]; b != 0; addr++) {
            result += (char)b;
            b = cpu.Memory[addr];
        }

        return result;
    }

    static void Main(string[] args) {
        var prog = File.ReadAllBytes(args[0]);
        var entryPoint = Convert.ToInt32(args[1], 16);
        
        var vm = new RISCVSystem();

        RISCVSystem.SyscallHandler syscallHandler = int? (int syscallId, int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6) => {
            Syscalls syscall = (Syscalls)syscallId;

            switch(syscall) {
                case Syscalls.SYS_write:
                    return Write(vm, arg0, arg1, arg2);
                case Syscalls.OUTB:
                    Console.WriteLine($"OUTB: 0x{arg0.ToString("X2")} ({(char)(byte)arg0})");
                    return 0;
                case Syscalls.OUTH:
                    Console.WriteLine($"OUTH: 0x{arg0.ToString("X4")}");
                    return 0;
                case Syscalls.OUTW:
                    Console.WriteLine($"OUTW: 0x{arg0.ToString("X8")}");
                    return 0;
                case Syscalls.SQRTF:
                    float x = BitConverter.Int32BitsToSingle(arg0);
                    float y = (float)Math.Sqrt((double)x);
                    return BitConverter.SingleToInt32Bits(y);
                case Syscalls.FTOSTR:
                    float f = BitConverter.Int32BitsToSingle(arg0);
                    var fmt = CopyStringFromVM(vm, arg1);
                    var str = f.ToString(fmt);
                    return CopyStringToVM(vm, str, arg2, arg3);
            }

            return null;
        };

        vm.SetLoadHandler((int)Constants.PERIPH_BASE, (int addr, int size, bool signExtend) => {
            Console.WriteLine($"INFO: loadHandler(0x{addr.ToString("X8")}, {size}, {signExtend})");

            if(size != 4) {
                return null;
            }

            switch(addr) {
                case (int)Constants.PERIPH_BASE:
                    return unchecked((int)0xBEEFBEEF);
                default:
                    return null;
            }
        });

        vm.SetStoreHandler((int)Constants.PERIPH_BASE, (int addr, int size, int value) => {
            Console.WriteLine($"INFO: storeHandler(0x{addr.ToString("X8")}, {size}, 0x{value.ToString("X8")})");

            if(size != 4) {
                return false;
            }

            return true;
        });

        vm.SetSyscallHandler(syscallHandler);
        vm.setLogger(new DefaultLogger());
        vm.Initialize(prog, 64 * 1024, 64 * 1024);

        var timer = new Stopwatch();
        timer.Start();

        vm.Reset(entryPoint);
        int lastPc = -1;
        var rand = new Random();

        do {
            lastPc = (int)vm.Pc;
            vm.Cycle(1);

            if(vm.Cycles() % rand.Next(5000, 10000) == 0) {
                vm.ExternalInterrupt(McauseInterrupt.CODE_INT_PLATFORM_0);
            }
        } while(vm.Pc != lastPc);

        timer.Stop();
        var seconds = timer.ElapsedMilliseconds * 1e-3;
        var cyclesPerSecond = vm.Cycles() / seconds;

        Console.WriteLine($"{vm.Cycles()} cycles in {seconds.ToString("F4")}s ({cyclesPerSecond.ToString("F0")} cycle/s)");
    }
}
