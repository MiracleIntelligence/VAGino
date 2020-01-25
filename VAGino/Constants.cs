﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SerialArduino;
using Windows.UI.Xaml.Controls;

namespace SerialArduino
{
    public class DeviceProperties
    {
        //public const String DeviceInstanceId = "System.Devices.DeviceInstanceId";
        public const String DeviceInstanceId = "System.ItemNameDisplay";
    }

    public class ArduinoDevice
    {
        public const UInt16 Vid = 0x2341;
        public const UInt16 Pid = 0x0043;
    }

    public class CanId
    {
        public const string HYBRID = "7ED";
    }

    public class Commands
    {
        public const string BATTERY = "temp";
        public const string LFW_DOWN = "lfwd";
    }

    public class Filters
    {
        public static List<string> IDFilters = new List<string>
        {
            //"380",
            //"48E",
            //"AE",
            //"1A0",
            //"1AC",
            //"280",
            //"288",
            //"2A0",
            //"320",
            //"480",
            //"5A0",
            //"448",
            //"452",
            //"284",
            //"50"
            //"19 7",
            //"17 7",
            //"280 8",
            //"448 5",
            //"482 8",
            //"540 8",
            //"38A 4",
            //"56A 8",
            //"4A8 8",
            //"520 8",
            //"48A 8",
            //"D0 6",
            //"5E0 8",
            //"51A 8",
            //"5D2 8",
            //"288 8",
            //"548 3",
            //"3D2 7",
            //"3A0 8",
            //"288 8",
            //"1AC 8",
            //"393 8",
            //"380 8",
            //"588 8",
            //"397 8",
            //"3B1 8",
            //"488 8",
            //"7D0 8",
            //"C2 8",
            //"580 8",
            //"58F 8",
            //"57C 8"
        };
    }

}