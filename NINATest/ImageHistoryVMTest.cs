﻿using FluentAssertions;
using Moq;
using NINA.Model.ImageData;
using NINA.Profile;
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class ImageHistoryVMTest {
        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();

        [Test]
        public void ImageHistory_ConcurrentId_Order_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);

            Parallel.For(0, 100, (i) => {
                sut.Add(new StarDetectionAnalysis() { DetectedStars = i, HFR = i });
            });

            for (int i = 0; i < 100; i++) {
                sut.ImageHistory[i].Id.Should().Be(i + 1);
            }
        }

        [Test]
        public void ImageHistory_Value_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);
            var hfr = 10.1234;
            var stars = 12323;

            sut.Add(new StarDetectionAnalysis() { DetectedStars = stars, HFR = hfr });

            sut.LimitedImageHistoryStack.First().Value.HFR.Should().Be(hfr);
            sut.LimitedImageHistoryStack.First().Value.DetectedStars.Should().Be(stars);
            sut.ImageHistory[0].HFR.Should().Be(hfr);
            sut.ImageHistory[0].DetectedStars.Should().Be(stars);
        }

        [Test]
        public void ImageHistory_LimitedStack_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);

            Parallel.For(0, 300, (i) => {
                sut.Add(new StarDetectionAnalysis() { DetectedStars = i, HFR = i });
            });

            sut.LimitedImageHistoryStack.Count.Should().Be(100);
            sut.ImageHistory.Count.Should().Be(300);
        }

        [Test]
        public void ImageHistory_ClearPlot_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);

            Parallel.For(0, 100, (i) => {
                sut.Add(new StarDetectionAnalysis() { DetectedStars = i, HFR = i });
            });

            sut.PlotClear();

            sut.LimitedImageHistoryStack.Count.Should().Be(0);
            sut.ImageHistory.Count.Should().Be(100);
        }
    }
}