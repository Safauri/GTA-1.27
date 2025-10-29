#include "stdafx.h"

CTools Tools;

BOOL(*DLSetOptValByName)(const char* Name, DWORD* Val) = 0;

BYTE StubSection[0x1000] = { 0 };
int StubIndex = 0;

void CTools::XNotify(XNOTIFYQUEUEUI_TYPE Type, wchar_t* pMessage) {
	BOOL Options[4];
	XNotifyUIGetOptions(&Options[0], &Options[1], &Options[2], &Options[3]);
	XNotifyUISetOptions(TRUE, TRUE, TRUE, TRUE);
	XNotifyQueueUI(Type, XUSER_INDEX_ANY, XNOTIFYUI_PRIORITY_HIGH, pMessage, 0);
	XNotifyUISetOptions(Options[0], Options[1], Options[2], Options[3]);
}

DWORD CTools::ResolveFunction(HANDLE hModule, int Ordinal) {
	return (DWORD)GetProcAddress((HMODULE)hModule, (LPCSTR)Ordinal);
}

DWORD CTools::ResolveFunction(const char* ModuleName, int Ordinal) {
	return ResolveFunction(GetModuleHandle(ModuleName), Ordinal);
}

void CTools::PatchInJump(DWORD Address, void* pDestination, bool Linked) {
	DWORD* pdwAddress = (DWORD*)Address;
	DWORD Destination = (DWORD)pDestination;

	if (Destination & 0x8000) {
		pdwAddress[0] = 0x3D600000 + (((Destination >> 16) & 0xFFFF) + 1);
	}
	else {
		pdwAddress[0] = 0x3D600000 + ((Destination >> 16) & 0xFFFF);
	}

	pdwAddress[1] = 0x396B0000 + (Destination & 0xFFFF);
	pdwAddress[2] = 0x7D6903A6;
	pdwAddress[3] = 0x4E800420;

	if (Linked) {
		pdwAddress[3] += 1;
	}

	doSync(pdwAddress);
}

bool CTools::PatchModuleImport(HANDLE hModule, const char* ImportedModuleName, int Ordinal, void* pDestination) {
	DWORD ImportAddress = ResolveFunction(ImportedModuleName, Ordinal);

	if (ImportAddress == 0) {
		return false;
	}

	XEX_IMPORT_DESCRIPTOR* pImportDesc = (XEX_IMPORT_DESCRIPTOR*)RtlImageXexHeaderField(((LDR_DATA_TABLE_ENTRY*)hModule)->XexHeaderBase, XEX_HEADER_IMPORTS);

	if (pImportDesc == 0) {
		return false;
	}

	XEX_IMPORT_TABLE* pImportTable = (XEX_IMPORT_TABLE*)((BYTE*)pImportDesc + sizeof(*pImportDesc) + pImportDesc->NameTableSize);

	for (DWORD i = 0; i < pImportDesc->ModuleCount; i++) {
		for (WORD j = 0; j < pImportTable->ImportCount; j++) {
			DWORD StubAddress = *((DWORD*)pImportTable->ImportStubAddr[j]);

			if (ImportAddress != StubAddress) {
				continue;
			}

			StubAddress = (DWORD)pImportTable->ImportStubAddr[j + 1];
			PatchInJump(StubAddress, pDestination, false);

			j = pImportTable->ImportCount;
		}

		pImportTable = (XEX_IMPORT_TABLE*)((BYTE*)pImportTable + pImportTable->TableSize);
	}

	return true;
}

bool CTools::PatchModuleImport(const char* ModuleName, const char* ImportedModuleName, int Ordinal, void* pDestination) {
	return PatchModuleImport(GetModuleHandle(ModuleName), ImportedModuleName, Ordinal, pDestination);
}

void __declspec(naked) GPLR() {
	__asm {
		std r14, -0x98(r1)
		std r15, -0x90(r1)
		std r16, -0x88(r1)
		std r17, -0x80(r1)
		std r18, -0x78(r1)
		std r19, -0x70(r1)
		std r20, -0x68(r1)
		std r21, -0x60(r1)
		std r22, -0x58(r1)
		std r23, -0x50(r1)
		std r24, -0x48(r1)
		std r25, -0x40(r1)
		std r26, -0x38(r1)
		std r27, -0x30(r1)
		std r28, -0x28(r1)
		std r29, -0x20(r1)
		std r30, -0x18(r1)
		std r31, -0x10(r1)
		stw r12, -0x8(r1)
		blr
	}
}

DWORD RelinkGPLR(DWORD Offset, DWORD* pdwSaveStub, DWORD* pdwOriginal) {
	DWORD Instruction = 0, Reply;
	DWORD* pdwSaver = (DWORD*)GPLR;

	if (Offset & 0x2000000) {
		Offset |= 0xFC000000;
	}

	Reply = pdwOriginal[Offset / 4];

	for (int i = 0; i < 20; i++) {
		if (Reply == pdwSaver[i]) {
			int NewOffset = (int)&pdwSaver[i] - (int)pdwSaveStub;
			Instruction = 0x48000001 | (NewOffset & 0x3FFFFFC);
		}
	}

	return Instruction;
}

#pragma optimize("", off)

bool CTools::HookFunctionStart(DWORD Address, void* pSaveStub, void* pDestination) {
	if (Address == BackflipValue(0x7476040C) /*0x82CF6308*/) {
		Address = BackflipValue(0xEC44B83A); /*0x83337240*/
		pDestination = &sub_83337240_Hook;
	}

	if (*(DWORD*)pSaveStub == 0) {
		DWORD* pdwAddress = (DWORD*)Address;
		DWORD* pdwSaveStub = (DWORD*)&StubSection[StubIndex];
		int Size = 0;

		for (int i = 0; i < 4; i++) {
			if ((pdwAddress[i] & 0x48000003) == 0x48000001) {
				pdwSaveStub[i] = RelinkGPLR((pdwAddress[i] & ~0x48000003), &pdwSaveStub[i + 3], &pdwAddress[i]);
			}
			else {
				pdwSaveStub[i] = pdwAddress[i];
			}
		}
		Size += 0x10;

		DWORD JumpAddress = Address + 0x10;

		if (JumpAddress & 0x8000) {
			pdwSaveStub[4] = 0x3D800000 + (((JumpAddress >> 16) & 0xFFFF) + 1);
		}
		else {
			pdwSaveStub[4] = 0x3D800000 + ((JumpAddress >> 16) & 0xFFFF);
		}

		pdwSaveStub[5] = 0x398C0000 + (JumpAddress & 0xFFFF);
		pdwSaveStub[6] = 0x7D8903A6;
		pdwSaveStub[7] = 0x4E800420;
		Size += 0x14;

		doSync(pdwSaveStub);

		*(DWORD*)pSaveStub = (DWORD)pdwSaveStub;

		StubIndex += Size;
	}

	PatchInJump(Address, pDestination, false);

	return true;
}

#pragma optimize("", on)

bool CTools::HookFunctionStart(HANDLE Module, int Ordinal, void* pSaveStub, void* pDestination) {
	return HookFunctionStart(ResolveFunction(Module, Ordinal), pSaveStub, pDestination);
}

bool CTools::HookFunctionStart(const char* ModuleName, int Ordinal, void* pSaveStub, void* pDestination) {
	return HookFunctionStart(ResolveFunction(ModuleName, Ordinal), pSaveStub, pDestination);
}
