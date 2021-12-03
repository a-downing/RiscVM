namespace RiscVM;

using static CSRAddress;

class CSRs {
    private int _invalid;
    public int mstatus = 0;
    public int mtvec = 0;
    public int mcause = 0;
    public int mepc = 0;
    public int mscratch = 0;
    public int mtval = 0;
    public int mie = 0;
    public int mip = 0;

    public Logger? Logger;

    public enum AccessType {
        READ,
        WRITE,
        SET,
        CLEAR
    }

    private ref int resolve(int addr, out bool resolved) {
        resolved = true;

        switch((CSRAddress)addr) {
            case MSTATUS:
                return ref mstatus;
            case MTVEC:
                return ref mtvec;
            case MEPC:
                return ref mepc;
            case MCAUSE:
                return ref mcause;
            case MSCRATCH:
                return ref mscratch;
            case MTVAL:
                return ref mtval;
            case MIE:
                return ref mie;
            case MIP:
                return ref mip;
            default:
                resolved = false;
                return ref _invalid;
        }
    }

    [Conditional("DEBUG")]
    void Log(AccessType type, int addr, int value, int outcome) {
        if(Logger != null) {
            Logger.LogCSRAccess(type, addr, value, outcome);
        }
    }
    
    public bool read(int addr, ref int value) {
        bool resolved;
        ref int csr = ref resolve(addr, out resolved);

        if(resolved) {
            value = csr;
            Log(AccessType.READ, addr, value, value);
            return true;
        }

        Log(AccessType.READ, addr, 0, 0);
        return false;
    }

    public bool write(int addr, int value) {
        bool resolved;
        ref int csr = ref resolve(addr, out resolved);

        if(resolved) {
            csr = value;
        }

        Log(AccessType.WRITE, addr, value, csr);
        return resolved;
    }

    public bool set(int addr, int value) {
        bool resolved;
        ref int csr = ref resolve(addr, out resolved);

        if(resolved) {
            csr |= value;
        }

        Log(AccessType.SET, addr, value, csr);
        return resolved;
    }

    public bool clear(int addr, int value) {
        bool resolved;
        ref int csr = ref resolve(addr, out resolved);

        if(resolved) {
            csr &= ~value;
        }

        Log(AccessType.CLEAR, addr, value, csr);
        return resolved;
    }
}
