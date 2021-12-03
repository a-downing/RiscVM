namespace RiscVM;

using static CSRAddress;

class CSRs {
    private int _unknown = 0;
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

    private ref int resolve(int addr) {
        switch((CSRAddress)addr) {
            case MSTATUS: return ref mstatus;
            case MTVEC: return ref mtvec;
            case MEPC: return ref mepc;
            case MCAUSE: return ref mcause;
            case MSCRATCH: return ref mscratch;
            case MTVAL: return ref mtval;
            case MIE: return ref mie;
            case MIP: return ref mip;
            default: return ref _unknown;
        }
    }

    [Conditional("DEBUG")]
    void Log(AccessType type, int addr, int value, int outcome) {
        if(Logger != null) {
            Logger.LogCSRAccess(type, addr, value, outcome);
        }
    }
    
    public void read(int addr, ref int value) {
        value = resolve(addr);
        Log(AccessType.READ, addr, value, value);
    }

    public void write(int addr, int value) {
        Log(AccessType.WRITE, addr, value, value);
        resolve(addr) = value;
    }

    public void set(int addr, int mask) {
        int csr = resolve(addr) |= mask;
        Log(AccessType.SET, addr, mask, csr);
    }

    public void clear(int addr, int mask) {
        int csr = resolve(addr) &= ~mask;
        Log(AccessType.CLEAR, addr, mask, csr);
    }
}
