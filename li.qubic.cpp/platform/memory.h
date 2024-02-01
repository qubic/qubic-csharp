#pragma once

#include <cstring>

static inline void setMem(void* buffer, unsigned long long size, unsigned char value)
{
    memset(buffer, value, size);
}

static inline void copyMem(void* destination, const void* source, unsigned long long length)
{
    memcpy(destination, source, length);
}

