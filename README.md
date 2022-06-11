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



Protocol definitions (serial and UDP):

Driver Command: PING:#
Arduino Response: PONG:# 
Confirms a flatbox device supporting the protocol is on the other side


Driver Command: SET:<int>:#
Arduino Response: SET:<int>:#
Tells flatbox the set brightness to level sepcificed by <int>. Returns the current brightness returns the same value as a sanity check and confirm it is complete

Driver Command: BRIGHT:#
Arduino Response: BRIGHT:<int>:#
Asks flatbox the current brightness level. Returns the current brightness


Protocol definition (serial only)

Driver Command: GETCONFIG:#             
Arduino Response: GETCONFIG:<int>:<int>:<ip_address>:<network_mask>:<dns_server>:<default_gw>:<ssid>:# - sorry, will not return password of your wifi
Asks flatbox to get the current config (tbd for configuration program)  
  
Driver Command SETCONFIG:<int>:<int>:<ip_address>:<network_mask>:<dns_server>:<default_gw>:<ssid>:<password>#                      
Arduino response SETCONFIG:<int>:#    returns 0 or 1 if configuration change was successful or not



Note for values:
      Values in config (in order)
      <int> enable wifi (0 or 1)
      <int> static IP or DHCP (1 for static, 0 for DHCP)
      <strinig>IP address (string format, e.g. 192.168.1.230)
      <string>Subnet mask (string format, e.g. 255.255.255.0)
      <string>DNS server (string format, e.g. 192.168.1.1)
      <string>Default gateway (string format, e.g. 192.168.1.1)      
      <string>WiFi SSID = text string
      <string>WiFi Password = text string

      TODO: decide if there is a wildcard in the GUI (e.g.8 "*" and determine to maintain the password stored in EEPROM since we will not send the password back via serial)

      example:
      SETCONFIG:1:1:192.168.1.230:255.255.255.0:192.168.1.1:192.168.1.1:my_ssid:my_password:# would set wifi on and static IP based on above parameters
      SETCONFIG:1:0:::::my_ssid:my_password:# would set wifi on and DHCP. Data would be clear. A driver side call can still set this if you want to use get config and preserve it between calls.
      SETCONFIG:0:0::::::::# would set it to serial only.
