# .NET library for BACnet

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://raw.githubusercontent.com/ela-compil/BACnet/master/MIT_license.txt)
[![NuGet version](https://badge.fury.io/nu/bacnet.svg)](https://www.nuget.org/packages/BACnet)
[![Donate](https://img.shields.io/badge/%24-donate-ff00ff.svg)](https://www.paypal.me/JakubBartkowiak)

This library allows C# developers to communicate with BACnet devices. It has a full C# BACnet stack implementation. Application that uses this library can read/write/control remote devices as well as share it's own data with the BACnet network. 

## Feature highlight:

* Client and Server
* Serializing
* IP over UDP
* IPv6
* MSTP over local pipe or serial port
* Ethernet
* Segmentation support
* Service functions such as read/write/subscribeCOV etc.
* Exotic functions such as TimeSync, DeviceCommunicationControl, LifeSafetyOperations
* Remote BBMD Tables edition (Read & Write)
* CreateObject & DeleteObject services
* BACnet PTP (not tested yet)
* TrendLog, Calendar, Notification class, Schedule, Alarms summary

## History

The foundation of this library has been made by Morten Kvistgaard. Later F. Chaxel made a lot of contribution to it and based his [YABE (Yet Another BACnet Explore)](https://sourceforge.net/projects/yetanotherbacnetexplorer/) project on it. F.Chaxel was not interested in separating YABE from it's core library and making it available thru NuGet. Because of that a fork has been made here from SourceForge SVN source. Original history of commits has been preserved. YABE application source code has been removed leaving only the core library part that has been splitted into separate repositories and made available to download from Nuget.

## How to use

Easiest way is to use BACnet library is grabbing the latest [NuGet package](https://www.nuget.org/packages/BACnet).

## Getting Started

[See examples](https://github.com/ela-compil/BACnet.Examples) to learn how to use this library.

## Donate

If you like this library, would like me to add something special for you or just motivate me to support it and keep investing my time, you can [use PayPal](https://www.paypal.me/JakubBartkowiak) to show your support :angel:
