#include <stdint.h>
#include <string.h>
#include <errno.h>
#include <stdio.h>

#include <machine/syscall.h>

#include "system.h"

DEFINE_GET_CSR(mepc)
DEFINE_GET_CSR(mstatus)
DEFINE_GET_CSR(mcause)
DEFINE_GET_CSR(mie)
DEFINE_GET_CSR(mip)
DEFINE_GET_CSR(mscratch)
DEFINE_GET_CSR(mtval)

DEFINE_SET_CSR(mepc)
DEFINE_SET_CSR(mstatus)
DEFINE_SET_CSR(mcause)
DEFINE_SET_CSR(mie)
DEFINE_SET_CSR(mip)
DEFINE_SET_CSR(mscratch)
DEFINE_SET_CSR(mtval)

void __attribute__ ((naked)) sei() {
    asm volatile("csrsi mstatus, 0b1000");
    asm volatile("ret");
}

void __attribute__ ((naked)) cli() {
    asm volatile("csrci mstatus, 0b1000");
    asm volatile("ret");
}

uint32_t __attribute__ ((naked)) ecall(uint32_t a0, uint32_t a1, uint32_t a2, uint32_t a3, uint32_t a4, uint32_t a5, uint32_t a6, uint32_t a7) {
    asm volatile("ecall");
    asm volatile("ret");
}
