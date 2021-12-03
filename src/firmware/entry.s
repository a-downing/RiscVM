.section .text.vectors
mtvec:
    j _trap
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr
    j _isr

    .weak platform_int_0
    .set platform_int_0, _isr
    j platform_int_0

.section .text.entry
.global _start
_start:
    .option push
    .option norelax
    la gp, __global_pointer$
    .option pop

    csrsi mtvec, 1

    jal ra, __libc_init_array
    jal ra, main
    j _forever

.weak _trap
_trap:
    nop

.weak _isr
_isr:
    nop
    
_forever:
    j _forever
