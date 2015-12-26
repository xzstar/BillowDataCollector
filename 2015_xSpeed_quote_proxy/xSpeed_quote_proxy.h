

#include "../2015_include_lib/DFITCMdApi.h"

using namespace DFITCXSPEEDMDAPI;

class CxSpeed_quote_proxy : DFITCMdSpi
{
private:
	DFITCErrorRtnField rif;
	DFITCErrorRtnField* repareInfo(DFITCErrorRtnField *pRspInfo)
	{
		if (pRspInfo == NULL)
		{
			memset(&rif, 0, sizeof(DFITCErrorRtnField));
			rif.nErrorID = 0;
			strcpy_s(rif.errorMsg, "no error");
			return &rif;
		}
		else
			return pRspInfo;
	}

public:
	CxSpeed_quote_proxy(void);

	// TODO:  �ڴ�������ķ�����
	/**
	* ��������������Ӧ
	*/
	virtual void OnFrontConnected();

	/**
	* �������Ӳ�������Ӧ
	*/
	virtual void OnFrontDisconnected(int nReason);

	/**
	* ��½������Ӧ:���û�������¼�����ǰ�û�������Ӧʱ�˷����ᱻ���ã�֪ͨ�û���¼�Ƿ�ɹ���
	* @param pRspUserLogin:�û���¼��Ϣ�ṹ��ַ��
	* @param pRspInfo:������ʧ�ܣ����ش�����Ϣ��ַ���ýṹ���д�����Ϣ��
	*/
	virtual void OnRspUserLogin(struct DFITCUserLoginInfoRtnField * pRspUserLogin, struct DFITCErrorRtnField * pRspInfo);

	/**
	* �ǳ�������Ӧ:���û������˳������ǰ�û�������Ӧ�˷����ᱻ���ã�֪ͨ�û��˳�״̬��
	* @param pRspUsrLogout:�����û��˳���Ϣ�ṹ��ַ��
	* @param pRspInfo:������ʧ�ܣ����ش�����Ϣ��ַ��
	*/
	virtual void OnRspUserLogout(struct DFITCUserLogoutInfoRtnField * pRspUsrLogout, struct DFITCErrorRtnField * pRspInfo) ;

	/*����Ӧ��*/
	virtual void OnRspError(struct DFITCErrorRtnField *pRspInfo) ;

	/**
	* ���鶩��Ӧ��:���û��������鶩�ĸ÷����ᱻ���á�
	* @param pSpecificInstrument:ָ���Լ��Ӧ�ṹ���ýṹ������Լ�������Ϣ��
	* @param pRspInfo:������Ϣ������������󣬸ýṹ���д�����Ϣ��
	*/
	virtual void OnRspSubMarketData(struct DFITCSpecificInstrumentField * pSpecificInstrument, struct DFITCErrorRtnField * pRspInfo) {};

	/**
	* ȡ����������Ӧ��:���û������˶������÷����ᱻ���á�
	* @param pSpecificInstrument:ָ���Լ��Ӧ�ṹ���ýṹ������Լ�������Ϣ��
	* @param pRspInfo:������Ϣ������������󣬸ýṹ���д�����Ϣ��
	*/
	virtual void OnRspUnSubMarketData(struct DFITCSpecificInstrumentField * pSpecificInstrument, struct DFITCErrorRtnField * pRspInfo) {};

	/**
	* ������ϢӦ��:�����������ɹ��������鷵��ʱ���÷����ᱻ���á�
	* @param pMarketDataField:ָ��������Ϣ�ṹ��ָ�룬�ṹ���а��������������Ϣ��
	*/
	virtual void OnMarketData(struct DFITCDepthMarketDataField * pMarketDataField);

	/**
	* ������ȷ����Ӧ:���ڽ��ս�������Ϣ��
	* @param DFITCTradingDayRtnField: ���ؽ���������ȷ����Ӧ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspTradingDay(struct DFITCTradingDayRtnField * pTradingDayRtnData);
};

