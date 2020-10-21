﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyFilterWheel;
using NINA.Profile;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.WindowService;
using NINA.ViewModel;
using NINA.ViewModel.AutoFocus;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINATest.Sequencer.Trigger.Autofocus {

    [TestFixture]
    public class AutofocusAfterTimeTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<IImageHistoryVM> historyMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            historyMock = new Mock<IImageHistoryVM>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyCamera.CameraInfo { Connected = true });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyFocuser.FocuserInfo { Connected = true });
        }

        [Test]
        public void CloneTest() {
            var initial = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (AutofocusAfterTimeTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        [TestCase(30, 10, true)]
        [TestCase(10, 10, true)]
        [TestCase(0, 10, false)]
        [TestCase(5, 6, false)]
        [TestCase(6, 5, true)]
        public void ShouldTrigger_LastAFAsPerInput_ReturnExpected(double lastAFTime, double afterAFTime, bool shouldTrigger) {
            var afHistory = new NINA.Utility.AsyncObservableCollection<ImageHistoryPoint>();
            var report = new AutoFocusReport() { Timestamp = DateTime.Now - TimeSpan.FromMinutes(lastAFTime) };
            var point = new ImageHistoryPoint(0, null);
            point.PopulateAFPoint(report);
            afHistory.Add(point);

            var afTrigger = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            afTrigger.Amount = TimeSpan.FromMinutes(afterAFTime).TotalMinutes;

            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(afHistory);

            var should = afTrigger.ShouldTrigger(null);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(10, 15, true)]
        [TestCase(100, 5, false)]
        [TestCase(10, 10, true)]
        public async Task ShouldTrigger_NoLastAFRun(double milliseconds, double initDelay, bool shouldTrigger) {
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new NINA.Utility.AsyncObservableCollection<ImageHistoryPoint>());
            var afTrigger = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            afTrigger.Amount = TimeSpan.FromMilliseconds(milliseconds).TotalMinutes;

            afTrigger.Initialize();

            await Task.Delay(TimeSpan.FromMilliseconds(initDelay));

            var should = afTrigger.ShouldTrigger(null);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var report = new AutoFocusReport();

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });

            var sut = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);

            await sut.Execute(default, default, default);

            // Todo proper assertion
            // historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var sut = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterTimeTrigger, Amount: 30s");
        }
    }
}