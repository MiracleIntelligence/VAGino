#include <SPI.h>
#include <mcp2515.h>
#include <SoftwareSerial.h>
#include <SerialCommand.h> 


#define DBGMSG(A)    if (dbg){ Serial.print("DBG: "); Serial.println(A);}

struct can_frame canMsg1;
struct can_frame canMsg2;
struct can_frame canMsg;
struct can_frame canMsgLFWD;
MCP2515 mcp2515(10);
SerialCommand serialCommand;

boolean dbg = false;

void setup() {

  canMsg1.can_id = 0x570;
  canMsg1.can_dlc = 5;
  canMsg1.data[0] = 0xC7;
  canMsg1.data[1] = 0x2;
  canMsg1.data[2] = 0x76;
  canMsg1.data[3] = 0xDF;
  canMsg1.data[4] = 0x80;
  //canMsg1.data[5] = 0x55;
  //canMsg1.data[6] = 0x55;
  //canMsg1.data[7] = 0x55;

  //canMsg2.can_id = 0x714;
  //canMsg2.can_dlc = 8;
  //canMsg2.data[0] = 0x03;
  //canMsg2.data[1] = 0x22;
  //canMsg2.data[2] = 0x22;
  //canMsg2.data[3] = 0x03;
  //canMsg2.data[4] = 0x55;
  //canMsg2.data[5] = 0x55;
  //canMsg2.data[6] = 0x55;
  //canMsg2.data[7] = 0x55;

  canMsg2.can_id = 0x7E5;
  canMsg2.can_dlc = 8;
  canMsg2.data[0] = 0x03;
  canMsg2.data[1] = 0x22;
  canMsg2.data[2] = 0x02;
  canMsg2.data[3] = 0x8C;
  canMsg2.data[4] = 0x55;
  canMsg2.data[5] = 0x55;
  canMsg2.data[6] = 0x55;
  canMsg2.data[7] = 0x55;

  //left fron window
  canMsgLFWD.can_id = 0x181;
  canMsgLFWD.can_dlc = 2;
  canMsgLFWD.data[0] = 0x02;
  canMsgLFWD.data[1] = 0x00;

  serialCommand.addCommand("ledon", SetLedOn );
  serialCommand.addCommand("ledoff", SetLedOff );
  serialCommand.addCommand("temp", GetTemp );
  serialCommand.addCommand("lfwd", LFWDown );
  serialCommand.addCommand("debug", SetDebug ); 
  serialCommand.addCommand("read", ReadCan );
  serialCommand.addCommand("write", WriteCan );
  serialCommand.addDefaultHandler(UnrecognizedCommand );

  while (!Serial);
  Serial.begin(1000000);
  SPI.begin();

  mcp2515.reset();
  mcp2515.setBitrate(CAN_1000KBPS, MCP_16MHZ);
  mcp2515.setNormalMode();

  //Serial.println("------- CAN Read ----------");
  //Serial.println("ID  DLC   DATA");

  
  //mcp2515.sendMessage(&canMsg1);
  //mcp2515.sendMessage(&canMsg2);
}

int i = 0;
void loop() {


  serialCommand.readSerial();
  
  
  //WriteCan();
  
   ReadCan();

}


//
// Turn on the LED connected to the specified port
//
void SetLedOn() {
  char *arg = serialCommand.next();
  
  //if (arg != NULL ){
  //    if ( atoi(arg) >= LED_LOW && atoi(arg) <= LED_HIGH){
  //      DBGMSG( F("SetLedOn") );
  //      digitalWrite(atoi(arg), HIGH);
  //    } else {
  //      DBGMSG( F("out of range.  3 <= led <= 6") );
  //    } 
  //} else {
  //  DBGMSG( F("led not specified") );
  //}
}

//
// Turn off the led connected to the specified port
//
void SetLedOff() {
  char *arg = serialCommand.next();
  
  //if (arg != NULL){
  //  if (atoi(arg) >= LED_LOW && atoi(arg) <= LED_HIGH){
  //    DBGMSG( F("SetLedOff") );
  //    digitalWrite(atoi(arg), LOW);
  //  } else {
  //      DBGMSG( F("out of range.  3 <= led <= 6") );
  //  }      
  //} else {
  //  DBGMSG( F("led not specified") );
  //}
}

//
// Locate the temperature sensor, read the temperature in Celcius,
// and return the temperature via the serial interface as a 5 character
// string.  Valid temperatures are from 0.00 to 99.99 degrees
// Celcius.  -9.99 is returned on an error condition.
//
void GetTemp() {
  float temperature=-9.99;
  
//  if (!sensors.getAddress(thermometerAddress, 0)){
//    DBGMSG( F("Unable to locate sensor") );
//  } else {
//    sensors.setWaitForConversion(true);
//    sensors.requestTemperaturesByAddress(thermometerAddress);  
//    temperature = sensors.getTempC(thermometerAddress);
//    sensors.requestTemperaturesByAddress(thermometerAddress);  
 //   temperature = sensors.getTempC(thermometerAddress);   
//  }
  
//  if ( temperature < 9.995 && temperature > TEMP_MIN )
//    Serial.print("0");
//  if (temperature > TEMP_MAX || temperature < TEMP_MIN ){
//    DBGMSG( F("Temperature out of bounds") );
//    temperature = -9.99;
//  }
  
  //Serial.println(13, 2);
  //Serial.println("CAN:0AE 8 E1 E0 00 00 00 00 01 00;");
  mcp2515.sendMessage(&canMsg2);
  //Serial.println(13, 2);
}


void LFWDown() {
  mcp2515.sendMessage(&canMsgLFWD);
}

//
// Turn on the LED connected to the specified port
//
void ReadCan() {
  //DBGMSG( F("ReadCan begin") );
  
  mcp2515.readMessage(&canMsg);
  //if (mcp2515.readMessage(&canMsg) == MCP2515::ERROR_OK) 
  {
    Serial.print("CAN:");
    Serial.print(canMsg.can_id, HEX); // print ID
    Serial.print(" ");
    Serial.print(canMsg.can_dlc, HEX); // print DLC
    Serial.print(" ");

    for (int i = 0; i < canMsg.can_dlc; i++) {  // print the data

      Serial.print(canMsg.data[i], HEX);
      Serial.print(" ");
    }
    Serial.print(";");
    //Serial.print(255,2);

    //Serial.println();

    //DBGMSG( F("ReadCan end") );
  }   
}


//
// Turn on the LED connected to the specified port
//
void WriteCan() {
  char *arg = serialCommand.next();
  DBGMSG( F("WriteCan begin") );
  //if (arg != NULL ){
  //    if ( atoi(arg) >= LED_LOW && atoi(arg) <= LED_HIGH){
  //      DBGMSG( F("SetLedOn") );
  //      digitalWrite(atoi(arg), HIGH);
  //    } else {
  //      DBGMSG( F("out of range.  3 <= led <= 6") );
  //    } 
  //} else {
  //  DBGMSG( F("led not specified") );
  //}

  //if (i < 32)
  //{
  //  i++;      
  //}else
  //{
  //  mcp2515.sendMessage(&canMsg2);
  //  i=0;
  //}
  //Serial.println("Messages sent");

  
  DBGMSG( F("WriteCan end") );
}


//
// Enable or disable debug messages from being printed
// on the serial terminal
//
void SetDebug() {
  char *arg = serialCommand.next();
  
  if (arg != NULL){
    if ( strcmp(arg, "on" ) == 0){
      dbg=true;
      DBGMSG(F("Turn on debug"));
    }
    if ( strcmp(arg, "off" ) == 0){
      DBGMSG(F("Turn off debug"));
      dbg=false;
    }
  }
}

//
// An unrecognized command was recieved
//
void UnrecognizedCommand(){  
  DBGMSG(F("Unrecognized command"));
  DBGMSG(F(" ledon 3  - turn on led connected to digital I/O 3"));
  DBGMSG(F(" ledon 4  - turn on led connected to digital I/O 4"));
  DBGMSG(F(" ledon 5  - turn on led connected to digital I/O 5"));
  DBGMSG(F(" ledon 6  - turn on led connected to digital I/O 6"));
  DBGMSG(F(" ledoff 3 - turn off led connected to digital I/O 3"));
  DBGMSG(F(" ledoff 4 - turn off led connected to digital I/O 4"));
  DBGMSG(F(" ledoff 5 - turn off led connected to digital I/O 5"));
  DBGMSG(F(" ledoff 6 - turn off led connected to digital I/O 6"));
  DBGMSG(F(" temp     - read temperature" ));
  DBGMSG(F(" debug on - turn on debug messages" ));
  DBGMSG(F(" debug off- turn off debug messages" ));
}
