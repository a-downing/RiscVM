namespace RiscVM;

interface Logger {
    public void AcceptSystem(RISCVSystem sys);
    public void LogInstruction(Instruction inst);
    public void LogLoad(int size, int addr, int[] regs, int reg, bool signExtend, bool postLoad);
    public void LogStore(int size, int addr, int[] regs, int reg);
    public void LogException(McauseException cause);
    public void LogInterrupt(McauseInterrupt cause, bool pending);
    public void LogCSRAccess(CSRs.AccessType type, int addr, int value, int outcome);
}

class DefaultLogger : Logger {
    RISCVSystem? _sys;
    int _pc;

    public void AcceptSystem(RISCVSystem sys) {
        _sys = sys;
    }

    void Log(string text) {
        if(_pc != -1) {
            Console.WriteLine($"0x{_pc.ToString("X8")}: {text}");
        } else {
            Console.WriteLine($"            {text}");
        }
    }

    public void LogInstruction(Instruction inst) {
        _pc = _sys?.Pc ?? -1;
        var name = inst.Handler!.Method.Name;

        if(name == "addi" && inst.Rd == 0 && inst.Rs1 == 0 && inst.Imm == 0) {
            Log($"nop");
            _pc = -1;
            return;
        }

        switch(inst.Type) {
            case IType.R:
                Log($"{name} {((Register)inst.Rd).ToString()}, {((Register)inst.Rs1).ToString()}, {((Register)inst.Rs2).ToString()}");
                break;
            case IType.I:
                Log($"{name} {((Register)inst.Rd).ToString()}, {((Register)inst.Rs1).ToString()}, {inst.Imm}");
                break;
            case IType.S:
                Log($"{name} {((Register)inst.Rs2).ToString()}, {inst.Imm}({((Register)inst.Rs1).ToString()})");
                break;
            case IType.B:
                Log($"{name} {((Register)inst.Rs1).ToString()}, {((Register)inst.Rs2).ToString()}, {inst.Imm}(0x{(_pc + inst.Imm).ToString("X")})");
                break;
            case IType.U:
                Log($"{name} {((Register)inst.Rd).ToString()}, 0x{inst.Imm.ToString("X")}");
                break;
            case IType.J:
                Log($"{name} {((Register)inst.Rd).ToString()}, {inst.Imm}(0x{(_pc + inst.Imm).ToString("X")})");
                break;
        }

        _pc = -1;
    }

    public void LogLoad(int size, int addr, int[] regs, int reg, bool signExtend, bool postLoad) {
        reg = regs[reg];

        if(!postLoad) {
            Log($"Load(size: {size}, addr: {addr.ToString("X8")}, reg: {reg.ToString("X8")}, signExtend: {signExtend})");
        } else {
            Log($"Loaded {reg.ToString("X8")}");
        }
    }

    public void LogStore(int size, int addr, int[] regs, int reg) {
        reg = regs[reg];
        Log($"Store(size: {size}, addr: {addr.ToString("X8")}, reg: {reg.ToString("X8")})");
    }

    public void LogException(McauseException cause) {
        Log($"Exception: {cause}");
    }

    public void LogInterrupt(McauseInterrupt cause, bool pending) {
        if(pending) {
            Log($"Interrupt pending: {cause}");
        } else {
            Log($"Interrupt: {cause}");
        }
    }

    public void LogCSRAccess(CSRs.AccessType type, int addr, int value, int outcome) {
        switch(type) {
            case CSRs.AccessType.READ:
                Log($"CSR: read {Enum.GetName(typeof(CSRAddress), addr)} = 0x{value.ToString("X8")}");
                break;
            case CSRs.AccessType.WRITE:
                Log($"CSR: write {Enum.GetName(typeof(CSRAddress), addr)} = 0x{value.ToString("X8")} -> {outcome.ToString("X8")}");
                break;
            case CSRs.AccessType.SET:
                Log($"CSR: set {Enum.GetName(typeof(CSRAddress), addr)} mask 0x{value.ToString("X8")} -> {outcome.ToString("X8")}");
                break;
            case CSRs.AccessType.CLEAR:
                Log($"CSR: clear {Enum.GetName(typeof(CSRAddress), addr)} mask 0x{value.ToString("X8")} -> {outcome.ToString("X8")}");
                break;
        }
    }
}
