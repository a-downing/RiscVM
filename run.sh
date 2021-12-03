#!/bin/bash
set -e

dotnet run --configuration debug artifacts/main.bin 0x100
