//
// Copyright (c) Tabitha Moon and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace headpatosc;
using Buildetech.OscCore;
using System.Diagnostics;
using VRC.OSCQuery;
using CommandLine;
using Fleck;

class HeadpatOSC
{
    private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private static DateTime _lastTimestamp = DateTime.Now;
    private static float _lastProximity = 0;
    
    // Options
    private static float _velSensitivity;
    private static string _oscParameter;
    private static bool _useVelocity;
    private static string _wsHost;
    private static bool _verbose;
    private static bool _useOSC;
    private static bool _useWS;
    private static int _wsPort;
    
    public class Options
    {
        [Option('v', "verbose", Required = false, Default = true, HelpText = "Log headset vibration values to stdout")]
        public bool Verbose { get; set; }
        
        [Option("velocity", Required = false, Default = true, HelpText = "Use velocity calculation for OSC parameter")]
        public bool Velocity { get; set; }
        
        [Option("websocket-port", Required = false, Default = 8181, HelpText = "WebSocket server port")]
        public int WebSocketPort { get; set; }
        
        [Option("websocket-host", Required = false, Default = "0.0.0.0", HelpText = "WebSocket server host")]
        public string WebSocketHost { get; set; }
        
        [Option("enable-osc", Required = false, Default = true, HelpText = "Enable OSC server")]
        public bool EnableOSC { get; set; }
        
        [Option("enable-ws", Required = false, Default = false, HelpText = "Enable WebSocket server")]
        public bool EnableWS { get; set; }
        
        [Option("osc-parameter", Required = false, Default = "HeadpatValue", HelpText = "Avatar parameter to read updates from over OSC")]
        public string OSCParameter { get; set; }
        
        [Option("velocity-sensitivity", Required = false, Default = 4f, HelpText = "Tunable sensitivity for velocity calculation, higher number = less sensitive")]
        public float VelocitySensitivity { get; set; }
    }
    
    private static void Main(string[] args)
    {
        // Parse command line args
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o => {
                _velSensitivity = o.VelocitySensitivity;
                _oscParameter = o.OSCParameter;
                _useVelocity = o.Velocity;
                _wsHost = o.WebSocketHost;
                _wsPort = o.WebSocketPort;
                _verbose = o.Verbose;
                _useOSC = o.EnableOSC;
                _useWS = o.EnableWS;
            });
        
        if (PSVR2ToolkitCAPI.Init() != 0) {
            Console.Error.WriteLine("Failed to connect to PSVR2Toolkit.");
            return;
        }
        
		if (_useWS) WebsocketStart();
		if (_useOSC) OSCStart();
		Console.ReadLine();
    }
	
	private static void WebsocketStart() {
		var server = new WebSocketServer($"ws://{_wsHost}:{_wsPort}");
		server.Start(socket => {
			socket.OnOpen = () => Console.WriteLine("WebSocket Connected");
			socket.OnClose = () => Console.WriteLine("WebSocket Disconnected");
			socket.OnMessage = message => {
				SetRumble(float.Parse(message));
			};
		});
	}
	
	private static void OSCStart() {
		var tcpPort = Extensions.GetAvailableTcpPort();
		var udpPort = Extensions.GetAvailableUdpPort();
		var receiver = OscServer.GetOrCreate(udpPort);
		
		var oscQuery = new OSCQueryServiceBuilder()
			.WithTcpPort(tcpPort)
			.WithUdpPort(udpPort)
			.WithServiceName("headpatosc")
			.AdvertiseOSC()
			.AdvertiseOSCQuery()
			.WithDefaults()
			.Build();
			
		Console.WriteLine($"Started OSCQueryService at TCP {tcpPort}, UDP {udpPort}");
		
		oscQuery.AddEndpoint<float>($"/avatar/parameters/{_oscParameter}", Attributes.AccessValues.WriteOnly);
        
        if (_useVelocity) {
            receiver.TryAddMethod($"/avatar/parameters/{_oscParameter}",(message) => {
                float currentProximity = message.ReadFloatElement(0);            
                DateTime currentTimestamp = DateTime.Now;
                
                if (currentProximity == 0f) {
                    SetRumble(0f);
                    return;
                }
                
                var deltaTime = (currentTimestamp - _lastTimestamp).TotalSeconds;
                float velocity = (currentProximity - _lastProximity) / (float)deltaTime;
                SetRumble(Math.Abs(velocity) / _velSensitivity);
                
                _lastProximity = currentProximity;
                _lastTimestamp = currentTimestamp;
            });
        }
        else {
            receiver.TryAddMethod($"/avatar/parameters/{_oscParameter}",(message) => {
                SetRumble(message.ReadFloatElement(0));
            });
        }
	}
	
	private static void SetRumble(float rumbleAmount) {
		float value = Math.Clamp(rumbleAmount, 0f, 1f);
        
		if (value == 0) {
			PSVR2ToolkitCAPI.SetHmdRumble(0);
			if (_verbose) Console.WriteLine("Rumble stopped");
			return;
		}

		int vibehz = (int)(10 + value * (25 - 10));
		PSVR2ToolkitCAPI.SetHmdRumble((byte)vibehz);
        
		if (_verbose) Console.WriteLine($"Set rumble frequency to {vibehz}Hz");		
	}
}