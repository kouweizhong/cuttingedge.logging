﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration.Provider;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace CuttingEdge.Logging
{
    /// <summary>
    /// Forwards logging information to other configured logging providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <b>CompositeLoggingProvider</b> allows duplication and distribution of log events over multiple
    /// other logging providers. In conjunction with the usage of the
    /// <see cref="LoggingProviderBase.Threshold">Threshold</see> attribute, this enables scenario's where
    /// users can log all events to a particular destination (such as SQL server) and at
    /// the same time log those events of a particular severity to another destination (such as sending by
    /// mail).</para>
    /// <para>
    /// When a single provider is configured to which the <b>CompositeLoggingProvider</b> should forward
    /// logging events and it succeeds forwarding an event to that configured provider, the
    /// <b>CompositeLoggingProvider</b> will return the identifier returned that configured provider, or null
    /// when that provider returned null. When multiple providers are configured to which the 
    /// <b>CompositeLoggingProvider</b> should forward logging events, it will always return null.
    /// </para>
    /// <para>
    /// When the <b>CompositeLoggingProvider</b>'s <see cref="LoggingProviderBase.Threshold">Threshold</see>
    /// was set lower or equals to the event's <see cref="LogEntry.Severity">Severity</see>, it will log that
    /// event to all configured providers. The order in which the providers are called to log is determined by
    /// the configuration. Providers are ordered based on their number in the configuration. The attributes
    /// contain a number. See the list with the attribute specification for more information about this.</para>
    /// <para>
    /// Even when one of the providers fails (by throwing an exception) during the forwarding process, the 
    /// <b>CompositeLoggingProvider</b> will continue forwarding the event to the other providers. Because 
    /// multiple providers could fail, multiple exceptions could be thrown. In the situation where multiple
    /// providers are configured, and one or more of those providers fail, the <b>CompositeLoggingProvider</b>
    /// will always wrap those exceptions (even if it is a single exception) in a
    /// <see cref="CompositeException" />. This <see cref="CompositeException" /> will be forwarded to the 
    /// configured <see cref="LoggingProviderBase.FallbackProvider">FallbackProvider</see> or, when no
    /// fallback provider is configured, thrown from the provider. When just a single provider is configured, 
    /// the <b>CompositeLoggingProvider</b> will not wrap the thrown exception, but simply forward that to the 
    /// <see cref="LoggingProviderBase.FallbackProvider">FallbackProvider</see> or, when no fallback provider
    /// is configured, let that exception bubble up the call stack.
    /// </para>
    /// <para>
    /// The table below shows the list of valid attributes for the <b>CompositeLoggingProvider</b>
    /// configuration:
    /// <list type="table">  
    /// <listheader>
    ///     <attribute>Attribute</attribute>
    ///     <description>Description</description>
    /// </listheader>
    /// <item>
    ///     <attribute>name</attribute>
    ///     <description>
    ///         The name of the provider. This attribute is mandatory.
    ///     </description>
    /// </item>
    /// <item>
    ///     <attribute>description</attribute>
    ///     <description>
    ///         A description of the provider. This attribute is optional.
    ///     </description>
    /// </item>
    /// <item>
    ///     <attribute>fallbackProvider</attribute>
    ///     <description>
    ///         A fallback provider that this provider will use when logging failed. The value must contain 
    ///         the name of an existing logging provider. This attribute is optional.
    ///     </description>
    /// </item>  
    /// <item>
    ///     <attribute>threshold</attribute>
    ///     <description>
    ///         The logging threshold. The threshold limits the number of events logged. The threshold can be
    ///         defined as follows: Debug &lt; Information &lt; Warning &lt; Error &lt; Critical. i.e., When
    ///         the threshold is set to Information, events with a severity of Debug  will not be logged. When
    ///         no value is specified, all events are logged. This attribute is optional.
    ///      </description>
    /// </item>  
    /// <item>
    ///     <attribute>provider[n]</attribute>
    ///     <description>
    ///         A provider that will be used to forward logging information to, where [n] is a arbitrary
    ///         number. The value must contain the name of an existing logging provider. Multiple attributes
    ///         can be specified, as long as [n] is unique. At Least one attribute must be specified.
    ///     </description>
    /// </item>  
    /// </list>
    /// The attributes can be specified within the provider configuration. See the example below on how to
    /// use.
    /// </para>
    /// <example>
    /// <para>
    /// This example demonstrates how to specify values declaratively for several attributes of the logging
    /// section, which can also be accessed as members of the <see cref="LoggingSection"/> class. The
    /// following configuration file example shows how to specify values declaratively for the logging
    /// section.</para>
    /// <para>
    /// In this example a <b>CompositeLoggingProvider</b> is configured as default provider and it forwards
    /// all events to the configured <see cref="SqlLoggingProvider"/> and <see cref="MailLoggingProvider"/>.
    /// The <see cref="SqlLoggingProvider"/> will store all events in the database. The 
    /// <see cref="MailLoggingProvider"/> is configured to mail only events with a severity of 
    /// <see cref="LoggingEventType.Error">Error</see> and <see cref="LoggingEventType.Critical">Critical</see>
    /// to a specified mail address.</para>
    /// <para>
    /// <code lang="xml"><![CDATA[
    /// <?xml version="1.0"?>
    /// <configuration>
    ///     <configSections>
    ///         <section name="logging" type="CuttingEdge.Logging.LoggingSection, CuttingEdge.Logging" />
    ///     </configSections>
    ///     <connectionStrings>
    ///         <add name="SqlLoggingConnection" 
    ///             connectionString="Data Source=.;Integrated Security=SSPI;Initial Catalog=Logging;" />
    ///     </connectionStrings>
    ///     <logging defaultProvider="CompositeLoggingProvider">
    ///         <providers>
    ///             <add 
    ///                 name="CompositeLoggingProvider"
    ///                 type="CuttingEdge.Logging.CompositeLoggingProvider, CuttingEdge.Logging"
    ///                 threshold="Debug"
    ///                 provider1="SqlLoggingProvider"
    ///                 provider2="MailLoggingProvider"
    ///                 description="Composite logging provider"
    ///             />
    ///             <add 
    ///                 name="SqlLoggingProvider"
    ///                 type="CuttingEdge.Logging.SqlLoggingProvider, CuttingEdge.Logging"
    ///                 connectionStringName="SqlLoggingConnection"
    ///             />
    ///             <add 
    ///                 name="MailLoggingProvider"
    ///                 type="CuttingEdge.Logging.MailLoggingProvider, CuttingEdge.Logging"
    ///                 threshold="Error"
    ///                 to="support@company.com"
    ///             />
    ///         </providers>
    ///     </logging>
    ///     <system.net>
    ///         <mailSettings>
    ///             <smtp from="test@foo.com">
    ///                 <network
    ///                     host="smtpserver1" 
    ///                     port="25" 
    ///                     userName="john" 
    ///                     password="secret" 
    ///                     defaultCredentials="true"
    ///                 />
    ///             </smtp>
    ///         </mailSettings>
    ///     </system.net>   
    /// </configuration>
    /// ]]></code>
    /// </para>
    /// </example>
    /// </remarks>
    public class CompositeLoggingProvider : LoggingProviderBase
    {
        private const string ProviderAttributePrefix = "provider";
        private List<string> providerNames;
        private ReadOnlyCollection<LoggingProviderBase> providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLoggingProvider"/> class. Please do not use 
        /// this constructor to initialize this type. Use one of the overloaded constructors instead.
        /// </summary>
        public CompositeLoggingProvider()
        {
            // Set Initialized to false explicitly to prevent this type from being used until it is correctly
            // initialized using Initialize().
            this.SetInitialized(false);
        }

        /// <summary>Initializes a new instance of the <see cref="CompositeLoggingProvider"/> class.</summary>
        /// <param name="threshold">The <see cref="LoggingEventType"/> logging threshold. The threshold limits
        /// the number of event logged. <see cref="LoggingProviderBase.Threshold">Threshold</see> for more 
        /// information.</param>
        /// <param name="fallbackProvider">The optional fallback provider.</param>
        /// <param name="providers">The providers to which events will be forwarded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="providers"/> argument is
        /// a null reference (Nothing in VB).</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="providers"/> contains
        /// duplicate elements, null references or no elements.</exception>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="threshold"/> has an
        /// invalid value.</exception>
        public CompositeLoggingProvider(LoggingEventType threshold, LoggingProviderBase fallbackProvider,
            params LoggingProviderBase[] providers)
            : this(threshold, fallbackProvider, (IEnumerable<LoggingProviderBase>)providers)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CompositeLoggingProvider"/> class.</summary>
        /// <param name="threshold">The <see cref="LoggingEventType"/> logging threshold. The threshold limits
        /// the number of event logged. <see cref="LoggingProviderBase.Threshold">Threshold</see> for more 
        /// information.</param>
        /// <param name="fallbackProvider">The optional fallback provider.</param>
        /// <param name="providers">The providers to which events will be forwarded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="providers"/> argument is
        /// a null reference (Nothing in VB).</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="providers"/> contains
        /// duplicate elements, null references or no elements.</exception>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="threshold"/> has an
        /// invalid value.</exception>
        public CompositeLoggingProvider(LoggingEventType threshold, LoggingProviderBase fallbackProvider,
            IEnumerable<LoggingProviderBase> providers)
            : base(threshold, fallbackProvider)
        {
            if (providers == null)
            {
                throw new ArgumentNullException("providers");
            }

            // Make a copy to prevent changes during or after validation.
            var providerList = new List<LoggingProviderBase>(providers);

            if (providerList.Contains(null))
            {
                throw new ArgumentException(SR.CollectionMustNotContainNullElements(), "providers");
            }

            // We can't use HashSet<T> here, because it is a .NET 3.5 call and we need to stay compatible
            // with .NET 2.0.
            var set = new Dictionary<object, object>();

            foreach (var provider in providerList)
            {
                // Add provider.
                set[provider] = null;
            }

            if (set.Count != providerList.Count)
            {
                throw new ArgumentException(SR.CollectionMustNotContainDuplicates(), "providers");
            }

            if (providerList.Count == 0)
            {
                throw new ArgumentException(SR.CollectionShouldContainAtleastOneElement(), "providers");
            }

            this.providers = new ReadOnlyCollection<LoggingProviderBase>(providerList.ToArray());
        }

        /// <summary>
        /// Gets the list of logging provider to which the logging events will be forwarded. This list will
        /// contain at least one provider.
        /// </summary>
        /// <value>The read only list of <see cref="LoggingProviderBase"/> objects.</value>
        /// <exception cref="InvalidOperationException">Thrown when the provider hasn't been initialized
        /// properly.</exception>
        public ReadOnlyCollection<LoggingProviderBase> Providers
        {
            get
            {
                if (this.providers == null)
                {
                    throw new InvalidOperationException(
                        SR.ProviderHasNotBeenInitializedCorrectlyCallAnOverloadedConstructor(this));
                }

                return this.providers;
            }
        }

        /// <summary>Initializes the provider.</summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">A collection of the name/value pairs representing the provider-specific
        /// attributes specified in the configuration for this provider.</param>
        /// <exception cref="ArgumentNullException">Thrown when the name of the provider is null or when the
        /// <paramref name="config"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the name of the provider has a length of zero.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an attempt is made to call Initialize on a
        /// provider after the provider has already been initialized.</exception>
        /// <exception cref="ProviderException">Thrown when the <paramref name="config"/> contains
        /// unrecognized attributes.</exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            LoggingHelper.SetDescriptionWhenMissing(config, "Composite logging provider");

            // Call base initialize first. This method prevents initialize from being called more than once.
            base.Initialize(name, config);

            // Performing implementation-specific provider initialization here (this is the 1st part).
            this.InitializeProviderNames(config);

            // Always call this method last
            this.CheckForUnrecognizedAttributes(name, config);
        }

        internal override void CompleteInitialization(LoggingProviderCollection configuredProviders, 
            LoggingProviderBase defaultProvider)
        {
            // Finish performing implementation-specific provider initialization here (this is 2nd/last part).
            // This operation has to be executed here, because during Initialize this list of configured is
            // providers not yet available.
            this.InitializeProviders(configuredProviders);

            this.SetInitialized(true);
        }

        internal override List<LoggingProviderBase> GetReferencedProviders()
        {
            List<LoggingProviderBase> referencedProviders = base.GetReferencedProviders();

            if (this.providers != null)
            {
                referencedProviders.AddRange(this.providers);
            }

            return referencedProviders;
        }

        /// <summary>Forwards the supplied <paramref name="entry"/> to all configured <see cref="Providers"/>.
        /// </summary>
        /// <param name="entry">The entry to log.</param>
        /// <returns>
        /// When a single provider is configured and forwarding the <paramref name="entry"/> succeeds, this
        /// method will return the value obtained from that provider (which could be null). When multiple
        /// providers are configured, this method will always return null.
        /// </returns>
        /// <exception cref="CompositeException">Thrown when one multiple <see cref="Providers"/> are
        /// configured and one (or more) providers fail.</exception>
        /// <exception cref="Exception">Thrown when a single provider is configured in the list of
        /// <see cref="Providers"/>. The exact type of exception being thrown depends on the configured
        /// provider.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is a null (Nothing
        /// in VB) reference.</exception>
        protected override object LogInternal(LogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (this.Providers.Count == 1)
            {
                return this.ForwardToSingleProvider(entry);
            }
            else
            {
                this.ForwardToMultipleProviders(entry);

                return null;
            }
        }

        private static bool IsValidProviderAttributeName(string attributeName)
        {
            if (!attributeName.StartsWith(ProviderAttributePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string number = attributeName.Substring(ProviderAttributePrefix.Length);

            int n;

            return int.TryParse(number, out n);
        }

        private static void SortProviderAttributes(List<string> providerNames)
        {
            // The attributes should be sorted in the following order 'provider1, provider2, provider3, ...'
            // While to CompositeLoggingProvider sends logs to all registered providers, even in case of a
            // failing provider, users might expect the logging to take place in the particular order they
            // specified (and this expectation is documented).
            providerNames.Sort(CompareProviderAttributeNames);
        }

        private static int CompareProviderAttributeNames(string attribute1, string attribute2)
        {
            IFormatProvider invariant = CultureInfo.InvariantCulture;

            int attribute1Number = int.Parse(attribute1.Substring(ProviderAttributePrefix.Length), invariant);
            int attribute2Number = int.Parse(attribute2.Substring(ProviderAttributePrefix.Length), invariant);

            return attribute1Number - attribute2Number;
        }

        private static string BuildExceptionMessage(List<Exception> thrownExceptions)
        {
            var exceptionMessages = new List<string>();

            foreach (var exception in thrownExceptions)
            {
                exceptionMessages.Add(exception.Message);
            }

            var extraInformation = string.Join(" ", exceptionMessages.ToArray());
            
            return SR.LoggingFailed(extraInformation);
        }

        private object ForwardToSingleProvider(LogEntry entry)
        {
            // When the composite logger only references a single provider, we can simply forward the entry
            // to that provider, without having to worry about calling all providers and wrapping thrown
            // exceptions.
            ILogger provider = this.Providers[0];

            return provider.Log(entry);
        }

        private void ForwardToMultipleProviders(LogEntry entry)
        {
            List<Exception> thrownExceptions = this.LogToMultipleProvidersGetThrownExceptions(entry);

            ThrowCompositeExceptionWhenExceptionsAreThrown(thrownExceptions);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Catching the Exception base class is not a problem, " +
                "because we will wrap and re throw these caught exceptions.")]
        private List<Exception> LogToMultipleProvidersGetThrownExceptions(LogEntry entry)
        {
            List<Exception> thrownExceptions = null;

            foreach (ILogger provider in this.Providers)
            {
                try
                {
                    provider.Log(entry);
                }
                catch (Exception ex)
                {
                    if (thrownExceptions == null)
                    {
                        thrownExceptions = new List<Exception>();
                    }

                    thrownExceptions.Add(ex);
                }
            }

            return thrownExceptions;
        }

        private static void ThrowCompositeExceptionWhenExceptionsAreThrown(List<Exception> thrownExceptions)
        {
            if (thrownExceptions != null && thrownExceptions.Count > 0)
            {
                string exceptionMessage = BuildExceptionMessage(thrownExceptions);

                // Here we would like to be able to throw an AggregateException, but it's a .NET 4.0 feature
                // :-(. Please see the NOTE on the CompositeException for more information.
                // Note that we have to wrap the exceptions in a new exception, even if there is only one
                // failing provider, because re throwing that same exception will make us loose the call stack.
                throw new CompositeException(exceptionMessage, thrownExceptions);
            }
        }

        private void InitializeProviderNames(NameValueCollection config)
        {
            this.providerNames = this.GetConfiguredProviderNames(config);

            this.RemoveProviderAttributesFromConfiguration(config);
        }

        private List<string> GetConfiguredProviderNames(NameValueCollection config)
        {
            var providerNames = new List<string>();

            var providerAttributes = this.GetProviderAttributesFromConfiguration(config);

            foreach (string attribute in providerAttributes)
            {
                string providerName = config[attribute];

                if (providerNames.Contains(providerName, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ProviderException(SR.ProviderReferencedMultipleTimes(this, providerName));
                }

                providerNames.Add(providerName);
            }

            return providerNames;
        }

        private List<string> GetProviderAttributesFromConfiguration(NameValueCollection config)
        {
            List<string> providerAttributes = new List<string>();

            foreach (string key in config.Keys)
            {
                if (IsValidProviderAttributeName(key))
                {
                    providerAttributes.Add(key);
                }
            }

            if (providerAttributes.Count == 0)
            {
                throw new ProviderException(SR.CompositeLoggingProviderDoesNotReferenceAnyProviders(this));
            }

            SortProviderAttributes(providerAttributes);

            return providerAttributes;
        }

        private void RemoveProviderAttributesFromConfiguration(NameValueCollection config)
        {
            var providerAttributes = this.GetProviderAttributesFromConfiguration(config);

            foreach (string key in providerAttributes)
            {
                config.Remove(key);
            }
        }

        private void InitializeProviders(LoggingProviderCollection configuredProviders)
        {
            var providers = this.GetReferencedProviders(configuredProviders);

            this.providers = new ReadOnlyCollection<LoggingProviderBase>(providers);
        }

        private LoggingProviderBase[] GetReferencedProviders(LoggingProviderCollection configuredProviders)
        {
            var providers = new List<LoggingProviderBase>();

            foreach (string providerName in this.providerNames)
            {
                var provider = configuredProviders[providerName];

                if (configuredProviders[providerName] == null)
                {
                    throw new ProviderException(SR.ReferencedProviderDoesNotExist(this, providerName));
                }

                providers.Add(provider);
            }

            return providers.ToArray();
        }
    }
}