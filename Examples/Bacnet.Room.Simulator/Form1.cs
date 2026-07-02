/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DemoServer;
using System.IO.BACnet;
using System.Linq;
using System.Net.NetworkInformation;
using Bacnet.Room.Simulator.Properties;

namespace Bacnet.Room.Simulator
{
    public partial class BacForm : Form
    {
        Button[] Bts;
        Label[] Lbs;
        PictureBox[] NivChauf, NivClim;
        double RollingCounter = 0;
        Random random = new Random();
        uint Niveausoufflage = 0;

        // Si ConsigneEffective.OutOfService=vrai alors l'action locale n'est pas prise en compte
        bool Remoteconsigne;

        int NiveauChoisi = 0;

        BacnetObjectId Bac_TempInt = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 0);
        BacnetObjectId Bac_TempEau = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 1);
        BacnetObjectId Bac_TempExterieure = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2);

        BacnetObjectId Bac_ConsigneTemp = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 0);

        BacnetObjectId Bac_Mode =  new BacnetObjectId(BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE, 0);
        BacnetObjectId Bac_Niveausoufflage = new BacnetObjectId(BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE, 1);

        BacnetObjectId Bac_Cmdchauffage = new BacnetObjectId(BacnetObjectTypes.OBJECT_BINARY_VALUE, 0);
        BacnetObjectId Bac_CmdClim = new BacnetObjectId(BacnetObjectTypes.OBJECT_BINARY_VALUE, 1);

        RoomModel Room=new RoomModel(21);
        bool TempFarenheit;

        public BacForm()
        {
            InitializeComponent();
            Bts = new Button[3] { Set1, Set2, Set3 };
            Lbs = new Label[3] { Set1Label, Set2Label, Set3Label};
            NivClim = new PictureBox[3] { Clim1, Clim2, Clim3};
            NivChauf = new PictureBox[3] {Chauf1, Chauf2, Chauf3 };

            AddressSelection();

            BacnetActivity.m_local_ip_endpoint = networkInterfaces.SelectedItem.ToString();

            bacnetid.Text = "Bacnet device Id :  " + BacnetActivity.deviceId.ToString();

            TempFarenheit = (((Application.CurrentCulture.ToString() == "en-US") && (Settings.Default.ChangeTemperatureDefaultUnit == false))) ||
                            ((Application.CurrentCulture.ToString() != "en-US") && (Settings.Default.ChangeTemperatureDefaultUnit == true));

            AdaptationFarenheit();

            AnimateData();
            UpdateIhm();            
        }

        private void AddressSelection()
        {
            var selectedInterface = (from netiface in NetworkInterface.GetAllNetworkInterfaces()
                                     where (netiface.OperationalStatus == OperationalStatus.Up) &&
                                           (netiface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                                            netiface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                                     let props = netiface.GetIPProperties()
                                     let ipv4Address = props.UnicastAddresses.FirstOrDefault(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                     where ipv4Address != null
                                     select ipv4Address.Address.ToString()).ToList();

            // Clear existing items in the ComboBox
            networkInterfaces.Items.Clear();
            networkInterfaces.Items.Add("Default");

            // Add the results to the ComboBox
            foreach (var address in selectedInterface)
            {
                networkInterfaces.Items.Add(address);
            }

            networkInterfaces.Text = Program.IPAddress;
            
        }

        private void AdaptationFarenheit()
        {
            BacnetObjectId b;
            BacnetValue bv;

            if (TempFarenheit==false) return;

            for (int i = 0; i < 4; i++)
            {
                b = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, (uint)(i));
                bv = BacnetActivity.GetBacObjectPresentValue(b);

                BacnetActivity.SetBacObjectPresentValue(b, new BacnetValue((float)Math.Round(TempDegre2Value((float)bv.Value))));

                IList<BacnetValue> val = new BacnetValue[1] { new BacnetValue(64) };
                BacnetActivity.m_storage.WriteProperty(b, BacnetPropertyIds.PROP_UNITS, 1, val, true);
            }

            for (int i = 0; i < 3; i++)
            {
                b = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, (uint)(i));
                IList<BacnetValue> val = new BacnetValue[1] { new BacnetValue(64) };
                BacnetActivity.m_storage.WriteProperty(b, BacnetPropertyIds.PROP_UNITS, 1, val, true);
            }
        }

        private float Truncate(double v)
        {
            return (float)(Math.Truncate(v * 10) / 10);
        }

        private string TempDegre2Text(double C)
        {

            if (TempFarenheit)
                return Truncate(C).ToString()+"°F";
            else
                return Truncate(C).ToString()+"°C";
        }

        private float TempDegre2Value(double C)
        {

            if (TempFarenheit)
                return Truncate(C * 1.8 + 32);
            else
                return Truncate(C);
        }

        private double Temp2Degree(double C)
        {
            if (TempFarenheit)
                return (C - 32) / 1.8;
            else
                return C;
        }

        private void AnimateData()
        {

            BacnetValue bv1,bv2;
            double TempCons;
            double TempInt=0;
            double TempEau=0;
            double TempExt = 0;
            bool ModeChauf=false, ModeClim=false;

            for (int i = 0; i < 3; i++)
                NivClim[i].BackColor = Color.White;
            for (int i = 0; i < 3; i++)
                NivChauf[i].BackColor = Color.White;

            RollingCounter += 2 * random.NextDouble();

            // Récupération de la consigne de temperature
            bv1 = BacnetActivity.GetBacObjectPresentValue(Bac_ConsigneTemp);
            TempCons = Temp2Degree((float)bv1.Value);

            // Récupération du mode choisi
            bv2 = BacnetActivity.GetBacObjectPresentValue(Bac_Mode);
            uint mode = (uint)bv2.Value;
            if (mode > 3)
            {
                mode = 1;
                BacnetActivity.SetBacObjectPresentValue(Bac_Mode, new BacnetValue(mode));
            }

            switch (mode)
            {
                case 1: // Mode arret
                    TempEau = 20.0 + 3 * Math.Cos(RollingCounter / 10.0);
                    TempExt = 20.0;
                    TempInt = Room.GetNextTemp(TempEau, TempExt, (int)Niveausoufflage);

                    Niveausoufflage = 0;
                    ModeChauf = ModeClim = false;
                    pictureModeArret.Visible = true;
                    pictureModeChaud.Visible = false;
                    pictureModeFroid.Visible = false;

                    break;

                case 2: // Mode chauffage
                    TempEau = 37.0 + 3 * Math.Cos(RollingCounter / 10.0);
                    TempExt = 12.0;
                    TempInt = Room.GetNextTemp(TempEau, TempExt, (int)Niveausoufflage);

                    ModeClim = false;

                    if (TempInt >= TempCons)
                    {
                        Niveausoufflage = 0;
                        ModeChauf = false;
                       
                    }
                    else
                    {
                        Niveausoufflage = (uint)(1 + (TempCons - TempInt) * 4);
                        if (Niveausoufflage > 3) Niveausoufflage = 3;
                        ModeChauf = true;
                
                    }

                    pictureModeArret.Visible = false;
                    pictureModeChaud.Visible = true;
                    pictureModeFroid.Visible = false;

                    break;
                case 3: // Mode clim
                    TempEau = 5.0 + 3 * Math.Cos(RollingCounter / 10.0);
                    TempExt = 30.0;
                    TempInt = Room.GetNextTemp(TempEau, TempExt, (int)Niveausoufflage);

                    ModeChauf = false;

                    if (TempInt <= TempCons)
                    {
                        Niveausoufflage = 0;
                        ModeClim = false;
                    }
                    else
                    {
                        Niveausoufflage = (uint)(1 + (TempInt - TempCons) * 4);
                        if (Niveausoufflage > 3) Niveausoufflage = 3;

                        ModeClim = true;
                    }
                    pictureModeArret.Visible = false;
                    pictureModeChaud.Visible = false;
                    pictureModeFroid.Visible = true;
                    break;
             }

            if (ModeChauf==true)
                for (int i = 0; i < 3; i++)
                    if (i < Niveausoufflage)
                        NivChauf[i].BackColor = Color.Red;
            if (ModeClim == true)
                for (int i = 0; i < 3; i++)
                    if (i < Niveausoufflage)
                        NivClim[i].BackColor = Color.Blue;

            BacnetActivity.SetBacObjectPresentValue(Bac_TempEau, new BacnetValue(TempDegre2Value(TempEau)));
            BacnetActivity.SetBacObjectPresentValue(Bac_TempInt, new BacnetValue(TempDegre2Value(TempInt)));
            BacnetActivity.SetBacObjectPresentValue(Bac_TempExterieure, new BacnetValue(TempDegre2Value(TempExt)));
            BacnetActivity.SetBacObjectPresentValue(Bac_Niveausoufflage, new BacnetValue(Niveausoufflage+1));

            BacnetActivity.SetBacObjectPresentValue(Bac_Cmdchauffage, new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, Convert.ToUInt32(ModeChauf)));
            BacnetActivity.SetBacObjectPresentValue(Bac_CmdClim, new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, Convert.ToUInt32(ModeClim)));

        }

        private void UpdateIhm()
        {
            BacnetObjectId b;
            BacnetValue bv;
            float f;

            // Les labels associés aux Bp pour choisir la temperature
            b = new BacnetObjectId(BacnetObjectTypes.OBJECT_CHARACTERSTRING_VALUE, 1);
            String Txt = (string)BacnetActivity.GetBacObjectPresentValue(b).Value;

            String[] Messages=Txt.Split(';');
            if (Messages.Length == 3)
            {
                Set1Label.Text = Messages[0];
                Set2Label.Text = Messages[1];
                Set3Label.Text = Messages[2];
            }
           
            // Les temperatures

            bv = BacnetActivity.GetBacObjectPresentValue(Bac_TempInt);
            f = (float)bv.Value;
            TempInt.Text =  TempDegre2Text(f);

            bv = BacnetActivity.GetBacObjectPresentValue(Bac_ConsigneTemp);
            f = (float)bv.Value;
            TempSet.Text = "T Set : " + f.ToString()+"°";

            bv = BacnetActivity.GetBacObjectPresentValue(Bac_TempExterieure);
            f = (float)bv.Value;
            TempExt.Text = "T Ext : " + f.ToString() + "°";          
        }

        private void TmrUpdate_Tick(object sender, EventArgs e)
        {

            // Si consigne_Effective OutofService alors l'écriture de Present_Value à lieu via Bacnet
            // sinon on remet à jour ici la valeur choisie 'au clavier' par l'utilisateur
            IList<BacnetValue> val = null;
            BacnetActivity.m_storage.ReadProperty(Bac_ConsigneTemp, BacnetPropertyIds.PROP_OUT_OF_SERVICE, 1, out val);
            Remoteconsigne = (bool)val[0].Value;

            // Copie de la valeur utilisateur
            if (Remoteconsigne == false)
            {
                BacnetObjectId b;
                BacnetValue bv;
                b = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, (uint)(NiveauChoisi + 1));
                bv = BacnetActivity.GetBacObjectPresentValue(b);
                BacnetActivity.SetBacObjectPresentValue(Bac_ConsigneTemp, bv);
            }

            // Animation
            AnimateData();
            UpdateIhm();
        }

        private void SetRef_Click(object sender, EventArgs e)
        {
            BacnetObjectId b;
            BacnetValue bv;

            for (int i = 0; i < 3; i++)
            {
                if (sender != Bts[i])
                {
                    Bts[i].BackColor = SystemColors.Control;
                    Lbs[i].ForeColor = SystemColors.ControlDark;
                }
                else
                {
                    NiveauChoisi = i;
                    Bts[i].BackColor = Color.Red;
                    Lbs[i].ForeColor = SystemColors.ControlText;
                    if (Remoteconsigne == false)
                    {
                        b = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, (uint)(NiveauChoisi + 1));
                        bv = BacnetActivity.GetBacObjectPresentValue(b);
                        BacnetActivity.SetBacObjectPresentValue(Bac_ConsigneTemp, bv);
                    }
                }
            }

        }

        private void networkInterfaces_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (networkInterfaces.Text != "Default")
                BacnetActivity.m_local_ip_endpoint = networkInterfaces.Text;
            else
                BacnetActivity.m_local_ip_endpoint = "";

            BacnetActivity.ReInitialize();

        }

        private void ScreenOnOff_Click(object sender, EventArgs e)
        {
            panel1.Visible = !panel1.Visible;
        }


    }

}
