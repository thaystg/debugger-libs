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
using Mono.Debugger.Client;

namespace Mono.Debugger.VsProtocol.Soft
{
	class Program
	{
		static System.IO.StreamWriter file;
		static DebugProtocolClient protocolClient;
		static string programName;
		static VirtualMachine vm;
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
				Mono.Debugger.Client.Debugger.Connect (IPAddress.Parse ("127.0.0.1"), 5555);
				e.Response = new LaunchResponse ();
			} else if (e.Command == "configurationDone") {
				e.Response = new ConfigurationDoneResponse ();
				protocolClient.SendEvent (new ProcessEvent (programName, null, true, ProcessEvent.StartMethodValue.Launch));
			} else if (e.Command == "setBreakpoints") {
				var args = (SetBreakpointsArguments)e.Args;
				bool insideLoadedRange;
				bool generic;
				foreach (var bp in args.Breakpoints) {

					var id = Mono.Debugger.Client.Debugger.GetBreakpointId ();

					Mono.Debugger.Client.Debugger.Breakpoints.Add (id, Mono.Debugger.Client.Debugger.BreakEvents.Add (args.Source.Path, bp.Line));

				}
				e.Response = new SetBreakpointsResponse ();
			} else if (e.Command == "setFunctionBreakpoints") {
				e.Response = new SetFunctionBreakpointsResponse ();
			} else if (e.Command == "stackTrace") {
				List<Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame> stackFrame = new List<Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame> ();
				var bt = Mono.Debugger.Client.Debugger.ActiveBacktrace;
				for (int i = 0; i < bt.FrameCount; i++) {
					var frame = bt.GetFrame (0);
					stackFrame.Add (new Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame (frame.Index + 1000, frame.SourceLocation.MethodName, frame.SourceLocation.Line, frame.SourceLocation.Column, new Source ("Program.cs", frame.SourceLocation.FileName)));
				}
				//<- (R) {"seq":22,"type":"response","request_seq":8,"success":true,"command":"stackTrace","message":"","body":{"stackFrames":[{"id":1000,"name":"testeDebugApiNetCore.Program.Main(string[] args) Line 9","source":{"name":"Program.cs","path":"/Users/thaysgrazia/Projects/testeDebugApiNetCore/testeDebugApiNetCore/Program.cs","sourceReference":0,"sources":[],"checksums":[{"algorithm":"SHA256","checksum":"f1fb9eedc1f28b9317099649cc52e32f02e54d8287861aa18b301df7cb55646d"}]},"line":9,"column":13,"endLine":9,"endColumn":49,"instructionPointerReference":"0x000000011E3703B3","moduleId":"{7eaa0f14-59b1-4652-abb5-b8e0f49cff2b}"}],"totalFrames":2}}
				e.Response = new StackTraceResponse (stackFrame, stackFrame.Count);
			} else if (e.Command == "threads") {
				var threadsList = new List<Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.Thread> ();
				var p = Mono.Debugger.Client.Debugger.ActiveProcess;
				var threads = p.GetThreads ();
				for (var i = 0; i < threads.Length; i++) {
					var t = threads[i];
					threadsList.Add (new Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.Thread ((int)t.Id, t.Name));
				}

				e.Response = new ThreadsResponse (threadsList);
			} else if (e.Command == "scopes") {
				e.Response = new ScopesResponse();
			} else if (e.Command == "continue") {
				Mono.Debugger.Client.Debugger.Continue ();
			}
				
		}

		static protected void OnDebugAdaptorRequestCompleted (object sender, RequestCompletedEventArgs e)
		{
			
		}

		static protected void OnLogMessage (object sender, LogEventArgs e)
		{
			file.WriteLine (e.Message);
			file.Flush ();
		}
		

		static void Main (string[] args)
		{
			/*Mono.Debugger.Client.Debugger.Connect (IPAddress.Parse ("127.0.0.1"), 5555);
			var id = Mono.Debugger.Client.Debugger.GetBreakpointId ();

			Mono.Debugger.Client.Debugger.Breakpoints.Add (id, Mono.Debugger.Client.Debugger.BreakEvents.Add ("/Users/thaysgrazia/Projects/testDebugMono/testDebugMono/Program.cs", 9));

			while (true)
				System.Threading.Thread.Sleep (1000);*/
			file = new System.IO.StreamWriter (@"/Users/thaysgrazia/saida2.txt");
			protocolClient = new DebugProtocolClient (Console.OpenStandardInput (), Console.OpenStandardOutput ());
			protocolClient.RequestReceived += OnDebugAdaptorRequestReceived;
			protocolClient.RequestCompleted += OnDebugAdaptorRequestCompleted;
			Mono.Debugger.Client.Debugger.protocol = protocolClient;
			protocolClient.Run ();
			protocolClient.LogMessage += OnLogMessage ;
			protocolClient.WaitForReader ();
		}

	}
}
