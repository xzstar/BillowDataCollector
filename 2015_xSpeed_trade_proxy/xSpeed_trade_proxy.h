

#include "../2015_include_lib/DFITCTraderApi.h"

using namespace DFITCXSPEEDAPI;

class CxSpeed_trade_proxy : DFITCTraderSpi
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
	CxSpeed_trade_proxy(void);

	/* ��������������Ӧ:���ͻ����뽻�׺�̨�轨����ͨ������ʱ����δ��¼ǰ�����ͻ���API���Զ������ǰ�û�֮������ӣ�
	* ��������ã����Զ��������ӣ������ø÷���֪ͨ�ͻ��ˣ� �ͻ��˿�����ʵ�ָ÷���ʱ������ʹ���ʽ��˺Ž��е�¼��
	*���÷�������Api��ǰ�û��������Ӻ󱻵��ã��õ��ý�����˵��tcp�����Ѿ������ɹ����û���Ҫ���е�¼���ܽ��к�����ҵ�������
	*  ��¼ʧ����˷������ᱻ���á���
	*/
	virtual void OnFrontConnected();

	/**
	* �������Ӳ�������Ӧ�����ͻ����뽻�׺�̨ͨ�����ӶϿ�ʱ���÷��������á���������������API���Զ��������ӣ��ͻ��˿ɲ�������
	* @param  nReason:����ԭ��
	*        0x1001 �����ʧ��
	*        0x1002 ����дʧ��
	*        0x2001 ����������ʱ
	*        0x2002 ��������ʧ��
	*        0x2003 �յ�������
	*/
	virtual void OnFrontDisconnected(int nReason);

	/**
	* ��½������Ӧ:���û�������¼�����ǰ�û�������Ӧʱ�˷����ᱻ���ã�֪ͨ�û���¼�Ƿ�ɹ���
	* @param pUserLoginInfoRtn:�û���¼��Ϣ�ṹ��ַ��
	* @param pErrorInfo:������ʧ�ܣ����ش�����Ϣ��ַ���ýṹ���д�����Ϣ��
	*/
	virtual void OnRspUserLogin(struct DFITCUserLoginInfoRtnField * pUserLoginInfoRtn, struct DFITCErrorRtnField * pErrorInfo);

	/**
	* �ǳ�������Ӧ:���û������˳������ǰ�û�������Ӧ�˷����ᱻ���ã�֪ͨ�û��˳�״̬��
	* @param pUserLogoutInfoRtn:�����û��˳���Ϣ�ṹ��ַ��
	* @param pErrorInfo:������ʧ�ܣ����ش�����Ϣ��ַ��
	*/
	virtual void OnRspUserLogout(struct DFITCUserLogoutInfoRtnField * pUserLogoutInfoRtn, struct DFITCErrorRtnField * pErrorInfo);

	/**
	* �ڻ�ί�б�����Ӧ:���û�¼�뱨����ǰ�÷�����Ӧʱ�÷����ᱻ���á�
	* @param pOrderRtn:�����û��µ���Ϣ�ṹ��ַ��
	* @param pErrorInfo:������ʧ�ܣ����ش�����Ϣ��ַ��
	*/
	virtual void OnRspInsertOrder(struct DFITCOrderRspDataRtnField * pOrderRtn, struct DFITCErrorRtnField * pErrorInfo);

	/**
	* �ڻ�ί�г�����Ӧ:���û�������ǰ�÷�����Ӧ�Ǹ÷����ᱻ���á�
	* @param pOrderCanceledRtn:���س�����Ӧ��Ϣ�ṹ��ַ��
	* @param pErrorInfo:������ʧ�ܣ����ش�����Ϣ��ַ��
	*/
	virtual void OnRspCancelOrder(struct DFITCOrderRspDataRtnField * pOrderCanceledRtn, struct DFITCErrorRtnField * pErrorInfo){};

	/**
	* ����ر�
	* @param pErrorInfo:������Ϣ�Ľṹ��ַ��
	*/
	virtual void OnRtnErrorMsg(struct DFITCErrorRtnField * pErrorInfo);

	/**
	* �ɽ��ر�:��ί�гɹ����׺�η����ᱻ���á�
	* @param pRtnMatchData:ָ��ɽ��ر��Ľṹ��ָ�롣
	*/
	virtual void OnRtnMatchedInfo(struct DFITCMatchRtnField * pRtnMatchData);

	/**
	* ί�лر�:�µ�ί�гɹ��󣬴˷����ᱻ���á�
	* @param pRtnOrderData:ָ��ί�лر���ַ��ָ�롣
	*/
	virtual void OnRtnOrder(struct DFITCOrderRtnField * pRtnOrderData);

	/**
	* �����ر�:�������ɹ���÷����ᱻ���á�
	* @param pCancelOrderData:ָ�򳷵��ر��ṹ�ĵ�ַ���ýṹ�������������Լ�������Ϣ��
	*/
	virtual void OnRtnCancelOrder(struct DFITCOrderCanceledRtnField * pCancelOrderData);

	/**
	* ��ѯ����ί����Ӧ:���û�����ί�в�ѯ�󣬸÷����ᱻ���á�
	* @param pRtnOrderData:ָ��ί�лر��ṹ�ĵ�ַ��
	* @param bIsLast:�����Ƿ������һ����Ӧ��Ϣ��0 -��   1 -�ǣ���
	*/
	virtual void OnRspQryOrderInfo(struct DFITCOrderCommRtnField * pRtnOrderData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast);

	/**
	* ��ѯ���ճɽ���Ӧ:���û������ɽ���ѯ��÷����ᱻ���á�
	* @param pRtnMatchData:ָ��ɽ��ر��ṹ�ĵ�ַ��
	* @param bIsLast:�����Ƿ������һ����Ӧ��Ϣ��0 -��   1 -�ǣ���
	*/
	virtual void OnRspQryMatchInfo(struct DFITCMatchedRtnField * pRtnMatchData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast);

	/**
	* �ֲֲ�ѯ��Ӧ:���û������ֲֲ�ѯָ���ǰ�÷�����Ӧʱ�÷����ᱻ���á�
	* @param pPositionInfoRtn:���سֲ���Ϣ�ṹ�ĵ�ַ��
	* @param pErrorInfo:������Ϣ�ṹ������ֲֲ�ѯ���������򷵻ش�����Ϣ��
	* @param bIsLast:�����Ƿ������һ����Ӧ��Ϣ��0 -��   1 -�ǣ���
	*/
	virtual void OnRspQryPosition(struct DFITCPositionInfoRtnField * pPositionInfoRtn, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast);

	/**
	* �ͻ��ʽ��ѯ��Ӧ:���û������ʽ��ѯָ���ǰ�÷�����Ӧʱ�÷����ᱻ���á�
	* @param pCapitalInfoRtn:�����ʽ���Ϣ�ṹ�ĵ�ַ��
	* @param pErrorInfo:������Ϣ�ṹ������ͻ��ʽ��ѯ���������򷵻ش�����Ϣ��
	*/
	virtual void OnRspCustomerCapital(struct DFITCCapitalInfoRtnField * pCapitalInfoRtn, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast);

	/**
	* ��������Լ��ѯ��Ӧ:���û�������Լ��ѯָ���ǰ�÷�����Ӧʱ�÷����ᱻ���á�
	* @param pInstrumentData:���غ�Լ��Ϣ�ṹ�ĵ�ַ��
	* @param pErrorInfo:������Ϣ�ṹ������ֲֲ�ѯ���������򷵻ش�����Ϣ��
	* @param bIsLast:�����Ƿ������һ����Ӧ��Ϣ��0 -��   1 -�ǣ���
	*/
	virtual void OnRspQryExchangeInstrument(struct DFITCExchangeInstrumentRtnField * pInstrumentData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast);

	/**
	* ������Լ��ѯ��Ӧ:���û�����������Լ��ѯָ���ǰ�÷�����Ӧʱ�÷����ᱻ���á�
	* @param pAbiInstrumentData:����������Լ��Ϣ�ṹ�ĵ�ַ��
	* @param pErrorInfo:������Ϣ�ṹ������ֲֲ�ѯ���������򷵻ش�����Ϣ��
	* @param bIsLast:�����Ƿ������һ����Ӧ��Ϣ��0 -��   1 -�ǣ���
	*/
	virtual void OnRspArbitrageInstrument(struct DFITCAbiInstrumentRtnField * pAbiInstrumentData, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast);

	/**
	* ��ѯָ����Լ��Ӧ:���û�����ָ����Լ��ѯָ���ǰ�÷�����Ӧʱ�÷����ᱻ���á�
	* @param pInstrument:����ָ����Լ��Ϣ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspQrySpecifyInstrument(struct DFITCInstrumentRtnField * pInstrument, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast){};

	/**
	* ��ѯ�ֲ���ϸ��Ӧ:���û�������ѯ�ֲ���ϸ��ǰ�÷�����Ӧʱ�÷����ᱻ���á�
	* @param pInstrument:���سֲ���ϸ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspQryPositionDetail(struct DFITCPositionDetailRtnField * pPositionDetailRtn, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast){};

	/**
	* ����֪ͨ��Ӧ:���ڽ���XSPEED��̨�ֶ�����֪ͨ����֧��ָ���ͻ���Ҳ֧��ϵͳ�㲥��
	* @param pTradingNoticeInfo: �����û��¼�֪ͨ�ṹ�ĵ�ַ��
	*/
	virtual void OnRtnTradingNotice(struct DFITCTradingNoticeInfoField * pTradingNoticeInfo);

	/**
	* ��Լ����״̬֪ͨ��Ӧ:���ڽ��պ�Լ�ڿ�������µ�״̬��
	* @param pInstrumentStatus: ���ؽ��׺�Լ״̬֪ͨ�ṹ�ĵ�ַ��
	*/
	virtual void OnRtnInstrumentStatus(struct DFITCInstrumentStatusField * pInstrumentStatus);

	/**
	* �����޸���Ӧ:�����޸��ʽ��˻���¼���롣
	* @param pResetPassword: ���������޸Ľṹ�ĵ�ַ��
	*/
	virtual void OnRspResetPassword(struct DFITCResetPwdRspField * pResetPassword, struct DFITCErrorRtnField * pErrorInfo){};

	/**
	* ���ױ����ѯ��Ӧ:���ؽ��ױ�����Ϣ
	* @param pTradeCode: ���ؽ��ױ����ѯ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspQryTradeCode(struct DFITCQryTradeCodeRtnField * pTradeCode, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast){};

	/**
	* �˵�ȷ����Ӧ:���ڽ��տͻ��˵�ȷ��״̬��
	* @param pBillConfirm: �����˵�ȷ�Ͻṹ�ĵ�ַ��
	*/
	virtual void OnRspBillConfirm(struct DFITCBillConfirmRspField * pBillConfirm, struct DFITCErrorRtnField * pErrorInfo){};

	/**
	* ��ѯ�ͻ�Ȩ����㷽ʽ��Ӧ:���ؿͻ�Ȩ�����ķ�ʽ
	* @param pEquityComputMode: ���ؿͻ�Ȩ����㷽ʽ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspEquityComputMode(struct DFITCEquityComputModeRtnField * pEquityComputMode){};

	/**
	* �ͻ������˵���ѯ��Ӧ:�����˵���Ϣ
	* @param pQryBill: ���ؿͻ������˵���ѯ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspQryBill(struct DFITCQryBillRtnField *pQryBill, struct DFITCErrorRtnField * pErrorInfo, bool bIsLast){};

	/**
	* ����IDȷ����Ӧ:���ڽ��ճ�����Ϣ��
	* @param pProductRtnData: ���س���IDȷ����Ӧ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspConfirmProductInfo(struct DFITCProductRtnField * pProductRtnData){};

	/**
	* ������ȷ����Ӧ:���ڽ��ս�������Ϣ��
	* @param DFITCTradingDayRtnField: ���ؽ���������ȷ����Ӧ�ṹ�ĵ�ַ��
	*/
	virtual void OnRspTradingDay(struct DFITCTradingDayRtnField * pTradingDayRtnData);
};

