﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Sequencer.SequenceItem.Focuser {

    [TestFixture]
    internal class MoveFocuserAbsoluteTest {
        public Mock<IFocuserMediator> focuserMediatorMock;

        [SetUp]
        public void Setup() {
            focuserMediatorMock = new Mock<IFocuserMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new MoveFocuserAbsolute(focuserMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (MoveFocuserAbsolute)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Position.Should().Be(sut.Position);
        }

        [Test]
        public void Validate_NoIssues() {
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true });

            var sut = new MoveFocuserAbsolute(focuserMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = false });

            var sut = new MoveFocuserAbsolute(focuserMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(0)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task Execute_NoIssues_LogicCalled(int position) {
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true });

            var sut = new MoveFocuserAbsolute(focuserMediatorMock.Object);
            sut.Position = position;
            await sut.Execute(default, default);

            focuserMediatorMock.Verify(x => x.MoveFocuser(It.Is<int>(p => p == position), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public Task Execute_HasIssues_LogicNotCalled() {
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = false });

            var sut = new MoveFocuserAbsolute(focuserMediatorMock.Object);
            sut.Position = 10;
            Func<Task> act = () => { return sut.Execute(default, default); };

            focuserMediatorMock.Verify(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            return act.Should().ThrowAsync<SequenceItemSkippedException>(string.Join(",", sut.Issues));
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new MoveFocuserAbsolute(focuserMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}