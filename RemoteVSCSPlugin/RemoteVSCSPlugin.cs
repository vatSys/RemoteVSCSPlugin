using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using vatsys;
using vatsys.Plugin;
using System.IO;
using System.Reflection;

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

        private List<Line> vscsLines;
        private List<Frequency> vscsFreqs;

        public string Name => nameof(RemoteVSCSPlugin);

        public string Folder;

        public RemoteVSCSPlugin()
        {
            Folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";

            Audio.VSCSFrequenciesChanged += Freqs_Changed;
            Audio.VSCSLinesChanged += Lines_Changed;
            Audio.FrequencyErrorStateChanged += Freqs_Changed;
            Audio.TransmittingChanged += Audio_ChangedEvent;
            Network.PrimaryFrequencyChanged += Freqs_Changed;
            ConvertLines();
            ConvertFrequencies();
            UpdateState();

            cancellationToken = new CancellationTokenSource();
            httpServer = new HttpServer(Folder, PORT, cancellationToken.Token);
            vatSysWebSocketServer = new VatSysWebSocketServer(PORT + 1, cancellationToken.Token);
            vatSysWebSocketServer.VSCSCommandReceived += VatSysWebSocketServer_VSCSCommandReceived;
        }

        ~RemoteVSCSPlugin()
        {
            cancellationToken?.Cancel();
        }

        private void Lines_Changed(object sender, EventArgs e)
        {
            UpdateState(updateLines: true);
        }
        private void Freqs_Changed(object sender, EventArgs e)
        {
            UpdateState(updateFreqs: true);
        }
        private void Audio_ChangedEvent(object sender, EventArgs e)
        {
            UpdateState();
        }

        private void UpdateState(bool updateFreqs = false, bool updateLines = false)
        {
            if (updateFreqs)
                ConvertFrequencies();
            if (updateLines)
                ConvertLines();

            var state = new VSCSState()
            {
                Connected = Network.IsConnected && Audio.IsAFVConnected,
                Transmitting = Audio.Transmitting,
                Group = Audio.GroupFrequencies,
                AllToSpeaker = Audio.VSCSAllToSpeaker,
                TonesToSpeaker = Audio.VSCSTonesToSpeaker,
                Lines = vscsLines,
                Frequencies = vscsFreqs
            };
            currentState = state;

            vatSysWebSocketServer?.BroadcastToVSCS(currentState.Serialize());
        }

        private void ConvertLines()
        {
            if (vscsLines != null)
            {
                foreach (var l in vscsLines)
                    l.VSCSLine.StateChanged -= Lines_Changed;
            }

            List<Line> lines = new List<Line>();
            foreach (var l in Audio.VSCSLines)
            {
                lines.Add(new Line(l));
                l.StateChanged += Lines_Changed;
            }
            vscsLines = lines;
        }

        private void ConvertFrequencies()
        {
            if (vscsFreqs != null)
            {
                foreach (var f in vscsFreqs)
                {
                    f.VSCSFrequency.ReceiveChanged -= Freqs_Changed;
                    f.VSCSFrequency.ReceivingChanged -= Freqs_Changed;
                    f.VSCSFrequency.TransmitChanged -= Freqs_Changed;
                }
            }

            List<Frequency> freqs = new List<Frequency>();
            foreach (var f in Audio.VSCSFrequencies)
            {
                f.ReceiveChanged += Freqs_Changed;
                f.TransmitChanged += Freqs_Changed;
                f.ReceivingChanged += Freqs_Changed;
                freqs.Add(new Frequency(f));
            }
            vscsFreqs = freqs;
        }

        private void VatSysWebSocketServer_VSCSCommandReceived(object sender, VSCSCommandReceivedEventArgs e)
        {
            switch (e.VSCSCommand.Name)
            {
                case "Group":
                    Audio.GroupFrequencies = (bool)e.VSCSCommand.Value;
                    break;
                case "AllToSpeaker":
                    Audio.VSCSAllToSpeaker = (bool)e.VSCSCommand.Value;
                    break;
                case "TonesToSpeaker":
                    Audio.VSCSTonesToSpeaker = (bool)e.VSCSCommand.Value;
                    break;
                case "Call":
                    {
                        var line = vscsLines?.FirstOrDefault(l=>l.Id == Convert.ToInt32(e.VSCSCommand.Value))?.VSCSLine;
                        if (line != null)
                            Audio.Call(line);
                        break;
                    }
                case "Answer":
                    {
                        var line = vscsLines?.FirstOrDefault(l => l.Id == Convert.ToInt32(e.VSCSCommand.Value))?.VSCSLine;
                        if (line != null)
                            Audio.Answer(line);
                        break;
                    }
                case "HangUp":
                    {
                        var line = vscsLines?.FirstOrDefault(l => l.Id == Convert.ToInt32(e.VSCSCommand.Value))?.VSCSLine;
                        if (line != null)
                            Audio.HangUp(line);
                        break;
                    }
                case "AddFreq":
                    Audio.LoadFrequency((string)e.VSCSCommand.Value, false);
                    break;
                case "AddFreqGroup":
                    Audio.LoadFrequency((string)e.VSCSCommand.Value, true);
                    break;
                case "RemoveFreq":
                    {
                        var freq = vscsFreqs?.FirstOrDefault(f => f.Id == Convert.ToInt32(e.VSCSCommand.Value))?.VSCSFrequency;
                        if (freq != null)
                            Audio.RemoveFrequency(freq);
                        break;
                    }
                case "Primary":
                    {
                        //Primary is obsolete
                        break;
                    }
                case "Idle":
                    {
                        var freq = vscsFreqs?.FirstOrDefault(f => f.Id == Convert.ToInt32(e.VSCSCommand.Value))?.VSCSFrequency;
                        if (freq != null)
                        {
                            freq.Receive = false;
                            freq.Transmit = false;
                        }
                        break;
                    }
                case "Receive":
                    {
                        var freq = vscsFreqs?.FirstOrDefault(f => f.Id == Convert.ToInt32(e.VSCSCommand.Value))?.VSCSFrequency;
                        if (freq != null)
                        {
                            freq.Receive = true;
                            freq.Transmit = false;
                        }
                        break;
                    }
                case "Transmit":
                    {
                        var freq = vscsFreqs?.FirstOrDefault(f => f.Id == Convert.ToInt32(e.VSCSCommand.Value))?.VSCSFrequency;
                        if (freq != null && Network.IsValidATC)
                        {
                            freq.Receive = true;
                            freq.Transmit = true;
                        }
                        break;
                    }
            }
            UpdateState();
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
