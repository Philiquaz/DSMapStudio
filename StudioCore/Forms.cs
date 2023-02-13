using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
#if NOWINFORMS
#else
using WinForms = System.Windows.Forms;
#endif

namespace StudioCore.Forms
{
    public class Forms
    {
#if NOWINFORMS
        public static string ProductVersion = "NOWINFORMSVERSION";
#else
        public static string ProductVersion = WinForms.Application.ProductVersion;
#endif

        public static void Exit()
        {
#if NOWINFORMS
            Environment.Exit(0);
#else
            WinForms.Application.Exit();
#endif
        }


        internal static (FolderBrowserDialog, Action<DialogResult>) fbd = (null, null);
        internal static (OpenFileDialog, Action<DialogResult>) ofd = (null, null);
        internal static (SaveFileDialog, Action<DialogResult>) sfd = (null, null);
        internal static (string, string, MessageBoxButtons, MessageBoxIcon, Action<DialogResult>) mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);

        public static void FormsPopups()
        {
            if (fbd != (null, null))
            {
                if (!ImGui.IsPopupOpen("FormsFolderBrowserDialog"))
                {
                    fbd.Item2(DialogResult.Cancel);
                    fbd = (null, null);
                }
                else if (ImGui.BeginPopupModal(fbd.Item1.Description+"##FormsFolderBrowserDialog"))
                {
                    ImGui.Text("FBD");
                    ImGui.EndPopup();
                }
            }
            if (ofd != (null, null))
            {
                if (!ImGui.IsPopupOpen("FormsOpenFileDialog"))
                {
                    ofd.Item2(DialogResult.Cancel);
                    ofd = (null, null);
                }
                else if (ImGui.BeginPopupModal(ofd.Item1.Title+"##FormsOpenFileDialog"))
                {
                    ImGui.Text("OFD");
                    ImGui.EndPopup();
                }
            }
            if (sfd != (null, null))
            {
                if (!ImGui.IsPopupOpen("FormsSaveFileDialog"))
                {
                    sfd.Item2(DialogResult.Cancel);
                    sfd = (null, null);
                }
                else if (ImGui.BeginPopupModal(sfd.Item1.Title+"##FormsSaveFileDialog"))
                {
                    ImGui.Text("SFD");
                    ImGui.EndPopup();
                }
            }
            if (mb != (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null))
            {
                if (!ImGui.IsPopupOpen("FormsMessageBox"))
                {
                    mb.Item5(DialogResult.Cancel);
                    mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);
                }
                else if (ImGui.BeginPopupModal(mb.Item1+"##FormsMessageBox"))
                {
                    ImGui.Text("MB");
                    ImGui.EndPopup();
                }
            }
#if NOWINFORMS

#else
#endif
        }
    }

    public class FolderBrowserDialog
    {
        public string Description = null;
        public bool UseDescriptionForTitle = false;

        public string SelectedPath;

        public void ShowDialog(Action<DialogResult> Callback)
        {
#if NOWINFORMS
            Forms.fbd = (this, Callback);
            ImGui.OpenPopup("FormsFolderBrowserDialog");
#else
            WinForms.FolderBrowserDialog dlg = new WinForms.FolderBrowserDialog();

            if (Description != null)
                dlg.Description = Description;
            if (UseDescriptionForTitle)
                dlg.UseDescriptionForTitle = UseDescriptionForTitle;

            var result = dlg.ShowDialog();
            SelectedPath = dlg.SelectedPath;
            Callback(result switch
            {
                WinForms.DialogResult.Cancel => DialogResult.Cancel,
                WinForms.DialogResult.OK => DialogResult.OK,
                WinForms.DialogResult.Yes => DialogResult.Yes,
                WinForms.DialogResult.No => DialogResult.No,
                _ => DialogResult.Cancel
            });
        }
#endif
    }
    public class OpenFileDialog
    {
        public string Title = null;
        public string Filter = null;
        public bool ValidateNames = false;
        public bool CheckFileExists = false;
        public bool CheckPathExists = false;
        public bool Multiselect = false;

        public string FileName;
        public string[] FileNames;

        public void ShowDialog(Action<DialogResult> Callback)
        {
#if NOWINFORMS
            Forms.ofd = (this, Callback);
            ImGui.OpenPopup("FormsOpenFileDialog");
#else
            WinForms.OpenFileDialog dlg = new WinForms.OpenFileDialog();

            if (Title != null)
                dlg.Title = Title;
            if (Filter != null)
                dlg.Filter = Filter;
            if (ValidateNames)
                dlg.ValidateNames = ValidateNames;
            if (CheckFileExists)
                dlg.CheckFileExists = CheckFileExists;
            if (CheckPathExists)
                dlg.CheckPathExists = CheckPathExists;
            if (Multiselect)
                dlg.Multiselect = Multiselect;

            var result = dlg.ShowDialog();
            FileName = dlg.FileName;
            FileNames = dlg.FileNames;
            Callback(result switch
            {
                WinForms.DialogResult.Cancel => DialogResult.Cancel,
                WinForms.DialogResult.OK => DialogResult.OK,
                WinForms.DialogResult.Yes => DialogResult.Yes,
                WinForms.DialogResult.No => DialogResult.No,
                _ => DialogResult.Cancel
            });
#endif
        }
    }
    public class SaveFileDialog
    {
        public string Title = null;
        public string Filter = null;
        public bool ValidateNames = false;
        public bool CheckFileExists = false;
        public bool CheckPathExists = false;

        public string FileName;

        public void ShowDialog(Action<DialogResult> Callback)
        {
#if NOWINFORMS
            Forms.sfd = (this, Callback);
            ImGui.OpenPopup("FormsSaveFileDialog");
#else
            WinForms.SaveFileDialog dlg = new WinForms.SaveFileDialog();

            if (Title != null)
                dlg.Title = Title;
            if (Filter != null)
                dlg.Filter = Filter;
            if (ValidateNames)
                dlg.ValidateNames = ValidateNames;
            if (CheckFileExists)
                dlg.CheckFileExists = CheckFileExists;
            if (CheckPathExists)
                dlg.CheckPathExists = CheckPathExists;

            var result = dlg.ShowDialog();
            FileName = dlg.FileName;
            Callback(result switch
            {
                WinForms.DialogResult.Cancel => DialogResult.Cancel,
                WinForms.DialogResult.OK => DialogResult.OK,
                WinForms.DialogResult.Yes => DialogResult.Yes,
                WinForms.DialogResult.No => DialogResult.No,
                _ => DialogResult.Cancel
            });
#endif
        }
    }
    
    public class MessageBox
    {
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
        {
#if NOWINFORMS
            Forms.mb = (message, title, buttons, icon, Callback);
            ImGui.OpenPopup("FormsMessageBox");
#else
            WinForms.MessageBoxButtons btn = buttons switch
            {
                MessageBoxButtons.OK => WinForms.MessageBoxButtons.OK,
                MessageBoxButtons.OKCancel => WinForms.MessageBoxButtons.OKCancel,
                MessageBoxButtons.YesNo => WinForms.MessageBoxButtons.YesNo
            };
            WinForms.MessageBoxIcon icn = icon switch
            {
                MessageBoxIcon.None => WinForms.MessageBoxIcon.None,
                MessageBoxIcon.Warning => WinForms.MessageBoxIcon.Warning,
                MessageBoxIcon.Error => WinForms.MessageBoxIcon.Error,
                MessageBoxIcon.Question => WinForms.MessageBoxIcon.Question,
                MessageBoxIcon.Information => WinForms.MessageBoxIcon.Information
            };
            var result = WinForms.MessageBox.Show(message, title, btn, icn);
            switch (result)
            {
                case WinForms.DialogResult.Cancel:
                    return DialogResult.Cancel;
                case WinForms.DialogResult.OK:
                    return DialogResult.OK;
                case WinForms.DialogResult.Yes:
                    return DialogResult.Yes;
                case WinForms.DialogResult.No:
                    return DialogResult.No;
            }
            return DialogResult.Cancel;
#endif
        }

    }
    public enum DialogResult
    {
        Cancel,
        OK,
        Yes,
        No
    }
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo
    }
    public enum MessageBoxIcon
    {
        None,
        Warning,
        Error,
        Question,
        Information
    }
}
