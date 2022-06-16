# ArduinoFlatbox

The code contained within this repository is distributed under the terms of GPL version 2.0. Please refer to License.txt for more details

This project is an intended to provide a codebase for an Arduino Controlled flatbox for astrophotography.

While there are other examples that utilize the Alnitak command protocol, and I use the techniques to write the
value to the LED pin, I am updating the protocol to make it readable.

Additionally, the code is intended to allow the user to select WiFi or serial based communications, in the current alpha stage, they both work just fine
A configurator program needs to still be created to allow an end user to plug in a USB-B cable to the arduino and configure wifi paramterers. Currently a user will
need to send a command manually of the serial connection to configure the WiFi parameters.

If using serially, I recommend disabling WiFi

ASCOM driver has been tested with NINA (my primary imaging software) and SGP


Good luck and feel free to report bugs or submit patches.


Building the package:
You need the ASCOM platform version 6.5.2 and development libraries to build the driver. I have included NUGET to allow automated download of the platform for proper compilation.

The build process will attempt to register the DLL on the machine upon build. You will get a failure unless you are running Visual Studio as an administrator.

You can build your own installation package by running InnoSetup found here: https://jrsoftware.org/isinfo.php

Inside the ASCOM directory in an ISS file that you can right click and select "compile." This will place a setup executable in the same directory. You can then take this and install
on any other computer. Note: the InnoSetup .iss file expects you to build a release version. The setup will not build a setup package for a debug version.




