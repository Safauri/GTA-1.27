#pragma once

class CTools {
public:
	void XNotify(XNOTIFYQUEUEUI_TYPE Type, wchar_t* pMessage);
	DWORD ResolveFunction(HANDLE hModule, int Ordinal);
	DWORD ResolveFunction(const char* ModuleName, int Ordinal);
	void PatchInJump(DWORD Address, void* pDestination, bool Linked);
	bool PatchModuleImport(HANDLE hModule, const char* ImportedModuleName, int Ordinal, void* pDestination);
	bool PatchModuleImport(const char* ModuleName, const char* ImportedModuleName, int Ordinal, void* pDestination);
	bool HookFunctionStart(DWORD Address, void* pSaveStub, void* pDestination);
	bool HookFunctionStart(HANDLE Module, int Ordinal, void* pSaveStub, void* pDestination);
	bool HookFunctionStart(const char* ModuleName, int Ordinal, void* pSaveStub, void* pDestination);
};

extern CTools Tools;

extern BOOL(*DLSetOptValByName)(const char* Name, DWORD* Val);

