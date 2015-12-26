// xSpeed_trade_proxy.cpp : ���� DLL Ӧ�ó���ĵ���������
//
#include "../2015_include_lib/HFTrade.h"

#include "xSpeed_trade_proxy.h"

DFITCTraderApi* api;

void QryAccount()
{
	while (_session != 0)
	{
		ReqQryAccount();
		Sleep(500);
	}
}

//����api
DllExport void WINAPI CreateApi()
{
	api = DFITCTraderApi::CreateDFITCTraderApi();
}

///ע��ǰ�û������ַ
DllExport int WINAPI ReqConnect(char *pFront)
{
	CxSpeed_trade_proxy* spi = new CxSpeed_trade_proxy();
	return api->Init(pFront, (DFITCTraderSpi*)spi);
}

///�û���¼����
DllExport int WINAPI ReqUserLogin(char* pInvestor, char* pPwd, char* pBroker)
{
	strcpy_s(_investor, sizeof(_investor), pInvestor);
	strcpy_s(_broker, sizeof(_broker), pBroker);

	DFITCUserLoginField f;
	memset(&f, 0, sizeof(DFITCUserLoginField));
	strcpy_s(f.accountID, sizeof(f.accountID), pInvestor);
	strcpy_s(f.passwd, sizeof(f.passwd), pPwd);
	return api->ReqUserLogin(&f);
}

///�ǳ�����
DllExport void WINAPI ReqUserLogout()
{
	_session = 0;
	api->Release();
}

///��ȡ��ǰ������
///@retrun ��ȡ���Ľ�����
///@remark ֻ�е�¼�ɹ���,���ܵõ���ȷ�Ľ�����
DllExport const char* WINAPI GetTradingDay()
{
	return _TradingDay;
}

/// <summary>
/// ί�б���
/// </summary>
/// <param name="pInstrument">��Լ</param>
/// <param name="pDirection">����</param>
/// <param name="pOffset">��ƽ</param>
/// <param name="pPrice">�۸�:�м�ʱ��ʹ��,����0.</param>
/// <param name="pVolume">����</param>
/// <param name="pHedge">���Ա�ʶ,����Ϊĳһ�౨���ı�־</param>
/// <param name="type">0-�м�;1-�޼�;2-FAK;3-FOK[FAK����FOK]</param>
/// <returns>���ر�����ʶ</returns>
DllExport int WINAPI ReqOrderInsert(char *pInstrument, DirectionType pDirection, OffsetType pOffset, double pPrice, int pVolume, HedgeType pHedge, OrderType pType, char *pCustom)
{
	DFITCInsertOrderField f;
	memset(&f, 0, sizeof(DFITCInsertOrderField));
	strcpy_s(f.accountID, sizeof(f.accountID), _investor);
	switch (pDirection)
	{
	case Buy:
		f.buySellType = DFITC_SPD_BUY;
		break;
	case Sell:
		f.buySellType = DFITC_SPD_SELL;
		break;
	}
	strcpy_s(f.instrumentID, sizeof(f.instrumentID), pInstrument);
	
	switch (pOffset)
	{
	case Open:
		f.openCloseType = DFITC_SPD_OPEN;
		break;
	case Close:
		f.openCloseType = DFITC_SPD_CLOSE;
		break;
	case CloseToday:
		f.openCloseType = DFITC_SPD_CLOSETODAY;
		break;
	case Excute:
		f.openCloseType = DFITC_SPD_EXECUTE;
		break;
	}
	switch (pHedge)
	{
	case Speculation:
		f.speculator = DFITC_SPD_SPECULATOR;
		break;
	case Arbitrage:
		f.speculator = DFITC_SPD_ARBITRAGE;
		break;
	case Hedge:
		f.speculator = DFITC_SPD_HEDGE;
		break;
	}
	f.instrumentType = DFITC_COMM_TYPE; //DFITC_OPT_TYPE
	f.insertType = DFITC_BASIC_ORDER;// DFITC_AUTO_ORDER
	f.orderType = DFITC_LIMITORDER; //DFITC_MKORDER; DFITC_ARBITRAGE;
	f.insertPrice = pPrice;
	f.orderAmount = pVolume;
	switch (pType)
	{
	case Limit:
		f.orderProperty = DFITC_SP_NON; //DFITC_SP_FAK; DFITC_SP_FOK
		break;
	case Market:
		f.orderProperty = DFITC_SP_FAK;// DFITC_SP_NON; //DFITC_SP_FAK; DFITC_SP_FOK
		f.orderType = DFITC_MKORDER; //DFITC_ARBITRAGE;
		break;
	case FAK:
		f.orderProperty = DFITC_SP_FAK; //DFITC_SP_FOK;
		break;
	case FOK:
		f.orderProperty = DFITC_SP_FOK;
		break;
	}
	f.lRequestID = ++req;
	if (pCustom == NULL)
		pCustom = new char('\n');
	string str(pCustom);
	str = str.length() > 6 ? str.substr(0, 6) : (string(6 - str.length(), '0') + str);

	f.localOrderID = req * 1000000 + atoi(str.c_str());
	return api->ReqInsertOrder(&f);
}

/// <summary>
/// ����
/// </summary>
/// <param name="pOrderId">ReqOrderInsert�з��صı�ʶ</param>
/// <returns></returns>
DllExport int WINAPI ReqOrderAction(long pOrderId)
{
	if (_id_order.find(pOrderId) == _id_order.end())
		return -1;
	OrderField of = _id_order[pOrderId];

	DFITCCancelOrderField f;
	memset(&f, 0, sizeof(DFITCCancelOrderField));
	strcpy_s(f.accountID, sizeof(f.accountID), _investor);
	strcpy_s(f.instrumentID, sizeof(f.instrumentID), of.InstrumentID);

	f.localOrderID = 0;
	f.spdOrderID = pOrderId;	//��̨ί�к�
	f.lRequestID = ++req;
	if (0 == api->ReqCancelOrder(&f))
		return 0;
	return -1;
}

int QryOrder()
{
	DFITCOrderField of;
	memset(&of, 0, sizeof(DFITCOrderField));
	strcpy_s(of.accountID, sizeof(of.accountID), _investor);
	of.lRequestID = ++req;
	return api->ReqQryOrderInfo(&of);
}
int QryTrade()
{
	DFITCMatchField tf;
	memset(&tf, 0, sizeof(DFITCMatchField));
	strcpy_s(tf.accountID, sizeof(tf.accountID), _investor);
	tf.lRequestID = ++req;
	return api->ReqQryMatchInfo(&tf);
}
DllExport int WINAPI ReqQryPosition()
{
	DFITCPositionField f;
	memset(&f, 0, sizeof(DFITCPositionField));
	strcpy_s(f.accountID, sizeof(f.accountID), _investor);
	f.lRequestID = ++req;
	return api->ReqQryPosition(&f);
}
DllExport int WINAPI ReqQryAccount()
{
	DFITCCapitalField f;
	memset(&f, 0, sizeof(DFITCCapitalField));
	strcpy_s(f.accountID, sizeof(f.accountID), _investor);
	f.lRequestID = ++req;
	return api->ReqQryCustomerCapital(&f);
}

//////////////// �����й���Ӧ ////////////////////

CxSpeed_trade_proxy::CxSpeed_trade_proxy(void)
{

}

void CxSpeed_trade_proxy::OnFrontConnected()
{
	if (_OnFrontConnected)
	{
		((DefOnFrontConnected)_OnFrontConnected)();
	}
}

void CxSpeed_trade_proxy::OnFrontDisconnected(int nReason)
{
	if (_OnRspUserLogout)
	{
		((DefOnRspUserLogout)_OnRspUserLogout)(nReason);
	}
}

void CxSpeed_trade_proxy::OnRspUserLogin(struct DFITCUserLoginInfoRtnField * pUserLoginInfoRtn, struct DFITCErrorRtnField * pErrorInfo)
{
	if (_OnRspUserLogin)
	{
		if (repareInfo(pErrorInfo)->nErrorID == 0)
		{
			_session = pUserLoginInfoRtn->sessionID;

			DFITCBillConfirmField f;
			memset(&f, 0, sizeof(DFITCBillConfirmField));
			strcpy_s(f.accountID, sizeof(f.accountID), _investor);
			f.confirmFlag = DFITC_CON_CONFIRM;
			//f.date
			f.lRequestID = ++req;
			api->ReqBillConfirm(&f);

			DFITCTradingDayField fDate;
			memset(&fDate, 0, sizeof(DFITCTradingDayField));
			fDate.lRequestID = ++req;
			api->ReqTradingDay(&fDate);
		}
		else
			((DefOnRspUserLogin)_OnRspUserLogin)(repareInfo(pErrorInfo)->nErrorID);
	}
}

void CxSpeed_trade_proxy::OnRspUserLogout(struct DFITCUserLogoutInfoRtnField * pUserLogoutInfoRtn, struct DFITCErrorRtnField * pErrorInfo)
{

}

void CxSpeed_trade_proxy::OnRspTradingDay(struct DFITCTradingDayRtnField * pTradingDayRtnData)
{
	strcpy_s(_TradingDay, strlen(pTradingDayRtnData->date) + 1, pTradingDayRtnData->date);
	string s(_TradingDay);
	s.erase(4, 1);
	s.erase(6, 1);
	strcpy_s(_TradingDay, sizeof(_TradingDay), s.c_str());
	((DefOnRspUserLogin)_OnRspUserLogin)(0);


	DFITCExchangeInstrumentField f;
	memset(&f, 0, sizeof(DFITCExchangeInstrumentField));
	strcpy_s(f.accountID, sizeof(f.accountID), _investor);
	strcpy_s(f.exchangeID, "DCE");
	f.lRequestID = ++req;
	api->ReqQryExchangeInstrument(&f);
}

void CxSpeed_trade_proxy::OnRspQryExchangeInstrument(struct DFITCExchangeInstrumentRtnField * pInstrumentData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast)
{
	if (_OnRspQryInstrument)
	{
		InstrumentField f;
		memset(&f, 0, sizeof(InstrumentField));
		strcpy_s(f.ExchangeID, sizeof(f.ExchangeID), pInstrumentData->exchangeID);
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pInstrumentData->instrumentID);
		f.PriceTick = 1;
		strcpy_s(f.ProductID, sizeof(f.ProductID), pInstrumentData->VarietyName);
		f.VolumeMultiple = (int)pInstrumentData->contractMultiplier;
		((DefOnRspQryInstrument)_OnRspQryInstrument)(&f, bIsLast);
	}
	if (bIsLast)
	{
		char exc[8];
		if (strcmp(pInstrumentData->exchangeID, "DCE") == 0)
			strcpy_s(exc, "CZCE");
		else if (strcmp(pInstrumentData->exchangeID, "CZCE") == 0)
			strcpy_s(exc, "SHFE");
		else if (strcmp(pInstrumentData->exchangeID, "SHFE") == 0)
			strcpy_s(exc, "CFFEX");
		else
		{
			if (!_started)
			{
				ReqQryAccount();
			}
			return;
		}
		DFITCExchangeInstrumentField f;
		memset(&f, 0, sizeof(DFITCExchangeInstrumentField));
		strcpy_s(f.accountID, sizeof(f.accountID), _investor);
		strcpy_s(f.exchangeID, exc);
		f.lRequestID = ++req;
		api->ReqQryExchangeInstrument(&f);
	}
}

void CxSpeed_trade_proxy::OnRspCustomerCapital(struct DFITCCapitalInfoRtnField * pCapitalInfoRtn, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast)
{
	if (bIsLast)
	{
		if (_OnRspQryTradingAccount)
		{
			TradingAccount a;
			memset(&a, 0, sizeof(TradingAccount));
			a.Available = pCapitalInfoRtn->available;
			a.CloseProfit = pCapitalInfoRtn->closeProfitLoss;
			a.Commission = pCapitalInfoRtn->fee;
			a.CurrMargin = pCapitalInfoRtn->margin;
			a.FrozenCash = pCapitalInfoRtn->frozenMargin;
			a.Fund = pCapitalInfoRtn->todayEquity;
			a.PositionProfit = pCapitalInfoRtn->positionProfitLoss;
			a.PreBalance = pCapitalInfoRtn->preEquity;
			((DefOnRspQryTradingAccount)_OnRspQryTradingAccount)(&a);
		}
		//��ֲ�
		if (!_started)
		{
			ReqQryPosition();
		}
	}
}

void CxSpeed_trade_proxy::OnRspQryPosition(struct DFITCPositionInfoRtnField * pPositionInfoRtn, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast)
{
	if (_OnRspQryPosition)
	{
		PositionField f;
		memset(&f, 0, sizeof(PositionField));
		switch (pPositionInfoRtn->buySellType)
		{
		case DFITC_SPD_BUY:
			f.Direction = Buy;
			break;
		case  DFITC_SPD_SELL:
			f.Direction = Sell;
			break;
		}
		switch (pPositionInfoRtn->speculator)
		{
		case DFITC_SPD_SPECULATOR:
			f.Hedge = Speculation;
			break;
		case  DFITC_SPD_ARBITRAGE:
			f.Hedge = Arbitrage;
			break;
		case  DFITC_SPD_HEDGE:
			f.Hedge = Hedge;
		}
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pPositionInfoRtn->instrumentID);
		//f.Margin = pPositionInfoRtn->dMargin;
		f.Price = pPositionInfoRtn->positionAvgPrice;
		f.Position = pPositionInfoRtn->totalAvaiAmount;
		f.TdPosition = pPositionInfoRtn->todayAvaiAmount;
		f.YdPosition = pPositionInfoRtn->lastAvaiAmount;
		((DefOnRspQryPosition)_OnRspQryPosition)(&f, bIsLast);
	}
	if (bIsLast && !_started)
	{
		ReqQryOrder();
	}
}

void CxSpeed_trade_proxy::OnRspQryOrderInfo(struct DFITCOrderCommRtnField * pRtnOrderData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast)
{
	OrderField f;
	memset(&f, 0, sizeof(OrderField));
	if (_id_order.find(pRtnOrderData->spdOrderID) == _id_order.end()) //δ�ҵ�:���Լ����͵�ί��
	{

		//�����µ�orderfield
		switch (pRtnOrderData->buySellType)
		{
		case DFITC_SPD_BUY:
			f.Direction = Buy;
			break;
		case  DFITC_SPD_SELL:
			f.Direction = Sell;
			break;
		}
		switch (pRtnOrderData->speculator)
		{
		case DFITC_SPD_SPECULATOR:
			f.Hedge = Speculation;
			break;
		case  DFITC_SPD_ARBITRAGE:
			f.Hedge = Arbitrage;
			break;
		case  DFITC_SPD_HEDGE:
			f.Hedge = Hedge;
		}
		switch (pRtnOrderData->openClose)
		{
		case DFITC_SPD_OPEN:
			f.Offset = Open;
			break;
		case DFITC_SPD_CLOSE:
			f.Offset = Close;
			break;
		case DFITC_SPD_CLOSETODAY:
			f.Offset = CloseToday;
			break;
		case DFITC_SPD_EXECUTE:
			f.Offset = Excute;
			break;
		}
		strcpy_s(f.InsertTime, 16, pRtnOrderData->commTime);
		strcpy_s(f.InstrumentID, 32, pRtnOrderData->instrumentID);
		switch (pRtnOrderData->orderStatus)
		{
		case DFITC_SPD_PARTIAL:
			f.Status = Partial;
			break;
		case DFITC_SPD_FILLED:
			f.Status = Filled;
			break;
		case DFITC_SPD_CANCELED:
			f.Status = Canceled;
			break;
		default:
			f.Status = Normal;
		}
		f.IsLocal = false;
		f.LimitPrice = pRtnOrderData->insertPrice;

		//f.OrderID = pRtnOrderData->localOrderID;
		f.OrderID = pRtnOrderData->spdOrderID;
		f.Volume = pRtnOrderData->orderAmount;
		f.VolumeLeft = f.Volume;
		_id_order[f.OrderID] = f;
	}
	else
		f = _id_order[pRtnOrderData->spdOrderID];

	//if (_OnRspQryOrder)// &&  && pRtnOrderData->orderStatus == DFITC_SPD_TRIGGERED) //��̨����,δ��������
	//	((DefOnRspQryOrder)_OnRspQryOrder)(&f, bIsLast);

	if (bIsLast && !_started)
	{
		ReqQryTrade();
	}
}

void CxSpeed_trade_proxy::OnRspQryMatchInfo(struct DFITCMatchedRtnField * pRtnMatchData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast)
{
	TradeField t;
	memset(&t, 0, sizeof(TradeField));
	if (pRtnMatchData)
	{
		switch (pRtnMatchData->buySellType)
		{
		case DFITC_SPD_BUY:
			t.Direction = Buy;
			break;
		case  DFITC_SPD_SELL:
			t.Direction = Sell;
			break;
		}
		switch (pRtnMatchData->speculator)
		{
		case DFITC_SPD_SPECULATOR:
			t.Hedge = Speculation;
			break;
		case  DFITC_SPD_ARBITRAGE:
			t.Hedge = Arbitrage;
			break;
		case  DFITC_SPD_HEDGE:
			t.Hedge = Hedge;
		}
		switch (pRtnMatchData->openClose)
		{
		case DFITC_SPD_OPEN:
			t.Offset = Open;
			break;
		case DFITC_SPD_CLOSE:
			t.Offset = Close;
			break;
		case DFITC_SPD_CLOSETODAY:
			t.Offset = CloseToday;
			break;
		case DFITC_SPD_EXECUTE:
			t.Offset = Excute;
			break;
		}
		strcpy_s(t.InstrumentID, sizeof(t.InstrumentID), pRtnMatchData->instrumentID);
		strcpy_s(t.ExchangeID, sizeof(t.ExchangeID), pRtnMatchData->exchangeID);
		t.OrderID = pRtnMatchData->spdOrderID;//*��*������localOrderID, ֻ��psdid;
		t.Price = pRtnMatchData->matchedPrice;
		strcpy_s(t.TradeTime, sizeof(t.TradeTime), pRtnMatchData->matchedTime);
		strcpy_s(t.TradeID, sizeof(t.TradeID), pRtnMatchData->matchedID);
		strcpy_s(t.TradingDay, sizeof(t.TradingDay), _TradingDay);
		t.Volume = pRtnMatchData->matchedAmount;

		char tid[128];
		sprintf_s(tid, "%s%d", t.TradeID, t.Direction);
		_id_trade[string(tid)] = t;
	}
	/*
		if (_OnRspQryTrade)
		{
		((DefOnRspQryTrade)_OnRspQryTrade)(&t, bIsLast);
		}*/
	if (bIsLast && !_started)
	{
		_started = true;
		hThread = CreateThread(
			NULL,                                   // SD  
			0,                                  // initial stack size  
			(LPTHREAD_START_ROUTINE)QryOnLaunch,    // thread function  
			NULL,                                    // thread argument  
			0,                                   // creation option  
			NULL//threadID                               // thread identifier  
			);
	}
}

void CxSpeed_trade_proxy::OnRspArbitrageInstrument(struct DFITCAbiInstrumentRtnField * pAbiInstrumentData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast)
{
	if (_OnRspQryInstrument)
	{
		InstrumentField f;
		memset(&f, 0, sizeof(InstrumentField));
		strcpy_s(f.ExchangeID, sizeof(f.ExchangeID), pAbiInstrumentData->exchangeID);
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pAbiInstrumentData->InstrumentID);
		f.PriceTick = 1;
		((DefOnRspQryInstrument)_OnRspQryInstrument)(&f, bIsLast);
	}
}

void CxSpeed_trade_proxy::OnRtnInstrumentStatus(struct DFITCInstrumentStatusField * pInstrumentStatus)
{
	if (_OnRtnExchangeStatus)
	{
		ExchangeStatusType s = Trading;
		switch (pInstrumentStatus->InstrumentStatus)
		{
		case 0:
			break;
		}
		((DefOnRtnExchangeStatus)_OnRtnExchangeStatus)(pInstrumentStatus->InstrumentID, s);
	}
}

void CxSpeed_trade_proxy::OnRtnErrorMsg(struct DFITCErrorRtnField * pErrorInfo)
{
	if (_OnRtnError)
	{
		((DefOnRtnError)_OnRtnError)(repareInfo(pErrorInfo)->nErrorID, repareInfo(pErrorInfo)->errorMsg);
	}
}

void CxSpeed_trade_proxy::OnRtnTradingNotice(struct DFITCTradingNoticeInfoField * pTradingNoticeInfo)
{
	if (_OnRtnNotice)
	{
		((DefOnRtnNotice)_OnRtnNotice)(pTradingNoticeInfo->FieldContent);
	}
}


void CxSpeed_trade_proxy::OnRspInsertOrder(struct DFITCOrderRspDataRtnField * pOrderRtn, struct DFITCErrorRtnField * pErrorInfo)
{
	if (_OnRtnError)
	{
		if (_id_order.find(pOrderRtn->spdOrderID) == _id_order.end()) //�ҵ�:��ʾΪ�Լ����͵�ί��
		{
			return;
		}

		OrderField f = _id_order[pOrderRtn->spdOrderID];
		f.Status = Canceled;
		_id_order[pOrderRtn->spdOrderID] = f;
		((DefOnRtnError)_OnRtnError)(repareInfo(pErrorInfo)->nErrorID, repareInfo(pErrorInfo)->errorMsg);

	}
}


void CxSpeed_trade_proxy::OnRtnOrder(struct DFITCOrderRtnField * pRtnOrderData)
{
	OrderField f;
	if (_id_order.find(pRtnOrderData->spdOrderID) == _id_order.end()) //δ�ҵ�:���Լ����͵�ί��
	{
		memset(&f, 0, sizeof(OrderField));
		//�����µ�orderfield
		switch (pRtnOrderData->buySellType)
		{
		case DFITC_SPD_BUY:
			f.Direction = Buy;
			break;
		case  DFITC_SPD_SELL:
			f.Direction = Sell;
			break;
		}
		switch (pRtnOrderData->speculator)
		{
		case DFITC_SPD_SPECULATOR:
			f.Hedge = Speculation;
			break;
		case  DFITC_SPD_ARBITRAGE:
			f.Hedge = Arbitrage;
			break;
		case  DFITC_SPD_HEDGE:
			f.Hedge = Hedge;
		}
		switch (pRtnOrderData->openCloseType)
		{
		case DFITC_SPD_OPEN:
			f.Offset = Open;
			break;
		case DFITC_SPD_CLOSE:
			f.Offset = Close;
			break;
		case DFITC_SPD_CLOSETODAY:
			f.Offset = CloseToday;
			break;
		case DFITC_SPD_EXECUTE:
			f.Offset = Excute;
			break;
		}
		strcpy_s(f.InsertTime, sizeof(f.InsertTime), pRtnOrderData->SuspendTime);
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pRtnOrderData->instrumentID);

		f.Status = Normal;

		f.IsLocal = pRtnOrderData->sessionID == _session;
		f.LimitPrice = pRtnOrderData->insertPrice;

		//f.OrderID = pRtnOrderData->localOrderID;
		f.OrderID = pRtnOrderData->spdOrderID;
		f.Volume = pRtnOrderData->orderAmount;
		f.VolumeLeft = f.Volume;

		sprintf_s(f.Custom, "%d", pRtnOrderData->localOrderID % 1000000);
		_id_order[f.OrderID] = f;

		//ֻ���״���Ӧʱ��֪ͨ
		if (_OnRtnOrder)// &&  && pRtnOrderData->orderStatus == DFITC_SPD_TRIGGERED) //��̨����,δ��������
		{
			((DefOnRtnOrder)_OnRtnOrder)(&f);
		}
	}
	else
	{
		f = _id_order[pRtnOrderData->spdOrderID];
		switch (pRtnOrderData->orderStatus)
		{
		case DFITC_SPD_PARTIAL:
			f.Status = Partial;
			break;
		case DFITC_SPD_FILLED:
			f.Status = Filled;
			break;
		case DFITC_SPD_CANCELED:
			f.Status = Canceled;
			break;
		default:
			f.Status = Normal;
		}
		_id_order[f.OrderID] = f;
	}
}

//�ɽ�
void CxSpeed_trade_proxy::OnRtnMatchedInfo(struct DFITCMatchRtnField * pRtnMatchData)
{
	if (_id_order.find(pRtnMatchData->spdOrderID) != _id_order.end())
	{
		OrderField f = _id_order[pRtnMatchData->spdOrderID];

		strcpy_s(f.TradeTime, 16, pRtnMatchData->matchedTime);
		//�˴���ȷ��:matchedAmount���ѳɽ������Ǵ˴γɽ���
		int preTrade = f.Volume - f.VolumeLeft;
		f.AvgPrice = (f.AvgPrice*(preTrade)+pRtnMatchData->matchedPrice*pRtnMatchData->matchedAmount) / (preTrade + pRtnMatchData->matchedAmount);
		f.TradeVolume = pRtnMatchData->matchedAmount;
		f.VolumeLeft -= f.TradeVolume;
		if (f.VolumeLeft == 0)
			f.Status = Filled;
		else
			f.Status = Partial;
		_id_order[f.OrderID] = f;
		if (_OnRtnOrder)
		{
			((DefOnRtnOrder)_OnRtnOrder)(&f);
		}
	}

	//�гɽ�ʱ,�ȵ�order�ٵ�trade
	if (_OnRtnTrade)
	{
		TradeField t;
		memset(&t, 0, sizeof(TradeField));
		switch (pRtnMatchData->buySellType)
		{
		case DFITC_SPD_BUY:
			t.Direction = Buy;
			break;
		case  DFITC_SPD_SELL:
			t.Direction = Sell;
			break;
		}
		switch (pRtnMatchData->speculator)
		{
		case DFITC_SPD_SPECULATOR:
			t.Hedge = Speculation;
			break;
		case  DFITC_SPD_ARBITRAGE:
			t.Hedge = Arbitrage;
			break;
		case  DFITC_SPD_HEDGE:
			t.Hedge = Hedge;
		}
		switch (pRtnMatchData->openCloseType)
		{
		case DFITC_SPD_OPEN:
			t.Offset = Open;
			break;
		case DFITC_SPD_CLOSE:
			t.Offset = Close;
			break;
		case DFITC_SPD_CLOSETODAY:
			t.Offset = CloseToday;
			break;
		case DFITC_SPD_EXECUTE:
			t.Offset = Excute;
			break;
		}
		strcpy_s(t.InstrumentID, sizeof(t.InstrumentID), pRtnMatchData->instrumentID);
		strcpy_s(t.ExchangeID, sizeof(t.ExchangeID), pRtnMatchData->exchangeID);
		t.OrderID = pRtnMatchData->spdOrderID;	//����spdOrderId
		t.Price = pRtnMatchData->matchedPrice;
		strcpy_s(t.TradeTime, sizeof(t.TradeTime), pRtnMatchData->matchedTime);
		strcpy_s(t.TradeID, sizeof(t.TradeID), pRtnMatchData->matchID);
		strcpy_s(t.TradingDay, sizeof(t.TradingDay), _TradingDay);
		t.Volume = pRtnMatchData->matchedAmount;
		((DefOnRtnTrade)_OnRtnTrade)(&t);
	}
	//���ʽ�,��������ֲ�
	DFITCCapitalField f;
	memset(&f, 0, sizeof(DFITCCapitalField));
	strcpy_s(f.accountID, sizeof(f.accountID), _investor);
	f.lRequestID = ++req;
	api->ReqQryCustomerCapital(&f);
}

//����
void CxSpeed_trade_proxy::OnRtnCancelOrder(struct DFITCOrderCanceledRtnField * pCancelOrderData)
{
	if (_OnRtnCancel)
	{

		if (_id_order.find(pCancelOrderData->spdOrderID) == _id_order.end())
			return;

		OrderField f = _id_order[pCancelOrderData->spdOrderID];
		f.Status = Canceled;
		_id_order[pCancelOrderData->spdOrderID] = f;
		((DefOnRtnCancel)_OnRtnCancel)(&f);
	}
}
