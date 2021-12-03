namespace RiscVM;

public enum CSRAddress {
    MSTATUS = 0x300,
    MTVEC = 0x305,
    MSCRATCH = 0x340,
    MEPC = 0x341,
    MCAUSE = 0x342,
    MTVAL = 0x343,
    MCYCLE = 0xb00,
    MINSTRET = 0xb02,
    MCYCLEH = 0xf80,
    MINSTRETH = 0xf82,
    MIE = 0x304,
    MIP = 0x344,
    FFLAGS = 0x001,
    FRM = 0x002,
    FCSR = 0x003
}

public enum Syscall {
    SYS_close = 57,
    SYS_write = 64,
    SYS_fstat = 80,
    SYS_exit = 93,
    SYS_brk = 214
}

public enum Errno {
    ENOSYS = 38
}

public enum Mie {
    MSIE_bp = 3,
    MTIE_bp = 7,
    MEIE_bp = 11,
    MSIE = (1 << MSIE_bp),
    MTIE = (1 << MTIE_bp),
    MEIE = (1 << MEIE_bp)
}

public enum Mip {
    MSIP_bp = 3,
    MTIP_bp = 7,
    MEIP_bp = 11,
    MSIP = (1 << MSIP_bp),
    MTIP = (1 << MTIP_bp),
    MEIP = (1 << MEIP_bp)
}

public enum Mstatus {
    MIE_bp = 3,
    MIE = (1 << MIE_bp),
    MPIE_bp = 7,
    MPIE = (1 << MPIE_bp),
}

public enum Mtvec {
    MODE_bp = 0,
    MODE_DIRECT = 0x0,
    MODE_VECTORED = 0x1,
    BASE_bp = 2,
    BASE_bm = 0x7fffffff,
}

public enum Mcause {
    INTERRUPT_bp = 31,
    INTERRUPT_bm = (1 << INTERRUPT_bp),
    INTERRUPT_INTERRUPT = INTERRUPT_bm,
    INTERRUPT_EXCEPTION = 0x0,
    CODE_bp = 0,
    CODE_bm = (int)(0xffffffff >> 1)
}

public enum McauseException {
    CODE_EX_INST_MISALIGNED = 0,
    CODE_EX_INST_ACCESS = 1,
    CODE_EX_ILLEGAL_INST = 2,
    CODE_EX_BREAKPOINT = 3,
    CODE_EX_LOAD_MISALIGNED = 4,
    CODE_EX_LOAD_FAULT = 5,
    CODE_EX_STORE_MISALIGNED = 6,
    CODE_EX_STORE_FAULT = 7,
    CODE_EX_ECALL_U = 8,
    CODE_EX_ECALL_S = 9,
    CODE_EX_ECALL_M = 11,
    CODE_EX_INST_PAGE_FAULT = 12,
    CODE_EX_LOAD_PAGE_FAULT = 13,
    CODE_EX_STORE_PAGE_FAULT = 15
}

public enum McauseInterrupt {
    CODE_INT_U_SOFTWARE = 0,
    CODE_INT_S_SOFTWARE = 1,
    CODE_INT_M_SOFTWARE = 3,

    CODE_INT_U_TIMER = 4,
    CODE_INT_S_TIMER = 5,
    CODE_INT_M_TIMER = 7,

    CODE_INT_U_EXTERNAL = 8,
    CODE_INT_S_EXTERNAL = 9,
    CODE_INT_M_EXTERNAL = 11,

    CODE_INT_PLATFORM_0 = 16
}
