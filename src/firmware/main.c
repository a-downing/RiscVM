#include <errno.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <stdio.h>
#include <stdarg.h>
#include <stdint.h>

#include <unistd.h>
#include <sys/stat.h>
#include <sys/types.h>

#include "system.h"
#include "riscv-defs.h"

#define USER_SYSCALL_START 0x40000000

volatile uint32_t *PERIPH_BASE = (uint32_t *)0x40000000;

void outb(uint8_t b) {
    ecall(b, 0, 0, 0, 0, 0, 0, USER_SYSCALL_START + 0);
}

void outh(uint16_t h) {
    ecall(h, 0, 0, 0, 0, 0, 0, USER_SYSCALL_START + 1);
}

void outw(uint32_t w) {
    ecall(w, 0, 0, 0, 0, 0, 0, USER_SYSCALL_START + 2);
}

float sqrtf(float f) {
    int32_t i;
    
    memcpy(&i, &f, sizeof(i));
    i = ecall(i, 0, 0, 0, 0, 0, 0, USER_SYSCALL_START + 3);
    memcpy(&f, &i, sizeof(f));
    
    return f;
}

int ftostr(float f, char *fmt, char *buf, int len) {
    int32_t i;
    memcpy(&i, &f, sizeof(i));
    return ecall(i, (int)fmt, (int)buf, len, 0, 0, 0, USER_SYSCALL_START + 4);
}

__attribute__((interrupt)) void platform_int_0() {
    printf("platform_int_0\n");
}

int main() {
    char buf[32];
    sei();
    set_csr_mie(MIE_MEIE);
    
    for(int i = 0; i < 10; i++) {
        float f = sqrtf(i + 1);

        cli();
        printf("sqrt(%d) = %f\n", i + 1, f);
        sei();
    }

    outb(0xDE);
    outh(0xDEAD);
    outw(0xDEADBEEF);
    
    outw(*PERIPH_BASE);
    *PERIPH_BASE = 0xDEADDEAD;

    float f = 123.456789;
    char *fmt = "";
    int len = ftostr(f, fmt, buf, sizeof(buf) - 1);

    cli();
    printf("ftostr(%f, \"%s\", buf -> \"%s\", %d) = %d\n", f, fmt, buf, sizeof(buf) - 1, len);
    sei();

    //*(int *)0x05000000 = 0;

    return 0;
}
