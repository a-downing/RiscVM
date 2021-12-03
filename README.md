# RiscVM
Simple RISC-V emulator/VM

The `RiscVM.RISCVSystem` class can be instantiated in a C# program to run RISC-V bare-metal machine code. There's a simple API for the host and VM to interact. Currently rv32im is implemented.

`src/firmware/main.c` gets compiled to rv32im machine code

`src/Program.cs` instantiates the VM, loads the machine code, and does some stuff for testing.
