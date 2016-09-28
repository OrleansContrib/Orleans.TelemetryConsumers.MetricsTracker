using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using Orleans;
using Orleans.Runtime;
using Orleans.Providers;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public class MetricsTrackerTelemetryConsumer : IEventTelemetryConsumer, IExceptionTelemetryConsumer,
        IDependencyTelemetryConsumer, IMetricTelemetryConsumer, IRequestTelemetryConsumer
    {
        Logger logger;

        IProviderRuntime Runtime;

        IManagementGrain Management;

        InvokeInterceptor PreviousInterceptor;

        MetricsConfiguration Configuration;
        DateTime LastConfigCheck = DateTime.MinValue;

        object CountersLock = new object();
        ConcurrentDictionary<string, long> Counters;
        ConcurrentDictionary<string, ConcurrentQueue<long>> CounterHistory;

        object MetricsLock = new object();
        ConcurrentDictionary<string, double> Metrics;
        ConcurrentDictionary<string, ConcurrentQueue<double>> MetricHistory;

        object TimeSpanMetricsLock = new object();
        ConcurrentDictionary<string, TimeSpan> TimeSpanMetrics;
        ConcurrentDictionary<string, ConcurrentQueue<TimeSpan>> TimeSpanMetricHistory;

        object RequestsLock = new object();
        ConcurrentDictionary<string, MeasuredRequest> Requests;
        ConcurrentDictionary<string, ConcurrentQueue<MeasuredRequest>> RequestHistory;

        public MetricsTrackerTelemetryConsumer(IProviderRuntime runtime)
        {
            try
            {
                Runtime = runtime;

                logger = Runtime.GetLogger(nameof(MetricsTrackerTelemetryConsumer));

                Configuration = new MetricsConfiguration();

                Counters = new ConcurrentDictionary<string, long>();
                CounterHistory = new ConcurrentDictionary<string, ConcurrentQueue<long>>();

                Metrics = new ConcurrentDictionary<string, double>();
                MetricHistory = new ConcurrentDictionary<string, ConcurrentQueue<double>>();

                TimeSpanMetrics = new ConcurrentDictionary<string, TimeSpan>();
                TimeSpanMetricHistory = new ConcurrentDictionary<string, ConcurrentQueue<TimeSpan>>();

                Requests = new ConcurrentDictionary<string, MeasuredRequest>();
                RequestHistory = new ConcurrentDictionary<string, ConcurrentQueue<MeasuredRequest>>();

                PreviousInterceptor = Runtime.GetInvokeInterceptor();

                // start a message pump to give ourselves the right synchronization context
                // from which we can communicate with grains via normal grain references
                // TODO: don't start the pump until it's been requested
                StartMessagePump().Ignore();

                Management = Runtime.GrainFactory.GetGrain<IManagementGrain>(0);
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }
        
        private void ConfigureSiloInterceptor(bool enable = true)
        {
            // TODO: harden this for production use
            // TODO: add tracking of the time each grain method call took to complete
            // TODO: make this optional, enabled or disabled via ClusterMetricsGrain calls
            Runtime.SetInvokeInterceptor(async (method, request, grain, invoker) =>
            {
                try
                {
                    if (PreviousInterceptor != null)
                        await PreviousInterceptor(method, request, grain, invoker);

                    // Invoke the request and return the result back to the caller.
                    var result = await invoker.Invoke(grain, request);

                    if (Configuration.TrackMethodGrainCalls)
                    {
                        // Would be nice if we could figure out if this is a local or remote call, 
                        // and perhaps caller / calling silo... Unless I've got something backwards
                        logger.IncrementMetric($"GrainMethodCall:{grain.GetType().Name}:{method.Name}");
                    }

                    return result;
                }				
                catch (TimeoutException ex) // Not sure if this is going to be an innerException here or if everything gets unrolled... Fingers crossed for now!
                {
                    if (Configuration.TrackExceptionCounters)
                    {
                        logger.IncrementMetric($"GrainInvokeTimeout:{grain.GetType().Name}:{method.Name}");
                        logger.TrackException(ex, new Dictionary<string, string>
                        {
                            {"GrainType", grain.GetType().Name},
                            {"MethodName", method.Name},
                        });
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    if (Configuration.TrackExceptionCounters)
                    {
                        logger.IncrementMetric($"GrainException:{grain.GetType().Name}:{method.Name}");
                        logger.TrackException(ex, new Dictionary<string, string>
                        {
                            {"GrainType", grain.GetType().Name},
                            {"MethodName", method.Name},
                        });
                    }
                    throw;
                }
            });
        }

        // TODO: figure out how to subscribe with a GrainObserver, or some other method
        // so that the server can push messages to this telemetry consumer
        async Task UpdateConfiguration()
        {
            try
            {
                var oldConfig = Configuration;

                var metricsGrain = Runtime.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
                Configuration = await metricsGrain.GetMetricsConfiguration();

                //if (Configuration.HistoryLength != oldConfig.HistoryLength)
                //    TrimHistories();

                ConfigureSiloInterceptor(enable: Configuration.NeedSiloInterceptor);
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
            finally
            {
                LastConfigCheck = DateTime.UtcNow;
            }
        }

        async Task StartMessagePump()
        {
            while (true)
            {
                try
                {
                    // do we need to update config?
                    if (DateTime.UtcNow.Subtract(LastConfigCheck) > Configuration.ConfigurationInterval)
                        await UpdateConfiguration();

                    if (Configuration.Enabled)
                        await SampleMetrics();

                    await Task.Delay((int)Configuration.SamplingInterval.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    logger.TrackException(ex);
                }
            }
        }

        async Task SampleMetrics()
        {
            try
            {
                var snapshot = new MetricsSnapshot { Source = Runtime.SiloIdentity };

                foreach (var metric in Metrics)
                {
                    // current value
                    snapshot.Metrics.Add(metric.Key, metric.Value);

                    // history
                    //MetricHistory.AddOrUpdate(metric.Key, new ConcurrentQueue<double>(),
                    //    (key, value) => value);

                    //MetricHistory[metric.Key].Enqueue(metric.Value);

                    logger.Verbose("[Metric] " + metric.Key + " = " + metric.Value);
                }

                foreach (var counter in Counters)
                {
                    // current value
                    snapshot.Counters.Add(counter.Key, counter.Value);

                    // history
                    //CounterHistory.AddOrUpdate(counter.Key, new ConcurrentQueue<long>(),
                    //    (key, value) => value);

                    //CounterHistory[counter.Key].Enqueue(counter.Value);

                    logger.Verbose("[Counter] " + counter.Key + " = " + counter.Value);
                }

                foreach (var request in Requests)
                {
                    //RequestHistory[request.Key].Enqueue(request.Value);

                    logger.Verbose("[Request] " + request.Key + " = " + request.Value);
                }

                foreach (var tsmetric in TimeSpanMetrics)
                {
                    snapshot.TimeSpanMetrics.Add(tsmetric.Key, tsmetric.Value);

                    // history
                    //TimeSpanMetricHistory.AddOrUpdate(tsmetric.Key, new ConcurrentQueue<TimeSpan>(),
                    //    (key, value) => value);

                    //TimeSpanMetricHistory[tsmetric.Key].Enqueue(tsmetric.Value);

                    logger.Verbose("[Time Span Metric] " + tsmetric.Key + " = " + tsmetric.Value);
                }

                //TrimHistories();

                // report silo statistics to cluster metrics grain
                var metricsGrain = Runtime.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
                await metricsGrain.ReportSiloStatistics(snapshot);
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                // don't freak out
            }
        }

        void TrimHistories()
        {
            foreach (var metric in Metrics.Keys)
                TrimMetricsHistory(metric);

            foreach (var counter in Counters.Keys)
                TrimCounterHistory(counter);

            foreach (var tsmetric in TimeSpanMetricHistory.Keys)
                TrimTimeSpanMetricsHistory(tsmetric);

            foreach (var request in RequestHistory.Keys)
                TrimTimeSpanMetricsHistory(request);
        }

        void TrimCounterHistory(string name)
        {
            try
            {
                long counter;
                while (CounterHistory[name].Count > Configuration.HistoryLength)
                    if (!CounterHistory[name].TryDequeue(out counter))
                        throw new ApplicationException("Couldn't dequeue oldest counter");
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        void TrimMetricsHistory(string name)
        {
            try
            {
                double metric;
                while (MetricHistory[name].Count > Configuration.HistoryLength)
                    if (!MetricHistory[name].TryDequeue(out metric))
                        throw new ApplicationException("Couldn't dequeue oldest double metric");
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        void TrimTimeSpanMetricsHistory(string name)
        {
            try
            {
                TimeSpan metric;
                while (TimeSpanMetricHistory[name].Count > Configuration.HistoryLength)
                    if (!TimeSpanMetricHistory[name].TryDequeue(out metric))
                        throw new ApplicationException("Couldn't dequeue oldest TimeSpan metric");
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        void TrimRequestsHistory(string name)
        {
            try
            {
                MeasuredRequest request;
                while (RequestHistory[name].Count > Configuration.HistoryLength)
                    if (!RequestHistory[name].TryDequeue(out request))
                        throw new ApplicationException("Couldn't dequeue oldest request");
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        void AddCounter(string name)
        {
            try
            {
                if (Counters.ContainsKey(name))
                    return;

                // Counters.GetOrAdd(name, 0);

                //Counters.Add(name, new List<int> { 0 });
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        //void AddMetric(string name, IList<double> values)
        void AddMetric(string name)
        {
            try
            {
                if (Metrics.ContainsKey(name))
                    return;

                //Metrics.Add(name, new List<double> { 0 });
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        void AddTimeSpanMetric(string name)
        {
            try
            {
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        void AddMeasuredRequest(string name)
        {
            try
            {
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        public void DecrementMetric(string name)
        {
            DecrementMetric(name, 1);
        }

        public void DecrementMetric(string name, double value)
        {
            try
            {
                double metric = 0;

                lock (MetricsLock)
                {
                    if (!Metrics.TryGetValue(name, out metric))
                    {
                        if (!Metrics.TryAdd(name, -value))
                            throw new ApplicationException("Couldn't add metric");

                        if (!MetricHistory.TryAdd(name, new ConcurrentQueue<double>()))
                            throw new ApplicationException("Couldn't add metric history");
                    }
                    else if (!Metrics.TryUpdate(name, metric - value, metric))
                        throw new ApplicationException("Couldn't update metric");
                }

                TrimMetricsHistory(name);
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        public void IncrementMetric(string name)
        {
            IncrementMetric(name, 1);
        }

        public void IncrementMetric(string name, double value)
        {
            try
            {
                double metric = 0;

                lock (MetricsLock)
                {
                    if (!Metrics.TryGetValue(name, out metric))
                    {
                        if (!Metrics.TryAdd(name, value))
                            throw new ApplicationException("Couldn't add metric");

                        if (!MetricHistory.TryAdd(name, new ConcurrentQueue<double>()))
                            throw new ApplicationException("Couldn't initialize metric history");
                    }
                    else if (!Metrics.TryUpdate(name, metric + value, metric))
                        throw new ApplicationException("Couldn't update metric");
                }

                TrimMetricsHistory(name);

                //var metricsGrain = Runtime.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
                //metricsGrain.ReportSiloStatistics(new MetricsSnapshot()).Wait();

                //StartMessagePump();
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                //throw;
            }
        }

        

        //async Task Experiment()
        //{
        //    try
        //    {
        //        var metricsGrain = Runtime.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
        //        await metricsGrain.ReportSiloStatistics(new MetricsSnapshot());
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.TrackException(ex);
        //    }
        //}

        public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration, bool success)
        {
            //NRClient.RecordResponseTimeMetric(string.Format(
            //"{0}\\{1}", 
            //dependencyName, commandName), 
            //(long)duration.TotalMilliseconds);
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //try
            //{
            //    AddMetric(metrics);
            //    AddProperties(properties);

            //    if (!Counters.ContainsKey(eventName))
            //    {
            //        AddCounter(eventName);
            //        Counters[eventName].Add(1);
            //    }
            //    else
            //        Counters[eventName].Add(Counters[eventName][Counters[eventName].Count - 1] + 1);

            //    TrimCounterHistory(eventName);
            //}
            //catch (Exception ex)
            //{
            //    logger.TrackException(ex);
            //    throw;
            //}
        }

        // TODO: figure out what to do with properties and metrics
        public void TrackException(Exception exception, 
            IDictionary<string, string> properties = null, 
            IDictionary<string, double> metrics = null)
        {
            try
            {
                if (!Configuration.TrackExceptionCounters)
                    return;

                var exceptionName = exception.GetType().Name;
                var metricName = $"Exception:{exceptionName}";

                lock (MetricsLock)
                {
                    if (!Metrics.ContainsKey(metricName))
                    {
                        AddMetric(metricName);
                        logger.IncrementMetric("UniqueExceptionsReported");
                    }
                }

                logger.IncrementMetric($"Exception:{exceptionName}");
                logger.IncrementMetric("ExceptionsReported");

            }
            catch (Exception ex)
            {
                // TODO: figure out if this could cause an infinite loop
                //logger.TrackException(ex);
                throw;
            }
        }

        public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null)
        {
            //try
            //{
            //    AddProperties(properties);

            //    if (!TimeSpanMetrics.ContainsKey(name))
            //    {
            //        AddTimeSpanMetric(name);
            //        TimeSpanMetrics.Add(name, new List<TimeSpan> { value });
            //    }
            //    else
            //        TimeSpanMetrics[name].Add(value);

            //    TrimMetricsHistory(name);
            //}
            //catch (Exception ex)
            //{
            //    logger.TrackException(ex);
            //    throw;
            //}
        }

        public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            try
            {
                AddProperties(properties);

                if (!Metrics.ContainsKey(name))
                {
                    AddMetric(name);
                    //Metrics[name].Add(value);
                }
                //else
                //    Metrics[name].Add(value);

                TrimMetricsHistory(name);
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
        {
            try
            {
                var request = new MeasuredRequest
                {
                    Name = name,
                    StartTime = startTime,
                    Duration = duration
                };

                if (!Requests.ContainsKey(name))
                {
                    AddMeasuredRequest(name);
                    //Requests[name].Add(request);
                }
                //else
                //    Requests[name].Add(request);

                TrimRequestsHistory(name);
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        private void AddMetric(IDictionary<string, double> metrics)
        {
            try
            {
                if (metrics != null)
                {
                    metrics.AsParallel().ForAll(m =>
                    {
                        if (!Metrics.ContainsKey(m.Key))
                            AddMetric(m.Key);
                    });
                }
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        private void AddProperties(IDictionary<string, string> properties)
        {
            try
            {
                //if (properties != null)
                //{
                //    properties.AsParallel().ForAll(p =>
                //    {
                //        //NRClient.AddCustomParameter(p.Key, p.Value);
                //    });
                //}
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        public void Flush() { }

        public void Close()
        {
            // TODO: figure out if this should be done
            //Runtime.SetInvokeInterceptor(null);
        }
    }
}
