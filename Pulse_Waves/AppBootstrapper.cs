/*
 * Copyright © 2013 
 * Rowe Technology Inc.
 * All rights reserved.
 * http://www.rowetechinc.com
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification is NOT permitted.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
 * COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 * 
 * HISTORY
 * -----------------------------------------------------------------
 * Date            Initials    Version    Comments
 * -----------------------------------------------------------------
 * 10/15/2014      RC          0.0.1       Initial coding
 * 
 */

namespace RTI
{
    using System;
    using System.Collections.Generic;
    using Caliburn.Micro;
    using System.Windows;

    /// <summary>
    /// Start the application.
    /// </summary>
    public class AppBootstrapper : BootstrapperBase
    {
        SimpleContainer container;

        /// <summary>
        /// Initialize the object.
        /// </summary>
        public AppBootstrapper()
        {
            Initialize();
        }

        /// <summary>
        /// Called at startup to display the first view model.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<IShellViewModel>();
        }


        /// <summary>
        /// Configure.
        /// </summary>
        protected override void Configure()
        {

            // Found at https://github.com/gblmarquez/mui-sample-chat/blob/master/src/MuiChat.App/Bootstrapper.cs
            // Allows the CaliburnContentLoader to find the viewmodel based off the view string given
            // Used with ModernUI to navigate between views.
            ViewLocator.NameTransformer.AddRule(
                @"(?<nsbefore>([A-Za-z_]\w*\.)*)?(?<nsvm>ViewModels\.)(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)(?<suffix>ViewModel$)",
                @"${nsbefore}Views.${nsafter}${basename}View",
                @"(([A-Za-z_]\w*\.)*)?ViewModels\.([A-Za-z_]\w*\.)*[A-Za-z_]\w*ViewModel$"
                );


            container = new SimpleContainer();

            base.Configure();


            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.PerRequest<IShellViewModel, ShellViewModel>();

            // Singleton PulseManager
            ContainerExtensions.Singleton<PulseManager, PulseManager>(container);

            // Singleton AdcpConnection
            ContainerExtensions.Singleton<AdcpConnection, AdcpConnection>(container);

            // Singleton DvlSetupViewModel
            ContainerExtensions.PerRequest<NavBarViewModel, NavBarViewModel>(container);

            // Singleton DvlSetupViewModel
            ContainerExtensions.Singleton<WavesSetupViewModel, WavesSetupViewModel>(container);

            // Singleton DvlSetupViewModel
            ContainerExtensions.Singleton<RecoverDataViewModel, RecoverDataViewModel>(container);

            // Singleton DvlSetupViewModel
            ContainerExtensions.Singleton<UpdateFirmwareViewModel, UpdateFirmwareViewModel>(container);

            // Singleton PlaybackViewModel
            ContainerExtensions.Singleton<PlaybackViewModel, PlaybackViewModel>(container);

            // Singleton ViewDataWavesViewModel
            ContainerExtensions.Singleton<ViewDataWavesViewModel, ViewDataWavesViewModel>(container);

            // Singleton CompassCalViewModel
            ContainerExtensions.Singleton<CompassCalViewModel, CompassCalViewModel>(container);

            // Singleton CompassUtilityViewModel
            ContainerExtensions.Singleton<CompassUtilityViewModel, CompassUtilityViewModel>(container);

            // Singleton TerminalViewModel
            ContainerExtensions.Singleton<TerminalViewModel, TerminalViewModel>(container);

            // Singleton ScreenDataBaseViewModel
            ContainerExtensions.Singleton<ScreenDataBaseViewModel, ScreenDataBaseViewModel>(container);

            // Singleton AveragingBaseViewModel
            ContainerExtensions.Singleton<AveragingBaseViewModel, AveragingBaseViewModel>(container);

            // Singleton DataFormatViewModel
            ContainerExtensions.Singleton<DataFormatViewModel, DataFormatViewModel>(container);
        }

        /// <summary>
        /// Select the assemblies.
        /// </summary>
        /// <returns>List of all the assemblies.</returns>
        protected override IEnumerable<System.Reflection.Assembly> SelectAssemblies()
        {
            //var a = base.SelectAssemblies();
            //return a;

            var list = new List<System.Reflection.Assembly>();

            // Add the Vault EXE
            list.Add(System.Reflection.Assembly.GetExecutingAssembly());

            // Add the Pulse_Display DLL
            var refs = System.Reflection.Assembly.GetEntryAssembly().GetReferencedAssemblies();
            foreach (var asmName in System.Reflection.Assembly.GetEntryAssembly().GetReferencedAssemblies())
            {
                var asm = System.Reflection.Assembly.Load(asmName);
                if (asm.GetName().ToString().Contains("Pulse_Display"))
                {
                    list.Add(asm);
                }
            }

            return list;
        }

        /// <summary>
        /// Get Instance.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected override object GetInstance(Type service, string key)
        {
            var instance = container.GetInstance(service, key);
            if (instance != null)
                return instance;

            throw new InvalidOperationException("Could not locate any instances.");
        }

        /// <summary>
        /// Get all instances.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        /// <summary>
        /// Buildup.
        /// </summary>
        /// <param name="instance"></param>
        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }
    }
}