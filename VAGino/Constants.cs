//*********************************************************
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

using VAGino;
using Windows.UI.Xaml.Controls;

namespace VAGino
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
        public const string CMD_CHECK_HYBRID_BATTERY = "B";
        public const string CMD_SET_DEBUG = "D";
        public const string CMD_START_CAN = "S";
        public const string CMD_STOP_CAN = "F";
        public const string LFW_DOWN = "lfwd";
    }

    public class FiltersList
    {
        public static List<string> IDFilters = new List<string>
        {
            "D0",
            "721",
            "723",
            "72E",
            "72F",
            "720",
            "5DC",
            "5E0",
            "1AC",
            "280",
            "3B1",
            "380",
            "3B2",
            "3D0",
            "420",
            "440",
            "448",
            "482",
            "4A8",
            "4A0",
            "548",
            "58C",
            "7D0",
            "10",
            "11",
            "18",
            "19"
        };
    }

}
