using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YerraAgent
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public int Target { get; set; }
        public bool Action { get; set; }
        public bool State { get; set; }
        public Agent Agent { get; set; }
        public string AgentId { get; set; }
        public ProcessInfo() { }
        public ProcessInfo(Agent agent, string name, string label)
        {
            this.Agent = agent;
            this.AgentId = agent.Id;
            this.Name = name;
            this.Label = label;
            this.Target = 0;
            this.Action = false;
        }
    }
}
