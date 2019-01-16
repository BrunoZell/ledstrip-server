using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

namespace LedStripServer.Services
{
    public class SoftwarePwmController : IDisposable
    {
        private bool _disposed;
        private readonly GpioController _gpioController;
        private readonly Dictionary<int, PwmTask> _running = new Dictionary<int, PwmTask>();

        public SoftwarePwmController(GpioController gpioController)
        {
            _gpioController = gpioController;
        }

        public void SetPinPwm(int pinNumber, double frequencyInHertz, double dutyCyclePercentage)
        {
            if (_disposed)
                throw new ObjectDisposedException(ToString());

            lock (this) {
                UnlockedStop(pinNumber);
                PreparePin(pinNumber);

                var cts = new CancellationTokenSource();
                var task = RunPwm(pinNumber, frequencyInHertz, dutyCyclePercentage, cts.Token);
                _running[pinNumber] = new PwmTask(pinNumber, task, cts);
            }
        }

        public void StopPinPwm(int pinNumber)
        {
            if (_disposed)
                throw new ObjectDisposedException(ToString());

            lock (this) {
                UnlockedStop(pinNumber);
            }
        }

        private void UnlockedStop(int pinNumber)
        {
            if (_running.Remove(pinNumber, out var task)) {
                task.Cancel();
            }
        }

        /// <summary>
        /// Ensures pin is open and is set to output mode
        /// </summary>
        private void PreparePin(int pinNumber)
        {
            if (!_gpioController.IsPinOpen(pinNumber)) {
                _gpioController.OpenPin(pinNumber, PinMode.Output);
            } else if (_gpioController.GetPinMode(pinNumber) != PinMode.Output) {
                _gpioController.SetPinMode(pinNumber, PinMode.Output);
            }
        }

        private async Task RunPwm(int pinNumber, double frequencyInHertz, double dutyCyclePercentage, CancellationToken cancellationToken)
        {
            double tick = 1000.0 / frequencyInHertz;
            double msHigh = tick * dutyCyclePercentage;
            double msLow = tick - msHigh;

            try {
                while (!cancellationToken.IsCancellationRequested) {
                    _gpioController.Write(pinNumber, PinValue.High);
                    await Task.Delay((int)msHigh, cancellationToken);
                    _gpioController.Write(pinNumber, PinValue.Low);
                    await Task.Delay((int)msLow, cancellationToken);
                }
            } catch (TaskCanceledException) {
                // Gracefully stop when canceled
            } finally {
                _gpioController.ClosePin(pinNumber);
            }
        }

        private class PwmTask
        {
            private readonly Task _task;
            private readonly CancellationTokenSource _cancellationTokenSource;

            public PwmTask(int pinNumber, Task task, CancellationTokenSource cancellationTokenSource)
            {
                PinNumber = pinNumber;
                _task = task;
                _cancellationTokenSource = cancellationTokenSource;
            }

            public int PinNumber { get; }

            public void Cancel()
            {
                _cancellationTokenSource.Cancel();
                _task.GetAwaiter().GetResult();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (int pinNumber in _running.Keys) {
                StopPinPwm(pinNumber);
            }
        }
    }
}
