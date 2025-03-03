using li.qubic.services.poolManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace li.qubic.lib.Network
{
    public class Peer : INotifyPropertyChanged
    {
        private PeerConnection _connection;

        public string Alias { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; } = 21841;

       
        private bool _isConnected;

        [JsonIgnore]
        public bool Isconnected
        {
            get => _isConnected; set
            {
                _isConnected = value;
                NotifyPropertyChanged(nameof(Isconnected));
            }
        }

        private uint _tick;
        [JsonIgnore]
        public uint Tick
        {
            get => _tick; set
            {
                _tick = value;
                NotifyPropertyChanged(nameof(Tick));
            }
        }

       
        // settings
        public int UpdateInterval { get; set; } = 1; // seconds to update node status

        private string _lastError;
        [JsonIgnore]
        public string LastError
        {
            get => _lastError;
            set
            {
                _lastError = value;
                NotifyPropertyChanged(nameof(LastError));
            }
        }

        // actions
        public void Connect(string? operatorSeed)
        {
            _connection = new PeerConnection(this, operatorSeed);
            _connection.OnLogMessage += (message, exception) =>
            {
                if (exception != null)
                {
                    LastError = exception.Message;
                }
            };
            _connection.Start();
        }

        public void Disconnect()
        {
            try
            {
                if (_connection != null)
                    _connection.Close();
            }
            catch (Exception e)
            {
                //ignore
            }
            Isconnected = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a">wait condition</param>
        /// <param name="timeout">miliseconds</param>
        private async Task<bool> WaitFor(Func<bool> a, int timeout = 3000)
        {
            return await Task.Run(() =>
            {
                var startWaiting = DateTime.UtcNow;
                while (startWaiting.AddMilliseconds(timeout) > DateTime.UtcNow && !a.Invoke())
                {
                    Thread.Sleep(100);
                }
                return a.Invoke();
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
