/********************************************************************
*                           MIT License
* 
* Copyright (C) 2016 Frederic Chaxel <fchaxel@free.fr>
* Icon Copyright David Vignoni, licence LGPL
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

This program (windows service or console) gets current weather data
from Weatherapi Internet Webservice and 'sends' them on Bacnet.

You must first creates a user account into https://www.weatherapi.com/
in order to get a Key : see https://www.weatherapi.com/signup.aspx
It's free of charge for basic services.

You have to modify and add information into the registry : have a look to 
the .Reg file (AppId, UserAccessKey, Latitude, Longitude must be modified).
Latitude, Longitude with no more than 3 decimal places.

On an old Win32 only PC, removes Wow6432Node from the .reg file entry :
	[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Weather2_to_Bacnet]
	changes by
	[HKEY_LOCAL_MACHINE\SOFTWARE\Weather2_to_Bacnet]

This .reg file should be registered by a double click.

To use it as a console application just run the application.

To register the Windows service : in an admin console type :
	installutil.exe Wheather2_to_Bacnet.exe
installutil is located on Windows\Microsoft.NET\Framework\V4xxxx

then start manually the service or reboot the pc


The code could be simply ported to Linux-Mono :
	removes the 3 lines of code concerning services in the Main method
	hard code (for instance) the 4 parameters : UserAccessKey ...
	removes the inheritence between MyService and ServiceBase
	removes MyServiceInstaller class

Weather2 was the service name before 1st March 2018: application name is unchanged.