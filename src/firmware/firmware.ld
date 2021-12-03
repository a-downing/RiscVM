OUTPUT_FORMAT("elf32-littleriscv", "elf32-littleriscv", "elf32-littleriscv")
OUTPUT_ARCH(riscv)
ENTRY(_start)

SECTIONS
{
  .text           :
  {
    . = 0x0;
    *(.text.vectors)
    . = 0x100;
    *(.text.entry)
    *(.text)
  }
  . = ALIGN(4);
  .data           :
  {
    __DATA_BEGIN__ = .;
    *(.data)
  }
  . = ALIGN(4);
  .sdata          :
  {
    __SDATA_BEGIN__ = .;
    *(.sdata)
  }
  _edata = .; PROVIDE (edata = .);
  . = .;
  __bss_start = .;
  .bss            :
  {
   *(.bss)
   . = ALIGN(. != 0 ? 32 / 8 : 1);
  }
  __BSS_END__ = .;
  __global_pointer$ = MIN(__SDATA_BEGIN__ + 0x800, MAX(__DATA_BEGIN__ + 0x800, __BSS_END__ - 0x800));
  _end = .;
  PROVIDE (end = .);
}