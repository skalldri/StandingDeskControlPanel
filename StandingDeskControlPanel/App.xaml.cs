﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Media.SpeechRecognition;
using StandingDeskControlPanel.Common;

namespace StandingDeskControlPanel
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Navigation service, provides a decoupled way to trigger the UI Frame
        /// to transition between views.
        /// </summary>
        public static NavigationService NavigationService { get; private set; }

        private RootFrameNavigationHelper rootFrameNavigationHelper;

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(View.Home), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            try
            {
                // Install the main VCD. 
                StorageFile vcdStorageFile = await Package.Current.InstalledLocation.GetFileAsync(@"StandingDeskControlPanelVoiceCommands.xml");

                await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(vcdStorageFile);
                
                // This is where we update the voice command definition so that we include custom targets. The voice commands contain 
                // {height} slots, and there is a fixed number of words which work in that slot.
                // We call this method to update the pre-defined list of words which work in that slot so that anything the user defines
                // will properly trigger Cortana.

                /*
                 * 
                // Update phrase list.
                ViewModel.ViewModelLocator locator = App.Current.Resources["ViewModelLocator"] as ViewModel.ViewModelLocator;
                if (locator != null)
                {
                    await locator.TripViewModel.UpdateDestinationPhraseList();
                }

                */
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Installing Voice Commands Failed: " + ex.ToString());
            }

        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// OnActivated is the entry point for an application when it is launched via
        /// means other normal user interaction. This includes Voice Commands, URI activation,
        /// being used as a share target from another app, etc. Here, we're going to handle the
        /// Voice Command activation from Cortana.
        /// 
        /// Note: Be aware that an older VCD could still be in place for your application if you
        /// modify it and update your app via the store. You should be aware that you could get 
        /// activations that include commands in older versions of your VCD, and you should try
        /// to handle them gracefully.
        /// </summary>
        /// <param name="args">Details about the activation method, including the activation
        /// phrase (for voice commands) and the semantic interpretation, parameters, etc.</param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            Type navigationToPageType;
            ViewModel.VoiceCommandData? navigationCommand = null;

            // If the app was launched via a Voice Command, this corresponds to the "show trip to <location>" command. 
            // Protocol activation occurs when a tile is clicked within Cortana (via the background task)
            if (args.Kind == ActivationKind.VoiceCommand)
            {
                // The arguments can represent many different activation types. Cast it so we can get the
                // parameters we care about out.
                var commandArgs = args as VoiceCommandActivatedEventArgs;

                Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = commandArgs.Result;

                // Get the name of the voice command and the text spoken. See AdventureWorksCommands.xml for
                // the <Command> tags this can be filled with.
                string voiceCommandName = speechRecognitionResult.RulePath[0];
                string textSpoken = speechRecognitionResult.Text;

                // The commandMode is either "voice" or "text", and it indictes how the voice command
                // was entered by the user.
                // Apps should respect "text" mode by providing feedback in silent form.
                string commandMode = this.SemanticInterpretation("commandMode", speechRecognitionResult);

                switch (voiceCommandName)
                {
                    /* Commenting out for now since there's only background service Cortana pages
                     * case "showTripToDestination":
                        // Access the value of the {destination} phrase in the voice command
                        string destination = this.SemanticInterpretation("destination", speechRecognitionResult);

                        // Create a navigation command object to pass to the page. Any object can be passed in,
                        // here we're using a simple struct.
                        navigationCommand = new ViewModel.VoiceCommandData(
                            voiceCommandName,
                            commandMode,
                            textSpoken,
                            destination);

                        // Set the page to navigate to for this voice command.
                        navigationToPageType = typeof(View.TripDetails);
                        break; */
                    default:
                        // If we can't determine what page to launch, go to the default entry point.
                        navigationToPageType = typeof(View.Home);
                        break;
                }
            }
            else if (args.Kind == ActivationKind.Protocol)
            {
                // Extract the launch context. In this case, we're just using the destination from the phrase set (passed
                // along in the background task inside Cortana), which makes no attempt to be unique. A unique id or 
                // identifier is ideal for more complex scenarios. We let the destination page check if the 
                // destination trip still exists, and navigate back to the trip list if it doesn't.
                var commandArgs = args as ProtocolActivatedEventArgs;
                Windows.Foundation.WwwFormUrlDecoder decoder = new Windows.Foundation.WwwFormUrlDecoder(commandArgs.Uri.Query);
                var destination = decoder.GetFirstValueByName("LaunchContext");

                navigationCommand = new ViewModel.VoiceCommandData(
                                        "protocolLaunch",
                                        "text",
                                        "destination",
                                        destination);

                navigationToPageType = typeof(View.Home);
            }
            else
            {
                // If we were launched via any other mechanism, fall back to the main page view.
                // Otherwise, we'll hang at a splash screen.
                navigationToPageType = typeof(View.Home);
            }

            // Repeat the same basic initialization as OnLaunched() above, taking into account whether
            // or not the app is already active.
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                App.NavigationService = new NavigationService(rootFrame);

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            // Since we're expecting to always show a details page, navigate even if 
            // a content frame is in place (unlike OnLaunched).
            // Navigate to either the main trip list page, or if a valid voice command
            // was provided, to the details page for that trip.
            rootFrame.Navigate(navigationToPageType, navigationCommand);

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Returns the semantic interpretation of a speech result. Returns null if there is no interpretation for
        /// that key.
        /// </summary>
        /// <param name="interpretationKey">The interpretation key.</param>
        /// <param name="speechRecognitionResult">The result to get an interpretation from.</param>
        /// <returns></returns>
        private string SemanticInterpretation(string interpretationKey, SpeechRecognitionResult speechRecognitionResult)
        {
            return speechRecognitionResult.SemanticInterpretation.Properties[interpretationKey].FirstOrDefault();
        }
    }
}
