using System;
using System.Threading;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using NLog;
using GasPressure.Grpc;

namespace GasPressure.Client
{
    /// <summary>
    /// The InputClient class connects to the gas container service and periodically increases
    /// the gas mass if the container's pressure is below a specific limit.
    /// </summary>
    public class InputClient
    {
        /// <summary>
        /// Logger for logging client events and status updates.
        /// </summary>
        private static readonly Logger mLog = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Random generator for generating random mass values to add to the gas container.
        /// </summary>
        private readonly Random rnd = new Random();

        /// <summary>
        /// Configures the logging system using NLog, setting up console output with a custom format.
        /// </summary>
        private void ConfigureLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var console = new NLog.Targets.ConsoleTarget("console")
            {
                Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
            };
            config.AddTarget(console);
            config.AddRuleForAllLevels(console);
            LogManager.Configuration = config;
        }

        /// <summary>
        /// Main logic for connecting to the gas container service, monitoring pressure,
        /// and adding mass if pressure is below a defined threshold.
        /// </summary>
        public void Run()
        {
            ConfigureLogging();  // Sets up logging
            mLog.Info("Starting Input Client...");  // Logs that the client is starting

            using var channel = GrpcChannel.ForAddress("http://localhost:5000");  // Connects to gRPC server at specified address
            var client = new GasContainerService.GasContainerServiceClient(channel);  // Initializes gRPC client for GasContainerService

            while (true)  // Main loop to periodically check pressure and add mass
            {
                try
                {
                    var state = client.GetContainerState(new Empty());  // Retrieves the current container state
                    mLog.Info($"Current pressure: {state.Pressure}");  // Logs the current pressure

                    if (state.IsDestroyed)  // Checks if the container is destroyed
                    {
                        mLog.Info("The container has been destroyed. Stopping updates.");  // Logs destruction status
                        break;  // Exits the loop if container is destroyed
                    }

                    if (state.Pressure < 100)  // Adds mass if pressure is below the threshold
                    {
                        double massToAdd = rnd.Next(1, 5);  // Generates a random amount of mass to add
                        var response = client.AddMass(new MassRequest { Amount = massToAdd });  // Sends add mass request to server
                        mLog.Info($"Added {massToAdd} units of mass. Server response: {response.Message}");  // Logs the addition and response
                    }
                    else
                    {
                        mLog.Info("Pressure is above the threshold, no mass added.");  // Logs if no mass was added
                    }

                    Thread.Sleep(2000);  // Waits for 2 seconds before next operation
                }
                catch (Exception ex)
                {
                    mLog.Warn(ex, "Error communicating with server. Retrying...");  // Logs any exception and waits before retrying
                    Thread.Sleep(5000);  // Waits 5 seconds before retrying after an error
                }
            }
        }

        /// <summary>
        /// Entry point for the InputClient program. Starts the Run method.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            var client = new InputClient();  // Creates an instance of InputClient
            client.Run();  // Runs the main logic for the client
        }
    }
}
