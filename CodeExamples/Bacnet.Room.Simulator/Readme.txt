
This application "simulates" an heating/cooling room controler.

The objective is to teach/learn Scada with BACnet and shows how it's simple to do compares to a lot of others protocols (even OPC is less good).

The application shows an Lcd/buttons interface facing the user in the room.

The user can only selects a pre-recorded temperature setpoint with the buttons (three options).

Setpoint values and the associated texts can be writen via BACnet, and also the running mode (stop/hot/cold) :
	ANALOG_VALUE:1, CHARACTERSSTRING:1 SetPoint1 and Text1
	ANALOG_VALUE:2, CHARACTERSSTRING:2 SetPoint2 and Text2
	ANALOG_VALUE:3, CHARACTERSSTRING:3 SetPoint3 and Text3
	MULTISTATE_VALUE:0, running mode

ANALOG_VALUE:0 Present_Value is the user selected setpoint but can be ovveride with BACnet by setting the Out_Of_Service property to true.

A 3 levels proportional ventillation is done depending on the difference between internal and external temperature (const. 12 degree celcius in heating mode, 30°C in cooling mode). 
For US people temperature are in °F.

Internal, external & hot/warm Water temperatures could be read through some ANALOG_INPUT objects.

This application can be launched multi-times on the same PC to simulates simultaneously a lot of rooms (each one get a specific BACnet device Id).

Sorry for any offens to HVAC systems engineers/designers ... it's just a BACnet simulation :=;

Code tested also on Raspberry Pi with Mono.