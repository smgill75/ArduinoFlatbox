# ArduinoFlatbox
This project is an intended to provide a codebased for an Arduino Controlled flatbox for astrophotography.

While there are other examples that utilize the Alnitak command protocol, and I use the techniques to write the
value to the LED pin, I am updating the protocol to make it readable.

Additionally, the code is intended to allow the user to select WiFi or serial based communications, in the current alpha stage, they both work just fine
A configurator program needs to still be created to allow an end user to plug in a USB-B cable to the arduino and configure wifi paramterers. Currently a user will
need to send a command manually of the serial connection to configure the WiFi parameters.

If using serially, I recommend disabling WiFi

ASCOM driver has been tested with NINA (my primary imaging software) and SGP


Good luck and feel free to report bugs or submit patches.


TODO: work out EEPROM write for initialization

Create HEX file for upload of sketch
Consider removing need for Arduino software for sketch upload and move more to a firmware model for non-DIY people

Add handling for cover open / close on both Arduino and ASCOM driver
