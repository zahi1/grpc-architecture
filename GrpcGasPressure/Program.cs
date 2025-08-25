using GasPressure.Grpc.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using System.Net;

namespace GasPressure
{
    /// <summary>
    /// The entry point for the GasPressure server application. Configures logging, sets up gRPC services, and starts the web server.
    /// </summary>
    public class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger(); // Logger for the server

        /// <summary>
        /// Main entry point of the application. Initiates the server by calling the Run method.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        public static void Main(string[] args)
        {
            var self = new Program();
            self.Run(args);
        }

        /// <summary>
        /// Runs the server by configuring logging, logging the startup message, and starting the server.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        private void Run(string[] args)
        {
            ConfigureLogging();  // Sets up NLog for logging

            log.Info("Server is about to start");  // Logs a message before the server starts

            StartServer(args);  // Initializes and starts the server
        }

        /// <summary>
        /// Configures NLog for logging, setting up console logging with a specified layout format.
        /// </summary>
        private void ConfigureLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Set up console target with a specific layout
            var console = new NLog.Targets.ConsoleTarget("console")
            {
                Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
            };
            config.AddTarget(console);  // Adds the console target to the configuration
            config.AddRuleForAllLevels(console);  // Applies the console logging rule for all log levels

            // Apply the logging configuration to LogManager
            LogManager.Configuration = config;
        }

        /// <summary>
        /// Starts the web server, configures gRPC services, and listens on port 5000.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        private void StartServer(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);  // Creates the web application builder

            builder.Services.AddGrpc();  // Adds support for gRPC services

            builder.Services.AddSingleton<GasContainerServiceImpl>();  // Registers GasContainerServiceImpl in the dependency injection container

            builder.WebHost.ConfigureKestrel(opts =>
            {
                opts.Listen(IPAddress.Any, 5000);  // Configures Kestrel to listen on port 5000 on all network interfaces
            });

            var app = builder.Build();  // Builds the web application

            app.UseRouting();  // Enables routing for the application

            app.MapGrpcService<GasContainerServiceImpl>();  // Maps the GasContainerServiceImpl as a gRPC service

            app.Run();  // Runs the server

            //log.Info("Server started and listening on http://127.0.0.1:5000");  // Logs confirmation that the server is running
        }
    }
}
