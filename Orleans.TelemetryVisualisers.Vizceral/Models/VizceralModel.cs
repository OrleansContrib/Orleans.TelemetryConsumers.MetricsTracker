using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Orleans.TelemetryVisualisation.Vizceral.Models
{
    public class VizceralRootObject
    {
        public string renderer { get; set; }
        public string name { get; set; }
        public List<Node> nodes { get; set; }
        public List<Connection> connections { get; set; }
        public double maxVolume { get; set; }
        public long serverUpdateTime { get; set; }
    }
    public class Node
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string renderer { get; set; }
        public string name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long updated { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Node> nodes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string @class { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Connection> connections { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? maxVolume { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Props props { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Metadata metadata { get; set; }
    }
    public class Connection
    {
        public Connection()
        {
            metrics = new Metrics();
            metadata = new Metadata();
        }

        public string source { get; set; }
        public Metadata metadata { get; set; }
        public Metrics metrics { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string @class { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string target { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Status status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Notice> notices { get; set; }
    }
    public class Notice
    {
        public string title { get; set; }
        public string link { get; set; }
        public int severity { get; set; }
    }
    public class Props
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<MaxSemaphore> maxSemaphores { get; set; }
    }

    public class Status
    {
        public double danger { get; set; }
        public double normal { get; set; }
        public double? warning { get; set; }
    }

    public class MaxSemaphore
    {
        public string targetRegion { get; set; }
        public string region { get; set; }
        public string value { get; set; }
    }

    public class Metadata
    {
        public Metadata()
        {
            streaming = 1;
        }
        public int streaming { get; set; }
    }

    public class Metrics
    {
        public Metrics()
        {
            danger = Double.NaN;
            normal = Double.NaN;
            warning = Double.NaN;
        }
        public double danger { get; set; }
        public double normal { get; set; }
        public double warning { get; set; }
    }
}