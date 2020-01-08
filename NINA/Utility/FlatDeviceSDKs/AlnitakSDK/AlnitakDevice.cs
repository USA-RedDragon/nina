﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public class AlnitakDevice : IAlnitakDevice {
        public static readonly IAlnitakDevice Instance = new AlnitakDevice();

        private ISerialPortProvider _serialPortProvider = new SerialPortProvider();
        private ISerialPort _serialPort;

        private const string ALNITAK_QUERY = @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'FTDIBUS\\VID_0403+PID_6001+A82%'";

        public ISerialPortProvider SerialPortProvider {
            set => _serialPortProvider = value;
        }

        public ReadOnlyCollection<string> PortNames => _serialPortProvider.GetPortNames(ALNITAK_QUERY);

        public bool InitializeSerialPort(string aPortName) {
            if (string.IsNullOrEmpty(aPortName)) return false;
            _serialPort = aPortName.Equals("AUTO")
                ? _serialPortProvider.GetSerialPort(_serialPortProvider.GetPortNames(ALNITAK_QUERY, addDivider: false, addGenericPorts: false).FirstOrDefault())
                : _serialPortProvider.GetSerialPort(aPortName);
            return _serialPort != null;
        }

        private readonly SemaphoreSlim ssSendCommand = new SemaphoreSlim(1, 1);

        public T SendCommand<T>(ICommand command) where T : Response, new() {
            var result = string.Empty;
            ssSendCommand.Wait();
            try {
                _serialPort.Open();
                Logger.Debug($"AlnitakFlatDevice: command : {command}");
                _serialPort.Write(command.CommandString);
                result = _serialPort.ReadLine();
                Logger.Debug($"AlnitakFlatDevice: response : {result}");
            } catch (TimeoutException ex) {
                Logger.Error($"AlnitakFlatDevice: timed out for port : {_serialPort.PortName} {ex}");
            } catch (Exception ex) {
                Logger.Error($"AlnitakFlatDevice: Unexpected exception : {ex}");
            } finally {
                _serialPort?.Close();
                ssSendCommand.Release();
            }
            return new T { DeviceResponse = result };
        }

        public void Dispose() {
            _serialPort?.Dispose();
        }
    }
}