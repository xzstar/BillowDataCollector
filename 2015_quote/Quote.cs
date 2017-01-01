﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Quote2015
{
    ///深度行情
    [StructLayout(LayoutKind.Sequential)]
	public class MarketData : IComparable
	{
		/// <summary>
		/// 合约代码
		/// </summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string InstrumentID;	//31
		/// <summary>
		/// 最新价
		/// </summary>
		public double LastPrice;
		/// <summary>
		///申买价一
		/// </summary>
		public double BidPrice;
		/// <summary>
		///申买量一
		/// </summary>
		public int BidVolume;
		/// <summary>
		///申卖价一
		/// </summary>
		public double AskPrice;
		/// <summary>
		///申卖量一
		/// </summary>
		public int AskVolume;
		/// <summary>
		///当日均价
		/// </summary>
		public double AveragePrice;
		/// <summary>
		///数量
		/// </summary>
		public int Volume;
		/// <summary>
		///持仓量
		/// </summary>
		public double OpenInterest;
		/// <summary>
		///最后修改时间:yyyyMMdd HH:mm:ss(20141114:日期由主程序处理,因大商所取到的actionday==tradingday)
		/// </summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string UpdateTime;
		/// <summary>
		///最后修改毫秒
		/// </summary>
		public int UpdateMillisec;
		/// <summary>
		///涨停板价
		/// </summary>
		public double UpperLimitPrice;
		/// <summary>
		///跌停板价
		/// </summary>
		public double LowerLimitPrice;
		int IComparable.CompareTo(object obj)
		{
			MarketData y = (MarketData)obj;
			return DateTime.ParseExact(UpdateTime, "yyyyMMdd HH:mm:ss", null).AddMilliseconds(UpdateMillisec).CompareTo(DateTime.ParseExact(y.UpdateTime, "yyyyMMdd HH:mm:ss", null).AddMilliseconds(y.UpdateMillisec));
		}
	}

	public class Proxy
	{

		/// <summary>
		///     原型是 :HMODULE LoadLibrary(LPCTSTR lpFileName);
		/// </summary>
		/// <param name="lpFileName"> DLL 文件名 </param>
		/// <returns> 函数库模块的句柄 </returns>
		[DllImport("kernel32.dll")]
		private static extern IntPtr LoadLibrary(string lpFileName);

		/// <summary>
		///     原型是 : FARPROC GetProcAddress(HMODULE hModule, LPCWSTR lpProcName);
		/// </summary>
		/// <param name="hModule"> 包含需调用函数的函数库模块的句柄 </param>
		/// <param name="lpProcName"> 调用函数的名称 </param>
		/// <returns> 函数指针 </returns>
		[DllImport("kernel32.dll")]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		/// <summary>
		///     原型是 : BOOL FreeLibrary(HMODULE hModule);
		/// </summary>
		/// <param name="hModule"> 需释放的函数库模块的句柄 </param>
		/// <returns> 是否已释放指定的 Dll </returns>
		[DllImport("kernel32", EntryPoint = "FreeLibrary", SetLastError = true)]
		protected static extern bool FreeLibrary(IntPtr hModule);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pHModule"></param>
		/// <param name="lpProcName"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private static Delegate Invoke(IntPtr pHModule, string lpProcName, Type t)
		{
			// 若函数库模块的句柄为空，则抛出异常 
			if (pHModule == IntPtr.Zero)
			{
				throw (new Exception(" 函数库模块的句柄为空 , 请确保已进行 LoadDll 操作 !"));
			}
			// 取得函数指针 
			IntPtr farProc = GetProcAddress(pHModule, lpProcName);
			// 若函数指针，则抛出异常 
			if (farProc == IntPtr.Zero)
			{
				throw (new Exception(" 没有找到 :" + lpProcName + " 这个函数的入口点 "));
			}
			return Marshal.GetDelegateForFunctionPointer(farProc, t);
		}


		private IntPtr _handle;

		private delegate void DefCreateApi();

		private delegate int DefReqConnect(string pFront);

		private delegate int DefReqUserLogin(string pInvestor, string pPwd, string pBroker);

		private delegate void DefReqUserLogout();

		private delegate IntPtr DefGetTradingDay();

		private delegate int DefReqSubMarketData(string pInstrument);

		private delegate int DefReqUnSubMarketData(string pInstrument);

		#region 注册响应

		private delegate void Reg(IntPtr pPtr);

		public delegate void FrontConnected();

		private FrontConnected _OnFrontConnected;

		public event FrontConnected OnFrontConnected
		{
			add
			{
				_OnFrontConnected += value;
				var reg = (Invoke(this._handle, "RegOnFrontConnected", typeof(Reg)) as Reg);
				reg((Marshal.GetFunctionPointerForDelegate(_OnFrontConnected)));
			}
			remove
			{
				_OnFrontConnected -= value;
				(Invoke(this._handle, "RegOnFrontConnected", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnFrontConnected));
			}
		}

		public delegate void RspUserLogin(int pErrId);

		private RspUserLogin _OnRspUserLogin;

		public event RspUserLogin OnRspUserLogin
		{
			add
			{
				_OnRspUserLogin += value;
				(Invoke(this._handle, "RegOnRspUserLogin", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRspUserLogin));
			}
			remove
			{
				_OnRspUserLogin -= value;
				(Invoke(this._handle, "RegOnRspUserLogin", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRspUserLogin));
			}
		}

		public delegate void RspUserLogout(int pReason);

		private RspUserLogout _OnRspUserLogout;

		public event RspUserLogout OnRspUserLogout
		{
			add
			{
				_OnRspUserLogout += value;
				(Invoke(this._handle, "RegOnRspUserLogout", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRspUserLogout));
			}
			remove
			{
				_OnRspUserLogout -= value;
				(Invoke(this._handle, "RegOnRspUserLogout", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRspUserLogout));
			}
		}

		public delegate void RtnError(int pErrId, string pMsg);

		private RtnError _OnRtnError;

		public event RtnError OnRtnError
		{
			add
			{
				_OnRtnError += value;
				(Invoke(this._handle, "RegOnRtnError", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRtnError));
			}
			remove
			{
				_OnRtnError -= value;
				(Invoke(this._handle, "RegOnRtnError", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRtnError));
			}
		}


		public delegate void RtnMarketData(MarketData pMarketData);

		private RtnMarketData _OnRtnMarketData;
		public event RtnMarketData OnRtnMarketData
		{
			add
			{
				_OnRtnMarketData += value;
				(Invoke(this._handle, "RegOnRtnDepthMarketData", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRtnMarketData));
			}
			remove
			{
				_OnRtnMarketData -= value;
				(Invoke(this._handle, "RegOnRtnDepthMarketData", typeof(Reg)) as Reg)(Marshal.GetFunctionPointerForDelegate(_OnRtnMarketData));
			}
		}
		#endregion

		public Proxy(string pFile)
		{
			LoadDll(pFile);
		}

		~Proxy()
		{
			FreeLibrary(_handle);
			if (File.Exists(_file))
				File.Delete(_file);
		}

		private string _file;


		/// <summary>
		/// 加载C++文件,取得相应函数
		/// </summary>
		protected void LoadDll(string pFile)
		{
			if (File.Exists(pFile))
			{
				_file = DateTime.Now.Ticks + ".dll";
				File.Copy(pFile, _file);

				this._handle = LoadLibrary(_file); // Environment.CurrentDirectory + "\\" + pFile);
			}
			if (this._handle == IntPtr.Zero)
			{
				throw (new Exception(String.Format(" 没有找到 :{0}.", Environment.CurrentDirectory + "\\" + pFile)));
			}
			Directory.CreateDirectory("log");
		}


		public int ReqConnect(string pFront)
		{
			((DefCreateApi)Invoke(this._handle, "CreateApi", typeof(DefCreateApi)))();
			return ((DefReqConnect)Invoke(this._handle, "ReqConnect", typeof(DefReqConnect)))(pFront);
		}

		public int ReqUserLogin(string pInvestor, string pPassword, string pBroker)
		{
			return ((DefReqUserLogin)Invoke(this._handle, "ReqUserLogin", typeof(DefReqUserLogin)))(pInvestor, pPassword, pBroker);
		}

		public void ReqUserLogout()
		{
			((DefReqUserLogout)Invoke(this._handle, "ReqUserLogout", typeof(DefReqUserLogout)))();
		}

		public IntPtr GetTradingDay()
		{
			return ((DefGetTradingDay)Invoke(this._handle, "GetTradingDay", typeof(DefGetTradingDay)))();
		}

		public int ReqSubscribeMarketData(string pInstrument)
		{
			return ((DefReqSubMarketData)Invoke(this._handle, "ReqSubMarketData", typeof(DefReqSubMarketData)))(pInstrument);
		}

		public int ReqUnSubscribeMarketData(string pInstrument)
		{
			return ((DefReqUnSubMarketData)Invoke(this._handle, "ReqUnSubMarketData", typeof(DefReqUnSubMarketData)))(pInstrument);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class IntEventArgs : EventArgs
	{
		/// <summary>
		/// 错误代码
		/// </summary>
		public int Value = 0;
	}

	/// <summary>
	/// 数据事件
	/// </summary>
	public class TickEventArgs : EventArgs
	{
		/// <summary>
		/// 
		/// </summary>
		public MarketData Tick;
	}
	public class ErrorEventArgs : EventArgs
	{
		/// <summary>
		/// 错误代码
		/// </summary>
		public int ErrorID = 0;
		/// <summary>
		/// 错误说明
		/// </summary>
		public string ErrorMsg = string.Empty;
	}

	public class Quote
	{
		private Proxy _proxy;

		/// <summary>
		/// 导入C++dll文件
		/// </summary>
		/// <param name="pFile">文件名(包含完整路径)</param>
		public Quote(string pFile)
		{
			_proxy = new Proxy(pFile);
			_proxy.OnFrontConnected += Quote_OnFrontConnected;
			_proxy.OnRspUserLogin += Quote_OnRspUserLogin;
			_proxy.OnRspUserLogout += Quote_OnRspUserLogout;
			_proxy.OnRtnMarketData += Quote_OnRtnDepthMarketData;
			_proxy.OnRtnError += Quote_OnRtnError;
		}

		/// <summary>
		/// Tick数据
		/// </summary>
		public ConcurrentDictionary<string, MarketData> DicTick = new ConcurrentDictionary<string, MarketData>();
		#region 响应
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public delegate void RtnTick(object sender, TickEventArgs e);

		/// <summary>
		/// 行情响应用
		/// </summary>
		private RtnTick _OnRtnTick;

		/// <summary>
		/// 
		/// </summary>
		public event RtnTick OnRtnTick
		{
			add
			{
				_OnRtnTick += value;
			}
			remove
			{
				_OnRtnTick -= value;
			}
		}

		public delegate void FrontConnected(object sender, EventArgs e);

		private FrontConnected _OnFrontConnected;

		public event FrontConnected OnFrontConnected
		{
			add
			{
				_OnFrontConnected += value;
			}
			remove
			{
				_OnFrontConnected -= value;
			}
		}

		public delegate void RspUserLogin(object sender, IntEventArgs e);

		private RspUserLogin _OnRspUserLogin;

		public event RspUserLogin OnRspUserLogin
		{
			add
			{
				_OnRspUserLogin += value;
			}
			remove
			{
				_OnRspUserLogin -= value;
			}
		}

		public delegate void RspUserLogout(object sender, IntEventArgs e);

		private RspUserLogout _OnRspUserLogout;

		public event RspUserLogout OnRspUserLogout
		{
			add
			{
				_OnRspUserLogout += value;
			}
			remove
			{
				_OnRspUserLogout -= value;
			}
		}

		public delegate void RtnError(object sender, ErrorEventArgs e);

		private RtnError _OnRtnError;

		public event RtnError OnRtnError
		{
			add
			{
				_OnRtnError += value;
			}
			remove
			{
				_OnRtnError -= value;
			}
		}
		#endregion

		#region 属性

		/// <summary>
		/// 服务器名称
		/// </summary>
		public string Server = string.Empty;

		/// <summary>
		/// 经济公司代码
		/// </summary>
		public string Broker = "0000";

		/// <summary>
		/// 帐号
		/// </summary>
		public string Investor = string.Empty;

		/// <summary>
		/// 密码
		/// </summary>
		public string Password = string.Empty;

		/// <summary>
		/// 交易日
		/// </summary>
		public string TradingDay { get; protected set; }

		/// <summary>
		///     登录成功
		/// </summary>
		public bool IsLogin { get; protected set; }
		#endregion

		void Quote_OnRtnDepthMarketData(MarketData pMarketData)
		{
			if (string.IsNullOrEmpty(pMarketData.InstrumentID) || string.IsNullOrEmpty(pMarketData.UpdateTime)
				|| pMarketData.LastPrice > pMarketData.UpperLimitPrice)
			{
				return;
			}

			//处理actionday==NULL{以下处理无效:大商所夜盘中的actionday==tradingday}
			//if (pMarketData.UpdateTime.Length == 8) //只有时间
			//{
			//	//首tick取机器日期,否则取上tick日期
			//	string aDay = string.IsNullOrEmpty(t.UpdateTime) ? DateTime.Today.ToString("yyyyMMdd") : t.UpdateTime.Split(' ')[0];

			//	pMarketData.UpdateTime = aDay + " " + pMarketData.UpdateTime;
			//	//时间更小->第2天
			//	if (String.Compare(pMarketData.UpdateTime, t.UpdateTime, StringComparison.Ordinal) < 0) 
			//	{
			//		pMarketData.UpdateTime = DateTime.ParseExact(aDay, "yyyyMMdd", null).AddDays(1).ToString("yyyyMMdd") + pMarketData.UpdateTime.Split(' ')[1];
			//	}
			//}
			//修正数据:涨跌板
			if (pMarketData.AskPrice > pMarketData.UpperLimitPrice)
			{
				pMarketData.AskPrice = pMarketData.LastPrice;
			}
			if (pMarketData.BidPrice > pMarketData.UpperLimitPrice)
			{
				pMarketData.BidPrice = pMarketData.LastPrice;
			}
			DicTick.AddOrUpdate(pMarketData.InstrumentID, pMarketData, (k, v) =>
			{
				if (pMarketData.UpdateTime == v.UpdateTime && v.UpdateMillisec < 990)  //某些交易所(如郑商所)相同秒数的ms均为0
				{
					pMarketData.UpdateMillisec = v.UpdateMillisec + 10;
				}
				return pMarketData;
			});

			if (_OnRtnTick != null)
			{
				MarketData t = new MarketData();
				foreach (FieldInfo fi in typeof(MarketData).GetFields())
				{
					fi.SetValue(t, fi.GetValue(pMarketData));
				}
				new Thread(() => _OnRtnTick(this, new TickEventArgs
				{
					Tick = t,
				})).Start();
			}
		}

		void Quote_OnRtnError(int pErrId, string pMsg)
		{
			if (_OnRtnError != null)
			{
				_OnRtnError(this, new ErrorEventArgs
				{
					ErrorID = pErrId,
					ErrorMsg = pMsg,
				});
			}
		}

		void Quote_OnRspUserLogout(int pReason)
		{
			IsLogin = false;
			if (_OnRspUserLogout != null)
			{
				_OnRspUserLogout(this, new IntEventArgs
				{
					Value = pReason,
				});
			}
		}

		void Quote_OnRspUserLogin(int pErrId)
		{
			IsLogin = pErrId == 0;
			if (IsLogin)
			{
				TradingDay = Marshal.PtrToStringAnsi(_proxy.GetTradingDay());
			}
			if (_OnRspUserLogin != null)
			{
				_OnRspUserLogin(this, new IntEventArgs
				{
					Value = pErrId,
				});
			}
		}

		void Quote_OnFrontConnected()
		{
			if (_OnFrontConnected != null)
			{
				_OnFrontConnected(this, new EventArgs());
			}
		}

		public int ReqConnect()
		{
			return _proxy.ReqConnect(Server);
		}

		public int ReqUserLogin()
		{
			return _proxy.ReqUserLogin(Investor, Password, Broker);
		}

		public void ReqUserLogout()
		{
			IsLogin = false;
			_proxy.ReqUserLogout();
		}


		public int ReqSubscribeMarketData(string pInstrument)
		{
			return _proxy.ReqSubscribeMarketData(pInstrument);
		}

		public int ReqUnSubscribeMarketData(string pInstrument)
		{
			return _proxy.ReqUnSubscribeMarketData(pInstrument);
		}

	}
}
