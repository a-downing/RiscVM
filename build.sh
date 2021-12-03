#!/bin/bash
set -e

mkdir -p artifacts

riscv64-elf-gcc \
-ggdb3 -Og -flto \
-march=rv32im -mabi=ilp32 \
-T src/firmware/firmware.ld \
-nostartfiles \
src/firmware/entry.s \
src/firmware/system.c \
src/firmware/main.c \
-o artifacts/main

riscv64-elf-objcopy -O binary --set-section-flags .bss=alloc,load,contents artifacts/main artifacts/main.bin
riscv64-elf-objdump -d artifacts/main > artifacts/main.S
