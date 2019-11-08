using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using MonoDevelop.Debugger.VsCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Diag = System.Diagnostics;
using Mono.Debugger.Soft;
using System.Net;
using Mono.Debugging.Soft;
using Mono.Debugging;
using Mono.Debugging.Client;

namespace Mono.Debugger.VsProtocol.Soft
{
	class Program
	{
		static System.IO.StreamWriter file;
		static DebugProtocolClient protocolClient;
		static string programName;
		static VirtualMachine vm;
		static SoftDebuggerSession softDebuggerSession;
		static Diag.ProcessStartInfo CreateStartInfo (string app, string method = null, string runtimeParameters = null)
		{
			var pi = new Diag.ProcessStartInfo ();
			pi.RedirectStandardOutput = true;
			pi.RedirectStandardError = true;
			pi.FileName = "mono";

			// expect test .exe's to be next to this assembly
			pi.Arguments = string.Join (" ", new string[] { app });
			return pi;
		}

		static protected void OnDebugAdaptorRequestReceived (object sender, RequestReceivedEventArgs e)
		{
			if (e.Command == "initialize") {
				e.Response = new InitializeResponse ();
			} else if (e.Command == "launch") {
				//TODO - find the right way to laucnh vm, maybe using softDebuggerSession
				System.Threading.Thread.Sleep (1000);
				var ep = new IPEndPoint (IPAddress.Loopback, 5556);
				//// Wait for the app to reach the Sleep () in attach ().
				vm = VirtualMachineManager.Connect (ep);
				/*try {
					var ops = new DebuggerSessionOptions {
						ProjectAssembliesOnly = true,
						EvaluationOptions = EvaluationOptions.DefaultOptions
					};
					//ops.EvaluationOptions.AllowTargetInvoke = AllowTargetInvokes;
					ops.EvaluationOptions.EvaluationTimeout = 100000;
					var dsi = new SoftDebuggerStartInfo (new SoftDebuggerListenArgs (programName, System.Net.IPAddress.Parse ("127.0.0.1"), 5555));
					dsi.StartArgs.MaxConnectionAttempts = 0;
					softDebuggerSession.Run (dsi, ops);
					softDebuggerSession.StartConnection (dsi);
				}
				catch (Exception ex) {
					file.WriteLine (ex.ToString());
					file.Flush ();
				}*/
				e.Response = new LaunchResponse ();
			} else if (e.Command == "configurationDone") {
				e.Response = new ConfigurationDoneResponse ();
				protocolClient.SendEvent (new ProcessEvent (programName, null, true, ProcessEvent.StartMethodValue.Launch));
			} else if (e.Command == "setBreakpoints") {
				var args = (SetBreakpointsArguments)e.Args;
				bool insideLoadedRange;
				bool generic;
				foreach (var bp in args.Breakpoints) {
					foreach (var location in softDebuggerSession.FindLocationsByFile (args.Source.Name, bp.Line, 0, out generic, out insideLoadedRange)) {
						vm.SetBreakpoint (location.Method, location.ILOffset);
					}
				}
				e.Response = new SetBreakpointsResponse ();
			} else if (e.Command == "setFunctionBreakpoints") {
				e.Response = new SetFunctionBreakpointsResponse ();
				protocolClient.SendEvent (new StoppedEvent (StoppedEvent.ReasonValue.Breakpoint));
				vm.Resume();
			} else if (e.Command == "stackTrace") {
				e.Response = new StackTraceResponse ();
			}
		}

		static protected void OnDebugAdaptorRequestCompleted (object sender, RequestCompletedEventArgs e)
		{
		}

		static void Main (string[] args)
		{
			softDebuggerSession = new SoftDebuggerSession ();
			protocolClient = new DebugProtocolClient (Console.OpenStandardInput (), Console.OpenStandardOutput ());
			protocolClient.RequestReceived += OnDebugAdaptorRequestReceived;
			protocolClient.RequestCompleted += OnDebugAdaptorRequestCompleted;
			protocolClient.Run ();
			protocolClient.WaitForReader ();
		}

	}
}
