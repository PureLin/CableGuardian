using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using Valve.VR;

namespace CableGuardian
{
    class VRObserverEventArgs : EventArgs
    {
        /// <summary>
        /// Current rotation around the y-axis (= Yaw) in radians.
        /// (range from -PI to +PI) (sign denotes direction from center - left or right depending on coordinate system)
        /// </summary>
        public double Yaw;

        public bool HmdYawChanged { get; }
        public VRObserverEventArgs(double yaw, bool hmdYawChanged)
        {
            Yaw = yaw;
            HmdYawChanged = hmdYawChanged;
        }
    }

    class VRObserver
    {
        public event EventHandler<VRObserverEventArgs> ValidYawReceived;
        public event EventHandler<EventArgs> InvalidYawReceived;

        VRConnection VR;
        public HmdRotation_t Rotation;
        public HmdPosition_t Position;

        BackgroundWorker Worker = new BackgroundWorker();
        bool StopFlag = false;

        /// <summary>
        /// Interval (ms) to read statistics from the VR API connection
        /// </summary>
        public int PollInterval { get; set; }

        /// <summary>
        /// Periodically reports statistics from an active VR API connection.
        /// </summary>
        /// <param name="vr"></param>
        /// <param name="pollInterval"></param>
        public VRObserver(VRConnection vr, int pollInterval = 150)
        {
            VR = vr ?? throw new Exception("null VR connection.");
            PollInterval = pollInterval;

            Worker.DoWork += DoWork;
            Worker.ProgressChanged += Worker_ProgressChanged;
            Worker.WorkerReportsProgress = true;
        }

        public void SetVRConnection(VRConnection vr)
        {
            VR = vr ?? throw new Exception("null VR connection.");
        }

        public bool IsVrConnectionOK()
        {
            return VR.Status == VRConnectionStatus.AllOK;
        }

        public void Start()
        {
            if (!Worker.IsBusy)
            {
                StopFlag = false;
                Worker.RunWorkerAsync();
            }
        }

        public void Stop()
        {
            StopFlag = true;
        }


        double PreviousYaw = 0;
        const int SameYawThreshold = 5;
        int SameYawCounter = 0;
        int InvalidYawCounter = 0;
        /// <summary>
        /// NEVER MAKE CHANGES TO VR-CONNECTION FROM THIS THREAD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DoWork(object sender, DoWorkEventArgs e)
        {
            while (StopFlag == false)
            {
                if (VR.GetHmdOrientationAndPosition(ref Rotation, ref Position))
                {
                    UDPSender.Send(Rotation, Position);
                    if (Rotation.yaw == PreviousYaw)
                    {
                        if (SameYawCounter < SameYawThreshold)
                            SameYawCounter++;
                        else
                            Worker.ReportProgress(1, EventArgs.Empty);
                    }
                    else
                    {
                        Worker.ReportProgress(0, EventArgs.Empty);
                        SameYawCounter = 0;
                    }

                    PreviousYaw = Rotation.yaw;
                    InvalidYawCounter = 0;
                }
                else
                {
                    if (InvalidYawCounter < SameYawThreshold)
                        InvalidYawCounter++;
                    else
                        Worker.ReportProgress(2, EventArgs.Empty);
                }

                Thread.Sleep(PollInterval);
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
                ValidYawReceived?.Invoke(this, new VRObserverEventArgs(Rotation.yaw, true));
            else if (e.ProgressPercentage == 1)
                ValidYawReceived?.Invoke(this, new VRObserverEventArgs(Rotation.yaw, false));
            else if (e.ProgressPercentage == 2)
                InvalidYawReceived?.Invoke(this, EventArgs.Empty);
        }
    }
}
