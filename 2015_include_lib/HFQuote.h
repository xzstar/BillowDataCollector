#pragma once
#define DllExport __declspec(dllexport)
#define WINAPI      __stdcall
#define WIN32_LEAN_AND_MEAN             //  �� Windows ͷ�ļ����ų�����ʹ�õ���Ϣ

#include <windows.h>

///�������
struct MarketData
{
	///��Լ����
	char	InstrumentID[32];
	///���¼�
	double	LastPrice;
	///�����һ
	double	BidPrice1;
	///������һ
	int	BidVolume1;
	///������һ
	double	AskPrice1;
	///������һ
	int	AskVolume1;
	///���վ���
	double	AveragePrice;
	///����
	int	Volume;
	///�ֲ���
	double	OpenInterest;
	///����޸�ʱ��:yyyyMMdd HH:mm:ss
	char	UpdateTime[32];
	///����޸ĺ���
	int	UpdateMillisec;
	///��ͣ���
	double	UpperLimitPrice;
	///��ͣ���
	double	LowerLimitPrice;
};


typedef int (WINAPI *DefOnFrontConnected)();		void* _OnFrontConnected;
typedef int (WINAPI *DefOnRspUserLogin)(int pErrId);	void* _OnRspUserLogin;
typedef int (WINAPI *DefOnRspUserLogout)(int pReason);	void* _OnRspUserLogout;
typedef int (WINAPI *DefOnRtnError)(int pErrId, const char* pMsg);	void* _OnRtnError;
typedef int (WINAPI *DefOnRtnDepthMarketData)(MarketData *pMarketData);	void* _OnRtnDepthMarketData;

//ע����Ӧ����
DllExport void WINAPI RegOnFrontConnected(void* onFunction){ _OnFrontConnected = onFunction; }
DllExport void WINAPI RegOnRspUserLogin(void* onFunction){ _OnRspUserLogin = onFunction; }
DllExport void WINAPI RegOnRspUserLogout(void* onFunction){ _OnRspUserLogout = onFunction; }
DllExport void WINAPI RegOnRtnDepthMarketData(void* onFunction){ _OnRtnDepthMarketData = onFunction; }
DllExport void WINAPI RegOnRtnError(void* onFunction){ _OnRtnError = onFunction; }

int req = 0;
char _TradingDay[16];

//����api
DllExport void WINAPI CreateApi();

///ע��ǰ�û������ַ
DllExport int WINAPI ReqConnect(char *pFront);

///�û���¼����
DllExport int WINAPI ReqUserLogin(char* pInvestor, char* pPwd, char* pBroker);

///�ǳ�����
DllExport void WINAPI ReqUserLogout();

///��ȡ��ǰ������
///@retrun ��ȡ���Ľ�����
///@remark ֻ�е�¼�ɹ���,���ܵõ���ȷ�Ľ�����
DllExport const char* WINAPI GetTradingDay();

///�������顣
///@param ppInstrumentID ��ԼID  
///@param nCount Ҫ����/�˶�����ĺ�Լ����
///@remark 
DllExport int WINAPI ReqSubMarketData(char *pInstrumentID);

///�˶����顣
///@param ppInstrumentID ��ԼID  
///@param nCount Ҫ����/�˶�����ĺ�Լ����
///@remark 
DllExport int WINAPI ReqUnSubMarketData(char *pInstrumentID);


