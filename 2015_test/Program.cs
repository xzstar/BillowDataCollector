﻿using System;
using System.Linq;
using System.Threading;
using Quote2015;
using Trade2015;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Text;

namespace ConsoleProxy
{
    [Serializable]
    class InstrumentData
    {
        public LinkedList<double> _highList;
        public LinkedList<double> _lowList;
        public int lastMin = -1;
        public int holder = 0;
        public double highest;
        public double lowest;
        public bool isToday = true;
        public InstrumentData()
        {   
            _highList = new LinkedList<double>();
            _lowList = new LinkedList<double>();
            highest = double.NaN;
            lowest = double.NaN;
        }
    }

    public class Log
    {
        public static void log(string LogStr)
        {
            StreamWriter sw = null;
            try
            {
                LogStr = Program.LogTitle + "[" +DateTime.Now.ToLocalTime().ToString() + "]" + LogStr+"\n";
                if(Program.isTest == true)
                {
                    sw = new StreamWriter("C:\\work\\TestLog.txt", true);
                }
                else
                { 
                 sw = new StreamWriter("C:\\work\\Log.txt", true);
                }
                sw.WriteLine(LogStr);
            }
            catch
            {
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        public static void logTrade(string LogStr)
        {
            StreamWriter sw = null;
            try
            {
                LogStr = /*Program.LogTitle + "[" + DateTime.Now.ToLocalTime().ToString() + "]" +*/ LogStr + "\n";
                if (Program.isTest == true)
                {
                    sw = new StreamWriter("C:\\work\\TestLogTrade.txt", true);
                }
                else
                {
                    sw = new StreamWriter("C:\\work\\LogTrade.txt", true);
                }
                sw.WriteLine(LogStr);
            }
            catch
            {
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }
    }

    public class Utils
    {
        public static bool isTradingTime(ExchangeStatusType type, DateTime dt)
        {
            if (/*type == ExchangeStatusType.Trading &&*/ dt.Hour!=8 && dt.Hour!=20)
                    return true;
            return false;
        }


    }
    class InstrumentWatcher
    {
        static Trade sTrade;
        static System.Threading.Timer timer;
        static Dictionary<string, DateTime> lastUpdateTime = new Dictionary<string, DateTime>();
        public static bool flag = true;
        static object mylock = new object();

        static string getInstumentName(string key)
        {
            char[] charArray = key.ToCharArray();
            for(int i=0;i<key.Length;i++)
            {
                char ch = charArray[i];
                if (ch >= '0' && ch <= '9')
                    return key.Substring(0, i);
            }
            return key;
        }

        static void Excute(object obj)
        {
            Thread.CurrentThread.IsBackground = true;

            lock (mylock)
            {
                if (!flag)
                {
                    return;
                }

                
                foreach(string key in lastUpdateTime.Keys)
                {
                    DateTime last = lastUpdateTime[key];
                    TimeSpan ts = DateTime.Now - last;
                    string sub = getInstumentName(key);
                    
                    if(sTrade.DicExcStatus.ContainsKey(sub) == true && sTrade.DicExcStatus[sub] == ExchangeStatusType.Trading)
                    { 
                        if (ts.TotalSeconds>180)
                        {
                            Console.WriteLine("InstrumentWatcher Excute:{0} 最近一次获得数据时间为{1}，距离现在{2}秒", key, last.ToString(),ts.TotalSeconds);
                            Log.log(string.Format("InstrumentWatcher Excute:{0} 最近一次获得数据时间为{1}，距离现在{2}秒", key, last.ToString(), ts.TotalSeconds));
                        }
                        else
                        {
                            Console.WriteLine("InstrumentWatcher Excute:{0} 最近一次获得数据时间为{1}，距离现在{2}秒", key, last.ToString(), ts.TotalSeconds);
                        }
                    }

                }

            }

        }

        public static void updateTime(string instrument, DateTime dt)
        {
            lock (mylock)
            {
                lastUpdateTime[instrument] = dt;
            }
        }

        public static void Init(Trade trade)
        {
            sTrade = trade;
            timer = new System.Threading.Timer(Excute, null, 0, 60000);
        }
    }


    class Program
    {
        static private string _inst;
        private static int _TOTALSIZE = 32;
        private static int MINSPAN = 3;
        private static double _LastPrice = double.NaN;

        private static int _orderId;
        private static int BUY_OPEN = 1;
        private static int SELL_CLOSETODAY = 2;
        private static int SELL_CLOSE = 21;
        private static int SELL_OPEN = 3;
        private static int BUY_CLOSETODAY = 4;
        private static int BUY_CLOSE = 41;

        private static ConcurrentQueue<TradeItem> _tradeQueue = new ConcurrentQueue<TradeItem>();
        public static bool isTest = true;
        public static string LogTitle = isTest?"[测试]":"[正式]";
        

        private static void operatord(Trade t, Quote q, int op, string inst)
        {
            //Console.WriteLine("操作start:{0}: {1}", op, inst);
            Log.log(string.Format(Program.LogTitle+"操作start:{0}: {1}", op, inst));
            string operation = string.Empty;
 

            DirectionType dire = DirectionType.Buy;
            OffsetType offset = OffsetType.Open;
            switch (op)
            {
                case 1:
                    dire = DirectionType.Buy;
                    offset = OffsetType.Open;
                    operation = "买开";
                    break;
                case 2:
                    dire = DirectionType.Sell;
                    offset = OffsetType.CloseToday;
                    operation = "买平";
                    break;
                case 21:
                    dire = DirectionType.Sell;
                    offset = OffsetType.Close;
                    operation = "买平昨";
                    break;
                case 3:
                    dire = DirectionType.Sell;
                    offset = OffsetType.Open;
                    operation = "卖开";
                    break;
                case 4:
                    dire = DirectionType.Buy;
                    offset = OffsetType.CloseToday;
                    operation = "卖平";
                    break;
                case 41:
                    dire = DirectionType.Buy;
                    offset = OffsetType.Close;
                    operation = "卖平昨";
                    break;
                    //case '5':
                    //    t.ReqOrderAction(_orderId);
                    //    break;
                    //case 'a':
                    //    Console.WriteLine(t.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
                    //    break;
                    //case 'b':
                    // 89oiy    Console.ForegroundColor = ConsoleColor.Cyan;
                    //    Console.WriteLine(t.DicOrderField.Aggregate("\r\n委托", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v)
                    //        => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'c':
                    //    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    //    Console.WriteLine(t.DicTradeField.Aggregate("\r\n成交", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'd': //持仓
                    //    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    //    Console.WriteLine(t.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'e':
                    //    Console.WriteLine(t.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'f':
                    //    Console.WriteLine(t.TradingAccount.GetType().GetFields().Aggregate("\r\n权益\t", (cur, n) => cur + ","
                    //        + n.GetValue(t.TradingAccount).ToString()));
                    //    break;
                    //case 'g':
                    //    Console.WriteLine("请输入合约:");
                    //    inst = Console.ReadLine();
                    //    q.ReqSubscribeMarketData(inst);
                    //    break;
                    //case 'q':
                    //    q.ReqUserLogout();
                    //    t.ReqUserLogout();
                    //    Thread.Sleep(2000); //待接口处理后续操作
                    //    Environment.Exit(0);
                    //    break;
            }
            //if (op >= 1 && op <= 4)
            {
                Console.WriteLine(Program.LogTitle + "操作:{0}: {1}", operation, inst);
                Log.log(string.Format(Program.LogTitle + "操作:{0}: {1}", operation, inst));
                //Console.WriteLine("请选择委托类型: 1-限价  2-市价  3-FAK  4-FOK");
                OrderType ot = OrderType.Limit;
                /*switch (Console.ReadKey().KeyChar)
                {
                    case '2':
                        ot = OrderType.Market;
                        break;
                    case '3':
                        ot = OrderType.FAK;
                        break;
                    case '4':
                        ot = OrderType.FOK;
                        break;
                }*/
                MarketData tick;
                if (q.DicTick.TryGetValue(inst, out tick))
                {
                    double price = dire == DirectionType.Buy ? tick.AskPrice : tick.BidPrice;
                    _orderId = -1;
                    Console.WriteLine(t.ReqOrderInsert(inst, dire, offset, price, 1, pType: ot));
                    for (int i = 0; i < 3; ++i)
                    {
                        Thread.Sleep(200);
                        if (-1 != _orderId)
                        {
                            Console.WriteLine(Program.LogTitle + "委托标识:" + _orderId);
                            break;
                        }
                    }
                }
            }
        }

        private static void operatorInstrument(int op, string inst)
        {
            TradeItem tradeItem = new TradeItem(inst, op);

            lock (_tradeQueue)
            {
                //Monitor.Wait(_tradeQueue);//暂时放弃调用线程对该资源的锁，让Consumer执行
                _tradeQueue.Enqueue(tradeItem);//生成一个资源
                Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Consumer线程即将执行
            }
        }

        //输入：q1ctp /t1ctp /q2xspeed /t2speed
        private static void Main(string[] args)
        {
            Trade trader;
            Quote quoter;
            //string inst = string.Empty;
            System.Object lockThis = new System.Object();
            Dictionary<string, InstrumentData> tradeData = new Dictionary<string, InstrumentData>();

            //LinkedList<double> _highList = new LinkedList<double>();
            //LinkedList<double> _lowList = new LinkedList<double>();
            //double highest = 0;
            //double lowest = 1000000;
        //int lastMin = -1;
        //int holder = 0;
        R:
            Console.WriteLine(Program.LogTitle + "选择接口:\t1-CTP  2-xSpeed  3-Femas  4-股指仿真  5-外汇仿真  6-郑商商品期权仿真");
            char c = '1';// Console.ReadKey(true).KeyChar;

            switch (c)
            {
                case '1': //CTP
                    if (isTest)
                    {
                        trader = new Trade("ctp_trade_proxy.dll")
                        {
                            Server = "tcp://180.168.146.187:10000", 
                            Broker = "9999"// "4040",
                        };
                        quoter = new Quote("ctp_quote_proxy.dll")
                        {

                            Server = "tcp://180.168.146.187:10010",
                            Broker = "9999",
                        };

                    }
                    else { 
                        trader = new Trade("ctp_trade_proxy.dll")
                        {
                            Server = "tcp://222.73.111.150:41205",//" tcp://101.95.8.178:51205", 
                            Broker = "9080"// "9999"// "4040",
                        };
                        quoter = new Quote("ctp_quote_proxy.dll")
                        {
                        
                            Server = "tcp://222.73.111.150:41213",//"tcp://101.95.8.178:51213",
                            Broker = "9080",
                        };
                    }
                    //t = new Trade("ctp_trade_proxy.dll")
                    //{
                    //	Server = "tcp://211.95.40.130:51205", 
                    //	Broker = "1017",
                    //};
                    //q = new Quote("ctp_quote_proxy.dll")
                    //{
                    //	Server = "tcp://211.95.40.130:51213",
                    //	Broker = "1017",
                    //};
                    break;
                case '2': //xSpeed
                    trader = new Trade("xSpeed_trade_proxy.dll")
                    {
                        Server = "tcp://203.187.171.250:10910",
                        Broker = "0001",
                    };
                    quoter = new Quote("xSpeed_quote_proxy.dll")
                    {
                        Server = "tcp://203.187.171.250:10915",
                        Broker = "0001",
                    };
                    break;
                case '3': //femas
                    trader = new Trade("femas_trade_proxy.dll")
                    {
                        Server = "tcp://116.228.53.149:6666",
                        Broker = "0001",
                    };
                    quoter = new Quote("femas_quote_proxy.dll")
                    {
                        Server = "tcp://116.228.53.149:6888",
                        Broker = "0001",
                    };
                    break;
                case '4': //CTP
                    trader = new Trade("ctp_trade_proxy.dll")
                    {
                        Server = "tcp://124.207.185.88:41205",
                        Broker = "1010",
                    };
                    quoter = new Quote("ctp_quote_proxy.dll")
                    {
                        Server = "tcp://124.207.185.88:41213",
                        Broker = "1010",
                    };
                    break;
                case '5': //CTP
                    trader = new Trade("femas_trade_proxy.dll")
                    {
                        Server = "tcp://117.184.207.111:7036",
                        Broker = "2713",
                    };
                    quoter = new Quote("femas_quote_proxy.dll")
                    {
                        Server = "tcp://117.184.207.111:7230",
                        Broker = "2713",
                    };
                    break;
                case '6': //CTP
                    trader = new Trade("ctp_trade_proxy.dll")
                    {
                        Server = "tcp://106.39.36.72:51205",
                        Broker = "1010",
                    };
                    quoter = new Quote("ctp_quote_proxy.dll")
                    {
                        Server = "tcp://106.39.36.72:51213",
                        Broker = "1010",
                    };
                    break;
                default:
                    Console.WriteLine(Program.LogTitle + "请重新选择");
                    goto R;
            }
            //Console.WriteLine("请输入帐号:");
            if (isTest)
            {
                trader.Investor = quoter.Investor ="043213"; //Console.ReadLine();
                                                               //Console.WriteLine("请输入密码:");
                trader.Password = quoter.Password = "xiezhestar";// Console.ReadLine();
            }
            else
            {
                trader.Investor = quoter.Investor = "10330177";// "043213"; //Console.ReadLine();
                                                               //Console.WriteLine("请输入密码:");
                trader.Password = quoter.Password = "525280";// "xiezhestar";// Console.ReadLine();

            }
           

            quoter.OnFrontConnected += (sender, e) =>
            {
                Console.WriteLine("OnFrontConnected");
                Log.log("OnFrontConnected");
                quoter.ReqUserLogin();
            };
            quoter.OnRspUserLogin += (sender, e) => 
            {
                Console.WriteLine("OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
            };
            quoter.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("OnRspUserLogout:{0}", e.Value);
                 Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
            };
            quoter.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            quoter.OnRtnTick += (sender, e) =>
            {
                lock (lockThis)
                {
                    Boolean isTriggerLow = false;
                    Boolean isTriggerHigh = false;
                    //Console.WriteLine("OnRtnTick:{0}", e.Tick.LastPrice);
                    //Console.WriteLine("OnRtnTick:{0}=>{1}=>{2}", e.Tick.UpdateTime, e.Tick.AskPrice, e.Tick.InstrumentID);
                    DateTime d1 = DateTime.Parse(e.Tick.UpdateTime);
                    InstrumentWatcher.updateTime(e.Tick.InstrumentID, d1);
                    InstrumentData instrumentdata;

                    //if (trader.DicExcStatus.ContainsKey(e.Tick.InstrumentID) == false)
                    //{
                    //    Console.WriteLine("OnRtnTick:{0} Not Containing TradingStatus {1}", e.Tick.InstrumentID, d1.ToString());
                    //    return;
                    //}
                    //if (Utils.isTradingTime(trader.DicExcStatus[e.Tick.InstrumentID],d1) == false)
                    //{
                    //    Console.WriteLine("OnRtnTick:{0} Not Trading time {1}", e.Tick.InstrumentID, d1.ToString());
                    //    return;
                    //}

                    if (tradeData.TryGetValue(e.Tick.InstrumentID, out instrumentdata) == false)
                    {
                        instrumentdata = new InstrumentData();
                        tradeData.Add(e.Tick.InstrumentID, instrumentdata);
                    }

                    LinkedList<double> _highList = instrumentdata._highList;
                    LinkedList<double> _lowList = instrumentdata._lowList;


                    if (instrumentdata.lastMin == -1 || (instrumentdata.lastMin != d1.Minute && d1.Minute % MINSPAN == 0))
                    {
                        //Console.WriteLine("OnRtnTick 目前记录数:{0} {1}", e.Tick.InstrumentID, _highList.Count);
                        _highList.AddLast(e.Tick.LastPrice);
                        if (_highList.Count > _TOTALSIZE)
                            _highList.RemoveFirst();

                        _lowList.AddLast(e.Tick.LastPrice);
                        if (_lowList.Count > _TOTALSIZE)
                            _lowList.RemoveFirst();
                        isTriggerHigh = true;
                        isTriggerLow = true;

                        instrumentdata.highest = 0;
                        instrumentdata.lowest = 1000000;
                        foreach (double value in _highList)
                        {
                            //Console.WriteLine("品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, highest, value);
                            Log.log(string.Format(Program.LogTitle + "品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.highest, value));

                            if (value > instrumentdata.highest)
                                instrumentdata.highest = value;

                        }

                        foreach (double value in _lowList)
                        {
                            //Console.WriteLine("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, lowest, value);
                            Log.log(string.Format("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.lowest, value));
                            if (value < instrumentdata.lowest)
                                instrumentdata.lowest = value;
                        }
                        //Console.WriteLine("品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, highest, lowest);
                        Log.log(string.Format(Program.LogTitle + "品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, instrumentdata.highest, instrumentdata.lowest));
                    }
                    else
                    {
                        double _lastHigh = _highList.Last();
                        double _lastLow = _lowList.Last();
                        if (e.Tick.LastPrice > _lastHigh)
                        {
                            _highList.RemoveLast();
                            _highList.AddLast(e.Tick.LastPrice);
                            isTriggerHigh = true;
                        }

                        if (e.Tick.LastPrice < _lastLow)
                        {
                            _lowList.RemoveLast();
                            _lowList.AddLast(e.Tick.LastPrice);
                            isTriggerLow = true;
                        }
                    }

                    instrumentdata.lastMin = d1.Minute;

                    if (isTriggerHigh)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新高:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
                        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新高:{2} 当前最高{3}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.highest));
                        // more than _totoalSize , can trade now
                        if (_highList.Count >= _TOTALSIZE && e.Tick.LastPrice > instrumentdata.highest)
                        {
                            //no trade before
                            if (instrumentdata.holder == 0)
                            {
                                //open buy
                                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
                                operatorInstrument(BUY_OPEN, e.Tick.InstrumentID);
                                instrumentdata.holder = 1;
                                instrumentdata.isToday = true;


                            }
                            else if (instrumentdata.holder == -1)
                            {
                                //close sell and open buy
                                if (instrumentdata.isToday)
                                {
                                    //operatord(trader, quoter, BUY_CLOSETODAY, e.Tick.InstrumentID);
                                    operatorInstrument(BUY_CLOSETODAY, e.Tick.InstrumentID);

                                }
                                else
                                {
                                    //operatord(trader, quoter, BUY_CLOSE, e.Tick.InstrumentID);
                                    operatorInstrument(BUY_CLOSE, e.Tick.InstrumentID);

                                }
                                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
                                operatorInstrument(BUY_OPEN, e.Tick.InstrumentID);
                                instrumentdata.holder = 1;
                                instrumentdata.isToday = true;
                            }
                        }


                    }

                    if (isTriggerLow)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新低:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
                        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新低:{2} 当前最低:{3}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.lowest));

                        // more than _totoalSize , can trade now
                        if (_lowList.Count >= _TOTALSIZE && e.Tick.LastPrice < instrumentdata.lowest)
                        {
                            //no trade before
                            if (instrumentdata.holder == 0)
                            {
                                //open sell
                                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
                                operatorInstrument(SELL_OPEN, e.Tick.InstrumentID);
                                instrumentdata.holder = -1;
                                instrumentdata.isToday = true;
                            }
                            else if (instrumentdata.holder == 1)
                            {
                                //close buy and open sell
                                if (instrumentdata.isToday)
                                {
                                    //operatord(trader, quoter, SELL_CLOSETODAY, e.Tick.InstrumentID);
                                    operatorInstrument(SELL_CLOSETODAY, e.Tick.InstrumentID);

                                }
                                else
                                {
                                    //operatord(trader, quoter, SELL_CLOSE, e.Tick.InstrumentID);
                                    operatorInstrument(SELL_CLOSE, e.Tick.InstrumentID);
                                }
                                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
                                operatorInstrument(SELL_OPEN, e.Tick.InstrumentID);
                                instrumentdata.holder = -1;
                                instrumentdata.isToday = true;
                            }
                        }
                    }

                    if (isTriggerHigh || isTriggerLow) { 
                        
                        string fileNameSerialize = @"C:\work\trade.dat";//文件名称与路径
                        if (isTest)
                        {
                            fileNameSerialize = @"C:\work\TestTrade.dat";//文件名称与路径
                        }

                        string jsonString = JsonConvert.SerializeObject(tradeData);
                        
                        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);

                        //Stream fs = new FileStream(fileNameSerialize, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        //byte[] data = new UTF8Encoding().GetBytes(jsonString);
                        //fs.Write(data, 0, data.Length);
                        //fs.Flush();
                        //fs.Close();
                        //BinaryFormatter binFormatSerialize = new BinaryFormatter();//创建二进制序列化器
                        //binFormatSerialize.Serialize(fStreamSerialize, tradeData);
                        //fStreamSerialize.Close();

                        //XmlSerializer xmlFormat = new XmlSerializer(typeof(Dictionary<string, InstrumentData>));
                        //xmlFormat.Serialize(fStreamSerialize, tradeData);//序列化对象
                        //fs.Dispose();//关闭文件
                        //fs.Close();
                  }
                }
            };

            trader.OnFrontConnected += (sender, e) => trader.ReqUserLogin();
            trader.OnRspUserLogin += (sender, e) =>
            {
                Console.WriteLine("OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
                if (e.Value == 0)
                    quoter.ReqConnect();
            };
            trader.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("OnRspUserLogout:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
            };
            trader.OnRtnCancel += (sender, e) =>
            {
                Console.WriteLine("OnRtnCancel:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnCancel:{0}", e.Value));

            };
            trader.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            trader.OnRtnExchangeStatus += (sender, e) =>
            {
                Console.WriteLine("OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status);
                Log.log(string.Format("OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status));
            };
            trader.OnRtnNotice += (sender, e) =>
            {
                Console.WriteLine("OnRtnNotice:{0}", e.Value);
                Log.log(string.Format("OnRtnNotice:{0}", e.Value));
            };
            trader.OnRtnOrder += (sender, e) =>
            {
                Console.WriteLine("OnRtnOrder:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnOrder:{0}", e.Value.OrderID));
                _orderId = e.Value.OrderID;
            };
            trader.OnRtnTrade += (sender, e) =>
            {
                Console.WriteLine("OnRtnTrade:{0}", e.Value.TradeID);
                Log.log(string.Format("OnRtnTrade:{0} OrderID {1}", e.Value.TradeID, e.Value.OrderID));
                //成交 需要下委托单
                string direction = e.Value.Direction == DirectionType.Buy ? "买" : "卖";
                string offsetType = "开";
                if (e.Value.Offset == OffsetType.Close)
                    offsetType = "平";
                else if (e.Value.Offset == OffsetType.CloseToday)
                    offsetType = "平今";
                Log.logTrade(string.Format("{0},{1},{2},{3},{4},{5}", e.Value.InstrumentID, e.Value.TradingDay, e.Value.TradeTime,
                    e.Value.Price, e.Value.Volume, direction+offsetType));
            };

            trader.ReqConnect();
            Thread.Sleep(3000);

            if (!trader.IsLogin)
                goto R;

            TradeCenter tradeCenter = new TradeCenter(trader, quoter, _tradeQueue);
            tradeCenter.start();

            InstrumentWatcher.Init(trader);

            Console.WriteLine(trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\t" + n.Value.InstrumentID));

            //使用二进制序列化对象
            string fileName = @"C:\work\trade.dat";//文件名称与路径
            if(isTest)
                fileName = @"C:\work\TestTrade.dat";//文件名称与路径
            Dictionary<string, InstrumentData> tempData = null;
            try
            {
                string text = File.ReadAllText(fileName);
                tempData = JsonConvert.DeserializeObject<Dictionary<string, InstrumentData>>(text);
            }
            catch (Exception e)
            {

            }


           // Stream fStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
           
           // BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
           //fStream.Position = 0;//
           // Dictionary<string, InstrumentData> tempData = null;
           // try {
           //     tempData = (Dictionary<string, InstrumentData>)binFormat.Deserialize(fStream);//反序列化对象

           //     //XmlSerializer xmlFormat = new XmlSerializer(typeof(Dictionary<string, InstrumentData>));
           //     //tempData = (Dictionary<string, InstrumentData>) xmlFormat.Deserialize(fStream);//序列化对象
           // }
           // catch(Exception e)
           // {

           // }
           // finally
           // {
           //     fStream.Dispose();
           // }

            if (tempData != null)
                tradeData = tempData;


            foreach(PositionField data in trader.DicPositionField.Values)
            {
                InstrumentData instrumentData;
                bool found = tradeData.TryGetValue(data.InstrumentID, out instrumentData);
                if (found)
                {
                    if (data.Position > 0)
                    {
                        if (data.Direction == DirectionType.Buy)
                            instrumentData.holder = 1;
                        else
                            instrumentData.holder = -1;

                        if (data.TdPosition > 0)
                        {
                           instrumentData.isToday = true;
                        }
                        else if(data.YdPosition > 0)
                        {
                            instrumentData.isToday = false;
                        }
                        
                    }
                    else
                    {
                        instrumentData.holder = 0;
                    }
                }
            }

            //使用二进制序列化对象
            fileName = @"C:\work\instrument.dat";//文件名称与路径
            if (isTest)
                fileName = @"C:\work\TestInstrument.dat";//文件名称与路径
            List<string> instrumentList = null;
            try
            {
                string text = File.ReadAllText(fileName);
                instrumentList = JsonConvert.DeserializeObject<List<string>>(text);
            }
            catch (Exception e)
            {

            }

            if(instrumentList == null || instrumentList.Count == 0)
            {
                string inst = string.Empty;
                Console.WriteLine(Program.LogTitle + "请输入合约:");
                inst = Console.ReadLine();
                quoter.ReqSubscribeMarketData(inst);
            }
            else
            {
                foreach(string inst in instrumentList)
                    quoter.ReqSubscribeMarketData(inst);
            }

        //quoter.ReqSubscribeMarketData("m1605");
        //quoter.ReqSubscribeMarketData("SR605");
        Inst:
            Console.WriteLine(Program.LogTitle + "q:退出  1-BK  2-SP  3-SK  4-BP  5-撤单");
            Console.WriteLine("a-交易所状态  b-委托  c-成交  d-持仓  e-合约  f-权益 g-换合约");

            DirectionType dire = DirectionType.Buy;
            OffsetType offset = OffsetType.Open;
            c = Console.ReadKey().KeyChar;
            switch (c)
            {
                case '1':
                    dire = DirectionType.Buy;
                    offset = OffsetType.Open;
                    break;
                case '2':
                    dire = DirectionType.Sell;
                    offset = OffsetType.CloseToday;
                    break;
                case '3':
                    dire = DirectionType.Sell;
                    offset = OffsetType.Open;
                    break;
                case '4':
                    dire = DirectionType.Buy;
                    offset = OffsetType.CloseToday;
                    break;
                case '5':
                    trader.ReqOrderAction(_orderId);
                    break;
                case 'a':
                    Console.WriteLine(trader.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
                    break;
                case 'b':
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(trader.DicOrderField.Aggregate("\r\n委托", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v)
                        => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'c':
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(trader.DicTradeField.Aggregate("\r\n成交", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'd': //持仓
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(trader.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'e':
                    Console.WriteLine(trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'f':
                    Console.WriteLine(trader.TradingAccount.GetType().GetFields().Aggregate("\r\n权益\t", (cur, n) => cur + ","
                        + n.GetValue(trader.TradingAccount).ToString()));
                    break;
                case 'g':
                    Console.WriteLine(Program.LogTitle + "请输入合约:");
                    string inst = Console.ReadLine();
                    quoter.ReqSubscribeMarketData(inst);
                    break;
                case 'q':
                    quoter.ReqUserLogout();
                    trader.ReqUserLogout();
                    tradeCenter.stop();
                    InstrumentWatcher.flag = false;
                    Thread.Sleep(2000); //待接口处理后续操作
                    Environment.Exit(0);
                    break;
            }
            if (c >= '1' && c <= '4')
            {
                Console.WriteLine(Program.LogTitle + "请选择委托类型: 1-限价  2-市价  3-FAK  4-FOK");
                OrderType ot = OrderType.Limit;
                switch (Console.ReadKey().KeyChar)
                {
                    case '2':
                        ot = OrderType.Market;
                        break;
                    case '3':
                        ot = OrderType.FAK;
                        break;
                    case '4':
                        ot = OrderType.FOK;
                        break;
                }
                //MarketData tick;
                //if (quoter.DicTick.TryGetValue(inst, out tick))
                //{
                //    double price = dire == DirectionType.Buy ? tick.AskPrice : tick.BidPrice;
                //    _orderId = -1;
                //    Console.WriteLine(trader.ReqOrderInsert(inst, dire, offset, price, 1, pType: ot));
                //    for (int i = 0; i < 3; ++i)
                //    {
                //        Thread.Sleep(200);
                //        if (-1 != _orderId)
                //        {
                //            Console.WriteLine(Program.LogTitle + "委托标识:" + _orderId);
                //            break;
                //        }
                //    }
                //}
            }
            goto Inst;
        }
    }
}
