#include "stdafx.h"

wchar_t LoadedMessage[] = {
	~'g', ~'t', ~'a', ~'M', ~'E', ~' ', ~'-', ~' ', ~'L', ~'o', ~'a', ~'d', ~'e', ~'d', ~'!', '\0'
};

int (*NetDll_XNetDnsLookupStub)(
	XNCALLER_TYPE xnc,
	LPCSTR pszHost,
	WSAEVENT hEvent,
	XNDNS** ppXNDns
	) = 0;

int NetDll_XNetDnsLookupHook(
	XNCALLER_TYPE xnc,
	LPCSTR pszHost,
	WSAEVENT hEvent,
	XNDNS** ppXNDns
) {
	//printf("[NetDll_XNetDnsLookup] %s\n", pszHost);

	if (strcmp(pszHost, "prod.ros.gtao.ca") == 0
		|| strcmp(pszHost, "tunables.gtao.ca") == 0) {
		XNDNS* pXNDns = new XNDNS();
		pXNDns->iStatus = 0;
		pXNDns->cina = 1;
		pXNDns->aina->S_un.S_un_b.s_b1 = 192;
		pXNDns->aina->S_un.S_un_b.s_b2 = 168;
		pXNDns->aina->S_un.S_un_b.s_b3 = 0;
		pXNDns->aina->S_un.S_un_b.s_b4 = 230;

		*ppXNDns = pXNDns;

		//printf("Status: %i, Count: %i, IP: %i.%i.%i.%i\n",
		//	pXNDns->iStatus,
		//	pXNDns->cina,
		//	pXNDns->aina->S_un.S_un_b.s_b1,
		//	pXNDns->aina->S_un.S_un_b.s_b2,
		//	pXNDns->aina->S_un.S_un_b.s_b3,
		//	pXNDns->aina->S_un.S_un_b.s_b4);

		return 0;
	}

	return NetDll_XNetDnsLookupStub(
		xnc,
		pszHost,
		hEvent,
		ppXNDns
	);
}

void MainThread() {
	if (Globals.hKernel == 0
		|| Globals.hXam == 0
		|| Globals.hModule == 0) {
		return;
	}

	if (!Globals.IsDevkit) {
		if (Globals.hLaunch != 0) {
			DLSetOptValByName = (BOOL(*)(const char*, DWORD*))Tools.ResolveFunction(Globals.hLaunch, 10);

			if (DLSetOptValByName != 0) {
				DWORD dwOn = 1;
				DWORD dwOff = 0;
				DLSetOptValByName("sockpatch", (DWORD*)&dwOn);
				DLSetOptValByName("xhttp", (DWORD*)&dwOff);
			}
		}

		*(DWORD*)0x8007AB40 = 0x38600001;
		*(DWORD*)0x8007CD04 = 0x38600001;
		*(DWORD*)0x816FEE9C = 0x60000000;
	}

	Tools.HookFunctionStart(Globals.hXam, 67, &NetDll_XNetDnsLookupStub, &NetDll_XNetDnsLookupHook);

	for (;; Sleep(100)) {
		if (XamGetCurrentTitleId() == 0x545408A7 && (*XexExecutableModuleHandle)->TimeDateStamp == 0x56374B99) {
			if (!Globals.IsLoaded) {
				if (Hooks.Initialize()) {
					for (int i = 0; i < 15; i++) {
						LoadedMessage[i] = ~LoadedMessage[i];
					}

					Tools.XNotify(XNOTIFYUI_TYPE_PREFERRED_REVIEW, LoadedMessage);

					for (int i = 0; i < 15; i++) {
						LoadedMessage[i] = ~LoadedMessage[i];
					}

					Globals.IsLoaded = true;
				}
			}
		}
		else {
			if (Globals.IsLoaded) {
				Globals.IsLoaded = false;
			}
		}
	}
}

BOOL APIENTRY DllMain(HANDLE hModule, DWORD dwReason, void* pvReserved) {
	if (dwReason == DLL_PROCESS_ATTACH) {
		if (XamLoaderGetDvdTrayState() != DVD_TRAY_STATE_OPEN) {
			Globals.IsDevkit = XboxHardwareInfo->BldrMagic != 0x4E4E;

			Globals.hKernel = GetModuleHandle(MODULE_KERNEL);
			Globals.hXam = GetModuleHandle(MODULE_XAM);
			Globals.hModule = hModule;

			if (!Globals.IsDevkit) {
				Globals.hLaunch = GetModuleHandle(MODULE_LAUNCH);
			}

			ExCreateThread(0, 0, 0, 0, (LPTHREAD_START_ROUTINE)MainThread, 0, 2);

			return TRUE;
		}
	}

	else if (dwReason == DLL_PROCESS_DETACH) {
		((LDR_DATA_TABLE_ENTRY*)hModule)->LoadCount = 1;
	}

	return FALSE;
}
