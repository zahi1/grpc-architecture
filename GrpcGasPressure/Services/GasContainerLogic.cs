using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using NLog;

namespace GasPressure.Grpc.Services
{
    /// <summary>
    /// Manages the gas container's state, including temperature, mass, and pressure limits.
    /// Provides methods for adjusting temperature, adding/removing mass, and checking destruction status.
    /// </summary>
    public class GasContainerLogic
    {
        private readonly object stateLock = new object();
        private double temperature = 293; // Initial temperature in Kelvin
        private double mass = 10;         // Initial mass of gas in container
        private bool isDestroyed = false; // Flag indicating if container is destroyed
        private Logger logger = LogManager.GetCurrentClassLogger(); // Logger instance for logging operations

        private const double PressureLimit = 100;       // Threshold for mass addition
        private const double UpperPressureLimit = 150;  // Threshold for mass removal
        private const double ExplosionLimit = 200;      // Maximum safe pressure before explosion
        private const double ImplosionLimit = 10;       // Minimum safe pressure before implosion

        /// <summary>
        /// Calculates and returns the current pressure based on mass and temperature using the ideal gas approximation.
        /// </summary>
        public double Pressure => (mass * temperature) / 22.4;

        /// <summary>
        /// Gets the current temperature of the gas container in a thread-safe manner.
        /// </summary>
        public double Temperature
        {
            get
            {
                lock (stateLock)
                {
                    return temperature;
                }
            }
        }

        /// <summary>
        /// Gets the current mass of the gas in the container in a thread-safe manner.
        /// </summary>
        public double Mass
        {
            get
            {
                lock (stateLock)
                {
                    return mass;
                }
            }
        }

        /// <summary>
        /// Checks if the container is destroyed in a thread-safe manner.
        /// </summary>
        public bool IsDestroyed
        {
            get
            {
                lock (stateLock)
                {
                    return isDestroyed;
                }
            }
        }

        /// <summary>
        /// Periodically adjusts the temperature of the container within a safe range and checks pressure limits.
        /// Logs each temperature adjustment.
        /// </summary>
        public void AdjustTemperature()
        {
            Random rnd = new Random();
            while (true)
            {
                Thread.Sleep(2000);
                lock (stateLock)
                {
                    if (!isDestroyed)
                    {
                        double tempChange = rnd.Next(-15, 16);
                        temperature += tempChange;
                        logger.Info($"Temperature changed by {tempChange}K. New temperature: {temperature}K");

                        CheckPressureLimits();
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to increase the mass of gas in the container if the pressure is below a safe threshold.
        /// Returns a message indicating the success or failure of the operation.
        /// </summary>
        /// <param name="amount">The amount of mass to add.</param>
        /// <returns>A message indicating whether the mass increase succeeded.</returns>
        public string IncreaseMass(double amount)
        {
            lock (stateLock)
            {
                if (!isDestroyed && Pressure < PressureLimit)
                {
                    mass += amount;
                    logger.Info($"Mass increased by {amount}. New mass: {mass}");
                    return "Mass successfully increased.";
                }
                return "Pressure too high to add mass.";
            }
        }

        /// <summary>
        /// Attempts to decrease the mass of gas in the container if the pressure is above a safe threshold.
        /// Returns a message indicating the success or failure of the operation.
        /// </summary>
        /// <param name="amount">The amount of mass to remove.</param>
        /// <returns>A message indicating whether the mass decrease succeeded.</returns>
        public string DecreaseMass(double amount)
        {
            lock (stateLock)
            {
                if (!isDestroyed && Pressure > UpperPressureLimit)
                {
                    mass -= amount;
                    logger.Info($"Mass decreased by {amount}. New mass: {mass}");
                    return "Mass successfully decreased.";
                }
                return "Pressure too low to remove mass.";
            }
        }

        /// <summary>
        /// Checks if the current pressure is within safe limits.
        /// If the pressure is outside limits, it marks the container as destroyed and logs the event.
        /// </summary>
        private void CheckPressureLimits()
        {
            if (Pressure < ImplosionLimit)
            {
                isDestroyed = true;
                logger.Warn("Container imploded!");
            }
            else if (Pressure > ExplosionLimit)
            {
                isDestroyed = true;
                logger.Warn("Container exploded!");
            }
        }
    }
}
