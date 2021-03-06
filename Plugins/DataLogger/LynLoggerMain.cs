﻿using Grabacr07.KanColleViewer.Composition;
using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models.Raw;
using LynLogger.DataStore.Serialization;
using LynLogger.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace LynLogger
{
    [Export(typeof(IToolPlugin))]
    [ExportMetadata("Title", "LynLogger")]
    [ExportMetadata("Description", "Test")]
    [ExportMetadata("Version", "1.0")]
    [ExportMetadata("Author", "@Linnaea")]
    public class LynLoggerMain : IToolPlugin, IDisposable
    {
        private const string Major = "3.8.2.1";
        private const string Mod = "2.3";
        private const string Revision = "3";
        private const string Train = "T";

        public static LynLoggerMain Instance { get; private set; }
        public static string Version
        {
            get
            {
                return Major + "-" + Mod + "(" + Train + Revision + ")"
#if DEBUG
                     + "d"
#endif
                     ;
            }
        }

        private static Action<LynLoggerMain> _onInstanceCreate;
        public static event Action<LynLoggerMain> OnInstanceCreate
        {
            add { _onInstanceCreate += value.MakeWeak(x => _onInstanceCreate -= x); if (Instance != null) value(Instance); }
            remove { }
        }

        internal Observers.ApiBattleObserver BattleObserver { get; private set; }
        internal Observers.ApiBattleResultObserver BattleResultObserver { get; private set; }
        internal Observers.ApiMapStartNextObserver MapStartNextObserver { get; private set; }
        internal Observers.ApiPracticeEnemyInfoObserver PracticeEnemyInfoObserver { get; private set; }
        internal Observers.ApiCreateItemObserver CreateItemObserver { get; private set; }
        internal Observers.ApiCreateShipObserver CreateShipObserver { get; private set; }
        internal Observers.ApiPortObserver PortObserver { get; private set; }

        private LinkedList<IDisposable> _disposables = new LinkedList<IDisposable>();
        private ToolsView _view;

        static LynLoggerMain()
        {
            Logger.ShipItemCreateLogger.Init();
            Logger.BasicInfoLogger.Init();
            Logger.ShipDataLogger.Init();
            Logger.SortieLogger.Init();
            Logger.DrillLogger.Init();

#if DEBUG
            AppDomain.CurrentDomain.FirstChanceException += (s, e) => {
                var stack = new System.Diagnostics.StackTrace(e.Exception);
                if (stack.GetFrames().Any(x => x.GetMethod().DeclaringType.Assembly.GetName().Name == typeof(LynLoggerMain).Assembly.GetName().Name)) {
                    System.IO.File.AppendAllText("lynlogger.log", string.Format(@"
================================================================================
First chance, Time={0}, Sender={1}
{2}
", DateTime.UtcNow, s, e.Exception));
                }
            };
#endif

            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                var stack = new System.Diagnostics.StackTrace((Exception)e.ExceptionObject);
                if (stack.GetFrames().Any(x => x.GetMethod().DeclaringType.Assembly.GetName().Name == typeof(LynLoggerMain).Assembly.GetName().Name)) {
                    System.IO.File.AppendAllText("lynlogger.log", string.Format(@"
================================================================================
Second chance {2}, Time={0}, Sender={1}
{3}
", DateTime.UtcNow, s, e.IsTerminating ? "Terminating" : "Continuing", e.ExceptionObject));
                }
            };
        }

        public LynLoggerMain()
        {
            PortObserver = new Observers.ApiPortObserver();
            _disposables.AddLast(KanColleClient.Current.Proxy.api_port.TryParse<kcsapi_port>().Subscribe(PortObserver));

            _disposables.AddLast(KanColleClient.Current.Proxy.api_get_member_ship2.TryParse<kcsapi_ship2[]>().Subscribe(new Observers.ApiShip2Observer()));

            BattleObserver = new Observers.ApiBattleObserver();
            _disposables.AddLast(KanColleClient.Current.Proxy.api_req_sortie_battle.Subscribe(BattleObserver)); //昼战
            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_sortie/airbattle").Subscribe(BattleObserver)); //航空战
            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_battle_midnight/battle").Subscribe(BattleObserver)); //普通夜战
            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_battle_midnight/sp_midnight").Subscribe(BattleObserver)); //夜战点

            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_practice/battle").Subscribe(BattleObserver)); //演习
            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_practice/midnight_battle").Subscribe(BattleObserver)); //演习夜战

            MapStartNextObserver = new Observers.ApiMapStartNextObserver();
            _disposables.AddLast(KanColleClient.Current.Proxy.api_req_map_start.Subscribe(MapStartNextObserver));
            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_map/next").Subscribe(MapStartNextObserver));

            BattleResultObserver = new Observers.ApiBattleResultObserver();
            _disposables.AddLast(KanColleClient.Current.Proxy.api_req_sortie_battleresult.TryParse<kcsapi_battleresult>().Subscribe(BattleResultObserver));
            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_practice/battle_result").TryParse<kcsapi_battleresult>().Subscribe(BattleResultObserver));

            PracticeEnemyInfoObserver = new Observers.ApiPracticeEnemyInfoObserver();
            _disposables.AddLast(KanColleClient.Current.Proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_member/get_practice_enemyinfo").Subscribe(PracticeEnemyInfoObserver));

            CreateShipObserver = new Observers.ApiCreateShipObserver();
            _disposables.AddLast(KanColleClient.Current.Proxy.api_get_member_kdock.TryParse<kcsapi_kdock[]>().Subscribe(CreateShipObserver));
            _disposables.AddLast(KanColleClient.Current.Proxy.api_req_kousyou_createship.TryParse<kcsapi_createship>().Subscribe(CreateShipObserver));

            CreateItemObserver = new Observers.ApiCreateItemObserver();
            _disposables.AddLast(KanColleClient.Current.Proxy.api_req_kousyou_createitem.TryParse<kcsapi_createitem>().Subscribe(CreateItemObserver));

            Instance = this;
            if(_onInstanceCreate != null) _onInstanceCreate(this);
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if(new AssemblyName(args.Name).Name == typeof(LynLoggerMain).Assembly.GetName().Name) {
                return typeof(LynLoggerMain).Assembly;
            }
            return null;
        }

        public string ToolName => "LynLogger";

        public object GetToolView()
        {
            return _view ?? (_view = new ToolsView());
        }

        public object GetSettingsView()
        {
            return null;
        }

        ~LynLoggerMain()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(!disposing) return;
            foreach(var d in _disposables) {
                d.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }

    public abstract class CustomComparerBase<T> : MarshalByRefObject, IComparer<T>
    {
        public abstract int Compare(T x, T y);
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ComparerClassAttribute : Attribute
    {
        readonly string _className;

        public ComparerClassAttribute(string className)
        {
            _className = className;
        }

        public string ClassName => _className;
    }

    public enum TriState
    {
        [Description("？")]
        DK,

        [Description("✖")]
        No,

        [Description("✔")]
        Yes,
    }

    public class FuzzyDouble : AbstractDSSerializable<FuzzyDouble>
    {
        [Serialize(0)] public double UpperBound { get; set; }
        [Serialize(1)] public double LowerBound { get; set; }

        public FuzzyDouble() { }
        internal FuzzyDouble(DataStore.Premitives.StoragePremitive x, LinkedList<object> path) : base(x, path) { }

        public static FuzzyDouble operator +(FuzzyDouble a, FuzzyDouble b)
        {
            return new FuzzyDouble() {
                UpperBound = a.UpperBound + b.UpperBound,
                LowerBound = a.LowerBound + b.LowerBound
            };
        }

        public static FuzzyDouble operator +(FuzzyDouble a, double b)
        {
            return new FuzzyDouble() {
                UpperBound = a.UpperBound + b,
                LowerBound = a.LowerBound + b
            };
        }

        public static TriState operator >(FuzzyDouble a, FuzzyDouble b)
        {
            if(a.LowerBound > b.UpperBound) return TriState.Yes;
            if(a.UpperBound <= b.LowerBound) return TriState.No;
            return TriState.DK;
        }

        public static TriState operator <(FuzzyDouble a, FuzzyDouble b)
        {
            if(a.UpperBound < b.LowerBound) return TriState.Yes;
            if(a.LowerBound >= b.UpperBound) return TriState.No;
            return TriState.DK;
        }

        public static TriState operator >=(FuzzyDouble a, FuzzyDouble b)
        {
            if(a.LowerBound >= b.UpperBound) return TriState.Yes;
            if(a.UpperBound < b.LowerBound) return TriState.No;
            return TriState.DK;
        }

        public static TriState operator <=(FuzzyDouble a, FuzzyDouble b)
        {
            if(a.UpperBound <= b.LowerBound) return TriState.Yes;
            if(a.LowerBound > b.UpperBound) return TriState.No;
            return TriState.DK;
        }

        public static FuzzyDouble UpperRange(FuzzyDouble a, FuzzyDouble b)
        {
            return new FuzzyDouble() {
                UpperBound = Math.Max(a.UpperBound, b.UpperBound),
                LowerBound = Math.Max(a.LowerBound, b.LowerBound)
            };
        }

        public override string ToString()
        {
            return LowerBound == UpperBound ? LowerBound.ToString() :  string.Format("[{0}, {1}]", LowerBound, UpperBound);
        }
    }
}
