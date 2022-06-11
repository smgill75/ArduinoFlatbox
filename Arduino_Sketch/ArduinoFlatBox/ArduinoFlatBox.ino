/*

  Protocol definitions:
  Command word is followed by a ":" to seprate fields and terminated with a ":#" Just makes reading it easier it the simple protocol parser

  
  Serial *and* WiFi:
  Command received                        Command returned to drivrer
  PING:# looks for device                 PONG:#
  SET:<int>:# set s brightness value      SET:<int>:# returns value requested for sanity check
              and writes to analog pin
  BRIGHT:#                                BRIGHT:<int>:# returns the current brightness level 


  Serial Only
  GETCONFIG:# asks for config             GETCONFIG:<int>:<int>:<ip_address>:<network_mask>:<dns_server>:<default_gw>:<ssid>:# - sorry, will not return password of your wifi
  
  
  SETCONFIG:values:#                      SETCONFIG:<int>    returns 0 or 1 if configuration change was successful or not
      Values in config (in order)
      enable wifi (0 or 1)
      static IP or DHCP (1 for static, 0 for DHCP)
      IP address (string format, e.g. 192.168.1.230)
      Subnet mask (string format, e.g. 255.255.255.0)
      DNS server (string format, e.g. 192.168.1.1)
      Default gateway (string format, e.g. 192.168.1.1)      
      WiFi SSID = text string
      WiFi Password = text string

      TODO: decide if there is a wildcard in the GUI (e.g.8 "*" and determine to maintain the password stored in EEPROM since we will not send the password back via serial)

      example:
      SETCONFIG:1:1:192.168.1.230:255.255.255.0:192.168.1.1:192.168.1.1:my_ssid:my_password:# would set wifi on and static IP based on above parameters
      SETCONFIG:1:0:::::my_ssid:my_password:# would set wifi on and DHCP. Data would be clear. A driver side call can still set this if you want to use get config and preserve it between calls.
      SETCONFIG:0:0::::::::# would set it to serial only.


      TODO: set timesouts on WiFi connect so that serial is usable and makes the below statement true. Also, define a period of time in which entire setup to WiFi just "gives up"
      Note: serial will always work, even when connected to WiFi, but if Wifi is enabled but not present, performance may suffer / lag due to reconnect attempts and waiting for timeouts.

*/

#include <WiFiNINA.h>
#include <WiFiUdp.h>
#include <EEPROMex.h>


// all of this struct maintains config for persitence between power cycles and is loaded from EEPROMex which allows block loading of the struct.
// EEPROMex is good too, because it checks the state of EERPOM before committing a write. If the user sends the config and it hasn't change, it won't add a cyclle to the EERPOm
// This particular use case has a very low likelihood to overwhelm the EEPROM, but it is worth it

// Note: as of 3/16/2022 there is a known issue with EERPOMex and Boards such as the NANO wifi rev2. It's not the EEPROMex per se as much as the isready calling EEPROM.h for this board.
// if compiling yourself, move the EEPROMex.cpp into your library. Note: here is the diff starting at line 98:

//Orig:
//  /** 
// * Check if EEPROM memory is ready to be accessed 
// */ 
//bool EEPROMClassEx::isReady() { 
//  return eeprom_is_ready(); 
//} 

//New:
//  /**
// * Check if EEPROM memory is ready to be accessed
// */
//bool EEPROMClassEx::isReady() {
//  #if defined(ARDUINO_ARCH_MEGAAVR) //work around a bug in <avr/eeprom.h>
//  return bit_is_clear(NVMCTRL.STATUS,NVMCTRL_EEBUSY_bp);
//  #else
//  return eeprom_is_ready();
//  #endif
//}



//
struct configuration {
  bool wifi_enable = false;
  bool static_ip = false;
  int wifi_ip[4];
  int subnet[4];
  int dns[4];
  int gateway[4];
  char ssid[32];
  char pass[64]; 
}; configuration my_config;

volatile int ledPin = 10;      // the pin that the LED is attached to, needs to be a PWM pin.
int brightness = 0;
char packetBuffer[256]; //buffer to hold incoming packet
WiFiUDP Udp;
int status = WL_IDLE_STATUS; 
int brightlevel = 0;
unsigned int localPort = 2390; 
 
void setup() {
  int retries = 5;
  Serial.begin(9600,SERIAL_8N1);
  //Serial.println("Starting up...");

  // set defaults for config prior to reading from EEPROM
  //load_defaults();
  //write_config();
  //preload the configuration from EEPROM
  // TODO: determine that EEPROM was never written and initialize default values
  read_config();
  
  // initialize the ledPin as an output:
  pinMode(ledPin, OUTPUT);
  // set brightness to 0
  analogWrite(ledPin, 0);

  // if the config says wifi is enabled, start it up
  if (my_config.wifi_enable) {
    
    if (my_config.static_ip) {
      // build IP structs from int arrays store in config struct
      IPAddress ip = IPAddress(my_config.wifi_ip[0], my_config.wifi_ip[1], my_config.wifi_ip[2], my_config.wifi_ip[3]);
      IPAddress dns = IPAddress(my_config.dns[0], my_config.dns[1], my_config.dns[2], my_config.dns[3]);
      IPAddress gateway = IPAddress(my_config.gateway[0], my_config.gateway[1], my_config.gateway[2], my_config.gateway[3]);
      IPAddress subnet = IPAddress(my_config.subnet[0], my_config.subnet[1], my_config.subnet[2], my_config.subnet[3]);
      

      WiFi.config(ip, dns, gateway, subnet);
    }
    while (status != WL_CONNECTED && retries >0) {
      retries--;
      status = WiFi.begin(my_config.ssid, my_config.pass);
      delay(1000);
    }
    if (retries > 0) {
      Udp.begin(localPort);    
    }
    else {
      //my_config.wifi_enable
      Serial.println("Timeout connecting WiFi. Giving up.");
    }
  }
}

void loop() {
  handleSerial();
  if(my_config.wifi_enable) {
    handleUDP();
  }
}

void handleUDP () {
  int packetSize = Udp.parsePacket();
  String commandresponse = "PONG:#";
  String command;
  String setting;
  String buffertostring;
  String terminator = ":#";
  String ackcommand;
  
  if (packetSize) {
    IPAddress remoteIp = Udp.remoteIP();

    // read the packet into packetBufffer
    command = Udp.readStringUntil(':'); 
    setting = Udp.readStringUntil('#');

    if (command == "PING"){
      commandresponse = "PONG:#";
    }

    if (command == "BRIGHT"){
      ackcommand = "BRIGHT:";
      commandresponse = ackcommand + brightlevel + terminator;
    }

    if (command == "SET") {
      brightlevel = setting.toInt();
      analogWrite(ledPin, brightlevel);
      ackcommand = "SET:";
      commandresponse = ackcommand + brightlevel + terminator;
    }

    char ReplyBuffer[commandresponse.length()+1];
    commandresponse.toCharArray(ReplyBuffer, commandresponse.length()+1);
    Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
    Udp.write(ReplyBuffer);
    Udp.endPacket();
  }
}


void handleSerial() {
  String command;
  String setting;
  int i;

  while ( Serial.available() > 0 ) {
    command = Serial.readStringUntil(':');
    if (command == "PING"){
      setting = Serial.readStringUntil('#');
      Serial.println("PONG:#");
    }
    
    if (command == "BRIGHT"){
      setting = Serial.readStringUntil('#');
      Serial.print("BRIGHT:"); Serial.print(brightlevel); Serial.println(":#");
    }

    if (command == "SET") {
      setting = Serial.readStringUntil('#');
      brightlevel = setting.toInt();
      analogWrite(ledPin, brightlevel);
      Serial.print("SET:"); Serial.print(brightlevel); Serial.println(":#");
    }

    if (command == "GETCONFIG") {
      setting = Serial.readStringUntil('#');
      Serial.print("GETCONFIG");
      Serial.print(':');
      Serial.print(String(my_config.wifi_enable));
      Serial.print(':');
      Serial.print(my_config.static_ip); 
      Serial.print(':'); 
      // print static IP config
      for (i=0; i<4; i++){
        Serial.print(my_config.wifi_ip[i]);
        if (i !=3){
          Serial.print('.');
        }
      }
      Serial.print(':');

       //subnet
      for (i=0; i<4; i++){
        Serial.print(my_config.subnet[i]);
        if (i !=3){
          Serial.print('.');
        }
      }
      Serial.print(':');

      // dns
      for (i=0; i<4; i++){
        Serial.print(my_config.dns[i]);
        if (i !=3){
          Serial.print('.');
        }
      }
      Serial.print(':');

      //gateway
      for (i=0; i<4; i++){
        Serial.print(my_config.gateway[i]);
        if (i !=3){
          Serial.print('.');
        }
      }
      Serial.print(':');
      Serial.println(String(my_config.ssid) + ":#");
    }
    if (command == "SETCONFIG") {
      /* we expect these parameters
       *  enable wifi
       *  static ip or DHCP
       *  IP address
       *  Subnet mask
       *  dns server
       *  default gateway
       * 
      */

      // TODO: evaluate inputs and discard if malformed. We currently expect perfect input

      // enable wifi?
      setting = Serial.readStringUntil(':');
      if (setting == "1") {
        my_config.wifi_enable = true;      
      }
      else {
        my_config.wifi_enable = false;              
      }

      // static or DHCP?
      setting = Serial.readStringUntil(':');
      if (setting == "1") {
        my_config.static_ip = true;      
      }
      else {
        my_config.static_ip = false;              
      }

      // ip address
      for (i=0; i<4; i++) {
        if (i < 3) {
          setting = Serial.readStringUntil('.');
        }
        else {
          setting = Serial.readStringUntil(':');          
        }
        my_config.wifi_ip[i] = setting.toInt();
      }      
      // subnet_mask 
      for (i=0; i<4; i++) {
        if (i < 3) {
          setting = Serial.readStringUntil('.');
        }
        else {
          setting = Serial.readStringUntil(':');          
        }
        my_config.subnet[i] = setting.toInt();
      }      
      
      // dns
      for (i=0; i<4; i++) {
        if (i < 3) {
          setting = Serial.readStringUntil('.');
        }
        else {
          setting = Serial.readStringUntil(':');          
        }
        my_config.dns[i] = setting.toInt();
      }      

      // default gateway
        for (i=0; i<4; i++) {
        if (i < 3) {
          setting = Serial.readStringUntil('.');
        }
        else {
          setting = Serial.readStringUntil(':');          
        }
        my_config.gateway[i] = setting.toInt();
      }
      //wifi ssid
      setting = Serial.readStringUntil(':');
      setting.toCharArray(my_config.ssid, sizeof(my_config.ssid)+1);
      

      //wifi pass
      setting = Serial.readStringUntil('#');
      //Serial.println(setting);
      setting.toCharArray(my_config.pass, sizeof(my_config.pass)+1);
      //Serial.println(my_config.pass);
      delay(1000);

      write_config();
      delay(1000);
      Serial.println("SETCONFIG:#");
    }
    
  }
}

void read_config() {
  EEPROM.readBlock(0, my_config);
}

void write_config() {
  EEPROM.writeBlock(0, my_config);
}

void load_defaults() {
  // make serial only and put placeholders in for static IP and wifi parameters
  my_config.wifi_enable = false;
  my_config.static_ip = false;

  my_config.wifi_ip[0] = 192;
  my_config.wifi_ip[1] = 168;
  my_config.wifi_ip[2] = 1;
  my_config.wifi_ip[3] = 200;

  my_config.subnet[0] = 255;
  my_config.subnet[1] = 255;
  my_config.subnet[2] = 255;
  my_config.subnet[3] = 0;

  my_config.dns[0] = 192;
  my_config.dns[1] = 168;
  my_config.dns[2] = 1;
  my_config.dns[3] = 1;

  my_config.gateway[0] = 192;
  my_config.gateway[1] = 168;
  my_config.gateway[2] = 1;
  my_config.gateway[3] = 1;

  strcpy(my_config.ssid, "your_ssid");
  strcpy(my_config.pass, "your_pass");

}


// residual debug functions

void printWifiData() {

  // print your board's IP address:

  IPAddress ip = WiFi.localIP();

  Serial.print("IP Address: ");

  Serial.println(ip);
}

void printCurrentNet() {

  // print the SSID of the network you're attached to:
  Serial.print("SSID: ");
  Serial.println(WiFi.SSID());

  // print the MAC address of the router you're attached to:

  byte bssid[6];
  WiFi.BSSID(bssid);
  Serial.print("BSSID: ");
  // print the received signal strength:
  long rssi = WiFi.RSSI();
  Serial.print("signal strength (RSSI):");
  Serial.println(rssi);
  // print the encryption type:
  byte encryption = WiFi.encryptionType();
  Serial.print("Encryption Type:");
  Serial.println(encryption, HEX);
  Serial.println();
}
