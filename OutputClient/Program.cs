using System;
using System.Threading;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using NLog;
using GasPressure.Grpc;

namespace GasPressure.Client
{
    /// <summary>
    /// The OutputClient class interacts with the gas container service to remove gas mass
    /// when the container's pressure is above a specified threshold.
    /// </summary>
    public class OutputClient
    {
        /// <summary>
        /// Logger for logging information, warnings, and errors in the client operations.
        /// </summary>
        private static readonly Logger mLog = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Random generator for generating random mass values to remove from the gas container.
        /// </summary>
        private readonly Random rnd = new Random();

        /// <summary>
        /// Configures the logging system using NLog, setting up console output with a specified layout.
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
        /// Main logic for the OutputClient. Connects to the gas container service and removes mass if pressure is above a defined threshold.
        /// </summary>
        public void Run()
        {
            ConfigureLogging();  // Sets up logging configuration
            mLog.Info("Starting Output Client...");  // Logs client start

            using var channel = GrpcChannel.ForAddress("http://localhost:5000");  // Connects to the gRPC server at the specified address
            var client = new GasContainerService.GasContainerServiceClient(channel);  // Initializes a gRPC client for the GasContainerService

            while (true)  // Main loop for checking pressure and removing mass
            {
                try
                {
                    var state = client.GetContainerState(new Empty());  // Retrieves current state of the gas container
                    mLog.Info($"Current pressure: {state.Pressure}");  // Logs the current pressure

                    if (state.IsDestroyed)  // Checks if the container is destroyed
                    {
                        mLog.Info("The container has been destroyed. Stopping updates.");  // Logs that updates are stopping
                        break;  // Exits the loop if the container is destroyed
                    }

                    if (state.Pressure > 100)  // Removes mass if pressure exceeds threshold
                    {
                        double massToRemove = rnd.Next(1, 5);  // Generates a random amount of mass to remove
                        var response = client.RemoveMass(new MassRequest { Amount = massToRemove });  // Calls RemoveMass on the server
                        mLog.Info($"Removed {massToRemove} units of mass. Server response: {response.Message}");  // Logs removal and server response
                    }
                    else
                    {
                        mLog.Info("Pressure is below the threshold, no mass removed.");  // Logs if no mass was removed
                    }

                    Thread.Sleep(2000);  // Waits for 2 seconds before the next operation
                }
                catch (Exception ex)
                {
                    mLog.Warn(ex, "Error communicating with server. Retrying...");  // Logs exceptions and retries
                    Thread.Sleep(5000);  // Waits 5 seconds before retrying after an error
                }
            }
        }

        /// <summary>
        /// Entry point for the OutputClient application. Starts the Run method to begin operations.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            var client = new OutputClient();  // Creates an instance of OutputClient
            client.Run();  // Runs the main logic for the client
        }
    }
}
