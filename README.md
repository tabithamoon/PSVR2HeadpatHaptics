# PSVR2HeadpatHaptics
A simple [PSVR2Toolkit](https://github.com/BnuuySolutions/PSVR2Toolkit) client that exposes the headset's rumble functionality over OSC and optionally WebSockets.

Requires .NET 10 Runtime installed and the PSVR2Toolkit CAPI client DLL in the same directory.

Command line options:
```
  -v, --verbose             (Default: true) Log headset vibration values to stdout

  --velocity                (Default: true) Use velocity calculation for OSC parameter

  --websocket-port          (Default: 8181) WebSocket server port

  --websocket-host          (Default: 0.0.0.0) WebSocket server host

  --enable-osc              (Default: true) Enable OSC server

  --enable-ws               (Default: false) Enable WebSocket server

  --osc-parameter           (Default: HeadpatValue) Avatar parameter to read updates from over OSC

  --velocity-sensitivity    (Default: 4) Tunable sensitivity for velocity calculation, higher number = less sensitive

  --help                    Display this help screen.

  --version                 Display version information.
```
