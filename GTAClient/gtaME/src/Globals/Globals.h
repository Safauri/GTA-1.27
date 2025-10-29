#pragma once

class CGlobals {
public:
	HANDLE hKernel;
	HANDLE hXam;
	HANDLE hModule;
	HANDLE hLaunch;

	bool IsLoaded;
	bool IsDevkit;
};

extern CGlobals Globals;
