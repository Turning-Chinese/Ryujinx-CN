using Gtk;
using Ryujinx.Ui.Common.Configuration;
using System;
using System.Reflection;
using GUI = Gtk.Builder.ObjectAttribute;

namespace Ryujinx.Ui.Widgets
{
    public class ProfileDialog : Dialog
    {
        public string FileName { get; private set; }

#pragma warning disable CS0649, IDE0044
        [GUI] Entry _profileEntry;
        [GUI] Label _errorMessage;
#pragma warning restore CS0649, IDE0044

        public ProfileDialog() : this(new Builder("Ryujinx.Ui.Widgets.ProfileDialog.glade")) { }

        private ProfileDialog(Builder builder) : base(builder.GetRawOwnedObject("_profileDialog"))
        {
            builder.Autoconnect(this);
            Icon = new Gdk.Pixbuf(Assembly.GetAssembly(typeof(ConfigurationState)), "Ryujinx.Ui.Common.Resources.Logo_Ryujinx.png");
        }

        private void OkToggle_Activated(object sender, EventArgs args)
        {
            ((ToggleButton)sender).SetStateFlags(StateFlags.Normal, true);

            bool validFileName = true;

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                if (_profileEntry.Text.Contains(invalidChar))
                {
                    validFileName = false;
                }
            }

            if (validFileName && !string.IsNullOrEmpty(_profileEntry.Text))
            {
                FileName = $"{_profileEntry.Text}.json";

                Respond(ResponseType.Ok);
            }
            else
            {
                _errorMessage.Text = "文件名包含无效字符。请再试一次.";
            }
        }

        private void CancelToggle_Activated(object sender, EventArgs args)
        {
            Respond(ResponseType.Cancel);
        }
    }
}