using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace percentage
{
    class TrayIcon
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        private const string iconFont = "Segoe UI";
        private const int iconFontSize = 14;
        const string CHARGING = "Charging";
        const string NOT_CHARGING = "Low charging power";
        const string PLUGGED_IN = "Plugged in";
        const string ON_BAT = "On battery";

        private string batteryPercentage;
        private NotifyIcon notifyIcon;

        public TrayIcon()
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();

            notifyIcon = new NotifyIcon();

            // initialize contextMenu
            contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem });

            // initialize menuItem
            menuItem.Index = 0;
            menuItem.Text = "Exit";
            menuItem.Click += new System.EventHandler(menuItem_Click);

            notifyIcon.ContextMenu = contextMenu;

            batteryPercentage = "?";

            notifyIcon.Visible = true;

            Timer timer = new Timer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 1000; // in miliseconds
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            batteryPercentage = (powerStatus.BatteryLifePercent * 100).ToString();

            using (Bitmap bitmap = new Bitmap(DrawText(batteryPercentage, new Font(iconFont, iconFontSize), Color.White, Color.Transparent)))
            {
                System.IntPtr intPtr = bitmap.GetHicon();
                try
                {
                    using (Icon icon = Icon.FromHandle(intPtr))
                    {
                        notifyIcon.Icon = icon;
                        notifyIcon.Text = FormatTooltip(powerStatus);
                    }
                }
                finally
                {
                    DestroyIcon(intPtr);
                }
            }
        }

        private void menuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        private Image DrawText(String text, Font font, Color textColor, Color backColor)
        {
            var textSize = GetImageSize(text, font);
            Image image = new Bitmap((int) textSize.Width, (int) textSize.Height);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                // paint the background
                graphics.Clear(backColor);

                // create a brush for the text
                using (Brush textBrush = new SolidBrush(textColor))
                {
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    graphics.DrawString(text, font, textBrush, 0, 0);
                    graphics.Save();
                }
            }

            return image;
        }

        private static SizeF GetImageSize(string text, Font font)
        {
            using (Image image = new Bitmap(1, 1))
            using (Graphics graphics = Graphics.FromImage(image))
                return graphics.MeasureString(text, font);
        }

        private string FormatBatLevel(PowerStatus status)
        {
            return string.Format("{0:P0}", status.BatteryLifePercent);
        }

        private static string FormatTooltip(PowerStatus status)
        {
            return string.Format(
                "{0:P0} - {2}: {1} remaining",
                status.BatteryLifePercent,
                HumanReadableRemainingTime(status.BatteryLifeRemaining),
                PlugStatus(status)
            );
        }

        private static string HumanReadableRemainingTime(int secondsRemaining)
        {
            if (secondsRemaining < 0)
            {
                return string.Format("∞");
            }
            int hours = 0;
            int minutes = 0;
            if (secondsRemaining >= 3600)
            {
                hours = secondsRemaining / 3600;
                secondsRemaining = secondsRemaining % 3600;
            }
            if (secondsRemaining >= 60)
            {
                minutes = secondsRemaining / 60;
                secondsRemaining = secondsRemaining % 60;
            }
            return string.Format("{0}:{1:D2}:{2:D2}", hours, minutes, secondsRemaining);
        }

        private static string PlugStatus(PowerStatus status)
        {
            string plugStatus = status.PowerLineStatus == PowerLineStatus.Online ? PLUGGED_IN : ON_BAT;
            if (status.PowerLineStatus == PowerLineStatus.Offline)
            {
                return plugStatus;
            }
            string chargeStatus = status.BatteryChargeStatus == BatteryChargeStatus.Charging ? CHARGING : NOT_CHARGING;
            return string.Format("{0}, {1}", plugStatus, chargeStatus);
        }
    }
}
