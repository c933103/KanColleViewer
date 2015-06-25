using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fiddler;
using Grabacr07.KanColleWrapper.Win32;
using Livet;

namespace Grabacr07.KanColleWrapper
{
	public partial class KanColleProxy
	{
		private readonly IConnectableObservable<Session> connectableSessionSource;
		private readonly IConnectableObservable<Session> apiSource;
		private readonly LivetCompositeDisposable compositeDisposable;
        private readonly Dictionary<string, Action<Session>> localRequestHandlers = new Dictionary<string, Action<Session>>();

		public IObservable<Session> SessionSource
		{
			get { return this.connectableSessionSource.AsObservable(); }
		}

		public IObservable<Session> ApiSessionSource
		{
			get { return this.apiSource.AsObservable(); }
		}

		public IProxySettings UpstreamProxySettings { get; set; }


		public KanColleProxy()
		{
			this.compositeDisposable = new LivetCompositeDisposable();

			this.connectableSessionSource = Observable
				.FromEvent<SessionStateHandler, Session>(
					action => new SessionStateHandler(action),
					h => FiddlerApplication.AfterSessionComplete += h,
					h => FiddlerApplication.AfterSessionComplete -= h)
				.Publish();

			this.apiSource = this.connectableSessionSource
				.Where(s => s.PathAndQuery.StartsWith("/kcsapi"))
				.Where(s => s.oResponse.MIMEType.Equals("text/plain"))
				#region .Do(debug)
#if DEBUG
.Do(session =>
				{
					Debug.WriteLine("==================================================");
					Debug.WriteLine("Fiddler session: ");
					Debug.WriteLine(session);
					Debug.WriteLine("");
				})
#endif
			#endregion
                .Synchronize()
				.Publish();
		}


		public void Startup(int proxy = 0)
		{
            if(proxy < 1024) proxy = new Random().Next(1024, 65535);

            var flags = FiddlerCoreStartupFlags.Default;
            flags &= ~FiddlerCoreStartupFlags.DecryptSSL;
            flags &= ~FiddlerCoreStartupFlags.AllowRemoteClients;
            flags &= ~FiddlerCoreStartupFlags.RegisterAsSystemProxy;
            flags |= FiddlerCoreStartupFlags.ChainToUpstreamGateway;

            FiddlerApplication.Startup(proxy, flags);
            FiddlerApplication.BeforeRequest += this.Fiddler_BeforeRequest;

            SetIESettings("localhost:" + proxy);

			this.compositeDisposable.Add(this.connectableSessionSource.Connect());
			this.compositeDisposable.Add(this.apiSource.Connect());
		}

        public void Shutdown()
		{
			this.compositeDisposable.Dispose();

			FiddlerApplication.BeforeRequest -= this.Fiddler_BeforeRequest;
			FiddlerApplication.Shutdown();
		}


		private static void SetIESettings(string proxyUri)
		{
			// ReSharper disable InconsistentNaming
			const int INTERNET_OPTION_PROXY = 38;
			const int INTERNET_OPEN_TYPE_PROXY = 3;
			// ReSharper restore InconsistentNaming

			INTERNET_PROXY_INFO proxyInfo;
			proxyInfo.dwAccessType = INTERNET_OPEN_TYPE_PROXY;
			proxyInfo.proxy = Marshal.StringToHGlobalAnsi(proxyUri);
			proxyInfo.proxyBypass = Marshal.StringToHGlobalAnsi("local");

			var proxyInfoSize = Marshal.SizeOf(proxyInfo);
			var proxyInfoPtr = Marshal.AllocCoTaskMem(proxyInfoSize);
			Marshal.StructureToPtr(proxyInfo, proxyInfoPtr, true);

			NativeMethods.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY, proxyInfoPtr, proxyInfoSize);

            Marshal.FreeCoTaskMem(proxyInfoPtr);
            Marshal.FreeHGlobal(proxyInfo.proxy);
            Marshal.FreeHGlobal(proxyInfo.proxyBypass);
        }

		/// <summary>
		/// Fiddler からのリクエスト発行時にプロキシを挟む設定を行います。
		/// </summary>
		/// <param name="requestingSession">通信を行おうとしているセッション。</param>
		private void Fiddler_BeforeRequest(Session requestingSession)
		{
            if(requestingSession.hostname == "kancolleviewer.local") {
                requestingSession.utilCreateResponseAndBypassServer();
                var path = requestingSession.PathAndQuery;
                var queryIndex = path.IndexOf('?');
                if(queryIndex >= 0) {
                    path = path.Substring(0, queryIndex);
                }

                Action<Session> handler;
                if (localRequestHandlers.TryGetValue(path, out handler)) {
                    requestingSession.oResponse.headers.HTTPResponseCode = 200;
                    requestingSession.oResponse.headers.HTTPResponseStatus = "200 OK";
                    handler?.Invoke(requestingSession);
                } else {
                    requestingSession.oResponse.headers.HTTPResponseCode = 410;
                    requestingSession.oResponse.headers.HTTPResponseStatus = "410 Gone";
                }
                return;
            }

			var settings = this.UpstreamProxySettings;
            if (settings == null) return;

            var compiled = settings.CompiledRules;
            if (compiled == null) settings.CompiledRules = compiled = ProxyRule.CompileRule(settings.Rules);
            var result = ProxyRule.ExecuteRules(compiled, requestingSession.RequestMethod, new Uri(requestingSession.fullUrl));

            if(result.Action == ProxyRule.MatchAction.Block) {
                requestingSession.utilCreateResponseAndBypassServer();
                requestingSession.oResponse.headers.HTTPResponseCode = 403;
                requestingSession.oResponse.headers.HTTPResponseStatus = "403 Forbidden";
                return;
            }

            if(result.Action == ProxyRule.MatchAction.Proxy && result.Proxy != null) {
                requestingSession["X-OverrideGateway"] = result.Proxy;
                if(result.ProxyAuth != null && !requestingSession.RequestHeaders.Exists("Proxy-Authorization")) {
                    requestingSession["X-OverrideGateway"] = result.Proxy;
                    requestingSession.RequestHeaders.Add("Proxy-Authorization", result.ProxyAuth);
                }
            }

            requestingSession.bBufferResponse = false;
        }

        public void SetLocalRerquestHandler(string path, Action<Session> proc)
        {
            if (proc != null) {
                localRequestHandlers[path] = proc;
            } else {
                localRequestHandlers.Remove(path);
            }
        }
	}
}
