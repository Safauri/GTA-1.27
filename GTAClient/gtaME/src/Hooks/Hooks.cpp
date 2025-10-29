#include "stdafx.h"

HANDLE hEnumXLSP;

IN_ADDR ServerAddr = { 31, 33, 33, 37 };

XTITLE_SERVER_INFO ServerInfo = {
	ServerAddr,
	0,
	"{cluster=qwest,name=EWR,svcid=0x54540875,titleid=0x545408A7,env=prod,tz=-5,ts={n=pres,p=30101}}"
};

BYTE Patch[0x18] = {
	/*
	lis %r30, 0x22C
	ori %r30, %r30, 0xC800
	stw %r30, 0x18(%r29)
	lis %r3, 0xF6D6
	ori %r3, %r3, 0xAA59
	nop
	*/

	0x3F, 0xC0, 0x02, 0x2C, 0x63, 0xDE, 0xC8, 0x00, 0x93, 0xDD, 0x00, 0x18,
	0x3C, 0x60, 0xF6, 0xD6, 0x60, 0x63, 0xAA, 0x59, 0x60, 0x00, 0x00, 0x00
};

CHooks Hooks;

SOCKET NetDll_socketHook(
	XNCALLER_TYPE xnc,
	int af,
	int type,
	int protocol
) {
	DWORD dwR31;

	__asm {
		mr dwR31, r31
	}

	SOCKET socket;

	if (protocol == IPPROTO_VDP && dwR31 == 0x83D78900) {
		protocol = IPPROTO_UDP;
	}

	socket = NetDll_socket(xnc, af, type, protocol);

	if (socket != INVALID_SOCKET) {
		BOOL bOption = 1;
		NetDll_setsockopt(xnc, socket, SOL_SOCKET, SO_GRANTINSECURE, (char*)&bOption, 4);
	}

	return socket;
}

int NetDll_XNetServerToInAddrHook(
	XNCALLER_TYPE xnc,
	IN_ADDR ina,
	DWORD dwServiceId,
	IN_ADDR* pina
) {
	pina->s_addr = ina.s_addr;
	return 0;
}

DWORD XamCreateEnumeratorHandleHook(
	DWORD dwUserIndex,
	HXAMAPP hxamapp,
	DWORD dwMsgIDEnum,
	DWORD dwMsgIDCloseEnum,
	DWORD cbSizeOfPrivateEnumStructure,
	DWORD cItemsRequested,
	DWORD dwEnumFlags,
	HANDLE* phEnum
) {
	DWORD dwResult = XamCreateEnumeratorHandle(
		dwUserIndex,
		hxamapp,
		dwMsgIDEnum,
		dwMsgIDCloseEnum,
		cbSizeOfPrivateEnumStructure,
		cItemsRequested,
		dwEnumFlags,
		phEnum
	);

	if (dwMsgIDEnum == 0x58039) {
		hEnumXLSP = *phEnum;
	}

	return dwResult;
}

DWORD XamEnumerateHook(
	HANDLE hEnum,
	DWORD dwFlags,
	void* pvBuffer,
	DWORD cbBuffer,
	DWORD* pcItemsReturned,
	XOVERLAPPED* pOverlapped
) {
	if (hEnum == hEnumXLSP && cbBuffer == 0x3400) {
		memcpy(pvBuffer, &ServerInfo, 0x6A);

		pOverlapped->InternalLow = 0;
		pOverlapped->InternalHigh = 1;
		pOverlapped->dwExtendedError = 0;

		return 0;
	}

	return XamEnumerate(
		hEnum,
		dwFlags,
		pvBuffer,
		cbBuffer,
		pcItemsReturned,
		pOverlapped
	);
}

bool CHooks::Initialize() {
	// Remove breakpoint
	*(DWORD*)0x827D2164 = 0x60000000;

	// e=1, v=27
	*(DWORD*)0x83336E84 = 0x39200001;
	*(DWORD*)0x83336E94 = 0x38C0001B;

	strcpy((char*)0x820B93FC, "http://");
	strcpy((char*)0x820BB7FC, "ros.gtao.ca");

	// Set user-agent
	strcpy((char*)0x820BB778, "gta ");

	// Set port
	//*(WORD*)0x8333AF4A = 5000;

	// Patch signature check on tunables
	*(DWORD*)0x83005424 = 0x38C00003;

	// Patch XNetConnect - return XNET_CONNECT_STATUS_IDLE
	*(QWORD*)0x837FFE98 = 0x386000004E800020;

	// Patch XNetGetConnectStatus - return XNET_CONNECT_STATUS_CONNECTED
	*(QWORD*)0x837FFEA8 = 0x386000024E800020;

	// Disable rockstar logos movie
	memset((BYTE*)0x820093A4, 0, 0x20);

	// Script bypass
	*(DWORD*)0x83288A30 = 0x48000104;
	memcpy((BYTE*)0x82FDB57C, Patch, 0x18);

	if (!Tools.PatchModuleImport(*XexExecutableModuleHandle, MODULE_XAM, 3, &NetDll_socketHook)) {
		return false;
	}

	if (!Tools.PatchModuleImport(*XexExecutableModuleHandle, MODULE_XAM, 58, &NetDll_XNetServerToInAddrHook)) {
		return false;
	}

	if (!Tools.PatchModuleImport(*XexExecutableModuleHandle, MODULE_XAM, 590, &XamCreateEnumeratorHandleHook)) {
		return false;
	}

	if (!Tools.PatchModuleImport(*XexExecutableModuleHandle, MODULE_XAM, 592, &XamEnumerateHook)) {
		return false;
	}

	return true;
}
