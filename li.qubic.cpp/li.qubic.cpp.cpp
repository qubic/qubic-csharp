// exported function to use in unsupported higher languages
//

#define NO_UEFI true

#include "pch.h"
#include "framework.h"

#include <iostream>
#include <string>
#include <fstream>
#include <stdlib.h>
#include <time.h>
#include <random>
#include <string.h>	

#include "four_q.h"
#include "kangaroo_twelve.h"
#include "definitions.h"

using namespace std;

#ifdef __GNUC__
#include <ammintrin.h>
#include <immintrin.h>
#include <emmintrin.h>


#define EXPORTED __attribute__ ((visibility ("default")))
#else

#include <windows.h>
#include <intrin.h>
#define EXPORTED __declspec(dllexport)

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReservedB
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}
#endif
extern "C"
{

	void GetIdentity(unsigned char* publicKey, unsigned char* identity, bool isLowerCase)
	{
		for (int i = 0; i < 4; i++)
		{
			unsigned long long publicKeyFragment = *((unsigned long long*) & publicKey[i << 3]);
			for (int j = 0; j < 14; j++)
			{
				identity[i * 14 + j] = publicKeyFragment % 26 + (isLowerCase ? L'a' : L'A');
				publicKeyFragment /= 26;
			}
		}
		unsigned int identityBytesChecksum;
		KangarooTwelve(publicKey, 32, (unsigned char*)&identityBytesChecksum, 3);
		identityBytesChecksum &= 0x3FFFF;
		for (int i = 0; i < 4; i++)
		{
			identity[56 + i] = identityBytesChecksum % 26 + (isLowerCase ? L'a' : L'A');
			identityBytesChecksum /= 26;
		}
		identity[60] = 0;
	}

	void GetOldIdentity(unsigned char* publicKey, unsigned char* identity)
	{
		//unsigned char identity[70];
		for (int i = 0; i < 32; i++)
		{
			identity[i << 1] = (publicKey[i] >> 4) + L'A';
			identity[(i << 1) + 1] = (publicKey[i] & 0xF) + L'A';
		}
		unsigned char identityBytesChecksum[3];
		KangarooTwelve(publicKey, 32, identityBytesChecksum, sizeof(identityBytesChecksum));
		for (int i = 0; i < sizeof(identityBytesChecksum); i++)
		{
			identity[64 + (i << 1)] = (identityBytesChecksum[i] >> 4) + L'A';
			identity[65 + (i << 1)] = (identityBytesChecksum[i] & 0xF) + L'A';
		}
		identity[70] = 0;
		//memcpy(newIdentity, identity, sizeof(identity));
	}

	void GetIdentityFromSeed(unsigned char* seed, unsigned char* identity) {

		unsigned char privateKey[32];
		unsigned char publicKey[32];
		unsigned char subSeed[32];

		getSubseed(seed, subSeed);
		getPrivateKey(subSeed, privateKey);
		getPublicKey(privateKey, publicKey);

		GetIdentity(publicKey, identity, false);
	}

	static bool getBinaryFromString(const unsigned char* identity, unsigned char* publicKey)
	{
		unsigned char publicKeyBuffer[32];
		for (int i = 0; i < 4; i++)
		{
			*((unsigned long long*) & publicKeyBuffer[i << 3]) = 0;
			for (int j = 14; j-- > 0; )
			{
				if (identity[i * 14 + j] < 'a' || identity[i * 14 + j] > 'z')
				{
					return false;
				}

				*((unsigned long long*) & publicKeyBuffer[i << 3]) = *((unsigned long long*) & publicKeyBuffer[i << 3]) * 26 + (identity[i * 14 + j] - 'a');
			}
		}
		*((__m256i*)publicKey) = *((__m256i*)publicKeyBuffer);

		return true;
	}


	bool GetPublicKeyFromIdentity(const unsigned char* identity, unsigned char* publicKey)
	{
		if (strlen((char*)identity) == 60) { // new 60 character id format
			return getPublicKeyFromIdentity(identity, publicKey);
		}
		else {
			// old 70er fid format
			for (int i = 0; i < 32; i++)
			{
				if (identity[i << 1] < 'A' || identity[i << 1] > 'P'
					|| identity[(i << 1) + 1] < 'A' || identity[(i << 1) + 1] > 'P')
				{
					return false;
				}
				publicKey[i] = ((identity[i << 1] - 'A') << 4) | (identity[(i << 1) + 1] - 'A');
			}

			return true;
		}
	}


	bool generatePrivateKey(unsigned char* seed, unsigned char* privateKey) {
		unsigned char subSeed[32];
		getSubseed(seed, subSeed);
		getPrivateKey(subSeed, privateKey);
		return true;
	}

	bool signStruct(unsigned char* seed, unsigned char* data, int structSize, unsigned char* signature) {

		unsigned char privateKey[32];
		unsigned char publicKey[32];
		unsigned char subSeed[32];

		getSubseed(seed, subSeed);
		getPrivateKey(subSeed, privateKey);
		getPublicKey(privateKey, publicKey);

		unsigned char digest[32];
		KangarooTwelve(data, structSize, digest, sizeof(digest));

		sign(subSeed, publicKey, digest, signature);

		return true;
	}

	bool VerifyQubicStruct(unsigned char* data, unsigned int packageSize, const unsigned char* publicKey) {
		unsigned char digest[32];
		KangarooTwelve((unsigned char*)data, packageSize - SIGNATURE_SIZE, digest, sizeof(digest));
		return verify(publicKey, digest, (((const unsigned char*)data) + packageSize - SIGNATURE_SIZE));
	}



#pragma region DLL Exports



	EXPORTED bool VerifyExported(const unsigned char* publicKey, const unsigned char* messageDigest, const unsigned char* signature) {
		return verify(publicKey, messageDigest, signature);
	}

	EXPORTED void getIdentityExported(unsigned char* publicKey, unsigned char* identity, bool lowerCase) {
		GetIdentity(publicKey, identity, lowerCase);
	}


	EXPORTED bool getPublicKeyFromIdentityExported(const unsigned char* identity, unsigned char* publicKey) {
		return GetPublicKeyFromIdentity(identity, publicKey);
	}

	EXPORTED bool getBinaryFromStringExported(const unsigned char* identity, unsigned char* publicKey) {
		return getBinaryFromString(identity, publicKey);
	}

	EXPORTED bool getSharedKeyExported(const unsigned char* privateKey, const unsigned char* publicKey, unsigned char* sharedKey) {
		return getSharedKey(privateKey, publicKey, sharedKey);
	}

	EXPORTED bool verifyQubicStructExported(unsigned char* data, unsigned int packageSize, const unsigned char* publicKey) {
		return VerifyQubicStruct(data, packageSize, publicKey);
	}



	EXPORTED bool generatePrivateKeyExported(unsigned char* seed, unsigned char* privateKey) {
		return generatePrivateKey(seed, privateKey);
	}

	EXPORTED bool signStructExported(unsigned char* seed, unsigned char* data, int structSize, unsigned char* signature) {
		return signStruct(seed, data, structSize, signature);
	}

	EXPORTED void getIdentityFromSeedExported(unsigned char* seed, unsigned char* identity) {
		GetIdentityFromSeed(seed, identity);
	}


	EXPORTED void kangarooTwelveExported(unsigned char* input, unsigned int inputByteLen, unsigned char* output, unsigned int outputByteLen) {
		KangarooTwelve(input, inputByteLen, output, outputByteLen);
	}

	EXPORTED void kangarooTwelve64To32Exported(unsigned char* input, unsigned char* output) {
		KangarooTwelve64To32(input, output);
	}





#pragma endregion
}

int main(int argc, char* argv[])
{

	cout << "test library";

	unsigned char* seed = (unsigned char*)"prwutqifhtqxjrhpliuhzvezyobjwilejelewskiykvogmvlgolkqiesqxcp";
	unsigned char identity[60];
	getIdentityFromSeedExported(seed, identity);


	cout << identity;
}