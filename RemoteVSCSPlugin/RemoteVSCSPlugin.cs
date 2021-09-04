using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using vatsys;
using vatsys.Plugin;

namespace RemoteVSCSPlugin
{
    [Export(typeof(IPlugin))]
    public class RemoteVSCSPlugin : IPlugin
    {
        private const int PORT = 7673;
        private static string[] HTTP_HEADER_SEPARATOR = new string[1] { "\r\n" };
        private static char[] HTTP_GET_SEPARATOR = new char[1] { ' ' };

        private CancellationTokenSource cancellationToken;

        private HttpServer httpServer;
        private VatSysWebSocketServer vatSysWebSocketServer;

        private VSCSState currentState;
        internal static string CurrentStateJSON { get; private set; }

        public string Name => nameof(RemoteVSCSPlugin);

        public RemoteVSCSPlugin()
        {
            Audio.VSCSFrequenciesChanged += Audio_ChangedEvent;
            Audio.VSCSLinesChanged += Audio_ChangedEvent;
            Audio.FrequencyErrorStateChanged += Audio_ChangedEvent;
            Audio.TransmittingChanged += Audio_ChangedEvent;
            Network.PrimaryFrequencyChanged += Audio_ChangedEvent;
            UpdateState();

            cancellationToken = new CancellationTokenSource();
            httpServer = new HttpServer(PORT, cancellationToken.Token);
            vatSysWebSocketServer = new VatSysWebSocketServer(PORT + 1, cancellationToken.Token);
        }

        ~RemoteVSCSPlugin()
        {
            cancellationToken?.Cancel();
        }

        private void Audio_ChangedEvent(object sender, EventArgs e)
        {
            UpdateState();
        }

        private void UpdateState()
        {
            var state = new VSCSState()
            {
                Connected = Network.IsConnected && Audio.IsAFVConnected,
                Transmitting = Audio.Transmitting,
                Group = Audio.GroupFrequencies,
                AllToSpeaker = Audio.VSCSAllToSpeaker,
                TonesToSpeaker = Audio.VSCSTonesToSpeaker,
                Lines = ConvertLines(Audio.VSCSLines),
                Frequencies = ConvertFrequencies(Audio.VSCSFrequencies)
            };
            currentState = state;
            CurrentStateJSON = currentState.Serialize();

            vatSysWebSocketServer?.BroadcastToVSCS(CurrentStateJSON);
        }

        private List<Line> ConvertLines(IList<VSCSLine> vscsLines)
        {
            List<Line> lines = new List<Line>();
            foreach (var l in vscsLines)
                lines.Add(new Line(l));
            return lines;
        }

        private List<Frequency> ConvertFrequencies(IList<VSCSFrequency> vscsFreqs)
        {
            if (currentState?.Frequencies != null)
            {
                foreach (var f in currentState.Frequencies)
                {
                    f.VSCSFrequency.ReceiveChanged -= Audio_ChangedEvent;
                    f.VSCSFrequency.ReceivingChanged -= Audio_ChangedEvent;
                    f.VSCSFrequency.TransmitChanged -= Audio_ChangedEvent;
                }
            }

            List<Frequency> freqs = new List<Frequency>();
            foreach (var f in vscsFreqs)
            {
                f.ReceiveChanged += Audio_ChangedEvent;
                f.TransmitChanged += Audio_ChangedEvent;
                f.ReceivingChanged += Audio_ChangedEvent;
                freqs.Add(new Frequency(f));
            }
            return freqs;
        }

        #region INTERFACE_FUNCTIONS
        public CustomLabelItem GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
        {
            return null;
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            
        }

        public CustomColour SelectASDTrackColour(Track track)
        {
            return null;
        }

        public CustomColour SelectGroundTrackColour(Track track)
        {
            return null;
        }
        #endregion
    }
}
