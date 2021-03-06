using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Reflection;
using System;
using System.Windows.Data;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Windows;

namespace L3.Cargo.Common
{
    /// <summary>
    /// Wraps up XAML access to instance of WPFLocalize.Properties.Resources, list of available cultures, and method to change culture
    /// </summary>
    public class CultureResources
    {
        /// <summary>
        /// Flag to mark when the cultures have been found.
        /// </summary>
        private static bool bFoundInstalledCultures = false;

        /// <summary>
        /// List of the supported cultures.
        /// </summary>
        private static List<CultureInfo> pSupportedCultures = new List<CultureInfo>();
        /// <summary>
        /// List of available cultures, enumerated at startup
        /// </summary>
        public static List<CultureInfo> SupportedCultures
        {
            get { return pSupportedCultures; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CultureResources()
        {
            if (!bFoundInstalledCultures)
            {
                //determine which cultures are available to this application
                Debug.WriteLine("Get Installed cultures:");
                CultureInfo tCulture = new CultureInfo("");
                String[] dirs = Directory.GetDirectories(System.Windows.Forms.Application.StartupPath);
                foreach (string dir in dirs)
                {
                    try
                    {
                        //see if this directory corresponds to a valid culture name
                        DirectoryInfo dirinfo = new DirectoryInfo(dir);
                        tCulture = CultureInfo.GetCultureInfo(dirinfo.Name);

                        //determine if a resources dll exists in this directory that has the correct extension
                        if (dirinfo.GetFiles("*.resources.dll").Length > 0)
                        {
                            pSupportedCultures.Add(tCulture);
                            Debug.WriteLine(string.Format(" Found Culture: {0} [{1}]", tCulture.DisplayName, tCulture.Name));
                        }
                    }
                    catch (ArgumentException) //ignore exceptions generated for any unrelated directories in the bin folder
                    {
                    }
                }
                // set the language from the stored setting
                ChangeCulture(Properties.Settings.Default.Language);
                bFoundInstalledCultures = true;
            }
        }

        /// <summary>
        /// Local resources.
        /// </summary>
        private static Resources resources = null;
        /// <summary>
        /// The Resources ObjectDataProvider uses this method to get an instance of the WPFLocalize.Properties.Resources class
        /// </summary>
        /// <returns></returns>
        public static Resources GetResourceInstance()
        {
            if (resources == null)
            {
                resources = new Resources();
            }
            return resources;
        }

        /// <summary>
        /// List of the providers from the controls. This list is used to update the language on all the controls.
        /// </summary>
        private static List<ObjectDataProvider> providers = new List<ObjectDataProvider>();


        /// <summary>
        /// Change the current culture used in the application.
        /// If the desired culture is available all localized elements are updated.
        /// </summary>
        /// <param name="culture">Culture to change to</param>
        public static void ChangeCulture(CultureInfo culture)
        {
            //remain on the current culture if the desired culture cannot be found
            // - otherwise it would revert to the default resources set, which may or may not be desired.
            if (pSupportedCultures.Contains(culture))
            {
                // set the culture
                Resources.Culture = culture;
                // set the input language on the computer.
                InputLanguage.CurrentInputLanguage = InputLanguage.FromCulture(culture);
                // save the culture to settings so it is persisted
                Properties.Settings.Default.Language = culture;
                Properties.Settings.Default.Save();
                // Set the culture of the current thread and ui thread.
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                // update the providers
                foreach (ObjectDataProvider provider in providers)
                {
                    provider.Refresh();
                }
            }
            else
                Debug.WriteLine(string.Format("Culture [{0}] not available", culture));
        }

        /// <summary>
        /// Local ODP object.
        /// </summary>
        private static ObjectDataProvider odp;

        /// <summary>
        /// Gets an ODP to use for registering for language changing.
        /// </summary>
        /// <returns>The ODP.</returns>
        public static ObjectDataProvider getDataProvider()
        {
            if (odp == null)
            {
                odp = new ObjectDataProvider();
                odp.ObjectType = (new CultureResources()).GetType();
                odp.MethodName = "GetResourceInstance";
                registerDataProvider(odp);
            }
            return odp;
        }

        /// <summary>
        /// Gets the stored culture in the settings. This will be the last selected language and is persisted across shutdown.
        /// </summary>
        /// <returns>The currently or last set culture.</returns>
        public static CultureInfo getCultureSetting()
        {
            return Properties.Settings.Default.Language;
        }

        /// <summary>
        /// Framework views must be registered with this method to recieve updates when the language changes.
        /// </summary>
        /// <param name="frameworkElement">The framework element to register.</param>
        public static void registerDataProvider(FrameworkElement frameworkElement)
        {
            // deregister the provider when the element is unloaded
            frameworkElement.Unloaded += FrameworkElement_Unloaded;

            // register the data provider when the element is loaded
            frameworkElement.Loaded += FrameworkElement_Loaded;

            // register for visibility changes because an element can already be loaded but be invisible
            frameworkElement.IsVisibleChanged += frameworkElement_IsVisibleChanged;

            if (frameworkElement.IsLoaded)
            {
                // call the loaded event if it's already loaded
                FrameworkElement_Loaded(frameworkElement, null);
            }
        }

        /// <summary>
        /// Handles visibility changing of framework elements.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private static void frameworkElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement elem = sender as FrameworkElement;
            if (elem.IsVisible)
            {
                ObjectDataProvider provider = ((sender as FrameworkElement).Resources["Resources"] as ObjectDataProvider);
                provider.Refresh();
                CultureResources.registerDataProvider(provider);
            }
        }

        /// <summary>
        /// Handles loading of a framework element.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private static void FrameworkElement_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement elem = sender as FrameworkElement;
            if (elem.IsVisible == false)
            {
                // don't register the provider if this element is not visible. The problem is the element is never unloaded unless it is made
                // visible first. The visibility handler will register the provider when the element becomes visible.
                return;
            }
            ObjectDataProvider provider = (elem.Resources["Resources"] as ObjectDataProvider);
            provider.Refresh();
            CultureResources.registerDataProvider(provider);
        }

        /// <summary>
        /// Handles unloading of a framework element.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private static void FrameworkElement_Unloaded(object sender, RoutedEventArgs e)
        {
            ObjectDataProvider provider = ((sender as FrameworkElement).Resources["Resources"] as ObjectDataProvider);
            CultureResources.deregisterDataProvider(provider);
        }

        /// <summary>
        /// ODP for views must be registered with this method to recieve updates when the language changes.
        /// </summary>
        /// <param name="frameworkElement">The framework element to register.</param>
        private static void registerDataProvider(ObjectDataProvider provider)
        {
            if (!providers.Contains(provider))
            {
                providers.Add(provider);
            }
        }

        /// <summary>
        /// Deregisters an ODP from recieving language change events.
        /// </summary>
        /// <param name="provider">The ODP to deregister.</param>
        public static void deregisterDataProvider(ObjectDataProvider provider)
        {
            providers.Remove(provider);
        }

        /// <summary>
        /// Default culture.
        /// </summary>
        private static CultureInfo defaultCulture = new CultureInfo("en");
        /// <summary>
        /// Returns the culture info used by default. DatTimes are parsed from strings and need
        /// to be in a consistent culture to not cause errors.
        /// </summary>
        /// <returns>The culture info to use for parsing and storing Dates in string format.</returns>
        public static CultureInfo getDefaultDisplayCulture()
        {
            return defaultCulture;
        }

        /// <summary>
        /// Performs the standard formatting for converting date time to strings for use in data. This is used for
        /// have standard format for dates stored as strings in the behind-the-scenes data.
        /// </summary>
        /// <param name="dateTime">The date to convert</param>
        /// <returns>The date as a string for use in data.</returns>
        public static string ConvertDateTimeToStringForData(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffzzz", getDefaultDisplayCulture());
        }

        /// <summary>
        /// Performs the standard formatting for converting date time to strings for display in the currently
        /// selected language.
        /// </summary>
        /// <param name="dateTime">The date to convert</param>
        /// <returns>The date as a string for display purposes.</returns>
        public static string ConvertDateTimeToStringForDisplay(DateTime dateTime)
        {
            return dateTime.ToString(getCultureSetting());
        }
    }
}

