using System.Runtime.CompilerServices;

namespace RiscVM;

enum IType {
    R, R4, I, S, B, U, J
};

enum Register {
    zero, ra, sp, gp, tp, t0, t1, t2, s0, s1, a0, a1, a2, a3, a4, a5, a6, a7, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, t3, t4, t5, t6
}

struct InstructionFields {
    public byte opcode;
    public byte funct7_imm7;
    public byte funct3;
    public byte rd_imm5;
    public byte rs1_uimm;
    public byte rs2;
    public short imm12;
    public int imm20;
    public byte funct2;
    public byte rs3;
}

class RISCVSystem {
    const int ALIGNMENT = 16;

    public int Pc;
    public int[] Regs = new int[32];
    public byte[] Memory = new byte[0];

    int _pcNext, _stackEnd, _heapEnd;
    InstructionFields _fields;
    int _imm;
    CSRs _csrs = new CSRs();
    bool _handled;
    long _cycles = 0;
    McauseInterrupt _external_cause;

    public delegate int? SyscallHandler(int syscallId, int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6);
    SyscallHandler? _syscallHandler;
    
    public delegate int? LoadHandler(int addr, int size, bool signExtend);
    LoadHandler? _loadHandler;
    int _loadHandlerAddr;

    public delegate bool StoreHandler(int addr, int size, int value);
    StoreHandler? _storeHandler;
    int _storeHandlerAddr;

    Logger? _logger;

    public void SetSyscallHandler(SyscallHandler handler) {
        _syscallHandler = handler;
    }

    public void SetLoadHandler(int addr, LoadHandler handler) {
        _loadHandlerAddr = addr;
        _loadHandler = handler;
    }

    public void SetStoreHandler(int addr, StoreHandler handler) {
        _storeHandlerAddr = addr;
        _storeHandler = handler;
    }

    public void setLogger(Logger logger) {
        logger.AcceptSystem(this);
        _logger = logger;
        _csrs.Logger = _logger;
    }

    static InstructionFields DecodeFields(int x) {
        return new InstructionFields {
            opcode = (byte)(x & 0b1111111),
            funct7_imm7 = (byte)((x >> 25) & 0b1111111),
            funct3 = (byte)((x >> 12) & 0b111),
            rd_imm5 = (byte)((x >> 7) & 0b11111),
            rs1_uimm = (byte)((x >> 15) & 0b11111),
            rs2 = (byte)((x >> 20) & 0b11111),
            imm12 = (short)((x >> 20) & 0b111111111111),
            imm20 = (x >> 12) & 0b11111111111111111111,
            funct2 = (byte)((x >> 25) & 0b11),
            rs3 = (byte)((x >> 27) & 0b11111)
        };
    }

    static int SignExtend(int value, int bits) {
        return value | (((value & (1 << (bits - 1))) != 0) ? (~0 << bits) : 0);
    }

    private ref int Reg(Register reg) {
        return ref Regs[(int)reg];
    }

    public long Cycles() => _cycles;

    public void Initialize(byte[] memory, int stackSize, int heapSize) {
        Memory = (byte[])memory.Clone();
        _stackEnd = (Memory.Length + stackSize + ALIGNMENT) & ~(ALIGNMENT - 1);
        _heapEnd = (_stackEnd + heapSize + ALIGNMENT) & ~(ALIGNMENT - 1);
        Array.Resize(ref Memory, _stackEnd);
    }

    public void Reset(int pc) {
        Pc = pc;
        Regs = new int[32];
        Reg(Register.sp) = _stackEnd;
        _csrs = new CSRs();
        _csrs.Logger = _logger;
        _cycles = 0;
    }

    void LUI() {
        Regs[_fields.rd_imm5] = _imm << 12;
    }

    void AUIPC() {
        Regs[_fields.rd_imm5] = Pc + (_imm << 12);
    }

    void JAL() {
        Regs[_fields.rd_imm5] = Pc + 4;
        _pcNext = Pc + _imm;
    }

    void JALR() {
        Regs[_fields.rd_imm5] = Pc + 4;
        _pcNext = Regs[_fields.rs1_uimm] + _imm;
    }

    void BEQ() {
        if(Regs[_fields.rs1_uimm] == Regs[_fields.rs2]) {
            _pcNext = Pc + _imm;
        }
    }

    void BNE() {
        if(Regs[_fields.rs1_uimm] != Regs[_fields.rs2]) {
            _pcNext = Pc + _imm;
        }
    }

    void BLT() {
        if(Regs[_fields.rs1_uimm] < Regs[_fields.rs2]) {
            _pcNext = Pc + _imm;
        }
    }

    void BGE() {
        if(Regs[_fields.rs1_uimm] >= Regs[_fields.rs2]) {
            _pcNext = Pc + _imm;
        }
    }

    void BLTU() {
        if((uint)Regs[_fields.rs1_uimm] < (uint)Regs[_fields.rs2]) {
            _pcNext = Pc + _imm;
        }
    }

    void BGEU() {
        if((uint)Regs[_fields.rs1_uimm] >= (uint)Regs[_fields.rs2]) {
            _pcNext = Pc + _imm;
        }
    }

    void LB() {
        Load(1, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rd_imm5, true);
    }

    void LH() {
        Load(2, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rd_imm5, true);
    }

    void LW() {
        Load(4, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rd_imm5, false);
    }

    void LBU() {
        Load(1, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rd_imm5, false);
    }

    void LHU() {
        Load(2, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rd_imm5, false);
    }

    void SB() {
        Store(1, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rs2);
    }

    void SH() {
        Store(2, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rs2);
    }

    void SW() {
        Store(4, Regs[_fields.rs1_uimm] + _imm, Regs, _fields.rs2);
    }

    void ADDI() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] + _imm;
    }

    void SLTI() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] < _imm ? 1 : 0;
    }

    void SLTIU() {
        Regs[_fields.rd_imm5] = (uint)Regs[_fields.rs1_uimm] < (uint)_imm ? 1 : 0;
    }

    void XORI() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] ^ _imm;
    }

    void ORI() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] | _imm;
    }

    void ANDI() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] & _imm;
    }

    void SLLI() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] << (_imm & 0b11111);
    }

    void SRLI() {
        Regs[_fields.rd_imm5] = (int)((uint)Regs[_fields.rs1_uimm] >> (_imm & 0b11111));
    }

    void SRAI() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] >> (_imm & 0b11111);
    }

    void ADD() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] + Regs[_fields.rs2];
    }

    void SLL() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] << (Regs[_fields.rs2] & 0b11111);
    }

    void SLT() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] < Regs[_fields.rs2] ? 1 : 0;
    }

    void SLTU() {
        Regs[_fields.rd_imm5] = (uint)Regs[_fields.rs1_uimm] < (uint)Regs[_fields.rs2] ? 1 : 0;
    }

    void XOR() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] ^ Regs[_fields.rs2];
    }

    void SRL() {
        Regs[_fields.rd_imm5] = (int)((uint)Regs[_fields.rs1_uimm] >> (Regs[_fields.rs2] & 0b11111));
    }

    void SRA() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] >> (Regs[_fields.rs2] & 0b11111);
    }

    void OR() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] | Regs[_fields.rs2];
    }

    void AND() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] & Regs[_fields.rs2];
    }

    void SUB() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] - Regs[_fields.rs2];
    }

    void ECALL() {
        var syscall = (Syscall)Reg(Register.a7);

        if(_syscallHandler != null) {
            var result = _syscallHandler(
                Reg(Register.a7),
                Reg(Register.a0),
                Reg(Register.a1),
                Reg(Register.a2),
                Reg(Register.a3),
                Reg(Register.a4),
                Reg(Register.a5),
                Reg(Register.a6)
            );

            if(result.HasValue) {
                Reg(Register.a0) = result.Value;
                return;
            }
        }

        switch(syscall) {
            case Syscall.SYS_brk:
                int size = Reg(Register.a0);

                if(size != 0) {
                    Array.Resize(ref Memory, (size > _heapEnd) ? _heapEnd : size);
                }

                Reg(Register.a0) = Memory.Length;
                break;
            case Syscall.SYS_fstat:
                Reg(Register.a0) = -(int)Errno.ENOSYS;
                break;
            default:
                Exception(McauseException.CODE_EX_ECALL_M);
                break;
        }
    }

    void EBREAK() {

    }

    void MRET() {
        _pcNext = _csrs.mepc;
        _csrs.mstatus = (_csrs.mstatus & ~(int)Mstatus.MIE) | ((_csrs.mstatus & (int)Mstatus.MPIE) >> (Mstatus.MPIE_bp - Mstatus.MIE_bp));

        if(_csrs.mtval != 0) {
            Reg(Register.a0) = _csrs.mscratch;
        }
    }

    void CSRRW() {
        int temp = Regs[_fields.rs1_uimm];
        
        if(_fields.rd_imm5 != 0) {
            if(!_csrs.read(_imm, ref Regs[_fields.rd_imm5])) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }
        
        if(_fields.rs1_uimm != 0) {
            if(!_csrs.write(_imm, temp)) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }
    }

    void CSRRS() {
        int temp = Regs[_fields.rs1_uimm];

        if(_fields.rd_imm5 != 0) {
            if(!_csrs.read(_imm, ref Regs[_fields.rd_imm5])) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }

        if(_fields.rs1_uimm != 0) {
            if(!_csrs.set(_imm, temp)) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }
    }

    void CSRRC() {
        int temp = Regs[_fields.rs1_uimm];

        if(_fields.rd_imm5 != 0) {
            if(!_csrs.read(_imm, ref Regs[_fields.rd_imm5])) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }

        if(_fields.rs1_uimm != 0) {
            if(!_csrs.clear(_imm, temp)) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }
    }

    void CSRRWI() {
        if(_fields.rd_imm5 != 0) {
            if(!_csrs.read(_imm, ref Regs[_fields.rd_imm5])) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }

        if(_fields.rs1_uimm != 0) {
            if(!_csrs.write(_imm, _fields.rs1_uimm)) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }
    }

    void CSRRSI() {
        if(_fields.rd_imm5 != 0) {
            if(!_csrs.read(_imm, ref Regs[_fields.rd_imm5])) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }

        if(_fields.rs1_uimm != 0) {
            if(!_csrs.set(_imm, _fields.rs1_uimm)) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }
    }

    void CSRRCI() {
        if(_fields.rd_imm5 != 0) {
            if(!_csrs.read(_imm, ref Regs[_fields.rd_imm5])) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }

        if(_fields.rs1_uimm != 0) {
            if(!_csrs.clear(_imm, _fields.rs1_uimm)) {
                Exception(McauseException.CODE_EX_ILLEGAL_INST);
                return;
            }
        }
    }

    void MUL() {
        Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] * Regs[_fields.rs2];
    }

    void MULHU() {
        Regs[_fields.rd_imm5] = (int)(((ulong)(Regs[_fields.rs1_uimm]) * (ulong)(Regs[_fields.rs2])) >> 32);
    }

    void DIV() {
        if(Regs[_fields.rs2] == 0) {
            Regs[_fields.rd_imm5] = ~0;
        } else if(Regs[_fields.rs1_uimm] == Int32.MinValue && Regs[_fields.rs2] == -1) {
            Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm];
        } else {
            Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] / Regs[_fields.rs2];
        }
    }

    void DIVU() {

        if(Regs[_fields.rs2] == 0) {
            Regs[_fields.rd_imm5] = ~0;
        } else {
            Regs[_fields.rd_imm5] = (int)((uint)Regs[_fields.rs1_uimm] / (uint)Regs[_fields.rs2]);
        }
    }

    void REM() {

        if(Regs[_fields.rs2] == 0) {
            Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm];
        } else if(Regs[_fields.rs1_uimm] == Int32.MinValue && Regs[_fields.rs2] == -1) {
            Regs[_fields.rd_imm5] = 0;
        } else {
            Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm] % Regs[_fields.rs2];
        }
    }

    void REMU() {

        if(Regs[_fields.rs2] == 0) {
            Regs[_fields.rd_imm5] = Regs[_fields.rs1_uimm];
        } else {
            Regs[_fields.rd_imm5] = (int)((uint)Regs[_fields.rs1_uimm] % (uint)Regs[_fields.rs2]);
        }
    }

    void Load(int size, int addr, int[] regs, int reg, bool signExtend) {
        LogLoad(size, addr, regs, reg, signExtend, false);

        if((addr & (size - 1)) != 0) {
            Exception(McauseException.CODE_EX_LOAD_MISALIGNED);
            return;
        }

        if(addr >= _loadHandlerAddr && _loadHandler != null) {
            int? loaded = _loadHandler(addr, size, signExtend);

            if(loaded.HasValue) {
                regs[reg] = loaded.Value;
            } else {
                Exception(McauseException.CODE_EX_LOAD_FAULT);
            }

            return;
        }

        if(addr + size > Memory.Length) {
            Exception(McauseException.CODE_EX_LOAD_FAULT);
            return;
        }

        int value = 0;

        switch(size) {
            case 1:
                value = Memory[addr];
                break;
            case 2:
                value = BitConverter.ToInt16(Memory, addr);
                break;
            case 4:
                value = BitConverter.ToInt32(Memory, addr);
                break;
        }

        regs[reg] = (signExtend) ? SignExtend(value, size * 8) : value;
        LogLoad(size, addr, regs, reg, signExtend, true);
    }

    void Store(int size, int addr, int[] regs, int reg) {
        LogStore(size, addr, regs, reg);

        if((addr & (size - 1)) != 0) {
            Exception(McauseException.CODE_EX_LOAD_MISALIGNED);
            return;
        }

        int value = regs[reg];

        if(addr >= _storeHandlerAddr && _storeHandler != null) {
            if(!_storeHandler(addr, size, value)) {
                Exception(McauseException.CODE_EX_STORE_FAULT);
            }

            return;
        }

        if(addr + size > Memory.Length) {
            Exception(McauseException.CODE_EX_STORE_FAULT);
            return;
        }

        switch(size) {
            case 1:
                Memory[addr] = (byte)value;
                break;
            case 2:
                if(!BitConverter.TryWriteBytes(new Span<byte>(Memory, addr, size), (short)value)) {
                    Exception(McauseException.CODE_EX_STORE_FAULT);
                    return;
                }

                break;
            case 4:
                if(!BitConverter.TryWriteBytes(new Span<byte>(Memory, addr, size), value)) {
                    Exception(McauseException.CODE_EX_STORE_FAULT);
                    return;
                }

                break;
        }
    }

    [Conditional("DEBUG")]
    void LogInstruction(IType type, string name) {
        if(_logger != null) {
            _logger.LogInstruction(type, name, _fields, _imm);
        }
    }

    [Conditional("DEBUG")]
    void LogLoad(int size, int addr, int[] regs, int reg, bool signExtend, bool postLoad) {
        if(_logger != null) {
            _logger.LogLoad(size, addr, regs, reg, signExtend, postLoad);
        }
    }

    [Conditional("DEBUG")]
    void LogStore(int size, int addr, int[] regs, int reg) {
        if(_logger != null) {
            _logger.LogStore(size, addr, regs, reg);
        }
    }

    [Conditional("DEBUG")]
    void LogException(McauseException cause) {
        if(_logger != null) {
            _logger.LogException(cause);
        }
    }

    [Conditional("DEBUG")]
    void LogInterrupt(McauseInterrupt cause, bool pending) {
        if(_logger != null) {
            _logger.LogInterrupt(cause, pending);
        }
    }

    void Exception(McauseException cause) {
        LogException(cause);

        _csrs.mcause = (int)Mcause.INTERRUPT_EXCEPTION | (int)cause;
        _csrs.mepc = Pc;
        _csrs.mstatus = (_csrs.mstatus & ~(int)Mstatus.MPIE) | ((_csrs.mstatus & (int)Mstatus.MIE) << (Mstatus.MPIE_bp - Mstatus.MIE_bp));
        
        _pcNext = (_csrs.mtvec & (int)Mtvec.BASE_bm) >> (int)Mtvec.BASE_bp;
        
        _csrs.mstatus &= ~(int)Mstatus.MIE;
        _csrs.mtval = 0;
    }

    public void ExternalInterrupt(McauseInterrupt cause) {
        LogInterrupt(cause, true);
        _external_cause = cause;
        _csrs.mip |= (int)Mip.MEIP;
    }

    void ExecInterrupt(McauseInterrupt cause, int mask) {
        LogInterrupt(cause, false);

        _csrs.mcause = (int)Mcause.INTERRUPT_INTERRUPT | (int)cause;
        _csrs.mepc = Pc;
        _csrs.mstatus = (_csrs.mstatus & ~(int)Mstatus.MPIE) | ((_csrs.mstatus & (int)Mstatus.MIE) << (Mstatus.MPIE_bp - Mstatus.MIE_bp));
        
        if((_csrs.mtvec & (int)Mtvec.MODE_VECTORED) != 0) {
            Pc = ((_csrs.mtvec & (int)Mtvec.BASE_bm) >> (int)Mtvec.BASE_bp) + (int)_external_cause * 4;
        } else {
            Pc = ((_csrs.mtvec & (int)Mtvec.BASE_bm) >> (int)Mtvec.BASE_bp);
        }

        _csrs.mstatus &= ~(int)Mstatus.MIE;
        _csrs.mip &= ~mask;
    }

    public void Cycle(int cycles = 1) {
        for(int i = 0; i < cycles; i++) {
            if((_csrs.mstatus & (int)Mstatus.MIE) != 0 && (_csrs.mie & (int)Mie.MEIE) != 0 && (_csrs.mip & (int)Mip.MEIP) != 0) {
                ExecInterrupt(_external_cause, (int)Mip.MEIP);
            }

            _pcNext = Pc + 4;
            Reg(Register.zero) = 0;

            if((Pc & 0b11) != 0) {
                Exception(McauseException.CODE_EX_INST_MISALIGNED);
            } else if(Pc < 0 || Pc > Memory.Length - 4) {
                Exception(McauseException.CODE_EX_INST_ACCESS);
            }

            int inst = BitConverter.ToInt32(Memory, Pc);
            Decode(inst);
            
            Pc = _pcNext;
            _cycles++;
        }
    }

    void Exec(Action f, IType type, bool signExtend = true) {
        _handled = true;

        switch(type) {
            case IType.R:
                break;
            case IType.I:
                _imm = SignExtend(_fields.imm12, 12);
                break;
            case IType.S:
                _imm = _fields.funct7_imm7 << 5 | _fields.rd_imm5;
                _imm = SignExtend(_imm, 12);
                break;
            case IType.B:
                _imm = ((_fields.funct7_imm7 & 0b1000000) << 6) | ((_fields.rd_imm5 & 0b1) << 11) | ((_fields.funct7_imm7 & 0b0111111) << 5) | (_fields.rd_imm5 & 0b11110);
                _imm = SignExtend(_imm, 13);
                break;
            case IType.U:
                _imm = _fields.imm20;
                break;
            case IType.J:
                _imm = ((_fields.imm20 & (1 << 19)) << 1) | ((_fields.imm20 & (1 << 8)) << 3) | ((_fields.imm20 & 0xFF) << 12) | ((_fields.imm20 & 0b01111111111000000000) >> 8);
                _imm = SignExtend(_imm, 21);
                break;
            default:
                _handled = false;
                break;
        }

        LogInstruction(type, f.Method.Name.ToLower());

        if(_handled) {
            f();
        }
    }

    void Decode(int x) {
        _fields = DecodeFields(x);
        _handled = false;

        switch(_fields.opcode) {
            case 0b0110111:
                Exec(LUI, IType.U, false);
                break;
            case 0b0010111:
                Exec(AUIPC, IType.U);
                break;
            case 0b1101111:
                Exec(JAL, IType.J);
                break;
            case 0b1100111:
                Exec(JALR, IType.I);
                break;
            case 0b1100011:
                switch(_fields.funct3) {
                    case 0b000:
                        Exec(BEQ, IType.B);
                        break;
                    case 0b001:
                        Exec(BNE, IType.B);
                        break;
                    case 0b100:
                        Exec(BLT, IType.B);
                        break;
                    case 0b101:
                        Exec(BGE, IType.B);
                        break;
                    case 0b110:
                        Exec(BLTU, IType.B);
                        break;
                    case 0b111:
                        Exec(BGEU, IType.B);
                        break;
                }

                break;
            case 0b0000011:
                switch(_fields.funct3) {
                    case 0b000:
                        Exec(LB, IType.I);
                        break;
                    case 0b001:
                        Exec(LH, IType.I);
                        break;
                    case 0b010:
                        Exec(LW, IType.I);
                        break;
                    case 0b100:
                        Exec(LBU, IType.I);
                        break;
                    case 0b101:
                        Exec(LHU, IType.I);
                        break;
                }

                break;
            case 0b0100011:
                switch(_fields.funct3) {
                    case 0b000:
                        Exec(SB, IType.S);
                        break;
                    case 0b001:
                        Exec(SH, IType.S);
                        break;
                    case 0b010:
                        Exec(SW, IType.S);
                        break;
                }

                break;
            case 0b0010011:
                switch(_fields.funct3) {
                    case 0b000:
                        Exec(ADDI, IType.I);
                        break;
                    case 0b010:
                        Exec(SLTI, IType.I);
                        break;
                    case 0b011:
                        Exec(SLTIU, IType.I);
                        break;
                    case 0b100:
                        Exec(XORI, IType.I);
                        break;
                    case 0b110:
                        Exec(ORI, IType.I);
                        break;
                    case 0b111:
                        Exec(ANDI, IType.I);
                        break;
                    case 0b001:
                        Exec(SLLI, IType.I);
                        break;
                    case 0b101:
                        switch(_fields.funct7_imm7) {
                            case 0b0:
                                Exec(SRLI, IType.I);
                                break;
                            default:
                                Exec(SRAI, IType.I);
                                break;
                        }

                        break;
                }

                break;
            case 0b0110011:
                switch(_fields.funct7_imm7) {
                    case 0b0000000:
                        switch(_fields.funct3) {
                            case 0b000:
                                Exec(ADD, IType.R);
                                break;
                            case 0b001:
                                Exec(SLL, IType.R);
                                break;
                            case 0b010:
                                Exec(SLT, IType.R);
                                break;
                            case 0b011:
                                Exec(SLTU, IType.R);
                                break;
                            case 0b100:
                                Exec(XOR, IType.R);
                                break;
                            case 0b101:
                                Exec(SRL, IType.R);
                                break;
                            case 0b110:
                                Exec(OR, IType.R);
                                break;
                            case 0b111:
                                Exec(AND, IType.R);
                                break;
                        }

                        break;
                    case 0b0000001:
                        switch(_fields.funct3) {
                            case 0b000:
                                Exec(MUL, IType.R);
                                break;
                            case 0b001:
                                //Exec(MULH, IType.R);
                                break;
                            case 0b010:
                                //Exec(MULHSU, IType.R);
                                break;
                            case 0b011:
                                Exec(MULHU, IType.R);
                                break;
                            case 0b100:
                                Exec(DIV, IType.R);
                                break;
                            case 0b101:
                                Exec(DIVU, IType.R);
                                break;
                            case 0b110:
                                Exec(REM, IType.R);
                                break;
                            case 0b111:
                                Exec(REMU, IType.R);
                                break;
                        }

                        break;
                    case 0b0100000:
                        switch(_fields.funct3) {
                            case 0b000:
                                Exec(SUB, IType.R);
                                break;
                            case 0b101:
                                Exec(SRA, IType.R);
                                break;
                        }

                        break;
                }

                break;
            case 0b0001111:
                switch(_fields.funct3) {
                    case 0b000:
                        //FENCE IType.I
                        _handled = true;
                        break;
                    case 0b001:
                        //FENCE_I IType.I
                        _handled = true;
                        break;
                }

                break;
            case 0b1110011:
                switch(_fields.funct3) {
                    case 0b000:
                        switch(_fields.imm12) {
                            case 0b000000000000:
                                Exec(ECALL, IType.I);
                                break;
                            case 0b000000000001:
                                Exec(EBREAK, IType.I);
                                break;
                            case 0b001100000010:
                                Exec(MRET, IType.I);
                                break;
                        }

                        break;
                    case 0b001:
                        Exec(CSRRW, IType.I, false);
                        break;
                    case 0b010:
                        Exec(CSRRS, IType.I, false);
                        break;
                    case 0b011: 
                        Exec(CSRRC, IType.I, false);
                        break;
                    case 0b101: 
                        Exec(CSRRWI, IType.I, false);
                        break;
                    case 0b110: 
                        Exec(CSRRSI, IType.I, false);
                        break;
                    case 0b111: 
                        Exec(CSRRCI, IType.I, false);
                        break;
                }

                break;
        }

        if(!_handled) {
            Exception(McauseException.CODE_EX_ILLEGAL_INST);
            return;
        }
    }
}
