#include "stdafx.h"
#include "nvGpuLoad.h"

// This code is from http://eliang.blogspot.de/2011/05/getting-nvidia-gpu-usage-in-c.html
#include <windows.h>
#include <iostream>

// magic numbers, do not change them
#define NVAPI_MAX_PHYSICAL_GPUS   64
#define NVAPI_MAX_USAGES_PER_GPU  34

// function pointer types
typedef int *(*NvAPI_QueryInterface_t)(unsigned int offset);
typedef int(*NvAPI_Initialize_t)();
typedef int(*NvAPI_EnumPhysicalGPUs_t)(int **handles, int *count);
typedef int(*NvAPI_GPU_GetUsages_t)(int *handle, unsigned int *usages);

extern "C"
{
	__declspec(dllexport) int getGpuLoad()
	{
		HMODULE hmod = LoadLibraryA("nvapi.dll");
		if (hmod == NULL)
		{
			std::cerr << "Couldn't find nvapi.dll" << std::endl;
			return 1;
		}

		// nvapi.dll internal function pointers
		NvAPI_QueryInterface_t      NvAPI_QueryInterface = NULL;
		NvAPI_Initialize_t          NvAPI_Initialize = NULL;
		NvAPI_EnumPhysicalGPUs_t    NvAPI_EnumPhysicalGPUs = NULL;
		NvAPI_GPU_GetUsages_t       NvAPI_GPU_GetUsages = NULL;

		// nvapi_QueryInterface is a function used to retrieve other internal functions in nvapi.dll
		NvAPI_QueryInterface = (NvAPI_QueryInterface_t)GetProcAddress(hmod, "nvapi_QueryInterface");

		// some useful internal functions that aren't exported by nvapi.dll
		NvAPI_Initialize = (NvAPI_Initialize_t)(*NvAPI_QueryInterface)(0x0150E828);
		NvAPI_EnumPhysicalGPUs = (NvAPI_EnumPhysicalGPUs_t)(*NvAPI_QueryInterface)(0xE5AC921F);
		NvAPI_GPU_GetUsages = (NvAPI_GPU_GetUsages_t)(*NvAPI_QueryInterface)(0x189A1FDF);

		if (NvAPI_Initialize == NULL || NvAPI_EnumPhysicalGPUs == NULL ||
			NvAPI_EnumPhysicalGPUs == NULL || NvAPI_GPU_GetUsages == NULL)
		{
			std::cerr << "Couldn't get functions in nvapi.dll" << std::endl;
			return 2;
		}

		// initialize NvAPI library, call it once before calling any other NvAPI functions
		(*NvAPI_Initialize)();

		int          gpuCount = 0;
		int         *gpuHandles[NVAPI_MAX_PHYSICAL_GPUS] = { NULL };
		unsigned int gpuUsages[NVAPI_MAX_USAGES_PER_GPU] = { 0 };

		// gpuUsages[0] must be this value, otherwise NvAPI_GPU_GetUsages won't work
		gpuUsages[0] = (NVAPI_MAX_USAGES_PER_GPU * 4) | 0x10000;

		(*NvAPI_EnumPhysicalGPUs)(gpuHandles, &gpuCount);

		(*NvAPI_GPU_GetUsages)(gpuHandles[0], gpuUsages);
		int usage = gpuUsages[3];

		return usage;
	}
}