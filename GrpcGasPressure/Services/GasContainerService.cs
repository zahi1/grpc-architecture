using System.Threading.Tasks;
using Grpc.Core;
using GasPressure.Grpc;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Microsoft.Extensions.Logging;

namespace GasPressure.Grpc.Services
{
    /// <summary>
    /// Implementation of the GasContainerService, providing gRPC endpoints for managing and interacting with a gas container.
    /// Handles state retrieval, mass addition/removal, and checks for container destruction.
    /// </summary>
    public class GasContainerServiceImpl : GasContainerService.GasContainerServiceBase
    {
        private readonly GasContainerLogic containerLogic = new GasContainerLogic();  // Instance of container logic to manage gas properties
        private Logger logger = LogManager.GetCurrentClassLogger();  // Logger instance for tracking service events

        /// <summary>
        /// Initializes a new instance of the GasContainerServiceImpl class.
        /// Starts a background thread to adjust container temperature periodically.
        /// </summary>
        public GasContainerServiceImpl()
        {
            new Thread(containerLogic.AdjustTemperature).Start();  // Starts temperature adjustment in a separate thread
        }

        /// <summary>
        /// Retrieves the current state of the gas container, including pressure, temperature, mass, and destruction status.
        /// </summary>
        /// <param name="request">Empty request parameter.</param>
        /// <param name="context">The gRPC server call context.</param>
        /// <returns>A task containing the current gas container state.</returns>
        public override Task<GasContainerState> GetContainerState(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new GasContainerState
            {
                Pressure = containerLogic.Pressure,
                Temperature = containerLogic.Temperature,
                Mass = containerLogic.Mass,
                IsDestroyed = containerLogic.IsDestroyed
            });
        }

        /// <summary>
        /// Adds a specified amount of mass to the gas container, if within safe limits, and returns the result.
        /// </summary>
        /// <param name="request">The request containing the mass amount to add.</param>
        /// <param name="context">The gRPC server call context.</param>
        /// <returns>A task containing the result of the mass addition, including success status and message.</returns>
        public override Task<MassAdjustmentResponse> AddMass(MassRequest request, ServerCallContext context)
        {
            string message = containerLogic.IncreaseMass(request.Amount);  // Attempts to increase mass
            return Task.FromResult(new MassAdjustmentResponse { Success = !containerLogic.IsDestroyed, Message = message });
        }

        /// <summary>
        /// Removes a specified amount of mass from the gas container, if within safe limits, and returns the result.
        /// </summary>
        /// <param name="request">The request containing the mass amount to remove.</param>
        /// <param name="context">The gRPC server call context.</param>
        /// <returns>A task containing the result of the mass removal, including success status and message.</returns>
        public override Task<MassAdjustmentResponse> RemoveMass(MassRequest request, ServerCallContext context)
        {
            string message = containerLogic.DecreaseMass(request.Amount);  // Attempts to decrease mass
            return Task.FromResult(new MassAdjustmentResponse { Success = !containerLogic.IsDestroyed, Message = message });
        }

        /// <summary>
        /// Checks if the container is destroyed and returns the destruction status.
        /// </summary>
        /// <param name="request">Empty request parameter.</param>
        /// <param name="context">The gRPC server call context.</param>
        /// <returns>A task containing the current destruction status of the container.</returns>
        public override Task<GasContainerState> IsDestroyed(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new GasContainerState { IsDestroyed = containerLogic.IsDestroyed });
        }
    }
}
