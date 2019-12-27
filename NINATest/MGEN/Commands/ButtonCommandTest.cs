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

using FluentAssertions;
using FTD2XX_NET;
using Moq;
using NINA.MGEN;
using NINA.MGEN.Commands.AppMode;

using NINA.MGEN.Exceptions;

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.MGEN.Commands {

    [TestFixture]
    public class ButtonCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new ButtonCommand(0);

            sut.CommandCode.Should().Be(0x5d);
            sut.AcknowledgeCode.Should().Be(0x5d);
            sut.SubCommandCode.Should().Be(0x01);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Successful_Scenario_Test(byte button) {
            SetupWrite(ftdiMock, new byte[] { 0x5d }, new byte[] { 0x01 }, new byte[] { button });
            SetupRead(ftdiMock, new byte[] { 0x5d }, new byte[] { 0x00 });

            var sut = new ButtonCommand((MGENButton)button);
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
        }
    }
}