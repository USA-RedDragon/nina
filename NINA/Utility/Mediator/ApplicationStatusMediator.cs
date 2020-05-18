#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;

namespace NINA.Utility.Mediator {

    internal class ApplicationStatusMediator : IApplicationStatusMediator {
        protected IApplicationStatusVM handler;

        public void RegisterHandler(IApplicationStatusVM handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
        }

        public void StatusUpdate(ApplicationStatus status) {
            handler.StatusUpdate(status);
        }
    }
}
