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

using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Interfaces;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    public interface ITelescopeMediator : IDeviceMediator<ITelescopeVM, ITelescopeConsumer, TelescopeInfo> {

        void MoveAxis(TelescopeAxes axis, double rate);

        void PulseGuide(GuideDirections direction, int duration);

        bool Sync(Coordinates coordinates);

        bool Sync(double ra, double dec);

        Task<bool> SlewToCoordinatesAsync(Coordinates coords);

        Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coords);

        Task<bool> MeridianFlip(Coordinates targetCoordinates);

        bool SetTracking(bool tracking);

        bool SendToSnapPort(bool start);

        Coordinates GetCurrentPosition();

        Task<bool> ParkTelescope();

        void UnparkTelescope();
    }
}