-------------------------------------------------------------------------------
                   YABE - YET ANOTHER BACNET EXPLORER
-------------------------------------------------------------------------------

1.  INTRO

1.1 ABOUT
	Yabe is a graphical windows program for exploring and navigating BACnet
	devices. 
	The project was created in order to test and evaluate the BACnet protocol.
	In specific I needed a platform on which to test theoretical performance 
	throughput for huge amounts of data over MSTP/RS485 multidrop network. 
	The BACnet feature "segmentation" will be required to increase throughput
	in order to fulfill our requirements. Or so I assume. 
	There're other free/open programs that also implements BACnet "exploring". 
	So far I haven't found any to my satisfaction though. Some are worth 
	mentioning. 
	InneaBACnetExplorer:
	http://www.inneasoft.com/index.php/en/products/products-bacnet/products-bacnet-explorer.html
	A very nice explorer program. Same concept as Yabe. (Perhaps even better
	than Yabe.) However if you want to do basic things like "write" or 
	"subscribe" to a node, you have to pay a cartful of money. Also it only 
	supports BACnet/IP and no segmentation. 
	BACnet Stack - By Steve Karg:
	http://bacnet.sourceforge.net/
	A very nice old open source ANSI C project with a lot of console based 
	sample programs. Implements both BACnet/IP and BACnet/MSTP. Steve is very 
	active and famous in the BACnet world. The code is "singleton" based 
	though. Meaning that the code is only able to function as either BACnet/IP
	or BACnet/MSTP and only 1 connection is supported. This is fine for a MCU
	device but not very useful on devices like PCs. 
	I would have liked to contribute and base my tests on Steves project, but
	the shift to "session" based code (as agreed upon in the mail list) may 
	have been too big a mouthful. My patches were ignored. 
	
	This document is subject to change.

1.2 CREDITS
	The projected is created by me, Morten Kvistgaard, anno 2014. 
	Graphics are the usual FamFamFam: http://www.famfamfam.com/
	Serializing (most/some) is ported from project by Steve Karg:
	http://bacnet.sourceforge.net/
	GUI and concept is inspired by UaExpert:
	http://www.unified-automation.com/products/development-tools/uaexpert.html

2.  USAGE

2.1 BACNET/IP OVER UDP
	- Start up the DemoServer program or another BACnet device.
	- Start Yabe.
	- Select "Add device" under "Search".
	- Press the "Add" button in the "BACnet/IP" field.
	  The program will now add a Udp connection to the "Devices" tree and send
	  out 3 "WhoIs" broadcasts. If there're any BACnet/IP devices in the 
	  network they will show up in the tree. The DemoServer will show up as 
	  something like "192.168.1.91:57049 - 389002". This is the IP, the Udp
	  port and the device_id.
	- In order to explore the device address space, click on the device in
	  the "Devices" tree.
	  The program will fetch all "registers" or "nodes" from the device and
	  display them in the "Address Space" tree.
	- In order to explore the properties for a given register, select the node
	  in the "Address Space" tree.
	  The program will fetch all properties and display them in the 
	  "Properties" field. 
	- Write a given property by clicking the given field and insert a 
	  new value. The value will be written and read back. Be aware that many 
	  properties cannot be written. The device will decide that.
	- Subscribe to a given register/node by draging the node from the 
	  "Address Space" tree to the "Subscriptions" field.
	- Download or upload a file to the device by right clicking a file node
	  in the "Address Space" tree and select "Upload File" or "Download File". 
	- To send out a new "WhoIs" right click the transport in the "Devices" tree
	  and select "WhoIs".

2.2 BACNET/MSTP OVER PIPE
	- For general usage refer to section 2.1. 
	- In the "Search" dialog select "COM1003" in the port combo box and press
	  "Add". This will add a MSTP pipe created by the DemoServer. 
	  Notice that the "Source Address" defaults to "-1". This is not a valid 
	  address and you will not be able to communicate with the device through 
	  this. The program will still be able to listen in on the network though.
	  The program will now list all the devices that communicates on the 
	  network. And it will also display all "PollForMaster" destinations that
	  goes unanswered. This way you'll be able to determine what source_address
	  to give to the program. 
	- Click on the device node in the "Devices" tree. If the source_address is
	  configured to "-1" the program will ask if you will define a new one.
	  You must do so, in order to continue communication. 

3.  TECHNICAL
    
3.1 MULTIPLE UDP CLIENTS ON SAME IP
	Most BACnet/IP programs that I've seen so far are not able to coexist on a
	single PC. This is because they only connect to the one BACnet udp port 
	and seeks to do all their traffic through this. This will work only if you 
	got max 1 client at each machine. Eg. you'll need a virtual machine if you 
	want to work with these. I cannot find anywhere in the standard that 
	defines that you have to do it this way. I could be mistaken though. 
	A better way to solve this, is to connect 2 sockets in your program 
	instead. The BACnet socket at 0xBAC0 should be used for receiving 
	broadcasts. Not to transmit anything or receive any unicasts. If you want 
	to transmit a broadcast you use the other socket (a random port given to 
	you by the PC) and send the broadcast to the 0xBAC0 port. Your random port
	will be flagged as the source for this broadcast. When other clients pick
	up the broadcast they will transmit back their unicast to this random port
	instead of the 0xBAC0 port. This way everything works.
	This is a common way to do it.

3.2 MSTP TIMINGS
	The timing requirements in the BACnet/MSTP standard is a bit tricky issue
	on systems like Windows. In particular the standard defines a time slot in
	which to assume ownership of the token. The time slot is definded as 
	[500 + TS*10 : 500 + (TS+1)*10] ms. This is a 10 ms time slot measured 
	after the last byte on the line. 
	My code implements the MSTP Master state machine a bit differently from the
	standard. The standard is designed for small unscheduled MCUs. Scheduled 
	systems like RTOS and Windows can take advantage of blocking code structure
	and thereby give a bit more simpler code. This is probably not wise to use 
	in non real time systems like Windows. But so I've done. My first version
	was implemented with a high resolution timer (The Stopwatch class in .NET)
	but I like this version better. And it still retains most of the state
	machine.

4.  TESTS
	The DemoServer and Yabe has been tested with other similar programs:
	- InneaBACnetExplorer
	- BACnet Stack - By Steve Karg
	- Wireshark. (This is most likely the same as the BACnet Stack)
	- CAS BACnet Explorer

5.  SUPPORT
	There's no support for the project at this time. That's reserved for our 
	customers. If you write to me, I'm unlikely to answer. 

6.  REPORT ERRORS
	Yeh, there be errors alright. There always are. Many won't be interesting
	though. Eg. if you find a computer that behaves differently from others, 
	I'm unlikely to care. This is not a commercial project and I'm not trying 
	to push it to the greater good of the GPL world. (This may change though.)
	If you find a device that doesn't work with Yabe, it might be interesting.
	But in order for me to fix it, I need either access to the physical device
	or printouts from programs like Wireshark, that displays the error.
	Write to me at mk@pch-engineering.dk.

7.  CONTRIBUTE
	Really? You think it's missing something? It's not really meant as a huge 
	glorified project you know, but if you really must, try contacting me
	at mk@pch-engineering.dk.
	
8.  MISC
	Project web page is located at: 
	https://sourceforge.net/projects/yetanotherbacnetexplorer/