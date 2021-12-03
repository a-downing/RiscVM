#ifndef SYSTEM_H
#define SYSTEM_H

#include <stdint.h>
#include <string.h>
#include <errno.h>
#include <stdio.h>

#include <machine/syscall.h>

// mcause
#define MCAUSE_INTERRUPT_bp 31
#define MCAUSE_INTERRUPT_bm (1 << MCAUSE_INTERRUPT_bp)
#define MCAUSE_INTERRUPT_INTERRUPT MCAUSE_INTERRUPT_bm
#define MCAUSE_INTERRUPT_EXCEPTION 0x0
#define MCAUSE_CODE_EX_ECALL_M 11

#define DEFINE_GET_CSR(x) \
uint32_t __attribute__ ((naked)) get_csr_##x() { \
    asm volatile("csrr a0, " #x); \
    asm volatile("ret"); \
} \

#define DEFINE_SET_CSR(x) \
void __attribute__ ((naked)) set_csr_##x(uint32_t a0) { \
    asm volatile("csrw " #x ", a0"); \
    asm volatile("ret"); \
} \

#define DECLARE_GET_CSR(x) uint32_t __attribute__ ((naked)) get_csr_##x()
#define DECLARE_SET_CSR(x) void __attribute__ ((naked)) set_csr_##x(uint32_t)

extern char _end[];

DECLARE_GET_CSR(mepc);
DECLARE_GET_CSR(mstatus);
DECLARE_GET_CSR(mcause);
DECLARE_GET_CSR(mie);
DECLARE_GET_CSR(mip);
DECLARE_GET_CSR(mscratch);
DECLARE_GET_CSR(mtval);

DECLARE_SET_CSR(mepc);
DECLARE_SET_CSR(mstatus);
DECLARE_SET_CSR(mcause);
DECLARE_SET_CSR(mie);
DECLARE_SET_CSR(mip);
DECLARE_SET_CSR(mcratch);
DECLARE_SET_CSR(mtval);

void __attribute__ ((naked)) sei();
void __attribute__ ((naked)) cli();

uint32_t __attribute__ ((naked)) ecall(uint32_t a0, uint32_t a1, uint32_t a2, uint32_t a3, uint32_t a4, uint32_t a5, uint32_t a6, uint32_t a7);

#endif