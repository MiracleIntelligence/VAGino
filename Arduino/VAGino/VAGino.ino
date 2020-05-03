#include <can.h>
#include <mcp2515.h>

#include <SPI.h>
#include <SoftwareSerial.h>
#include <SerialCommand.h> 


#define DBGMSG(A)    if (dbg){ Serial.write(0x2); Serial.print(A); Serial.write('\r');}

struct can_frame canMsg1;
struct can_frame canMsg2;
struct can_frame canMsg;
struct can_frame canMsgLFW;
struct can_frame canMsgLFS;

MCP2515 mcp2515(10);
SerialCommand serialCommand;

boolean dbg = false;
boolean readCan = false;

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
	canMsgLFW.can_id  = 0x181;
	canMsgLFW.can_dlc = 2;
	canMsgLFW.data[0] = 0x01;
	canMsgLFW.data[1] = 0x00;

 //left fron window
  canMsgLFS.can_id  = 0x181;
  canMsgLFS.can_dlc = 2;
  canMsgLFS.data[0] = 0x00;
  canMsgLFS.data[1] = 0x00; 


	serialCommand.addCommand("ledon", SetLedOn);
	serialCommand.addCommand("ledoff", SetLedOff);
	serialCommand.addCommand("B", CheckHybridBattery);
	serialCommand.addCommand("D", SetDebug);
	serialCommand.addCommand("S", StartReadCan);
	serialCommand.addCommand("WRITE", WriteCan);
	serialCommand.addCommand("F", StopReadCan);
	serialCommand.addCommand("H", HelpCommand);
  serialCommand.addCommand("LFWU", LeftForwardWindowUp); 
  serialCommand.addCommand("LFWD", LeftForwardWindowDown); 
	serialCommand.addDefaultHandler(UnrecognizedCommand);

	while (!Serial);
	Serial.begin(1000000);
	//Serial.begin(500000);
	SPI.begin();

	mcp2515.reset();
	mcp2515.setBitrate(CAN_100KBPS, MCP_8MHZ);
	mcp2515.setNormalMode();

	//Serial.println("------- CAN Read ----------");
	//Serial.println("ID  DLC   DATA");


	//mcp2515.sendMessage(&canMsg1);
	//mcp2515.sendMessage(&canMsg2);
}

int i = 0;
unsigned long _prevMillis;

void loop()
{
  //unsigned long m = millis();

  // pause to read command
  //if (Serial.available() > 0)
  //{
  // _prevMillis = m;    
  //  DBGMSG(F("READING COMMAND==================================================================================="));
    //DBGMSG(Serial.available());
  //  serialCommand.readSerial();
  //  delay(2000);
  //};

  
  serialCommand.readSerial();

	//while (Serial.available()) {
	//  delay(1);
	//  if (Serial.available() > 0) {
	//    char c = Serial.read();  //gets one byte from serial buffer
	//    readString += c; //makes the string readString
	//  }
	//}

	//WriteCan(); 
  //DBGMSG(F("========================================")); 
  //DBGMSG(millis());
  //DBGMSG(F("========================================")); 
  
	if (readCan)
	{
		ReadCan();
    //ReadCanS();
		//ReadCanTest();
	}
  //LeftForwardWindow();
	delay(20);
 
}


//
// Turn on the LED connected to the specified port
//
void SetLedOn() {
	char* arg = serialCommand.next();

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
	char* arg = serialCommand.next();

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
void CheckHybridBattery() {
  
  DBGMSG(F("Check hybrid battery level")); 
	mcp2515.sendMessage(&canMsg2);  
}

void LeftForwardWindowUp() {
  DBGMSG(F("Left forward window up"));  
  canMsgLFW.data[0] = 0x02;
  mcp2515.sendMessage(&canMsgLFW);
}


void LeftForwardWindowDown() {
  DBGMSG(F("Left forward window down"));  
  canMsgLFW.data[0] = 0x08;
  mcp2515.sendMessage(&canMsgLFW);
}

//float temperature = -9.99;

//  if (!sensors.getAddress(thermometerAddress, 0)){
//    DBGMSG( F("Unable to locate sensor") );
//  } else {
//    sensors.setWaitForConversion(true);
//    sensors.requestTemperaturesByAddress(thermometerAddress);  
//    temperature = sensors.CheckHybridBatteryC(thermometerAddress);
//    sensors.requestTemperaturesByAddress(thermometerAddress);  
 //   temperature = sensors.CheckHybridBatteryC(thermometerAddress);   
//  }

//  if ( temperature < 9.995 && temperature > TEMP_MIN )
//    Serial.print("0");
//  if (temperature > TEMP_MAX || temperature < TEMP_MIN ){
//    DBGMSG( F("Temperature out of bounds") );
//    temperature = -9.99;
//  }

  //Serial.println(13, 2);
  //Serial.println("CAN:0AE 8 E1 E0 00 00 00 00 01 00;");

//Serial.println(13, 2);
//}


void StartReadCan()
{
	readCan = true;
	DBGMSG(F("CAN started"));
}

void StopReadCan()
{
	readCan = false;
	DBGMSG(F("CAN stopped"));
}

//
// Read CAN bus
//
void ReadCan() {
	//DBGMSG(F("ReadCan begin"));
	mcp2515.readMessage(&canMsg);
	if (mcp2515.readMessage(&canMsg) == MCP2515::ERROR_OK)
	{
		//CAN data
		Serial.write(0x1);

		//Serial.print(canMsg.can_id, HEX); // print ID
		//int value = 0x7ED;
		byte* p;
		p = (byte*)&(canMsg.can_id);
		Serial.write(p, 4);


		//Serial.print(" ");
		//Serial.print(canMsg.can_dlc, HEX); // print DLC
		Serial.write(canMsg.can_dlc);
		//Serial.print(" ");
		Serial.write(canMsg.data, canMsg.can_dlc);
		//for (int i = 0; i < canMsg.can_dlc; i++)
		//{
		  //Serial.print(canMsg.data[i], HEX);
		  //Serial.print(" ");
		//}
		Serial.write('\r');
	}
	//Serial.println("CAN:65 2 2 4");
	//DBGMSG(F("ReadCan end"));
	//DBGMSG(F("R"));
}

void ReadCanS() {
  //DBGMSG(F("ReadCan begin"));
  mcp2515.readMessage(&canMsg);
  //if (mcp2515.readMessage(&canMsg) == MCP2515::ERROR_OK)
  {
    //CAN data
    //Serial.write(0x1);

    Serial.print(canMsg.can_id, HEX); // print ID
    //int value = 0x7ED;
    //byte* p;
    //p = (byte*)&(canMsg.can_id);
    //Serial.write(p, 4);


    Serial.print(" ");
    Serial.print(canMsg.can_dlc, HEX); // print DLC
    //Serial.write(canMsg.can_dlc);
    Serial.print(" ");
    //Serial.write(canMsg.data, canMsg.can_dlc);
    for (int i = 0; i < canMsg.can_dlc; i++)
    {
      Serial.print(canMsg.data[i], HEX);
      Serial.print(" ");
    }
    Serial.println();
    //Serial.write('\r');
  }
  //Serial.println("CAN:65 2 2 4");
  //DBGMSG(F("ReadCan end"));
  //DBGMSG(F("R"));
}

void ReadCanTest()
{
  //int count = Serial.availableForWrite();
	//if (count < 10)
	//{    
	// DBGMSG(F("AVAILABLE:"));
  // DBGMSG(count);   
	//}
 
	Serial.write(0x1);
	unsigned long value = 0x7ED;
	byte* p;
	p = (byte*)&value;
	Serial.write(p, 4);
	//Serial.write((char *)0x7ED, 2);
	Serial.write(0x8);
	Serial.write(0xFF);
	Serial.write(0xF1);
	Serial.write(0xF2);
	Serial.write(0xF3);
	Serial.write(0xF4);
	Serial.write(0xF5);
	Serial.write(0xF6);
	Serial.write(0xF7);
	Serial.write('\r');
  //Serial.write('\n');
  //count = Serial.availableForWrite(); 
  //{    
  // DBGMSG(F("AVAILABLE:"));
  // DBGMSG(count);   
  //}
}


//
// Turn on the LED connected to the specified port
//
void WriteCan() {
	char* arg = serialCommand.next();
	DBGMSG(F("WriteCan begin"));
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


	DBGMSG(F("WriteCan end"));
}


//
// Enable or disable debug messages from being printed
// on the serial terminal
//
void SetDebug() {
	char* arg = serialCommand.next();

	if (arg != NULL) {
		if (strcmp(arg, "ON") == 0) {
			dbg = true;
			DBGMSG(F("Turn on debug"));
		}
		if (strcmp(arg, "OFF") == 0) {
			DBGMSG(F("Turn off debug"));
			dbg = false;
		}
	}
}

//
// An unrecognized command was recieved
//
void UnrecognizedCommand() {

  //delay(2000);
	char* arg = serialCommand.next();
	if (strlen(arg) > 0)
	{
		DBGMSG(F("unknown command: "));
		DBGMSG(arg);
	}
}

void HelpCommand()
{
	DBGMSG(F(" ledon 3  - turn on led connected to digital I/O 3"));
	DBGMSG(F(" ledon 4  - turn on led connected to digital I/O 4"));
	DBGMSG(F(" ledon 5  - turn on led connected to digital I/O 5"));
	DBGMSG(F(" ledon 6  - turn on led connected to digital I/O 6"));
	DBGMSG(F(" ledoff 3 - turn off led connected to digital I/O 3"));
	DBGMSG(F(" ledoff 4 - turn off led connected to digital I/O 4"));
	DBGMSG(F(" ledoff 5 - turn off led connected to digital I/O 5"));
	DBGMSG(F(" ledoff 6 - turn off led connected to digital I/O 6"));
	DBGMSG(F(" temp     - read temperature"));
	DBGMSG(F(" debug on - turn on debug messages"));
	DBGMSG(F(" debug off- turn off debug messages"));
}
