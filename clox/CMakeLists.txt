cmake_minimum_required(VERSION 4.1)
project(clox C)

add_executable(clox main.c
        chunk.h
        chunk.c
        common.h
        memory.h
        memory.c
        debug.c
        value.h
        value.c
        vm.h
        vm.c
        compiler.h
        compiler.c
        scanner.h
        scanner.c)
