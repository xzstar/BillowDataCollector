#define DllExport __declspec(dllexport)
#define WINAPI      __stdcall
#define WIN32_LEAN_AND_MEAN             //  �� Windows ͷ�ļ����ų�����ʹ�õ���Ϣ

#include <windows.h>
#include <time.h>
#include <map>
#include <string>

// exchange[8], xxxtime[16], id[32], msg[128]
///////// ������װ ////////
#pragma region enum
enum OrderType : int
{
	/// <summary>
	/// �޼�
	/// </summary>
	Limit,
	/// <summary>
	/// �м�
	/// </summary>
	Market,
	/// <summary>
	/// ���ɼ���
	/// </summary>
	FAK,
	/// <summary>
	/// ȫ��ȫ��
	/// </summary>
	FOK
};

/// <summary>
/// ��������
/// </summary>
enum DirectionType : int
{
	/// <summary>
	/// ��
	/// </summary>
	Buy,

	/// <summary>
	/// ��
	/// </summary>
	Sell
};

/// <summary>
/// ��ƽ
/// </summary>
enum OffsetType : int
{
	/// <summary>
	/// 
	/// </summary>
	Open,
	/// <summary>
	/// 
	/// </summary>
	Close,
	/// <summary>
	/// ƽ��
	/// </summary>
	CloseToday,
	/// <summary>
	/// ��Ȩ��Ȩ
	/// </summary>
	Excute,
};

enum OrderStatus : int
{
	/// <summary>
	/// ί��
	/// </summary>
	Normal,
	/// <summary>
	/// ����
	/// </summary>
	Partial,
	/// <summary>
	/// ȫ��
	/// </summary>
	Filled,
	/// <summary>
	/// ����
	/// </summary>
	Canceled,
};

/// <summary>
/// ������״̬
/// </summary>
enum ExchangeStatusType : int
{
	/// <summary>
	/// ����ǰ
	/// </summary>
	BeforeTrading,
	/// <summary>
	/// �ǽ���
	/// </summary>
	NoTrading,
	/// <summary>
	/// ����
	/// </summary>
	Trading,
	/// <summary>
	/// ����
	/// </summary>
	Closed,
};

/// <summary>
/// Ͷ���ױ���־
/// </summary>
enum HedgeType : int
{
	/// <summary>
	/// Ͷ��
	/// </summary>
	Speculation,

	/// <summary>
	/// ����
	/// </summary>
	Arbitrage,

	/// <summary>
	/// �ױ�
	/// </summary>
	Hedge,
};

enum ProductClassType :int
{
	/// <summary>
	/// �ڻ�
	/// </summary>
	Futures,
	/// <summary>
	/// �ڻ���Ȩ
	/// </summary>
	Options,
	/// <summary>
	/// ���
	/// </summary>
	Combination,
	/// <summary>
	/// �ֻ���Ȩ
	/// </summary>
	SpotOption,
};
	///�ڻ�
//#define THOST_FTDC_PC_Futures '1'
//	///�ڻ���Ȩ
//#define THOST_FTDC_PC_Options '2'
//	///���
//#define THOST_FTDC_PC_Combination '3'
//	///����
//#define THOST_FTDC_PC_Spot '4'
//	///��ת��
//#define THOST_FTDC_PC_EFP '5'
//	///�ֻ���Ȩ
//#define THOST_FTDC_PC_SpotOption '6'
#pragma endregion enum

#pragma region structs
/// <summary>
/// ��Լ��Ϣ
/// </summary>
struct InstrumentField
{
	/// <summary>
	/// ��Լ����
	/// </summary>
	char InstrumentID[32];

	/// <summary>
	/// ��Ʒ����
	/// </summary>
	char ProductID[32];

	/// <summary>
	/// ����������
	/// </summary>
	char ExchangeID[8];

	/// <summary>
	/// ��Լ��������
	/// </summary>
	int VolumeMultiple;

	/// <summary>
	/// ��С�䶯��λ
	/// </summary>
	double PriceTick;
	
	/// <summary>
	/// Ʒ������
	/// </summary>
	ProductClassType ProductClass;
};

/// <summary>
/// �ֲ�
/// </summary>
struct PositionField
{
	/// <summary>
	/// ��Լ����
	/// </summary>
	char InstrumentID[32];

	/// <summary>
	/// ����
	/// </summary>
	DirectionType Direction;

	/// <summary>
	/// �ֲ־���
	/// </summary>
	double Price;

	/// <summary>
	/// �ֲܳ���
	/// </summary>
	int Position;

	/// <summary>
	/// ���
	/// </summary>
	int YdPosition;

	/// <summary>
	/// ���
	/// </summary>
	int TdPosition;

	/// <summary>
	/// ռ�ñ�֤��
	/// </summary>
	//double Margin;

	/// <summary>
	/// Ͷ���ױ���־
	/// </summary>
	HedgeType Hedge;
};

/// <summary>
/// �ʻ�Ȩ��
/// </summary>
struct TradingAccount
{
	/// <summary>
	/// �ϴν���׼����
	/// </summary>
	double PreBalance;

	/// <summary>
	/// �ֲ�ӯ��
	/// </summary>
	double PositionProfit;

	/// <summary>
	/// ƽ��ӯ��
	/// </summary>
	double CloseProfit;

	/// <summary>
	/// ������
	/// </summary>
	double Commission;

	/// <summary>
	/// ��ǰ��֤���ܶ�
	/// </summary>
	double CurrMargin;

	/// <summary>
	/// ������ʽ�
	/// </summary>
	double FrozenCash;

	/// <summary>
	/// �����ʽ�
	/// </summary>
	double Available;

	/// <summary>
	/// ��̬Ȩ��
	/// </summary>
	double Fund;
};

/// <summary>
/// ����
/// </summary>
struct OrderField
{
	/// <summary>
	/// ������ʶ
	/// </summary>
	long OrderID;

	/// <summary>
	/// ��Լ
	/// </summary>
	char InstrumentID[32];

	/// <summary>
	/// ����
	/// </summary>
	DirectionType Direction;

	/// <summary>
	/// ��ƽ
	/// </summary>
	OffsetType Offset;

	/// <summary>
	/// ����
	/// </summary>
	double LimitPrice;

	/// <summary>
	/// �ɽ�����
	/// </summary>
	double AvgPrice;

	/// <summary>
	/// ί��ʱ��(������)
	/// </summary>
	char InsertTime[16];

	/// <summary>
	/// ���ɽ�ʱ��
	/// </summary>
	char TradeTime[16];

	/// <summary>
	/// ���γɽ���,trade����
	/// </summary>
	int TradeVolume;

	/// <summary>
	/// ��������
	/// </summary>
	int Volume;

	/// <summary>
	/// δ�ɽ�,trade����
	/// </summary>
	int VolumeLeft;

	/// <summary>
	/// Ͷ��
	/// </summary>
	HedgeType Hedge;

	/// <summary>
	/// �Ƿ񱻳���
	/// </summary>
	OrderStatus Status;

	/// <summary>
	/// �Ƿ�������
	/// </summary>
	int IsLocal;
	
	/// <summary>
	/// �ͻ��Զ����ֶ�(xSpeed��֧������)
	/// </summary>
	char Custom[6];
};

/// <summary>
/// �ɽ�
/// </summary>
struct TradeField
{
	/// <summary>
	/// �ɽ����
	/// </summary>
	char TradeID[32];

	/// <summary>
	/// ��Լ����
	/// </summary>
	char InstrumentID[32];

	/// <summary>
	/// ����������
	/// </summary>
	char ExchangeID[8];

	/// <summary>
	/// ��������
	/// </summary>
	DirectionType Direction;

	/// <summary>
	/// ��ƽ��־
	/// </summary>
	OffsetType Offset;

	/// <summary>
	/// Ͷ���ױ���־
	/// </summary>
	HedgeType Hedge;

	/// <summary>
	/// �۸�
	/// </summary>
	double Price;

	/// <summary>
	/// ����
	/// </summary>
	int Volume;

	/// <summary>
	/// �ɽ�ʱ��
	/// </summary>
	char TradeTime[16];

	/// <summary>
	/// ������
	/// </summary>
	char TradingDay[16];

	/// <summary>
	/// ��Ӧ��ί�б�ʶ
	/// </summary>
	long OrderID;
};
#pragma endregion structs

#pragma region typedef
///���ͻ����뽻�׺�̨������ͨ������ʱ����δ��¼ǰ�����÷��������á�
typedef int (WINAPI *DefOnFrontConnected)();								void* _OnFrontConnected;

///��¼������Ӧ
typedef int (WINAPI *DefOnRspUserLogin)(int pErrId);		void* _OnRspUserLogin;

///�ǳ�������Ӧ
typedef int (WINAPI *DefOnRspUserLogout)(int pReason);						void* _OnRspUserLogout;

///����Ӧ��
typedef int (WINAPI *DefOnRtnError)(int pErrId, const char* pMsg);			void* _OnRtnError;

//������Ϣ
typedef int (WINAPI *DefOnRtnNotice)(const char* pMsg);						void* _OnRtnNotice;

//������״̬��Ϣ
typedef int (WINAPI *DefOnRtnExchangeStatus)(const char* pExchangeID, ExchangeStatusType pStatus);	void* _OnRtnExchangeStatus;

//���غ�Լ,��¼���Զ�����
typedef int (WINAPI *DefOnRspQryInstrument)(InstrumentField* pInstrument, bool pLast);	void* _OnRspQryInstrument;

//���غ�Լ,��¼���Զ�����
typedef int (WINAPI *DefOnRspQryOrder)(OrderField* pOrder, bool pLast);	void* _OnRspQryOrder;

//���غ�Լ,��¼���Զ�����
typedef int (WINAPI *DefOnRspQryTrade)(TradeField* pTrade, bool pLast);	void* _OnRspQryTrade;

//���غ�Լ,��¼���Զ�����
typedef int (WINAPI *DefOnRspQryPosition)(PositionField* pPosition, bool pLast);	void* _OnRspQryPosition;

//���غ�Լ,��¼���Զ�����
typedef int (WINAPI *DefOnRspQryTradingAccount)(TradingAccount* pAccount);	void* _OnRspQryTradingAccount;

//������Ӧ
typedef int(WINAPI *DefOnRtnOrder)(OrderField*);							void* _OnRtnOrder;

//�����ɽ���Ӧ
typedef int (WINAPI *DefOnRtnTrade)(TradeField*);							void* _OnRtnTrade;

//����������Ӧ
typedef int (WINAPI *DefOnRtnCancel)(OrderField*);							void* _OnRtnCancel;
#pragma endregion typedef


//ע����Ӧ����
DllExport void WINAPI RegOnFrontConnected(void* onFunction){ _OnFrontConnected = onFunction; }
DllExport void WINAPI RegOnRspUserLogin(void* onFunction){ _OnRspUserLogin = onFunction; }
DllExport void WINAPI RegOnRspUserLogout(void* onFunction){ _OnRspUserLogout = onFunction; }
DllExport void WINAPI RegOnRtnError(void* onFunction){ _OnRtnError = onFunction; }
DllExport void WINAPI RegOnRtnNotice(void* onFunction){ _OnRtnNotice = onFunction; }
DllExport void WINAPI RegOnRtnExchangeStatus(void* onFunction){ _OnRtnExchangeStatus = onFunction; }
DllExport void WINAPI RegOnRspQryInstrument(void* onFunction){ _OnRspQryInstrument = onFunction; }
DllExport void WINAPI RegOnRspQryOrder(void* onFunction){ _OnRspQryOrder = onFunction; }
DllExport void WINAPI RegOnRspQryTrade(void* onFunction){ _OnRspQryTrade = onFunction; }
DllExport void WINAPI RegOnRspQryPosition(void* onFunction){ _OnRspQryPosition = onFunction; }
DllExport void WINAPI RegOnRspQryTradingAccount(void* onFunction){ _OnRspQryTradingAccount = onFunction; }
DllExport void WINAPI RegOnRtnOrder(void* onFunction){ _OnRtnOrder = onFunction; }
DllExport void WINAPI RegOnRtnTrade(void* onFunction){ _OnRtnTrade = onFunction; }
DllExport void WINAPI RegOnRtnCancel(void* onFunction){ _OnRtnCancel = onFunction; }
////////////////////////////
DllExport void WINAPI CreateApi();
DllExport int WINAPI ReqConnect(char *pFront);
DllExport int WINAPI ReqUserLogin(char* pInvestor, char* pPwd, char* pBroker);
DllExport void WINAPI ReqUserLogout();
DllExport const char* WINAPI GetTradingDay();

HANDLE hThread;//����ʱ��ѯ��
using namespace std;
int req = 0;
char _TradingDay[16];
char _investor[16];
char _broker[16];
map<long, OrderField> _id_order;
map<string, TradeField> _id_trade;
bool _started = false;
int _session = -1; //==0ʱ��Ϊ��ѯѭ���˳�����

int QryOrder();
int QryTrade();
void QryAccount();

DllExport int WINAPI ReqQryOrder()
{	
	if (_started)
	{
		if (_OnRspQryOrder)
		{
			for (map<long, OrderField>::iterator i = _id_order.begin(); i != _id_order.end(); ++i)
			{
				((DefOnRspQryOrder)_OnRspQryOrder)(&i->second, i == _id_order.end());
			}
		}
		return 0;
	}
	return QryOrder();
}
DllExport int WINAPI ReqQryTrade()
{
	if (_started)
	{
		if (_OnRspQryTrade)
		{
			for (map<string, TradeField>::iterator i = _id_trade.begin(); i != _id_trade.end(); ++i)
			{
				((DefOnRspQryTrade)_OnRspQryTrade)(&i->second, i == _id_trade.end());
			}
		}
		return 0;
	}
	return QryTrade();
}
DllExport int WINAPI ReqQryPosition();
DllExport int WINAPI ReqQryAccount();

DllExport int WINAPI ReqOrderInsert(char *pInstrument, DirectionType pDirection, OffsetType pOffset, double pPrice, int pVolume, HedgeType pHedge, OrderType pType, char *pCustom);
DllExport int WINAPI ReqOrderAction(long pOrderId);

void QryOnLaunch()
{
	if (_OnRspQryOrder)
	{
		for (map<long, OrderField>::iterator i = _id_order.begin(); i != _id_order.end(); ++i)
		{
			((DefOnRspQryOrder)_OnRspQryOrder)(&i->second, i == _id_order.end());
		}
	}
	if (_OnRspQryTrade)
	{
		for (map<string, TradeField>::iterator i = _id_trade.begin(); i != _id_trade.end(); ++i)
		{
			((DefOnRspQryTrade)_OnRspQryTrade)(&i->second, i == _id_trade.end());
		}
	}
	QryAccount();
}

/*
��¼����ɻ�ȡtradingday
��¼��:qryaccount, qryposition, qryorder, qrytrade
*/


