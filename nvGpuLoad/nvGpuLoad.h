// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the NVGPULOAD_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// NVGPULOAD_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef NVGPULOAD_EXPORTS
#define NVGPULOAD_API __declspec(dllexport)
#else
#define NVGPULOAD_API __declspec(dllimport)
#endif

// This class is exported from the nvGpuLoad.dll
class NVGPULOAD_API CnvGpuLoad {
public:
	CnvGpuLoad(void);
	// TODO: add your methods here.
};

extern NVGPULOAD_API int nnvGpuLoad;

NVGPULOAD_API int fnnvGpuLoad(void);
