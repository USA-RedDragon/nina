﻿#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class CoordinatesTest {

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Create_RADegreesTest(double ra, double dec) {
            var epoch = Epoch.J2000;
            var coordinates = new Coordinates(ra, dec, epoch, Coordinates.RAType.Degrees);

            Assert.AreEqual(Astrometry.DegreesToHours(ra), coordinates.RA, 0.0001);
            Assert.AreEqual(ra, coordinates.RADegrees, 0.0001);
            Assert.AreEqual(dec, coordinates.Dec, 0.0001);
            Assert.AreEqual(epoch, coordinates.Epoch);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Create_RAHoursTest(double ra, double dec) {
            var epoch = Epoch.JNOW;
            var coordinates = new Coordinates(ra, dec, epoch, Coordinates.RAType.Hours);

            Assert.AreEqual(ra, coordinates.RA, 0.0001);
            Assert.AreEqual(Astrometry.HoursToDegrees(ra), coordinates.RADegrees, 0.0001);
            Assert.AreEqual(dec, coordinates.Dec, 0.0001);
            Assert.AreEqual(epoch, coordinates.Epoch);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Transform_CelestialToApparentTest(double ra, double dec) {
            //Arrange
            var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);

            //Act
            coordinates = coordinates.Transform(Epoch.JNOW);

            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetJ2000(ra, dec);

            //Check with ascom transformation that the transformation logic matches
            Assert.AreEqual(transform.RAApparent, coordinates.RA, 0.0001);
            Assert.AreEqual(transform.DECApparent, coordinates.Dec, 0.0001);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Transform_ApparentToCelestialTest(double ra, double dec) {
            //Arrange
            var coordinates = new Coordinates(ra, dec, Epoch.JNOW, Coordinates.RAType.Hours);

            //Act
            coordinates = coordinates.Transform(Epoch.J2000);

            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetApparent(ra, dec);

            //Assert
            //Check with ascom transformation that the transformation logic matches
            Assert.AreEqual(transform.RAJ2000, coordinates.RA, 0.0001);
            Assert.AreEqual(transform.DecJ2000, coordinates.Dec, 0.0001);
        }
    }
}