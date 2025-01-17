using Gtk;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;
using Ryujinx.HLE.Ui;
using Ryujinx.Ui.Widgets;
using System;
using System.Threading;
using Action = System.Action;

namespace Ryujinx.Ui.Applet
{
    internal class GtkHostUiHandler : IHostUiHandler
    {
        private readonly Window _parent;

        public IHostUiTheme HostUiTheme { get; }

        public GtkHostUiHandler(Window parent)
        {
            _parent = parent;

            HostUiTheme = new GtkHostUiTheme(parent);
        }

        public bool DisplayMessageDialog(ControllerAppletUiArgs args)
        {
            string playerCount = args.PlayerCountMin == args.PlayerCountMax ? $"exactly {args.PlayerCountMin}" : $"{args.PlayerCountMin}-{args.PlayerCountMax}";

            string message = $"应用程序请求 <b>{playerCount}</b> 具有的玩家:\n\n"
                           + $"<tt><b>TYPES:</b> {args.SupportedStyles}</tt>\n\n"
                           + $"<tt><b>PLAYERS:</b> {string.Join(", ", args.SupportedPlayers)}</tt>\n\n"
                           + (args.IsDocked ? "Docked mode set. <tt>Handheld</tt> is also invalid.\n\n" : "")
                           + "<i>请立即重新配置输入，然后按“确定”.</i>";

            return DisplayMessageDialog("控制器小程序", message);
        }

        public bool DisplayMessageDialog(string title, string message)
        {
            ManualResetEvent dialogCloseEvent = new ManualResetEvent(false);

            bool okPressed = false;

            Application.Invoke(delegate
            {
                MessageDialog msgDialog = null;

                try
                {
                    msgDialog = new MessageDialog(_parent, DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, null)
                    {
                        Title     = title,
                        Text      = message,
                        UseMarkup = true
                    };

                    msgDialog.SetDefaultSize(400, 0);

                    msgDialog.Response += (object o, ResponseArgs args) =>
                    {
                        if (args.ResponseId == ResponseType.Ok)
                        {
                            okPressed = true;
                        }

                        dialogCloseEvent.Set();
                        msgDialog?.Dispose();
                    };

                    msgDialog.Show();
                }
                catch (Exception ex)
                {
                    GtkDialog.CreateErrorDialog($"显示消息对话框时出错: {ex}");

                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            return okPressed;
        }

        public bool DisplayInputDialog(SoftwareKeyboardUiArgs args, out string userText)
        {
            ManualResetEvent dialogCloseEvent = new ManualResetEvent(false);

            bool   okPressed = false;
            bool   error     = false;
            string inputText = args.InitialText ?? "";

            Application.Invoke(delegate
            {
                try
                {
                    var swkbdDialog = new SwkbdAppletDialog(_parent)
                    {
                        Title         = "软件键盘",
                        Text          = args.HeaderText,
                        SecondaryText = args.SubtitleText
                    };

                    swkbdDialog.InputEntry.Text            = inputText;
                    swkbdDialog.InputEntry.PlaceholderText = args.GuideText;
                    swkbdDialog.OkButton.Label             = args.SubmitText;

                    swkbdDialog.SetInputLengthValidation(args.StringLengthMin, args.StringLengthMax);

                    if (swkbdDialog.Run() == (int)ResponseType.Ok)
                    {
                        inputText = swkbdDialog.InputEntry.Text;
                        okPressed = true;
                    }

                    swkbdDialog.Dispose();
                }
                catch (Exception ex)
                {
                    error = true;

                    GtkDialog.CreateErrorDialog($"显示软件键盘时出错: {ex}");
                }
                finally
                {
                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            userText = error ? null : inputText;

            return error || okPressed;
        }

        public void ExecuteProgram(HLE.Switch device, ProgramSpecifyKind kind, ulong value)
        {
            device.Configuration.UserChannelPersistence.ExecuteProgram(kind, value);
            ((MainWindow)_parent).RendererWidget?.Exit();
        }

        public bool DisplayErrorAppletDialog(string title, string message, string[] buttons)
        {
            ManualResetEvent dialogCloseEvent = new ManualResetEvent(false);

            bool showDetails = false;

            Application.Invoke(delegate
            {
                try
                {
                    ErrorAppletDialog msgDialog = new ErrorAppletDialog(_parent, DialogFlags.DestroyWithParent, MessageType.Error, buttons)
                    {
                        Title          = title,
                        Text           = message,
                        UseMarkup      = true,
                        WindowPosition = WindowPosition.CenterAlways
                    };

                    msgDialog.SetDefaultSize(400, 0);

                    msgDialog.Response += (object o, ResponseArgs args) =>
                    {
                        if (buttons != null)
                        {
                            if (buttons.Length > 1)
                            {
                                if (args.ResponseId != (ResponseType)(buttons.Length - 1))
                                {
                                    showDetails = true;
                                }
                            }
                        }

                        dialogCloseEvent.Set();
                        msgDialog?.Dispose();
                    };

                    msgDialog.Show();
                }
                catch (Exception ex)
                {
                    GtkDialog.CreateErrorDialog($"显示ErrorApplet对话框时出错: {ex}");

                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            return showDetails;
        }

        private void SynchronousGtkInvoke(Action action)
        {
            var waitHandle = new ManualResetEventSlim();

            Application.Invoke(delegate
            {
                action();
                waitHandle.Set();
            });

            waitHandle.Wait();
        }

        public IDynamicTextInputHandler CreateDynamicTextInputHandler()
        {
            return new GtkDynamicTextInputHandler(_parent);
        }
    }
}