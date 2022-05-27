﻿using Notadesigner.Tom.App.Properties;
using System.Media;

namespace Notadesigner.Tom.App
{
    public class WorkSessionGuiState : IGuiState
    {
        private readonly NotifyIcon _notifyIcon;

        private readonly SoundPlayer _tickPlayer;

        public WorkSessionGuiState(NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
            _tickPlayer = new(GuiRunnerResources.Tick);
        }

        public GuiState State => GuiState.WorkSession;

        public void Enter(int roundCounter)
        {
            string message;

            if (roundCounter == 0)
            {
                message = GuiRunnerResources.WORK_SESSION_0;
            }
            else if (roundCounter == 1)
            {
                message = GuiRunnerResources.WORK_SESSION_1;
            }
            else if (roundCounter == 2)
            {
                message = GuiRunnerResources.WORK_SESSION_2;
            }
            else
            {
                message = GuiRunnerResources.WORK_SESSION_3;
            }

            _notifyIcon.ShowBalloonTip(500, string.Empty, message, ToolTipIcon.None);
            _notifyIcon.ContextMenuStrip.Items[0].Enabled = false;

            _tickPlayer.PlayLooping();
        }

        public void Exit() => _tickPlayer.Stop();
    }
}