using System;
using System.Linq;
using System.Threading;
using Quote2015;
using Trade2015;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Globalization;

namespace ConsoleProxy
{
    [Serializable]
    public class UnitData
    {
        public ObjectId Id { get; set; }
        public string datetime { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double open { get; set; }
        public double close { get; set; }
        public double avg_480 { get; set; }
    }

    public class InstrumentTradeConfig
    {
        public string instrument;
        public bool trade;   //not used 
        public int volumn;   //not used 
        public int span;     //not used 
    }

    [Serializable]
    public class InstrumentData //not used just for keep same way
    {
        public string lastUpdateTime = null;
        public int holder = 0;
        public bool isToday = true;
        public double price = -1;
        public double curAvg = 0;
        public bool trade;
        public int closevolumn;
        public int openvolumn;
        public double span;

    }

    class Program
    {
        private static int _TOTALSIZE = 480 ;
        private static int _MIN_INTERVAL = 15;
        
        //Trade trader;
        Quote quoter;
        //TradeCenter tradeCenter;
        //static Object lockFile = new Object();
        //private static ConcurrentQueue<TradeItem> _tradeQueue = new ConcurrentQueue<TradeItem>();
        public const bool isTest = false;
        public static string LogTitle = isTest?"[测试]":"[正式]";

        //private List<InstrumentTradeConfig> _instrumentList = new List<InstrumentTradeConfig>();
        //private Dictionary<string, InstrumentTradeConfig> _instrumentMap = new Dictionary<string, InstrumentTradeConfig>();
        private Dictionary<string, InstrumentData> tradeData = new Dictionary<string, InstrumentData>();
        //private Dictionary<int, OrderField> _tradeOrders = new Dictionary<int, OrderField>();
        //private HashSet<int> _removingOrders = new HashSet<int>();
        //private Dictionary<string, InstrumentData> tradeData = new Dictionary<string, InstrumentData>();
        //private Dictionary<string, HashSet<string>> _waitingForOp = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, List<UnitData>> unitDataMap = new Dictionary<string, List<UnitData>>();
        private Dictionary<string, string> lastUpdateTimeMap = new Dictionary<string, string>();

        /// <summary>
        /// 数据库连接
        /// </summary>
        private const string connectionString = "mongodb://127.0.0.1:27017";
        /// <summary>
        /// 指定的数据库
        /// </summary>
        private const string dbName = "data_15min";
        /// <summary>
        /// 指定的表
        /// </summary>
        //private const string tbName = "table_text";

        //static String instrument;//= "RM705";
        //static String instrument_1m = "_1m";
        static String instrument_15m = "_15m";

        private MongoDatabase mongoDB;

        private bool isInit = false;

        void subscribeInstruments()
        {
            foreach (string instrument in tradeData.Keys)
            {
                Console.WriteLine(Program.LogTitle + "品种:{0}",instrument);

                quoter.ReqSubscribeMarketData(instrument);
            }
        }

        //public void checkStatusOneMin()
        //{
        //    if(Utils.isSyncPositionTime())
        //    {
        //        //Console.WriteLine(Program.LogTitle + "更新持仓");
        //        //Console.WriteLine(trader.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
        //        //       + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
        //        //trader.ReqQryPosition();

        //        foreach(InstrumentData data in tradeData.Values)
        //        {
        //            data.isToday = false;
        //        }
        //    }
        //}
        
        private void saveAll()
        {
            foreach (string key in unitDataMap.Keys)
            {
                List<UnitData> unitDataList;
                if (unitDataMap.TryGetValue(key, out unitDataList))
                {
                    if (unitDataList == null)
                        continue;
                    Log.log(string.Format("Quit:saving {0} ", key));

                    if (unitDataList.Count > 0)
                    {
                        UnitData lastUnitData = unitDataList.Last();
                        update(key, lastUnitData);
                    }

                }
            }
        }
        public void checkStatus()
        {
            Console.WriteLine(Program.LogTitle + "checkStatus");
            if (Utils.isLogoutTimeNow() && quoter.IsLogin)
            {
                Console.WriteLine(Program.LogTitle + "isLogoutTimeNow");
                quoter.ReqUserLogout();

                if(Utils.isOverDayNow())
                {
                    Console.WriteLine(Program.LogTitle + "isOverDayNow");
                    
                }

                Thread.Sleep(3000);
                Console.WriteLine(Program.LogTitle + "quoter logout");
                Log.log(Program.LogTitle + "quoter logout");

                saveAll();

            }
            else if(Utils.isLogInTimeNow() && !quoter.IsLogin)
            {
                Console.WriteLine(Program.LogTitle + "isLogInTimeNow");
                int errorCount = 0;
                while (!quoter.IsLogin && errorCount < 100)
                {
                    quoter.ReqConnect();
                    Thread.Sleep(3000);
                    errorCount++;
                }

                if(!quoter.IsLogin)
                {
                    Console.WriteLine(Program.LogTitle + "quoter login failed");
                    Log.log(Program.LogTitle + "quoter login failed");
                }
                else
                {
                    subscribeInstruments();
                    Console.WriteLine(Program.LogTitle + "quoter login");
                    Log.log(Program.LogTitle + "quoter login");
                }

            }
            
        }
        
        private bool isOpenMin(DateTime dt)
        {
            if ((dt.Hour == 9 && dt.Minute == 0)
                || (dt.Hour == 10 && dt.Minute == 30)
                || (dt.Hour == 13 && dt.Minute == 30)
                || (dt.Hour == 21 && dt.Minute == 0))
                return true;
            else
                return false;
        }

        private bool isStartMin(DateTime dt, string instrument)
        {
            if ((dt.Hour == 10 && dt.Minute == 15)
                || (dt.Hour == 11 && dt.Minute == 30)
                || (dt.Hour == 15 && dt.Minute == 0))
                return false;
            else if ((instrument.StartsWith("rb") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("bu") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("ru") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("ag") && dt.Hour == 2 && dt.Minute >= 30)
                || (instrument.StartsWith("al") && dt.Hour == 1 && dt.Minute >= 0))
                return false;
            else if (dt.Minute % _MIN_INTERVAL == 0)
                return true;
            else
                return false;
        }

        //Todo lastMin 加上日期时间，避免涨跌停无数据，需要判断时差超过15分钟也要新bar
        private bool isNewBar(string lastUpdateTime, DateTime dt, string instrument)
        {
            DateTime lastUpdateDT = DateTime.Parse(lastUpdateTime);
            if (lastUpdateTime == null || ((lastUpdateDT.Hour != dt.Hour || lastUpdateDT.Minute != dt.Minute) && isOpenMin(dt)))
                return true;


            TimeSpan span = dt - lastUpdateDT;

            if(((lastUpdateDT.Hour != dt.Hour || lastUpdateDT.Minute != dt.Minute) && isStartMin(dt, instrument)) || span.TotalMinutes > _MIN_INTERVAL)
                return true;
            return false;
        }

        private void update(string instrument, UnitData data)
        {
            IMongoQuery query = Query.EQ("datetime", data.datetime);
            Dictionary<string, BsonValue> dict = new Dictionary<string, BsonValue>();
            dict.Add("open", data.open);
            dict.Add("close", data.close);
            dict.Add("high", data.high);
            dict.Add("low", data.low);
            dict.Add("avg_480",data.avg_480);
            MongoDbHepler.Update(mongoDB, instrument + instrument_15m, query, dict);

        }

        private void save(string instrument, UnitData data)
        {
            MongoDbHepler.Insert<UnitData>(mongoDB, instrument + instrument_15m, data);
        }

        private void syncData()
        {
            string fileName = FileUtil.getTradeFilePath();
            Dictionary<string, InstrumentData> tempData = null;
            try
            {
                string text = File.ReadAllText(fileName);
                tempData = JsonConvert.DeserializeObject<Dictionary<string, InstrumentData>>(text);
            }
            catch (Exception e)
            {

            }

            if (tempData != null && tempData.Count != 0)
                tradeData = tempData;
            else if (isInit == false) //第一次启动
            {
                string inst = string.Empty;
                Console.WriteLine(Program.LogTitle + "请输入合约:");
                inst = Console.ReadLine();
                //program.quoter.ReqSubscribeMarketData(inst);
                InstrumentData instrumentData = new InstrumentData();
                instrumentData.holder = 0;
                instrumentData.isToday = false;
                instrumentData.lastUpdateTime = "";
                instrumentData.price = 0;
                instrumentData.span = 15;
                instrumentData.trade = true;
                instrumentData.openvolumn = 1;
                instrumentData.closevolumn = 1;
                instrumentData.curAvg = 0;
                tradeData = new Dictionary<string, InstrumentData>();
                tradeData.Add(inst, instrumentData);

            }
            
            
            unitDataMap.Clear();
            mongoDB = MongoDbHepler.GetDatabase(connectionString, dbName);

            foreach (string key in tradeData.Keys)
            {
                initUnitDataMap(key);
            }
            
            if (quoter.IsLogin)
                subscribeInstruments();
        }

        private void initUnitDataMap(string instrument)
        {
            List<UnitData> unitDataList = MongoDbHepler.GetAll<UnitData>(mongoDB, instrument + instrument_15m);
            unitDataMap.Add(instrument, unitDataList);
            int count = unitDataList.Count;
            if (count > _TOTALSIZE)
            {
                UnitData lastUnitData = unitDataList.Last();
                if (lastUnitData.avg_480 <= 0)
                {
                    if (count > _TOTALSIZE)
                    {
                        double total = 0;
                        for (int i = 0; i < _TOTALSIZE; i++)
                        {
                            total += unitDataList.ElementAt(count - i - 1).close;
                        }
                        lastUnitData.avg_480 = Math.Round(total / _TOTALSIZE, 2);
                    }
                }
                Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 个数{1} 平均:{2}", instrument, count,lastUnitData.avg_480));
            }
        }
        //输入：q1ctp /t1ctp /q2xspeed /t2speed
        private static void Main(string[] args)
        {
            Program program = new Program();
            System.Object lockThis = new System.Object();
           
        R:
            Console.WriteLine(Program.LogTitle + "选择接口:\t1-CTP  2-xSpeed  3-Femas  4-股指仿真  5-外汇仿真  6-郑商商品期权仿真");
            char c = '1';

            switch (c)
            {
                case '1': //CTP
                    if (isTest)
                    {
                        /*program.trader = new Trade("ctp_trade_proxy.dll")
                        {
                            Server = "tcp://180.168.146.187:10000", 
                            Broker = "9999"// "4040",
                        };*/
                        program.quoter = new Quote("ctp_quote_proxy.dll")
                        {
                            Server = "tcp://180.168.146.187:10010",
                            Broker = "9999",
                        };

                    }
                    else {
                       /* program.trader = new Trade("ctp_trade_proxy.dll")
                        {
                            Server = "tcp://180.166.37.129:41205", //国信
                            Broker = "8030"

                            //Server = "tcp://222.73.111.150:41205",//" tcp://101.95.8.178:51205",//中建 
                            //Broker = "9080"// "9999"// "4040",
                        };*/
                        program.quoter = new Quote("ctp_quote_proxy.dll")
                        {
                            Server = "tcp://180.166.37.129:41213",//国信
                            Broker = "8030",
                            //Server = "tcp://222.73.111.150:41213",//"tcp://101.95.8.178:51213",//中建 
                            //Broker = "9080",
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
                
                default:
                    Console.WriteLine(Program.LogTitle + "请重新选择");
                    goto R;
            }

            //if (isTest)
            //{
            //    ITradeCenter tradeCenter = new TradeCenterTestImp();

            //    //tradeCenter.init("m1609");
            //    tradeCenter.init("rb1610");
            //    tradeCenter.start();
            //    goto End;
            //}

            Config config = Config.loadConfig();
            if(config == null)
            {
                Console.WriteLine("请输入帐号:");
                program.quoter.Investor = Console.ReadLine();
                Console.WriteLine("请输入密码:");
                program.quoter.Password = Console.ReadLine();
            }
            else
            {
                 program.quoter.Investor = config.user;
                 program.quoter.Password = config.password;
            }

            program.quoter.OnFrontConnected += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" +"OnFrontConnected");
                Log.log("OnFrontConnected");
                if(Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
                    program.quoter.ReqUserLogin();
            };
            program.quoter.OnRspUserLogin += (sender, e) => 
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
            };
            program.quoter.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
                 Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
            };
            program.quoter.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            
            
            program.quoter.OnRtnTick += (sender, e) =>
            {
                lock (lockThis)
                {
                    //bool needUpdate = false;

                    //InstrumentData currentInstrumentdata;

                    //if (program.tradeData.TryGetValue(e.Tick.InstrumentID, out currentInstrumentdata) == false)
                    //{
                    //    currentInstrumentdata = new InstrumentData();
                    //    program.tradeData.Add(e.Tick.InstrumentID, currentInstrumentdata);
                    //}
                   

                    List<UnitData> unitDataList;
                    if (program.unitDataMap.TryGetValue(e.Tick.InstrumentID, out unitDataList) == false)
                        return;

                    
                    //DateTime d1 = DateTime.Parse(program.quoter.TradingDay+" "+e.Tick.UpdateTime);
                    DateTime d1 = DateTime.ParseExact(program.quoter.TradingDay + " " + e.Tick.UpdateTime, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (Utils.isValidData(e.Tick.InstrumentID,d1,e.Tick.UpdateTime) == false)
                    {
                        return;
                    }

                    if (Utils.isTradingTime(e.Tick.InstrumentID, d1) == false)
                        return;


                    string lastUpdateTime;
                    if (program.lastUpdateTimeMap.TryGetValue(e.Tick.InstrumentID, out lastUpdateTime) == false)
                    {
                        lastUpdateTime = null;
                    }

                    if (lastUpdateTime!= null && program.isNewBar(lastUpdateTime,d1,e.Tick.InstrumentID))
                    {
                        int count = unitDataList.Count;
                        if (count > 0)
                        {
                            UnitData lastUnitData = unitDataList.Last();

                            if(count > _TOTALSIZE)
                            {
                                double total = 0;
                                for(int i = 0; i< _TOTALSIZE; i++)
                                {
                                    total += unitDataList.ElementAt(count - i -1).close;
                                }
                                lastUnitData.avg_480 = Math.Round(total / _TOTALSIZE,2);
                            }
                            program.update(e.Tick.InstrumentID, lastUnitData);
                        }
                        UnitData unitData = new UnitData();
                        unitData.high = unitData.low = unitData.open = unitData.close = e.Tick.LastPrice;
                        unitData.datetime = d1.ToString();
                        unitDataList.Add(unitData);

                        Console.WriteLine(string.Format(Program.LogTitle + "new bar 品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
                           e.Tick.UpdateTime, e.Tick.LastPrice));

                        program.save(e.Tick.InstrumentID, unitData);
                        
                    }
                    else if(unitDataList.Count>0)
                    {
                        UnitData unitData = unitDataList.Last();
                        
                        if (e.Tick.LastPrice > unitData.high)
                        {
                            unitData.high = e.Tick.LastPrice;
                        }

                        if (e.Tick.LastPrice < unitData.low)
                        {
                            unitData.low = e.Tick.LastPrice;
                        }
                        unitData.close = e.Tick.LastPrice;

                    }
                    program.lastUpdateTimeMap[e.Tick.InstrumentID] = d1.ToString();

                    //currentInstrumentdata.lastUpdateTime = d1.ToString();

                }
            };
            
            

            program.quoter.ReqConnect();
            Thread.Sleep(3000);
            
            if (!program.quoter.IsLogin && (Utils.isLogInTimeNow() || Utils.isTradingTimeNow()))
                goto R;

           // program.tradeCenter = new TradeCenter(program.trader, program.quoter, _tradeQueue);
           // program.tradeCenter.start();

           // InstrumentWatcher.Init(program.trader);
            LoginWatcher.Init(program);
            // Console.WriteLine(program.trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\t" + n.Value.InstrumentID));
            program.mongoDB = MongoDbHepler.GetDatabase(connectionString,dbName);
            //使用二进制序列化对象
            //string fileName = @"C:\work\Trade.dat";//文件名称与路径
            //if(isTest)
            //    fileName = @"C:\work\TestTrade.dat";//文件名称与路径
            //string fileName = FileUtil.getTradeFilePath();
            //Dictionary<string, InstrumentData> tempData = null;
            //try
            //{
            //    string text = File.ReadAllText(fileName);
            //    tempData = JsonConvert.DeserializeObject<Dictionary<string, InstrumentData>>(text);
            //}
            //catch (Exception e)
            //{

            //}

            //if (tempData != null)
            //    program.tradeData = tempData;



            //string fileName = FileUtil.getInstrumentFilePath();
            //List<InstrumentTradeConfig> instrumentList = null;
            //try
            //{
            //    string text = File.ReadAllText(fileName);
            //    instrumentList = JsonConvert.DeserializeObject<List<InstrumentTradeConfig>>(text);
            //}
            //catch (Exception e)
            //{
            //    try
            //    {
            //        List<string> oldinstrumentList = null;
            //        string text = File.ReadAllText(fileName);
            //        oldinstrumentList = JsonConvert.DeserializeObject<List<string>>(text);
            //        instrumentList = new List<InstrumentTradeConfig>();
            //        foreach (string inst in oldinstrumentList)
            //        {
            //            InstrumentTradeConfig instrumentConfig = new InstrumentTradeConfig();
            //            instrumentConfig.instrument = inst;
            //            instrumentConfig.trade = true;
            //            instrumentConfig.volumn = 1;
            //            instrumentList.Add(instrumentConfig);
                        
            //        }
            //        string jsonString = JsonConvert.SerializeObject(instrumentList);
            //        File.WriteAllText(fileName, jsonString, Encoding.UTF8);
            //    }
            //    catch (Exception e2)
            //    {
            //    }
            //}

            //if (instrumentList == null || instrumentList.Count == 0)
            //{
            //    string inst = string.Empty;
            //    Console.WriteLine(Program.LogTitle + "请输入合约:");
            //    inst = Console.ReadLine();
            //    //program.quoter.ReqSubscribeMarketData(inst);
            //    InstrumentTradeConfig instrumentConfig = new InstrumentTradeConfig();
            //    instrumentConfig.instrument = inst;
            //    instrumentConfig.trade = true;
            //    instrumentConfig.volumn = 1;
            //    program._instrumentList.Clear();
            //    program._instrumentList.Add(instrumentConfig);
            //    program._instrumentMap.Add(inst, instrumentConfig);
            //}
            //else
            //{
            //    program._instrumentList.Clear();
            //    program._instrumentList.AddRange(instrumentList);
            //    foreach(InstrumentTradeConfig instrumentConfig in program._instrumentList)
            //    {
            //        program._instrumentMap.Add(instrumentConfig.instrument, instrumentConfig);
            //    }
                
            //}

            //foreach (string key in program._instrumentMap.Keys)
            //{
            //    program.initUnitDataMap(key);
            //    //string unitFileName = FileUtil.getUnitDataPath(key);
            //    //List<UnitData> unitData = new List<UnitData>();
            //    //if (File.Exists(unitFileName))
            //    //{
            //    //    string text = File.ReadAllText(unitFileName);
            //    //    unitData = JsonConvert.DeserializeObject<List<UnitData>>(text);
            //    //}

            //    //program.unitDataMap.Add(key, unitData);
            //    //if (unitData.Count > _TOTALSIZE)
            //    //{
            //    //    UnitData[] unitDataArray = unitData.ToArray();
            //    //    double allColse = 0;
            //    //    for (int i = 0; i < _TOTALSIZE; i++)
            //    //    {
            //    //        allColse += unitDataArray[unitData.Count - 1 - i].close;
            //    //    }
            //    //    Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 平均:{1}", key, allColse / _TOTALSIZE));
            //    //    Log.log(string.Format(Program.LogTitle + "品种{0} 平均:{1}", key, allColse / _TOTALSIZE), key);
            //    //}


            //}
            //if (program.quoter.IsLogin)
            //    program.subscribeInstruments();
            program.syncData();
            program.isInit = true;
        Inst:
            Console.WriteLine(Program.LogTitle + "q:退出 ");
            Console.WriteLine("s-立刻保存 t-当前值");
            
            c = Console.ReadKey().KeyChar;
            switch (c)
            {   
                case 's':
                    program.saveAll();
                    break;
                case 't':
                    //foreach (string key in program.tradeData.Keys)
                    //{
                    //    InstrumentData currentInstrumentdata;
                    //    if (program.tradeData.TryGetValue(key, out currentInstrumentdata) == false)
                    //    {
                    //        if (currentInstrumentdata == null)
                    //            continue;
                    //        Log.log(string.Format("品种:{0} 值:{1}", key,currentInstrumentdata.curAvg));

                    //    }


                    //}
                    Console.WriteLine("To be continued");
                    break;
                case 'q':
                    if(program.quoter.IsLogin)
                    {
                        program.saveAll();
                        program.quoter.ReqUserLogout();
                    }
                    


                    //InstrumentWatcher.flag = false;
                    Thread.Sleep(2000); //待接口处理后续操作
                    Environment.Exit(0);
                    break;
            }
            
            goto Inst;
            End:
                Console.WriteLine(Program.LogTitle + " end");
        }
        
    }


    class LoginWatcher
    {
        static Program _p;
        static System.Threading.Timer timer;
        static System.Threading.Timer oneMinTimer;

        static void Excute(object obj)
        {
            Thread.CurrentThread.IsBackground = true;
            _p.checkStatus();
        }

        static void ExcuteOneMin(object obj)
        {
            Thread.CurrentThread.IsBackground = true;
            //_p.checkStatusOneMin();
        }



        public static void Init(Program program)
        {
            _p = program;
            timer = new System.Threading.Timer(Excute, null, 60 * 1000, 10 * 60 * 1000);
            //oneMinTimer = new System.Threading.Timer(ExcuteOneMin, null, 60 * 1000, 1 * 60 * 1000);
        }
    }
}
