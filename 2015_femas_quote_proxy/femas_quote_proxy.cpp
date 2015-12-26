// femas_quote_proxy.cpp : ���� DLL Ӧ�ó���ĵ���������
//

#include "femas_quote_proxy.h"

#include "../2015_include_lib/HFQuote.h"
#include <stdio.h>

CUstpFtdcMduserApi *api;
CUstpFtdcMduserSpi *spi;


//����api
DllExport void WINAPI CreateApi()
{
	api = CUstpFtdcMduserApi::CreateFtdcMduserApi("./log/");
	spi = new CfemasQuote();
}

///ע��ǰ�û������ַ
DllExport int WINAPI ReqConnect(char *pFront)
{
	api->RegisterSpi(spi);
	api->RegisterFront(pFront);
	api->SubscribeMarketDataTopic(100, USTP_TERT_QUICK);
	api->Init();
	return 0;
}

///�û���¼����
DllExport int WINAPI ReqUserLogin(char* pInvestor, char* pPwd, char* pBroker)
{
	CUstpFtdcReqUserLoginField f;
	memset(&f, 0, sizeof(CUstpFtdcReqUserLoginField));
	strcpy_s(f.BrokerID, sizeof(f.BrokerID), pBroker);
	strcpy_s(f.UserID, sizeof(f.UserID), pInvestor);
	strcpy_s(f.Password, sizeof(f.Password), pPwd);
	return api->ReqUserLogin(&f, ++req);
}

///�ǳ�����
DllExport void WINAPI ReqUserLogout()
{
	api->RegisterSpi(NULL);
	api->Release();
}

///��ȡ��ǰ������
///@retrun ��ȡ���Ľ�����
///@remark ֻ�е�¼�ɹ���,���ܵõ���ȷ�Ľ�����
DllExport const char* WINAPI GetTradingDay()
{
	return _TradingDay;
}

///�������顣
///@param ppInstrumentID ��ԼID  
///@param nCount Ҫ����/�˶�����ĺ�Լ����
///@remark 
DllExport int WINAPI ReqSubMarketData(char *pInstrumentID)
{
	char *insts[] = { pInstrumentID };
	return api->SubMarketData(insts, 1);
}

///�˶����顣
///@param ppInstrumentID ��ԼID  
///@param nCount Ҫ����/�˶�����ĺ�Լ����
///@remark 
DllExport int WINAPI ReqUnSubMarketData(char *pInstrumentID)
{
	char *insts[] = { pInstrumentID };
	return api->UnSubMarketData(insts, 1);
}

void CfemasQuote::OnFrontConnected()
{
	if (_OnFrontConnected)
	{
		((DefOnFrontConnected)_OnFrontConnected)();
	}
}

void CfemasQuote::OnRspUserLogin(CUstpFtdcRspUserLoginField *pRspUserLogin, CUstpFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	int eId = -1;
	if (pRspInfo)
		eId = pRspInfo->ErrorID;
	if (eId == 0)
	{
		strcpy_s(_TradingDay, sizeof(_TradingDay), api->GetTradingDay());
	}
	if (_OnRspUserLogin)
	{
		((DefOnRspUserLogin)_OnRspUserLogin)(eId);
	}
}

void CfemasQuote::OnRspError(CUstpFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (_OnRtnError && pRspInfo)
	{
		((DefOnRtnError)_OnRtnError)(pRspInfo->ErrorID, pRspInfo->ErrorMsg);
	}
}

void CfemasQuote::OnFrontDisconnected(int nReason)
{
	if (_OnRspUserLogout)
	{
		((DefOnRspUserLogout)_OnRspUserLogout)(nReason);
	}
}

void CfemasQuote::OnRtnDepthMarketData(CUstpFtdcDepthMarketDataField *pDepthMarketData)
{
	if (_OnRtnDepthMarketData)
	{
		MarketData f;
		memset(&f, 0, sizeof(MarketData));
		f.AskPrice1 = pDepthMarketData->AskPrice1;
		f.AskVolume1 = pDepthMarketData->AskVolume1;
		f.AveragePrice = pDepthMarketData->SettlementPrice;
		f.BidPrice1 = pDepthMarketData->BidPrice1;
		f.BidVolume1 = pDepthMarketData->BidVolume1;
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pDepthMarketData->InstrumentID);
		f.LastPrice = pDepthMarketData->LastPrice;
		f.LowerLimitPrice = pDepthMarketData->LowerLimitPrice;
		f.OpenInterest = pDepthMarketData->OpenInterest;
		f.UpdateMillisec = pDepthMarketData->UpdateMillisec;
		sprintf_s(f.UpdateTime, "%s", pDepthMarketData->UpdateTime);// "%4d%2d%2d%s", day / 10000, day % 10000 / 100, day % 100, time.erase(':'));
		f.UpperLimitPrice = pDepthMarketData->UpperLimitPrice;
		f.Volume = pDepthMarketData->Volume;
		((DefOnRtnDepthMarketData)_OnRtnDepthMarketData)(&f);
	}
}



